namespace PersonalExpenseTracker.Models.Reports;

public sealed record CategoryExpenseViewModel(
    string Category,
    string Color,
    decimal Amount,
    decimal Percentage);
