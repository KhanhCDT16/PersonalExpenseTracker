using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PersonalExpenseTracker.Models.Budgets;

public sealed class BudgetFormViewModel
{
    public Guid Id { get; set; }

    [Required]
    [RegularExpression("^\\d{4}-\\d{2}$", ErrorMessage = "Choose a valid month.")]
    public string Month { get; set; } = DateTime.Today.ToString("yyyy-MM");

    [Display(Name = "Expense category")]
    public int? CategoryId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Range(1, 100)]
    [Display(Name = "Warning at (%)")]
    public int WarningThresholdPercent { get; set; } = 80;

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
}
