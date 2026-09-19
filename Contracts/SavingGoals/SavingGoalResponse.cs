using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Contracts.SavingGoals;

public sealed record SavingGoalResponse(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal SavedAmount,
    decimal Remaining,
    decimal ProgressPercent,
    DateOnly? TargetDate,
    SavingGoalStatus Status);
