using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Models.SavingGoals;

public sealed class SavingGoalFormViewModel
{
    public Guid Id { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Target amount")]
    public decimal TargetAmount { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Target date")]
    public DateOnly? TargetDate { get; set; }
}
