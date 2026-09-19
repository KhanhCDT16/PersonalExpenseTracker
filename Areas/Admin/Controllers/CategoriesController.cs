using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models.Categories;

namespace PersonalExpenseTracker.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class CategoriesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dbContext.Categories.AsNoTracking().Where(category => category.UserId == null)
            .OrderBy(category => category.Type).ThenBy(category => category.Name)
            .ToListAsync(cancellationToken));

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        await ValidateUniqueAsync(model, null, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        dbContext.Categories.Add(new Category
        {
            UserId = null, Name = model.Name.Trim(), Type = model.Type,
            ColorHex = model.ColorHex.ToUpperInvariant(),
            Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim(), IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Global category created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == null, cancellationToken);
        return category is null ? NotFound() : View(new CategoryFormViewModel
        {
            Id = category.Id, Name = category.Name, Type = category.Type,
            ColorHex = category.ColorHex, Icon = category.Icon
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == null, cancellationToken);
        if (category is null) return NotFound();
        await ValidateUniqueAsync(model, id, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        category.Name = model.Name.Trim(); category.Type = model.Type;
        category.ColorHex = model.ColorHex.ToUpperInvariant();
        category.Icon = string.IsNullOrWhiteSpace(model.Icon) ? null : model.Icon.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Global category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == null, cancellationToken);
        if (category is null) return NotFound();
        category.IsActive = !category.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"Global category {(category.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueAsync(CategoryFormViewModel model, int? excludedId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) return;
        var name = model.Name.Trim();
        if (await dbContext.Categories.AnyAsync(category => category.UserId == null
                && category.Name == name && category.Type == model.Type
                && (!excludedId.HasValue || category.Id != excludedId), cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Name), "This global category already exists.");
        }
    }
}
