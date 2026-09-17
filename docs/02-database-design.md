# Phase 2 — Database Design

## 1. Entity-relationship diagram

```mermaid
erDiagram
    APPLICATION_USER ||--o{ FINANCIAL_TRANSACTION : owns
    APPLICATION_USER ||--o{ CATEGORY : creates
    APPLICATION_USER ||--o{ BUDGET : sets
    APPLICATION_USER ||--o{ SAVING_GOAL : owns
    APPLICATION_USER ||--o{ REFRESH_TOKEN : receives
    CATEGORY ||--o{ FINANCIAL_TRANSACTION : classifies
    CATEGORY ||--o{ BUDGET : scopes
    SAVING_GOAL ||--o{ SAVING_GOAL_CONTRIBUTION : contains

    APPLICATION_USER {
        string Id PK
        string Email UK
        string DisplayName
        string CurrencyCode
        string TimeZoneId
        bool IsActive
        datetime CreatedAtUtc
    }
    CATEGORY {
        int Id PK
        string UserId FK nullable
        string Name
        int Type
        string ColorHex
        string Icon
        bool IsActive
        datetime CreatedAtUtc
    }
    FINANCIAL_TRANSACTION {
        guid Id PK
        string UserId FK
        int CategoryId FK
        int Type
        decimal Amount
        date TransactionDate
        string Description
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }
    BUDGET {
        guid Id PK
        string UserId FK
        int CategoryId FK nullable
        date Month
        decimal Amount
        int WarningThresholdPercent
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }
    SAVING_GOAL {
        guid Id PK
        string UserId FK
        string Name
        decimal TargetAmount
        date TargetDate nullable
        int Status
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }
    SAVING_GOAL_CONTRIBUTION {
        guid Id PK
        guid SavingGoalId FK
        decimal Amount
        date ContributionDate
        string Note
        datetime CreatedAtUtc
    }
    REFRESH_TOKEN {
        guid Id PK
        string UserId FK
        string TokenHash UK
        datetime ExpiresAtUtc
        datetime CreatedAtUtc
        datetime RevokedAtUtc nullable
    }
```

Identity also creates its standard user, role, user-role, claim, login, and token tables. `APPLICATION_USER` above extends the Identity user table rather than replacing it.

## 2. Keys and relationships

| Child | Foreign key | Parent | Delete behavior | Reason |
|---|---|---|---|---|
| Category | `UserId` nullable | ApplicationUser | Restrict | A user-owned category must not disappear while referenced; null denotes global category |
| FinancialTransaction | `UserId` | ApplicationUser | Cascade | Account deletion removes private financial records after explicit administrative confirmation |
| FinancialTransaction | `CategoryId` | Category | Restrict | Referenced categories cannot be deleted accidentally |
| Budget | `UserId` | ApplicationUser | Cascade | Budget belongs to one account |
| Budget | `CategoryId` nullable | Category | Restrict | Null represents an overall monthly budget |
| SavingGoal | `UserId` | ApplicationUser | Cascade | Goal belongs to one account |
| SavingGoalContribution | `SavingGoalId` | SavingGoal | Cascade | Contributions have no meaning without their goal |
| RefreshToken | `UserId` | ApplicationUser | Cascade | Revocable API sessions belong to one account |

## 3. Constraints

- Transaction, budget, goal, and contribution amounts must be greater than zero.
- Currency code is exactly three uppercase ISO-style characters; initial supported value is `USD` unless localization is configured.
- Category name is 2–50 characters; description/note lengths are bounded.
- Budget `Month` is stored as the first day of the month.
- Warning threshold is between 1 and 100 percent.
- Goal target date is optional; goal status is an enum constrained by application validation.
- `Category.Type` must match `FinancialTransaction.Type` when a transaction is created or updated.
- System/global categories have a null `UserId`; users may reference but not mutate them.

## 4. Unique keys and indexes

| Table | Index | Purpose |
|---|---|---|
| Category | Unique filtered `(UserId, Name, Type)` for user-owned rows | Prevent duplicate personal categories |
| Category | Unique filtered `(Name, Type)` where `UserId IS NULL` | Prevent duplicate global categories |
| FinancialTransaction | `(UserId, TransactionDate DESC)` | Transaction list, recent activity, monthly totals |
| FinancialTransaction | `(UserId, CategoryId, TransactionDate)` | Category reports and budget calculation |
| Budget | Unique `(UserId, Month, CategoryId)` | One budget per monthly scope |
| SavingGoal | `(UserId, Status)` | Active-goal list |
| SavingGoalContribution | `(SavingGoalId, ContributionDate)` | Progress calculation |
| RefreshToken | Unique `TokenHash` | Secure token lookup without storing raw tokens |
| Identity user | Unique normalized email | One account per email |

SQL Server treats nullable values specially in unique indexes, so global categories and overall budgets require filtered indexes or normalized computed keys in the migration. The migration is the final authority for provider-specific index definitions.

## 5. Reporting rules

- Income is the sum of owned `Income` transactions within the requested date range.
- Expenses are the sum of owned `Expense` transactions within the requested date range.
- Balance equals income minus expenses.
- Category expense totals include expense transactions only.
- Budget actual spending includes owned expense transactions in the budget month and, for a category budget, only that category.
- Saving progress equals the sum of non-deleted contributions; stored goal rows do not duplicate the current total.
- All ranges use an inclusive start and exclusive end to avoid end-of-day errors.

## 6. Data ownership invariant

Knowing a record ID must never be sufficient to retrieve or modify it. User-facing queries always combine the resource key with the authenticated `UserId`. Administrator endpoints are separate, explicitly role-protected, and must not reuse ordinary user mutation endpoints to bypass ownership.
