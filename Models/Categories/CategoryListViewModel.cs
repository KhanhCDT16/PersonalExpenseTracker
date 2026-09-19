using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Models.Categories;

public sealed class CategoryListViewModel
{
    public IReadOnlyList<Category> GlobalCategories { get; init; } = [];
    public IReadOnlyList<Category> PersonalCategories { get; init; } = [];
}
