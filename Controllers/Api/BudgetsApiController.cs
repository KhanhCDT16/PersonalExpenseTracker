using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Contracts.Budgets;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;

namespace PersonalExpenseTracker.Controllers.Api;

[ApiController]
[Route("api/budgets")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class BudgetsApiController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var budgets = await dbContext.Budgets.AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .OrderByDescending(budget => budget.Month)
            .ToListAsync(cancellationToken);
        var expenses = await LoadExpensesAsync(userId, budgets, cancellationToken);
        return Ok(BudgetCalculator.CreateItems(budgets, expenses).Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> Create(BudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);
        if (!await CategoryIsAvailableAsync(request.CategoryId, userId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Choose an available expense category.");
            return ValidationProblem(ModelState);
        }

        if (await DuplicateExistsAsync(userId, month, request.CategoryId, null, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Budget already exists", Status = 409 });
        }

        var now = DateTime.UtcNow;
        var budget = new Budget
        {
            Id = Guid.NewGuid(), UserId = userId, CategoryId = request.CategoryId,
            Month = month, Amount = request.Amount,
            WarningThresholdPercent = request.WarningThresholdPercent,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), await CreateResponseAsync(budget.Id, userId, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetResponse>> Update(Guid id, BudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var budget = await dbContext.Budgets.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (budget is null) return NotFound();

        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);
        if (!await CategoryIsAvailableAsync(request.CategoryId, userId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Choose an available expense category.");
            return ValidationProblem(ModelState);
        }
        if (await DuplicateExistsAsync(userId, month, request.CategoryId, id, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Budget already exists", Status = 409 });
        }

        budget.Month = month; budget.CategoryId = request.CategoryId; budget.Amount = request.Amount;
        budget.WarningThresholdPercent = request.WarningThresholdPercent; budget.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await CreateResponseAsync(id, userId, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var budget = await dbContext.Budgets.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId, cancellationToken);
        if (budget is null) return NotFound();
        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<BudgetResponse?> CreateResponseAsync(Guid id, string userId, CancellationToken cancellationToken)
    {
        var budget = await dbContext.Budgets.AsNoTracking().Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (budget is null) return null;
        var expenses = await LoadExpensesAsync(userId, [budget], cancellationToken);
        return ToResponse(BudgetCalculator.CreateItems([budget], expenses).Single());
    }

    private static BudgetResponse ToResponse(Models.Budgets.BudgetListItemViewModel item) => new(
        item.Id, item.Month, item.CategoryId, item.CategoryName, item.Amount, item.ActualSpending,
        item.Remaining, Math.Round(item.UtilizationPercent, 2), item.WarningThresholdPercent,
        item.IsExceeded ? "Exceeded" : item.IsWarning ? "Warning" : "OnTrack");

    private async Task<List<FinancialTransaction>> LoadExpensesAsync(string userId, IReadOnlyList<Budget> budgets, CancellationToken cancellationToken)
    {
        if (budgets.Count == 0) return [];
        var start = budgets.Min(item => item.Month); var end = budgets.Max(item => item.Month).AddMonths(1);
        return await dbContext.FinancialTransactions.AsNoTracking().Where(item =>
            item.UserId == userId && item.Type == TransactionType.Expense
            && item.TransactionDate >= start && item.TransactionDate < end).ToListAsync(cancellationToken);
    }

    private async Task<bool> CategoryIsAvailableAsync(int? categoryId, string userId, CancellationToken cancellationToken) =>
        !categoryId.HasValue || await dbContext.Categories.AnyAsync(category =>
            category.Id == categoryId && category.Type == TransactionType.Expense && category.IsActive
            && (category.UserId == null || category.UserId == userId), cancellationToken);

    private Task<bool> DuplicateExistsAsync(string userId, DateOnly month, int? categoryId, Guid? excludedId, CancellationToken cancellationToken) =>
        dbContext.Budgets.AnyAsync(item => item.UserId == userId && item.Month == month
            && item.CategoryId == categoryId && (!excludedId.HasValue || item.Id != excludedId), cancellationToken);
}
