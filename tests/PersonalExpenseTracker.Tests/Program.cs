using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PersonalExpenseTracker.Contracts.Authentication;
using PersonalExpenseTracker.Contracts.Budgets;
using PersonalExpenseTracker.Contracts.Categories;
using PersonalExpenseTracker.Contracts.Common;
using PersonalExpenseTracker.Contracts.Transactions;
using PersonalExpenseTracker.Contracts.SavingGoals;
using PersonalExpenseTracker.Models.Reports;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Services;

var tests = new (string Name, Action Run)[]
{
    ("EF model contains the required domain entities", ModelContainsRequiredEntities),
    ("Money columns and positive checks are configured", MoneyConstraintsAreConfigured),
    ("Ownership and reporting indexes are configured", OwnershipIndexesAreConfigured),
    ("Roles and global categories are seeded", ReferenceDataIsSeeded),
    ("Registration contract rejects invalid input", RegistrationValidationRejectsInvalidInput),
    ("Category contract rejects an invalid color", CategoryValidationRejectsInvalidColor),
    ("Transaction contract rejects invalid input", TransactionValidationRejectsInvalidInput),
    ("Paged response calculates page count", PagedResponseCalculatesPageCount),
    ("Currency preference supplies localized money formatting", CurrencyPreferenceFormatsMoney),
    ("Decimal validation works with VND formatting", DecimalValidationWorksWithVndCulture)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add(test.Name);
        Console.WriteLine($"FAIL  {test.Name}: {exception}");
    }
}

var integrationTests = new (string Name, Func<Task> Run)[]
{
    ("Protected API rejects anonymous requests", ProtectedApiRejectsAnonymousRequests),
    ("Registration rejects invalid input", RegistrationEndpointRejectsInvalidInput),
    ("JWT login and owned CRUD workflow succeeds", JwtAndOwnedCrudWorkflowSucceeds)
};

foreach (var test in integrationTests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add(test.Name);
        Console.WriteLine($"FAIL  {test.Name}: {exception}");
    }
}

Console.WriteLine();
var totalTests = tests.Length + integrationTests.Length;
Console.WriteLine($"Result: {totalTests - failures.Count}/{totalTests} tests passed.");
return failures.Count == 0 ? 0 : 1;

static async Task ProtectedApiRejectsAnonymousRequests()
{
    using var factory = new FinancialTrackerFactory();
    await factory.InitializeDatabaseAsync();
    using var client = factory.CreateClient();
    var response = await client.GetAsync("/api/categories");
    Equal(HttpStatusCode.Unauthorized, response.StatusCode,
        "Categories API should require a bearer token.");
}

static async Task RegistrationEndpointRejectsInvalidInput()
{
    using var factory = new FinancialTrackerFactory();
    await factory.InitializeDatabaseAsync();
    using var client = factory.CreateClient();
    var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
    {
        Email = "invalid",
        DisplayName = "A",
        Password = "short",
        ConfirmPassword = "different"
    });
    Equal(HttpStatusCode.BadRequest, response.StatusCode,
        "Invalid registration should return HTTP 400.");
}

static async Task JwtAndOwnedCrudWorkflowSucceeds()
{
    using var factory = new FinancialTrackerFactory();
    await factory.InitializeDatabaseAsync();
    using var firstClient = factory.CreateClient();
    var firstToken = await RegisterAsync(firstClient, "first@example.test", "First User");
    firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstToken);

    var categoryResponse = await firstClient.PostAsJsonAsync("/api/categories", new CategoryRequest
    {
        Name = "Pet care",
        Type = TransactionType.Expense,
        ColorHex = "#336699",
        Icon = "paw"
    });
    Equal(HttpStatusCode.Created, categoryResponse.StatusCode, "Category creation should return HTTP 201.");
    var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
    True(category is not null && !category.IsGlobal, "Created category should be personal.");

    var categoryUpdate = await firstClient.PutAsJsonAsync($"/api/categories/{category!.Id}", new CategoryRequest
    {
        Name = "Pet expenses", Type = TransactionType.Expense, ColorHex = "#336699", Icon = "paw"
    });
    Equal(HttpStatusCode.OK, categoryUpdate.StatusCode, "Category update should return HTTP 200.");

    var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
    var transactionResponse = await firstClient.PostAsJsonAsync("/api/transactions", new TransactionRequest
    {
        Type = TransactionType.Expense,
        Amount = 42.50m,
        Date = currentDate,
        CategoryId = category.Id,
        Description = "Veterinary supplies"
    });
    Equal(HttpStatusCode.Created, transactionResponse.StatusCode,
        "Transaction creation should return HTTP 201.");
    var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
    True(transaction is not null, "Created transaction response is required.");

    var transactionUpdate = await firstClient.PutAsJsonAsync($"/api/transactions/{transaction!.Id}", new TransactionRequest
    {
        Type = TransactionType.Expense, Amount = 50m, Date = currentDate,
        CategoryId = category.Id, Description = "Updated supplies"
    });
    Equal(HttpStatusCode.OK, transactionUpdate.StatusCode, "Transaction update should return HTTP 200.");

    var budgetCreate = await firstClient.PostAsJsonAsync("/api/budgets", new BudgetRequest
    {
        Month = new DateOnly(currentDate.Year, currentDate.Month, 1),
        CategoryId = category.Id,
        Amount = 100m,
        WarningThresholdPercent = 80
    });
    Equal(HttpStatusCode.Created, budgetCreate.StatusCode, "Budget creation should return HTTP 201.");
    var budgets = await firstClient.GetFromJsonAsync<List<BudgetResponse>>("/api/budgets");
    True(budgets is { Count: 1 } && budgets[0].ActualSpending == 50m,
        "Budget actual spending should include the updated expense.");

    var goalCreate = await firstClient.PostAsJsonAsync("/api/saving-goals", new SavingGoalRequest
    {
        Name = "Emergency fund", TargetAmount = 500m, TargetDate = currentDate.AddMonths(6)
    });
    Equal(HttpStatusCode.Created, goalCreate.StatusCode, "Saving goal creation should return HTTP 201.");
    var goal = await goalCreate.Content.ReadFromJsonAsync<SavingGoalResponse>();
    var contribution = await firstClient.PostAsJsonAsync($"/api/saving-goals/{goal!.Id}/contributions", new ContributionRequest
    {
        Amount = 125m, Date = currentDate, Note = "First contribution"
    });
    var updatedGoal = await contribution.Content.ReadFromJsonAsync<SavingGoalResponse>();
    True(updatedGoal?.SavedAmount == 125m && updatedGoal.Remaining == 375m,
        "Saving goal progress should be derived from contributions.");

    var report = await firstClient.GetFromJsonAsync<ReportViewModel>("/api/reports/summary?months=1");
    True(report?.TotalExpenses == 50m && report.Balance == -50m,
        "Report totals should include the owned current-month expense.");

    var firstOwnerRead = await firstClient.GetAsync($"/api/transactions/{transaction.Id}");
    Equal(HttpStatusCode.OK, firstOwnerRead.StatusCode, "Owner should read their transaction.");

    using var secondClient = factory.CreateClient();
    var secondToken = await RegisterAsync(secondClient, "second@example.test", "Second User");
    secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondToken);
    var crossUserRead = await secondClient.GetAsync($"/api/transactions/{transaction.Id}");
    Equal(HttpStatusCode.NotFound, crossUserRead.StatusCode,
        "Another user must not discover the transaction by ID.");

    var categoryConflict = await firstClient.DeleteAsync($"/api/categories/{category.Id}");
    Equal(HttpStatusCode.Conflict, categoryConflict.StatusCode,
        "A category referenced by a transaction or budget must not be deleted.");

    var deleteResponse = await firstClient.DeleteAsync($"/api/transactions/{transaction.Id}");
    Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode, "Owner deletion should return HTTP 204.");
}

static async Task<string> RegisterAsync(HttpClient client, string email, string displayName)
{
    const string password = "Valid-Test1!";
    var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
    {
        Email = email,
        DisplayName = displayName,
        Password = password,
        ConfirmPassword = password
    });
    Equal(HttpStatusCode.Created, register.StatusCode, "Valid registration should return HTTP 201.");
    var registeredToken = await register.Content.ReadFromJsonAsync<TokenResponse>();
    True(registeredToken is not null && !string.IsNullOrWhiteSpace(registeredToken.AccessToken),
        "Registration should return a JWT.");

    var invalidLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
    {
        Email = email,
        Password = "Wrong-Password1!"
    });
    Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode,
        "An incorrect password should return HTTP 401.");

    var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
    {
        Email = email,
        Password = password
    });
    Equal(HttpStatusCode.OK, login.StatusCode, "Valid login should return HTTP 200.");
    var loginToken = await login.Content.ReadFromJsonAsync<TokenResponse>();
    return loginToken?.AccessToken ?? throw new InvalidOperationException("Login did not return a JWT.");
}

static ApplicationDbContext CreateContext()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=(local);Database=ModelInspection;Trusted_Connection=True;TrustServerCertificate=True")
        .Options;
    return new ApplicationDbContext(options);
}

static void ModelContainsRequiredEntities()
{
    using var context = CreateContext();
    var requiredTypes = new[]
    {
        typeof(ApplicationUser),
        typeof(Category),
        typeof(FinancialTransaction),
        typeof(Budget),
        typeof(SavingGoal),
        typeof(SavingGoalContribution),
        typeof(RefreshToken)
    };

    foreach (var requiredType in requiredTypes)
    {
        True(GetDesignTimeModel(context).FindEntityType(requiredType) is not null,
            $"Missing EF entity: {requiredType.Name}.");
    }
}

static void MoneyConstraintsAreConfigured()
{
    using var context = CreateContext();
    foreach (var entityType in new[]
             {
                 typeof(FinancialTransaction),
                 typeof(Budget),
                 typeof(SavingGoal),
                 typeof(SavingGoalContribution)
             })
    {
        var metadata = GetDesignTimeModel(context).FindEntityType(entityType)!;
        var amountPropertyName = entityType == typeof(SavingGoal)
            ? nameof(SavingGoal.TargetAmount)
            : "Amount";
        var amount = metadata.FindProperty(amountPropertyName)!;
        Equal(18, amount.GetPrecision(), $"{entityType.Name} precision should be 18.");
        Equal(2, amount.GetScale(), $"{entityType.Name} scale should be 2.");
        True(metadata.GetCheckConstraints().Any(),
            $"{entityType.Name} should have a positive-amount check constraint.");
    }
}

static void OwnershipIndexesAreConfigured()
{
    using var context = CreateContext();
    var model = GetDesignTimeModel(context);
    var transaction = model.FindEntityType(typeof(FinancialTransaction))!;
    True(transaction.GetIndexes().Any(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "UserId", "TransactionDate" })),
        "Transactions need a user/date index.");

    var category = model.FindEntityType(typeof(Category))!;
    True(category.GetIndexes().Any(index => index.IsUnique && index.GetFilter() == "[UserId] IS NOT NULL"),
        "Personal categories need a filtered unique index.");

    var categoryForeignKey = transaction.GetForeignKeys()
        .Single(key => key.PrincipalEntityType.ClrType == typeof(Category));
    Equal(DeleteBehavior.Restrict, categoryForeignKey.DeleteBehavior,
        "Deleting a referenced category must be restricted.");
}

static void ReferenceDataIsSeeded()
{
    using var context = CreateContext();
    var model = GetDesignTimeModel(context);
    var roles = model.FindEntityType(typeof(IdentityRole))!.GetSeedData();
    True(roles.Any(role => Equals(role[nameof(IdentityRole.NormalizedName)], "USER")),
        "User role should be seeded.");
    True(roles.Any(role => Equals(role[nameof(IdentityRole.NormalizedName)], "ADMIN")),
        "Admin role should be seeded.");

    var categories = model.FindEntityType(typeof(Category))!.GetSeedData();
    Equal(10, categories.Count(), "Ten global categories should be seeded.");
    True(categories.All(category => category[nameof(Category.UserId)] is null),
        "Seed categories should be global.");
}

static void RegistrationValidationRejectsInvalidInput()
{
    var request = new RegisterRequest
    {
        Email = "not-an-email",
        DisplayName = "A",
        Password = "short",
        ConfirmPassword = "different"
    };
    var errors = Validate(request);
    True(errors.Count >= 4, "Invalid registration should produce at least four errors.");
}

static void CategoryValidationRejectsInvalidColor()
{
    var request = new CategoryRequest
    {
        Name = "Food",
        Type = TransactionType.Expense,
        ColorHex = "red"
    };
    True(Validate(request).Any(error => error.MemberNames.Contains(nameof(request.ColorHex))),
        "Invalid category color should be rejected.");
}

static void TransactionValidationRejectsInvalidInput()
{
    var request = new TransactionRequest
    {
        Type = TransactionType.Expense,
        Amount = 0,
        Date = DateOnly.FromDateTime(DateTime.Today),
        CategoryId = 0,
        Description = new string('x', 251)
    };
    var errors = Validate(request);
    True(errors.Count >= 3, "Amount, category, and description should be rejected.");
}

static void PagedResponseCalculatesPageCount()
{
    var page = new PagedResponse<int>([1, 2, 3], 2, 10, 21);
    Equal(3, page.TotalPages, "Twenty-one records at ten per page should produce three pages.");
}

static void CurrencyPreferenceFormatsMoney()
{
    var usd = CurrencyCulture.GetNumberFormat("USD");
    var vnd = CurrencyCulture.GetNumberFormat("VND");
    Equal("$", usd.CurrencySymbol, "USD should use the dollar symbol.");
    Equal("₫", vnd.CurrencySymbol, "VND should use the dong symbol.");
    Equal(0, vnd.CurrencyDecimalDigits, "VND should display without fractional digits.");
}

static void DecimalValidationWorksWithVndCulture()
{
    var originalCulture = CultureInfo.CurrentCulture;
    try
    {
        var vndCulture = (CultureInfo)originalCulture.Clone();
        vndCulture.NumberFormat = CurrencyCulture.GetNumberFormat("VND");
        CultureInfo.CurrentCulture = vndCulture;

        var model = new PersonalExpenseTracker.Models.Transactions.TransactionFormViewModel
        {
            Type = TransactionType.Expense,
            Amount = 1m,
            CategoryId = 1,
            Date = DateOnly.FromDateTime(DateTime.Today)
        };
        var errors = Validate(model);
        True(errors.All(error => !error.MemberNames.Contains(nameof(model.Amount))),
            "A valid amount should pass validation when VND formatting is active.");
    }
    finally
    {
        CultureInfo.CurrentCulture = originalCulture;
    }
}

static IReadOnlyList<ValidationResult> Validate(object value)
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(value, new ValidationContext(value), results, true);
    return results;
}

static IModel GetDesignTimeModel(ApplicationDbContext context) =>
    context.GetService<IDesignTimeModel>().Model;

static void True(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}; actual: {actual}.");
    }
}
