namespace PersonalExpenseTracker.Data.Entities;

public sealed class SavingGoalContribution
{
    public Guid Id { get; set; }
    public Guid SavingGoalId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ContributionDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public SavingGoal SavingGoal { get; set; } = null!;
}
