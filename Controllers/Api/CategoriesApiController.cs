using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Contracts.Categories;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Controllers.Api;

[ApiController]
[Route("api/categories")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class CategoriesApiController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List(
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive &&
                (category.UserId == null || category.UserId == userId))
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Type,
                category.ColorHex,
                category.Icon,
                category.UserId == null,
                category.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var name = request.Name.Trim();
        if (await dbContext.Categories.AnyAsync(category =>
                category.UserId == userId && category.Type == request.Type && category.Name == name,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Name), "A category with this name and type already exists.");
            return ValidationProblem(ModelState);
        }

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Type = request.Type,
            ColorHex = request.ColorHex.ToUpperInvariant(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim()
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(category);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, response);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var category = await dbContext.Categories.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == id && item.IsActive && (item.UserId == null || item.UserId == userId),
            cancellationToken);
        return category is null ? NotFound() : Ok(ToResponse(category));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        int id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var name = request.Name.Trim();
        if (await dbContext.Categories.AnyAsync(item =>
                item.Id != id && item.UserId == userId && item.Type == request.Type && item.Name == name,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Name), "A category with this name and type already exists.");
            return ValidationProblem(ModelState);
        }

        category.Name = name;
        category.Type = request.Type;
        category.ColorHex = request.ColorHex.ToUpperInvariant();
        category.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(category));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var isInUse = await dbContext.FinancialTransactions
                .AnyAsync(transaction => transaction.CategoryId == id, cancellationToken)
            || await dbContext.Budgets.AnyAsync(budget => budget.CategoryId == id, cancellationToken);
        if (isInUse)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Category is in use",
                Detail = "Change or remove related transactions and budgets before deleting this category.",
                Status = StatusCodes.Status409Conflict
            });
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static CategoryResponse ToResponse(Category category) => new(
        category.Id,
        category.Name,
        category.Type,
        category.ColorHex,
        category.Icon,
        category.UserId is null,
        category.IsActive);
}
