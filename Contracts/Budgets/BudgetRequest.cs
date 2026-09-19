using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Contracts.Budgets;

public sealed class BudgetRequest
{
    [Required]
    public DateOnly Month { get; set; }

    public int? CategoryId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Range(1, 100)]
    public int WarningThresholdPercent { get; set; } = 80;
}
