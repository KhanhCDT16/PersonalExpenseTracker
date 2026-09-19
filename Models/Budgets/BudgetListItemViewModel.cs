namespace PersonalExpenseTracker.Models.Budgets;

public sealed class BudgetListItemViewModel
{
    public Guid Id { get; init; }
    public DateOnly Month { get; init; }
    public int? CategoryId { get; init; }
    public string CategoryName { get; init; } = "Overall spending";
    public string? CategoryColor { get; init; }
    public decimal Amount { get; init; }
    public decimal ActualSpending { get; init; }
    public int WarningThresholdPercent { get; init; }
    public decimal Remaining => Amount - ActualSpending;
    public decimal UtilizationPercent => Amount == 0 ? 0 : ActualSpending / Amount * 100;
    public bool IsExceeded => ActualSpending > Amount;
    public bool IsWarning => !IsExceeded && UtilizationPercent >= WarningThresholdPercent;
}
