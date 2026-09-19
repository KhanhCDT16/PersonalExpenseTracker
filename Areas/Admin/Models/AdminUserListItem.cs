namespace PersonalExpenseTracker.Areas.Admin.Models;

public sealed class AdminUserListItem
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public bool IsAdmin { get; init; }
}
