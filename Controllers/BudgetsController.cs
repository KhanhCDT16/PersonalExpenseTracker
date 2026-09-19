using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Models.Budgets;
using PersonalExpenseTracker.Services;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class BudgetsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var budgets = await dbContext.Budgets.AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .OrderByDescending(budget => budget.Month)
            .ThenBy(budget => budget.CategoryId)
            .ToListAsync(cancellationToken);
        var expenses = await LoadRelevantExpensesAsync(userId, budgets, cancellationToken);
        return View(BudgetCalculator.CreateItems(budgets, expenses));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(new BudgetFormViewModel
        {
            Categories = await GetCategoryOptionsAsync(userManager.GetUserId(User)!, cancellationToken)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BudgetFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var month = ParseMonth(model.Month);
        if (!month.HasValue)
        {
            ModelState.AddModelError(nameof(model.Month), "Choose a valid month.");
        }

        await ValidateCategoryAsync(model.CategoryId, userId, cancellationToken);
        if (month.HasValue && await DuplicateExistsAsync(userId, month.Value, model.CategoryId, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "A budget already exists for this month and category scope.");
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(userId, cancellationToken);
            return View(model);
        }

        var now = DateTime.UtcNow;
        dbContext.Budgets.Add(new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = model.CategoryId,
            Month = month!.Value,
            Amount = model.Amount,
            WarningThresholdPercent = model.WarningThresholdPercent,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Budget created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var budget = await dbContext.Budgets.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (budget is null)
        {
            return NotFound();
        }

        return View(new BudgetFormViewModel
        {
            Id = budget.Id,
            Month = budget.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            CategoryId = budget.CategoryId,
            Amount = budget.Amount,
            WarningThresholdPercent = budget.WarningThresholdPercent,
            Categories = await GetCategoryOptionsAsync(userId, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BudgetFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var userId = userManager.GetUserId(User)!;
        var budget = await dbContext.Budgets
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (budget is null)
        {
            return NotFound();
        }

        var month = ParseMonth(model.Month);
        if (!month.HasValue)
        {
            ModelState.AddModelError(nameof(model.Month), "Choose a valid month.");
        }

        await ValidateCategoryAsync(model.CategoryId, userId, cancellationToken);
        if (month.HasValue && await DuplicateExistsAsync(userId, month.Value, model.CategoryId, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "A budget already exists for this month and category scope.");
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(userId, cancellationToken);
            return View(model);
        }

        budget.Month = month!.Value;
        budget.CategoryId = model.CategoryId;
        budget.Amount = model.Amount;
        budget.WarningThresholdPercent = model.WarningThresholdPercent;
        budget.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Budget updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var budget = await dbContext.Budgets
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (budget is null)
        {
            return NotFound();
        }

        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Budget deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<FinancialTransaction>> LoadRelevantExpensesAsync(
        string userId,
        IReadOnlyList<Budget> budgets,
        CancellationToken cancellationToken)
    {
        if (budgets.Count == 0)
        {
            return [];
        }

        var start = budgets.Min(budget => budget.Month);
        var end = budgets.Max(budget => budget.Month).AddMonths(1);
        return await dbContext.FinancialTransactions.AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.Type == TransactionType.Expense
                && transaction.TransactionDate >= start
                && transaction.TransactionDate < end)
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateCategoryAsync(int? categoryId, string userId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        if (!await dbContext.Categories.AnyAsync(category =>
                category.Id == categoryId.Value
                && category.Type == TransactionType.Expense
                && category.IsActive
                && (category.UserId == null || category.UserId == userId), cancellationToken))
        {
            ModelState.AddModelError(nameof(BudgetFormViewModel.CategoryId), "Choose an available expense category.");
        }
    }

    private Task<bool> DuplicateExistsAsync(
        string userId,
        DateOnly month,
        int? categoryId,
        Guid? excludedId,
        CancellationToken cancellationToken) =>
        dbContext.Budgets.AnyAsync(budget =>
            budget.UserId == userId
            && budget.Month == month
            && budget.CategoryId == categoryId
            && (!excludedId.HasValue || budget.Id != excludedId.Value), cancellationToken);

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .Where(category => category.IsActive
                && category.Type == TransactionType.Expense
                && (category.UserId == null || category.UserId == userId))
            .OrderBy(category => category.Name)
            .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
            .ToListAsync(cancellationToken);

    private static DateOnly? ParseMonth(string value) =>
        DateOnly.TryParseExact(
            value + "-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var month)
            ? month
            : null;
}
