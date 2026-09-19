namespace PersonalExpenseTracker.Areas.Admin.Models;

public sealed class AdminUsersViewModel
{
    public IReadOnlyList<AdminUserListItem> Users { get; init; } = [];
    public string? Search { get; init; }
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}
