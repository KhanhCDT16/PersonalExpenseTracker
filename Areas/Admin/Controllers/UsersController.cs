using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Areas.Admin.Models;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);
        var query = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                (user.Email != null && user.Email.Contains(term)) || user.DisplayName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);
        var users = await query.OrderBy(user => user.Email).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new AdminUserListItem
            {
                Id = user.Id, Email = user.Email ?? string.Empty, DisplayName = user.DisplayName,
                IsActive = user.IsActive, CreatedAtUtc = user.CreatedAtUtc,
                IsAdmin = dbContext.UserRoles.Any(role =>
                    role.UserId == user.Id
                    && role.RoleId == "10000000-0000-0000-0000-000000000002")
            }).ToListAsync(cancellationToken);
        return View(new AdminUsersViewModel
        {
            Users = users, Search = search, Page = page, TotalPages = totalPages, TotalCount = total
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var currentUserId = userManager.GetUserId(User)!;
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.IsActive = !user.IsActive;
        var result = await userManager.UpdateAsync(user);
        TempData[result.Succeeded ? "StatusMessage" : "ErrorMessage"] = result.Succeeded
            ? $"Account {(user.IsActive ? "activated" : "deactivated")}."
            : string.Join("; ", result.Errors.Select(error => error.Description));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var currentUserId = userManager.GetUserId(User)!;
        if (id == currentUserId)
        {
            TempData["ErrorMessage"] = "You cannot change your own administrator role.";
            return RedirectToAction(nameof(Index));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
        var result = isAdmin
            ? await userManager.RemoveFromRoleAsync(user, "Admin")
            : await userManager.AddToRoleAsync(user, "Admin");
        TempData[result.Succeeded ? "StatusMessage" : "ErrorMessage"] = result.Succeeded
            ? $"Administrator role {(isAdmin ? "removed" : "granted")}."
            : string.Join("; ", result.Errors.Select(error => error.Description));
        return RedirectToAction(nameof(Index));
    }
}
