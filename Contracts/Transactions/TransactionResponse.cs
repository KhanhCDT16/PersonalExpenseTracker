using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Contracts.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    int CategoryId,
    string CategoryName,
    string CategoryColor,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
