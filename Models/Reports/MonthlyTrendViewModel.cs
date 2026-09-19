namespace PersonalExpenseTracker.Models.Reports;

public sealed record MonthlyTrendViewModel(
    string Month,
    decimal Income,
    decimal Expenses,
    decimal Balance);
