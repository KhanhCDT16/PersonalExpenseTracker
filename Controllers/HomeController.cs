using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Models.Transactions;
using PersonalExpenseTracker.Models.SavingGoals;
using PersonalExpenseTracker.Services;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class HomeController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var month = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthEnd = month.AddMonths(1);
        var totals = await dbContext.FinancialTransactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.TransactionDate >= month
                && transaction.TransactionDate < monthEnd)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Income = group.Where(item => item.Type == TransactionType.Income)
                    .Sum(item => item.Amount),
                Expenses = group.Where(item => item.Type == TransactionType.Expense)
                    .Sum(item => item.Amount)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var recent = await dbContext.FinancialTransactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .Take(5)
            .Select(transaction => new TransactionListItemViewModel
            {
                Id = transaction.Id,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Date = transaction.TransactionDate,
                CategoryName = transaction.Category.Name,
                CategoryColor = transaction.Category.ColorHex,
                Description = transaction.Description
            })
            .ToListAsync(cancellationToken);

        var budgets = await dbContext.Budgets.AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId && budget.Month == month)
            .OrderBy(budget => budget.CategoryId)
            .ToListAsync(cancellationToken);
        var monthExpenses = await dbContext.FinancialTransactions.AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.Type == TransactionType.Expense
                && transaction.TransactionDate >= month
                && transaction.TransactionDate < monthEnd)
            .ToListAsync(cancellationToken);
        var goals = await dbContext.SavingGoals.AsNoTracking()
            .Where(goal => goal.UserId == userId && goal.Status != SavingGoalStatus.Archived)
            .OrderBy(goal => goal.Status)
            .ThenBy(goal => goal.TargetDate)
            .Take(3)
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

        return View(new DashboardViewModel
        {
            PeriodLabel = month.ToString("MMMM yyyy"),
            TotalIncome = totals?.Income ?? 0,
            TotalExpenses = totals?.Expenses ?? 0,
            RecentTransactions = recent,
            Budgets = BudgetCalculator.CreateItems(budgets, monthExpenses),
            SavingGoals = goals
        });
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
