using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models.SavingGoals;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class SavingGoalsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goals = await dbContext.SavingGoals.AsNoTracking()
            .Where(goal => goal.UserId == userId && goal.Status != SavingGoalStatus.Archived)
            .OrderBy(goal => goal.Status)
            .ThenBy(goal => goal.TargetDate)
            .Select(goal => new SavingGoalListItemViewModel
            {
                Id = goal.Id,
                Name = goal.Name,
                TargetAmount = goal.TargetAmount,
                SavedAmount = goal.Contributions.Sum(item => (decimal?)item.Amount) ?? 0,
                TargetDate = goal.TargetDate,
                Status = goal.Status
            })
            .ToListAsync(cancellationToken);
        return View(goals);
    }

    [HttpGet]
    public IActionResult Create() => View(new SavingGoalFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavingGoalFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var now = DateTime.UtcNow;
        dbContext.SavingGoals.Add(new SavingGoal
        {
            Id = Guid.NewGuid(), UserId = userManager.GetUserId(User)!, Name = model.Name.Trim(),
            TargetAmount = model.TargetAmount, TargetDate = model.TargetDate,
            Status = SavingGoalStatus.Active, CreatedAtUtc = now, UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Saving goal created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId && item.Status != SavingGoalStatus.Archived,
            cancellationToken);
        return goal is null ? NotFound() : View(new SavingGoalFormViewModel
        {
            Id = goal.Id, Name = goal.Name, TargetAmount = goal.TargetAmount, TargetDate = goal.TargetDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SavingGoalFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.Include(item => item.Contributions).SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId && item.Status != SavingGoalStatus.Archived,
            cancellationToken);
        if (goal is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        goal.Name = model.Name.Trim(); goal.TargetAmount = model.TargetAmount;
        goal.TargetDate = model.TargetDate; goal.UpdatedAtUtc = DateTime.UtcNow;
        goal.Status = goal.Contributions.Sum(item => item.Amount) >= goal.TargetAmount
            ? SavingGoalStatus.Completed : SavingGoalStatus.Active;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Saving goal updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddContribution(ContributionViewModel model, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.Include(item => item.Contributions).SingleOrDefaultAsync(
            item => item.Id == model.SavingGoalId && item.UserId == userId && item.Status != SavingGoalStatus.Archived,
            cancellationToken);
        if (goal is null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Enter a positive contribution amount and valid date.";
            return RedirectToAction(nameof(Index));
        }

        var saved = goal.Contributions.Sum(item => item.Amount) + model.Amount;
        var contribution = new SavingGoalContribution
        {
            Id = Guid.NewGuid(), SavingGoalId = goal.Id, Amount = model.Amount, ContributionDate = model.Date,
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(), CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.SavingGoalContributions.Add(contribution);
        if (saved >= goal.TargetAmount)
        {
            goal.Status = SavingGoalStatus.Completed;
        }
        goal.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Contribution added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (goal is null) return NotFound();
        goal.Status = SavingGoalStatus.Archived; goal.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Saving goal archived.";
        return RedirectToAction(nameof(Index));
    }
}
