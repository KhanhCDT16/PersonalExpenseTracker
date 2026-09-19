namespace PersonalExpenseTracker.Data.Entities;

public sealed class SavingGoal
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateOnly? TargetDate { get; set; }
    public SavingGoalStatus Status { get; set; } = SavingGoalStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<SavingGoalContribution> Contributions { get; set; } = [];
}
