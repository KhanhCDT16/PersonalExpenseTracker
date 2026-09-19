using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Data.Entities;

public sealed class Category
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string ColorHex { get; set; } = "#176B4D";
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
}
