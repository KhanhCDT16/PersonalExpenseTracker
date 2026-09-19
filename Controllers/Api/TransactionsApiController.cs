using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Contracts.Common;
using PersonalExpenseTracker.Contracts.Transactions;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Controllers.Api;

[ApiController]
[Route("api/transactions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class TransactionsApiController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> List(
        string? search,
        int? categoryId,
        TransactionType? type,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = userManager.GetUserId(User)!;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.FinancialTransactions.AsNoTracking()
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
        var items = await query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new TransactionResponse(
                transaction.Id,
                transaction.Type,
                transaction.Amount,
                transaction.TransactionDate,
                transaction.CategoryId,
                transaction.Category.Name,
                transaction.Category.ColorHex,
                transaction.Description,
                transaction.CreatedAtUtc,
                transaction.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<TransactionResponse>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var item = await FindResponseAsync(id, userId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(
        TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        if (!await CategoryIsAvailableAsync(request, userId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Choose an available category matching the transaction type.");
            return ValidationProblem(ModelState);
        }

        var now = DateTime.UtcNow;
        var transaction = new FinancialTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = request.CategoryId,
            Type = request.Type,
            Amount = request.Amount,
            TransactionDate = request.Date,
            Description = NormalizeOptional(request.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        dbContext.FinancialTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await FindResponseAsync(transaction.Id, userId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> Update(
        Guid id,
        TransactionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User)!;
        var transaction = await dbContext.FinancialTransactions
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (transaction is null)
        {
            return NotFound();
        }

        if (!await CategoryIsAvailableAsync(request, userId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Choose an available category matching the transaction type.");
            return ValidationProblem(ModelState);
        }

        transaction.CategoryId = request.CategoryId;
        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.TransactionDate = request.Date;
        transaction.Description = NormalizeOptional(request.Description);
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await FindResponseAsync(id, userId, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
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
        return NoContent();
    }

    private async Task<bool> CategoryIsAvailableAsync(
        TransactionRequest request,
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.Categories.AnyAsync(category =>
            category.Id == request.CategoryId
            && category.Type == request.Type
            && category.IsActive
            && (category.UserId == null || category.UserId == userId), cancellationToken);

    private async Task<TransactionResponse?> FindResponseAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken) =>
        await dbContext.FinancialTransactions.AsNoTracking()
            .Where(transaction => transaction.Id == id && transaction.UserId == userId)
            .Select(transaction => new TransactionResponse(
                transaction.Id,
                transaction.Type,
                transaction.Amount,
                transaction.TransactionDate,
                transaction.CategoryId,
                transaction.Category.Name,
                transaction.Category.ColorHex,
                transaction.Description,
                transaction.CreatedAtUtc,
                transaction.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
