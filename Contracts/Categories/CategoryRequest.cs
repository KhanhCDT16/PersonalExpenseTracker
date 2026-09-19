using System.ComponentModel.DataAnnotations;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Contracts.Categories;

public sealed class CategoryRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public string ColorHex { get; set; } = "#176B4D";

    [StringLength(50)]
    public string? Icon { get; set; }
}
