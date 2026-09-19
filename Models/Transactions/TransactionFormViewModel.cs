using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PersonalExpenseTracker.Models.Transactions;

public sealed class TransactionFormViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Transaction type")]
    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
}
