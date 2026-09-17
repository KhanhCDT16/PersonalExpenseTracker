# ExpenseTracker — Local Development

## Prerequisites

- .NET 10 SDK (the repository is pinned by `global.json`)
- SQL Server LocalDB, SQL Server Express/Developer, or another reachable SQL Server instance
- Optional: the `dotnet-ef` local tool is declared in `.config/dotnet-tools.json`

## First run

From the repository root:

```powershell
dotnet restore ExpenseTracker.sln
dotnet tool restore
dotnet ef database update --project PersonalExpenseTracker.csproj --startup-project PersonalExpenseTracker.csproj
dotnet run --project PersonalExpenseTracker.csproj
```

Open the HTTPS or HTTP address printed by ASP.NET Core. Swagger is available at `/swagger` only in Development.

The default connection string targets the `ExpenseTracker` database on `(localdb)\MSSQLLocalDB`. If LocalDB is unavailable, override it without modifying committed files:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=ExpenseTracker;Trusted_Connection=True;TrustServerCertificate=True"
dotnet ef database update --project PersonalExpenseTracker.csproj --startup-project PersonalExpenseTracker.csproj
dotnet run --project PersonalExpenseTracker.csproj
```

Development creates a random JWT signing key at each start if `Jwt:Key` is empty. Existing development API tokens therefore become invalid after restart.

## Administrator account

No default password is stored in source control. Set bootstrap values for the first run against an initialized database:

```powershell
$env:BootstrapAdmin__Email = "admin@example.com"
$env:BootstrapAdmin__Password = "replace-with-a-strong-password"
dotnet run --project PersonalExpenseTracker.csproj
```

Remove those environment variables after the account has been created. The bootstrap process is idempotent.

## Verification

```powershell
dotnet build ExpenseTracker.sln --configuration Release
dotnet run --project tests/PersonalExpenseTracker.Tests --configuration Release
```

The integration suite uses a unique in-memory database. It does not alter the development SQL Server database.

## Common build error

If MSBuild reports a path recursively containing `tests\...\bin\...\tests`, delete the existing root and test `bin`/`obj` folders once, then rebuild. The web project now excludes the complete `tests/**` tree from Web SDK content items, preventing that recursive copy problem.
