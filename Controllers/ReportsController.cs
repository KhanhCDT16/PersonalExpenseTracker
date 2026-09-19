using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Services;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class ReportsController(
    IReportService reportService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(int months = 6, CancellationToken cancellationToken = default) =>
        View(await reportService.CreateAsync(
            userManager.GetUserId(User)!,
            months,
            cancellationToken));
}
