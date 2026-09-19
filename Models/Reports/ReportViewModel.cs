namespace PersonalExpenseTracker.Models.Reports;

public sealed class ReportViewModel
{
    public int Months { get; init; }
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal Balance => TotalIncome - TotalExpenses;
    public IReadOnlyList<CategoryExpenseViewModel> ExpensesByCategory { get; init; } = [];
    public IReadOnlyList<MonthlyTrendViewModel> MonthlyTrends { get; init; } = [];
}
