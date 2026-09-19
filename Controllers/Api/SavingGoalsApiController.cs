using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Contracts.SavingGoals;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Controllers.Api;

[ApiController]
[Route("api/saving-goals")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class SavingGoalsApiController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SavingGoalResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goals = await Query(userId).Include(goal => goal.Contributions)
            .Where(goal => goal.Status != SavingGoalStatus.Archived)
            .OrderBy(goal => goal.Status).ThenBy(goal => goal.TargetDate)
            .ToListAsync(cancellationToken);
        return Ok(goals.Select(goal =>
            ToResponse(goal, goal.Contributions.Sum(item => item.Amount))).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SavingGoalResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await Query(userId).Include(item => item.Contributions)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return goal is null
            ? NotFound()
            : Ok(ToResponse(goal, goal.Contributions.Sum(item => item.Amount)));
    }

    [HttpPost]
    public async Task<ActionResult<SavingGoalResponse>> Create(SavingGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!; var now = DateTime.UtcNow;
        var goal = new SavingGoal
        {
            Id = Guid.NewGuid(), UserId = userId, Name = request.Name.Trim(), TargetAmount = request.TargetAmount,
            TargetDate = request.TargetDate, Status = SavingGoalStatus.Active, CreatedAtUtc = now, UpdatedAtUtc = now
        };
        dbContext.SavingGoals.Add(goal); await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = goal.Id }, ToResponse(goal, 0));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SavingGoalResponse>> Update(Guid id, SavingGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.Include(item => item.Contributions).SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId && item.Status != SavingGoalStatus.Archived, cancellationToken);
        if (goal is null) return NotFound();
        goal.Name = request.Name.Trim(); goal.TargetAmount = request.TargetAmount; goal.TargetDate = request.TargetDate;
        var saved = goal.Contributions.Sum(item => item.Amount);
        goal.Status = saved >= goal.TargetAmount ? SavingGoalStatus.Completed : SavingGoalStatus.Active;
        goal.UpdatedAtUtc = DateTime.UtcNow; await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(goal, saved));
    }

    [HttpPost("{id:guid}/contributions")]
    public async Task<ActionResult<SavingGoalResponse>> Contribute(Guid id, ContributionRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.Include(item => item.Contributions).SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId && item.Status != SavingGoalStatus.Archived, cancellationToken);
        if (goal is null) return NotFound();
        var saved = goal.Contributions.Sum(item => item.Amount) + request.Amount;
        var contribution = new SavingGoalContribution
        {
            Id = Guid.NewGuid(), SavingGoalId = goal.Id, Amount = request.Amount, ContributionDate = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(), CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.SavingGoalContributions.Add(contribution);
        if (saved >= goal.TargetAmount) goal.Status = SavingGoalStatus.Completed;
        goal.UpdatedAtUtc = DateTime.UtcNow; await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(goal, saved));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var goal = await dbContext.SavingGoals.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (goal is null) return NotFound();
        goal.Status = SavingGoalStatus.Archived; goal.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken); return NoContent();
    }

    private IQueryable<SavingGoal> Query(string userId) =>
        dbContext.SavingGoals.AsNoTracking().Where(goal => goal.UserId == userId);

    private static SavingGoalResponse ToResponse(SavingGoal goal, decimal saved) => new(
        goal.Id, goal.Name, goal.TargetAmount, saved, Math.Max(0, goal.TargetAmount - saved),
        goal.TargetAmount == 0 ? 0 : Math.Round(saved / goal.TargetAmount * 100, 2),
        goal.TargetDate, goal.Status);
}
