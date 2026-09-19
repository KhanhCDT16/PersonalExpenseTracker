using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models.Categories;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class CategoriesController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive &&
                (category.UserId == null || category.UserId == userId))
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return View(new CategoryListViewModel
        {
            GlobalCategories = categories.Where(category => category.UserId is null).ToList(),
            PersonalCategories = categories.Where(category => category.UserId == userId).ToList()
        });
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        await ValidateUniqueNameAsync(model, userId, excludedId: null, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.Categories.Add(new Category
        {
            UserId = userId,
            Name = model.Name.Trim(),
            Type = model.Type,
            ColorHex = model.ColorHex.ToUpperInvariant(),
            Icon = NormalizeOptional(model.Icon)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            ColorHex = category.ColorHex,
            Icon = category.Icon
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var userId = userManager.GetUserId(User)!;
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        await ValidateUniqueNameAsync(model, userId, id, cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        category.Name = model.Name.Trim();
        category.Type = model.Type;
        category.ColorHex = model.ColorHex.ToUpperInvariant();
        category.Icon = NormalizeOptional(model.Icon);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var isInUse = await dbContext.FinancialTransactions
                .AnyAsync(transaction => transaction.CategoryId == id, cancellationToken)
            || await dbContext.Budgets.AnyAsync(budget => budget.CategoryId == id, cancellationToken);
        if (isInUse)
        {
            TempData["ErrorMessage"] = "This category is in use and cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Category deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueNameAsync(
        CategoryFormViewModel model,
        string userId,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return;
        }

        var normalizedName = model.Name.Trim();
        var exists = await dbContext.Categories.AnyAsync(category =>
            category.UserId == userId
            && category.Type == model.Type
            && category.Name == normalizedName
            && (!excludedId.HasValue || category.Id != excludedId.Value), cancellationToken);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Name), "You already have this category for the selected type.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
