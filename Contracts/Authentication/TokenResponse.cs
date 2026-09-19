namespace PersonalExpenseTracker.Contracts.Authentication;

public sealed record TokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string TokenType = "Bearer");
