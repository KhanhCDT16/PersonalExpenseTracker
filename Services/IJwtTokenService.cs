using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Services;

public interface IJwtTokenService
{
    Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user);
}
