using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Models.Categories;

public sealed class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a six-digit hex color such as #176B4D.")]
    [Display(Name = "Color")]
    public string ColorHex { get; set; } = "#176B4D";

    [StringLength(50)]
    public string? Icon { get; set; }
}
