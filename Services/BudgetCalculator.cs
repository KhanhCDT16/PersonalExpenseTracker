using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Models.Budgets;

namespace PersonalExpenseTracker.Services;

public static class BudgetCalculator
{
    public static IReadOnlyList<BudgetListItemViewModel> CreateItems(
        IReadOnlyList<Budget> budgets,
        IReadOnlyList<FinancialTransaction> expenses) =>
        budgets.Select(budget =>
        {
            var monthEnd = budget.Month.AddMonths(1);
            var actual = expenses
                .Where(transaction =>
                    transaction.Type == TransactionType.Expense
                    && transaction.TransactionDate >= budget.Month
                    && transaction.TransactionDate < monthEnd
                    && (!budget.CategoryId.HasValue || transaction.CategoryId == budget.CategoryId))
                .Sum(transaction => transaction.Amount);
            return new BudgetListItemViewModel
            {
                Id = budget.Id,
                Month = budget.Month,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category?.Name ?? "Overall spending",
                CategoryColor = budget.Category?.ColorHex,
                Amount = budget.Amount,
                ActualSpending = actual,
                WarningThresholdPercent = budget.WarningThresholdPercent
            };
        }).ToList();
}
