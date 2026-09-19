using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Contracts.SavingGoals;

public sealed class ContributionRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }
}
