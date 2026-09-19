namespace PersonalExpenseTracker.Models.Transactions;

public sealed class TransactionListItemViewModel
{
    public Guid Id { get; init; }
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public DateOnly Date { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryColor { get; init; } = string.Empty;
    public string? Description { get; init; }
}
