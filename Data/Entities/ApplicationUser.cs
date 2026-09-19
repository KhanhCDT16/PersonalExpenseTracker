using Microsoft.AspNetCore.Identity;

namespace PersonalExpenseTracker.Data.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public string TimeZoneId { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<SavingGoal> SavingGoals { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
