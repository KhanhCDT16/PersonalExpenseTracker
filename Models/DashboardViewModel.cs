using PersonalExpenseTracker.Models.Transactions;
using PersonalExpenseTracker.Models.Budgets;
using PersonalExpenseTracker.Models.SavingGoals;

namespace PersonalExpenseTracker.Models;

public sealed class DashboardViewModel
{
    public string PeriodLabel { get; init; } = string.Empty;
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal Balance => TotalIncome - TotalExpenses;
    public IReadOnlyList<TransactionListItemViewModel> RecentTransactions { get; init; } = [];
    public IReadOnlyList<BudgetListItemViewModel> Budgets { get; init; } = [];
    public IReadOnlyList<SavingGoalListItemViewModel> SavingGoals { get; init; } = [];
}
