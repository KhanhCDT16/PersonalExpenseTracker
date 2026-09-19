using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Models.Reports;

namespace PersonalExpenseTracker.Services;

public sealed class ReportService(ApplicationDbContext dbContext) : IReportService
{
    public async Task<ReportViewModel> CreateAsync(
        string userId,
        int months,
        CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 24);
        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var start = currentMonth.AddMonths(-(months - 1));
        var end = currentMonth.AddMonths(1);
        var transactions = await dbContext.FinancialTransactions.AsNoTracking()
            .Where(transaction => transaction.UserId == userId
                && transaction.TransactionDate >= start
                && transaction.TransactionDate < end)
            .Select(transaction => new
            {
                transaction.Type,
                transaction.Amount,
                transaction.TransactionDate,
                Category = transaction.Category.Name,
                Color = transaction.Category.ColorHex
            })
            .ToListAsync(cancellationToken);

        var totalIncome = transactions.Where(item => item.Type == TransactionType.Income)
            .Sum(item => item.Amount);
        var totalExpenses = transactions.Where(item => item.Type == TransactionType.Expense)
            .Sum(item => item.Amount);
        var categories = transactions.Where(item => item.Type == TransactionType.Expense)
            .GroupBy(item => new { item.Category, item.Color })
            .Select(group => new CategoryExpenseViewModel(
                group.Key.Category,
                group.Key.Color,
                group.Sum(item => item.Amount),
                totalExpenses == 0 ? 0 : group.Sum(item => item.Amount) / totalExpenses * 100))
            .OrderByDescending(item => item.Amount)
            .ToList();

        var trends = Enumerable.Range(0, months).Select(offset => start.AddMonths(offset))
            .Select(month =>
            {
                var monthEnd = month.AddMonths(1);
                var items = transactions.Where(item =>
                    item.TransactionDate >= month && item.TransactionDate < monthEnd).ToList();
                var income = items.Where(item => item.Type == TransactionType.Income).Sum(item => item.Amount);
                var expenses = items.Where(item => item.Type == TransactionType.Expense).Sum(item => item.Amount);
                return new MonthlyTrendViewModel(
                    month.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    income,
                    expenses,
                    income - expenses);
            }).ToList();

        return new ReportViewModel
        {
            Months = months,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            ExpensesByCategory = categories,
            MonthlyTrends = trends
        };
    }
}
