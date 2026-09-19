using System.ComponentModel.DataAnnotations;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Contracts.Transactions;

public sealed class TransactionRequest
{
    [Required]
    public TransactionType Type { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }
}
