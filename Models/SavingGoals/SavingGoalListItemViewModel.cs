using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Models.SavingGoals;

public sealed class SavingGoalListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public decimal SavedAmount { get; init; }
    public DateOnly? TargetDate { get; init; }
    public SavingGoalStatus Status { get; init; }
    public decimal Remaining => Math.Max(0, TargetAmount - SavedAmount);
    public decimal ProgressPercent => TargetAmount == 0 ? 0 : SavedAmount / TargetAmount * 100;
}
