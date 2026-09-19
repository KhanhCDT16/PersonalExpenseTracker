using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalExpenseTracker.Models.Transactions;

namespace PersonalExpenseTracker.Models;

public sealed class TransactionListViewModel
{
    public IReadOnlyList<TransactionListItemViewModel> Transactions { get; init; } = [];
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public TransactionType? Type { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public IReadOnlyList<SelectListItem> Categories { get; init; } = [];
}
