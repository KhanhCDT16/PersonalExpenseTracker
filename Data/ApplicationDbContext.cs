using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<SavingGoal> SavingGoals => Set<SavingGoal>();
    public DbSet<SavingGoalContribution> SavingGoalContributions => Set<SavingGoalContribution>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(user => user.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
            entity.Property(user => user.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        builder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(50).IsRequired();
            entity.Property(category => category.ColorHex).HasMaxLength(7).IsUnicode(false).IsRequired();
            entity.Property(category => category.Icon).HasMaxLength(50);
            entity.HasIndex(category => new { category.UserId, category.Name, category.Type })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");
            entity.HasIndex(category => new { category.Name, category.Type })
                .IsUnique()
                .HasFilter("[UserId] IS NULL");
            entity.HasOne(category => category.User)
                .WithMany(user => user.Categories)
                .HasForeignKey(category => category.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FinancialTransaction>(entity =>
        {
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.TransactionDate).HasColumnType("date");
            entity.Property(transaction => transaction.Description).HasMaxLength(250);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.TransactionDate });
            entity.HasIndex(transaction => new
                { transaction.UserId, transaction.CategoryId, transaction.TransactionDate });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_FinancialTransactions_Amount_Positive",
                "[Amount] > 0"));
            entity.HasOne(transaction => transaction.User)
                .WithMany(user => user.Transactions)
                .HasForeignKey(transaction => transaction.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(transaction => transaction.Category)
                .WithMany(category => category.Transactions)
                .HasForeignKey(transaction => transaction.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Budget>(entity =>
        {
            entity.Property(budget => budget.Amount).HasPrecision(18, 2);
            entity.Property(budget => budget.Month).HasColumnType("date");
            entity.HasIndex(budget => new { budget.UserId, budget.Month, budget.CategoryId }).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Budgets_Amount_Positive", "[Amount] > 0");
                table.HasCheckConstraint(
                    "CK_Budgets_WarningThreshold_Range",
                    "[WarningThresholdPercent] BETWEEN 1 AND 100");
            });
            entity.HasOne(budget => budget.User)
                .WithMany(user => user.Budgets)
                .HasForeignKey(budget => budget.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(budget => budget.Category)
                .WithMany(category => category.Budgets)
                .HasForeignKey(budget => budget.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SavingGoal>(entity =>
        {
            entity.Property(goal => goal.Name).HasMaxLength(100).IsRequired();
            entity.Property(goal => goal.TargetAmount).HasPrecision(18, 2);
            entity.Property(goal => goal.TargetDate).HasColumnType("date");
            entity.HasIndex(goal => new { goal.UserId, goal.Status });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SavingGoals_TargetAmount_Positive",
                "[TargetAmount] > 0"));
            entity.HasOne(goal => goal.User)
                .WithMany(user => user.SavingGoals)
                .HasForeignKey(goal => goal.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SavingGoalContribution>(entity =>
        {
            entity.Property(contribution => contribution.Amount).HasPrecision(18, 2);
            entity.Property(contribution => contribution.ContributionDate).HasColumnType("date");
            entity.Property(contribution => contribution.Note).HasMaxLength(250);
            entity.HasIndex(contribution => new
                { contribution.SavingGoalId, contribution.ContributionDate });
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_SavingGoalContributions_Amount_Positive",
                "[Amount] > 0"));
            entity.HasOne(contribution => contribution.SavingGoal)
                .WithMany(goal => goal.Contributions)
                .HasForeignKey(contribution => contribution.SavingGoalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(token => token.TokenHash).HasMaxLength(88).IsUnicode(false).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedReferenceData(builder);
    }

    private static void SeedReferenceData(ModelBuilder builder)
    {
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "10000000-0000-0000-0000-000000000001",
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "10000000-0000-0000-0000-000000000001"
            },
            new IdentityRole
            {
                Id = "10000000-0000-0000-0000-000000000002",
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "10000000-0000-0000-0000-000000000002"
            });

        var createdAtUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);
        builder.Entity<Category>().HasData(
            GlobalCategory(1, "Salary", Models.TransactionType.Income, "#16794B", "briefcase", createdAtUtc),
            GlobalCategory(2, "Other income", Models.TransactionType.Income, "#2B8A6E", "plus-circle", createdAtUtc),
            GlobalCategory(3, "Food", Models.TransactionType.Expense, "#E07A5F", "utensils", createdAtUtc),
            GlobalCategory(4, "Housing", Models.TransactionType.Expense, "#9C6ADE", "house", createdAtUtc),
            GlobalCategory(5, "Transport", Models.TransactionType.Expense, "#3D7EA6", "car", createdAtUtc),
            GlobalCategory(6, "Utilities", Models.TransactionType.Expense, "#E3A72F", "bolt", createdAtUtc),
            GlobalCategory(7, "Health", Models.TransactionType.Expense, "#C84B66", "heart-pulse", createdAtUtc),
            GlobalCategory(8, "Education", Models.TransactionType.Expense, "#4666B0", "graduation-cap", createdAtUtc),
            GlobalCategory(9, "Entertainment", Models.TransactionType.Expense, "#D15A9C", "film", createdAtUtc),
            GlobalCategory(10, "Other expense", Models.TransactionType.Expense, "#65736D", "circle", createdAtUtc));
    }

    private static Category GlobalCategory(
        int id,
        string name,
        Models.TransactionType type,
        string colorHex,
        string icon,
        DateTime createdAtUtc) => new()
    {
        Id = id,
        UserId = null,
        Name = name,
        Type = type,
        ColorHex = colorHex,
        Icon = icon,
        IsActive = true,
        CreatedAtUtc = createdAtUtc
    };
}
