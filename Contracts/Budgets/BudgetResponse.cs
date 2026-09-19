namespace PersonalExpenseTracker.Contracts.Budgets;

public sealed record BudgetResponse(
    Guid Id,
    DateOnly Month,
    int? CategoryId,
    string CategoryName,
    decimal Amount,
    decimal ActualSpending,
    decimal Remaining,
    decimal UtilizationPercent,
    int WarningThresholdPercent,
    string State);
