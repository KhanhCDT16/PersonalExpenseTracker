using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;
using PersonalExpenseTracker.Models.Transactions;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class TransactionsController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    private const int DefaultPageSize = 10;

    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        TransactionType? type,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var userId = userManager.GetUserId(User)!;
        page = Math.Max(1, page);
        var query = dbContext.FinancialTransactions
            .AsNoTracking()
            .Where(transaction => transaction.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(transaction =>
                (transaction.Description != null && transaction.Description.Contains(term))
                || transaction.Category.Name.Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == type.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionDate <= dateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)DefaultPageSize));
        page = Math.Min(page, totalPages);
        var transactions = await query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(transaction => new TransactionListItemViewModel
            {
                Id = transaction.Id,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Date = transaction.TransactionDate,
                CategoryName = transaction.Category.Name,
                CategoryColor = transaction.Category.ColorHex,
                Description = transaction.Description
            })
            .ToListAsync(cancellationToken);

        return View(new TransactionListViewModel
        {
            Transactions = transactions,
            Search = search,
            CategoryId = categoryId,
            Type = type,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            Categories = await GetCategoryOptionsAsync(userId, cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var transaction = await dbContext.FinancialTransactions
            .AsNoTracking()
            .Where(item => item.Id == id && item.UserId == userId)
            .Select(item => new TransactionDetailsViewModel
            {
                Id = item.Id,
                Type = item.Type,
                Amount = item.Amount,
                Date = item.TransactionDate,
                CategoryName = item.Category.Name,
                CategoryColor = item.Category.ColorHex,
                Description = item.Description,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
        return transaction is null ? NotFound() : View(transaction);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        return View(new TransactionFormViewModel
        {
            Categories = await GetCategoryOptionsAsync(userId, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TransactionFormViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        await ValidateCategoryAsync(model, userId, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(userId, cancellationToken);
            return View(model);
        }

        var now = DateTime.UtcNow;
        dbContext.FinancialTransactions.Add(new FinancialTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = model.CategoryId,
            Type = model.Type,
            Amount = model.Amount,
            TransactionDate = model.Date,
            Description = NormalizeOptional(model.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Transaction added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var transaction = await dbContext.FinancialTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        return View(new TransactionFormViewModel
        {
            Id = transaction.Id,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Date = transaction.TransactionDate,
            CategoryId = transaction.CategoryId,
            Description = transaction.Description,
            Categories = await GetCategoryOptionsAsync(userId, cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        TransactionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var userId = userManager.GetUserId(User)!;
        var transaction = await dbContext.FinancialTransactions
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        await ValidateCategoryAsync(model, userId, cancellationToken);
        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoryOptionsAsync(userId, cancellationToken);
            return View(model);
        }

        transaction.CategoryId = model.CategoryId;
        transaction.Type = model.Type;
        transaction.Amount = model.Amount;
        transaction.TransactionDate = model.Date;
        transaction.Description = NormalizeOptional(model.Description);
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Transaction updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var transaction = await dbContext.FinancialTransactions
            .AsNoTracking()
            .Where(item => item.Id == id && item.UserId == userId)
            .Select(item => new TransactionDetailsViewModel
            {
                Id = item.Id,
                Type = item.Type,
                Amount = item.Amount,
                Date = item.TransactionDate,
                CategoryName = item.Category.Name,
                CategoryColor = item.Category.ColorHex,
                Description = item.Description,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
        return transaction is null ? NotFound() : View(transaction);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var transaction = await dbContext.FinancialTransactions
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        dbContext.FinancialTransactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Transaction deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCategoryAsync(
        TransactionFormViewModel model,
        string userId,
        CancellationToken cancellationToken)
    {
        var isValid = await dbContext.Categories.AnyAsync(category =>
            category.Id == model.CategoryId
            && category.IsActive
            && category.Type == model.Type
            && (category.UserId == null || category.UserId == userId), cancellationToken);
        if (!isValid)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Choose an available category that matches the transaction type.");
        }
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .Where(category => category.IsActive &&
                (category.UserId == null || category.UserId == userId))
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category => new SelectListItem(
                $"{category.Type} — {category.Name}",
                category.Id.ToString()))
            .ToListAsync(cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
