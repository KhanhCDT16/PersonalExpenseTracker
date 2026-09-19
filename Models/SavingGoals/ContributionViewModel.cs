using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Models.SavingGoals;

public sealed class ContributionViewModel
{
    [Required]
    public Guid SavingGoalId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(250)]
    public string? Note { get; set; }
}
