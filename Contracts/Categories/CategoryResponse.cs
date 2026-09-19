using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Contracts.Categories;

public sealed record CategoryResponse(
    int Id,
    string Name,
    TransactionType Type,
    string ColorHex,
    string? Icon,
    bool IsGlobal,
    bool IsActive);
