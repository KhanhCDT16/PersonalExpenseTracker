namespace PersonalExpenseTracker.Data.Entities;

public sealed class Budget
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public DateOnly Month { get; set; }
    public decimal Amount { get; set; }
    public int WarningThresholdPercent { get; set; } = 80;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Category? Category { get; set; }
}
