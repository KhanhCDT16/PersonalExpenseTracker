# ExpenseTracker

ExpenseTracker is a multi-user personal-finance website and authenticated API built with ASP.NET Core MVC, .NET 10, Entity Framework Core, SQL Server, Identity, and JWT bearer authentication.

## Features

- Registration, login/logout, account lockout, roles, cookie authentication, and JWT API access
- Per-user category and transaction CRUD with paging, search, filters, and ownership enforcement
- Monthly and category budgets with actual spending, utilization, and warnings
- Saving goals with contributions, progress, completion, and archiving
- Dashboard cards, budget health, goals, recent activity, monthly reports, and charts
- Profile preferences for display name, currency, and time zone
- Administrator user/role/status controls and global category management
- Swagger/OpenAPI in Development and a `/health` endpoint

## Prerequisites

- Windows 10 or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server Express LocalDB, or another reachable SQL Server instance

### Install SQL Server Express LocalDB

The default development connection uses `(localdb)\MSSQLLocalDB`. SQL Server 2025 does not always add `SqlLocalDB.exe` to the command search path. Check the standard installation path first:

```powershell
$sqlLocalDb = "C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe"
Test-Path $sqlLocalDb
```

If this prints `True`, LocalDB is installed; use the full executable path in the commands below. If the instance information command succeeds and shows `State: Running`, skip to **Create and run the application**:

```powershell
& $sqlLocalDb info MSSQLLocalDB
```

Otherwise, download Microsoft's current SQL Server 2025 Express installer from PowerShell:

```powershell
$installer = "$env:USERPROFILE\Downloads\SQL2025-SSEI-Expr.exe"

Invoke-WebRequest `
  -Uri "https://aka.ms/sql2025express" `
  -OutFile $installer

Start-Process $installer -Verb RunAs
```

In the installer, choose **Custom**, select **New SQL Server standalone installation**, and enable **LocalDB** on the Feature Selection page Only (Please ignore other pages, then click Next). Complete the installation and then open a new, non-administrator PowerShell window. Create and start the instance under your normal Windows account:

```powershell
$sqlLocalDb = "C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe"
& $sqlLocalDb create MSSQLLocalDB -s
& $sqlLocalDb info MSSQLLocalDB
```

If the instance already exists, start it instead:

```powershell
& $sqlLocalDb start MSSQLLocalDB
& $sqlLocalDb info MSSQLLocalDB
```

The result should show `State: Running`. See Microsoft's [SQL Server downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) and [LocalDB documentation](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) for the current installer and additional troubleshooting.

## Create and run the application

From the repository root, restore the solution, apply the checked-in migration, and start the website:

```powershell
dotnet restore ExpenseTracker.sln
dotnet tool restore
dotnet ef database update --project PersonalExpenseTracker.csproj --startup-project PersonalExpenseTracker.csproj
dotnet run --project PersonalExpenseTracker.csproj
```
Open Browser -> Input http://localhost:5169

The default database is `ExpenseTracker` on `(localdb)\MSSQLLocalDB`. See [local development](docs/04-local-development.md) for connection overrides, administrator bootstrap, testing, and the fix for the old recursive `tests/bin` build error.

## Verify

```powershell
dotnet build ExpenseTracker.sln --configuration Release
dotnet run --project tests/PersonalExpenseTracker.Tests --configuration Release
```

