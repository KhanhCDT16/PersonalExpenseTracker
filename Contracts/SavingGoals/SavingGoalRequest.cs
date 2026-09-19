using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Contracts.SavingGoals;

public sealed class SavingGoalRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal TargetAmount { get; set; }

    public DateOnly? TargetDate { get; set; }
}
