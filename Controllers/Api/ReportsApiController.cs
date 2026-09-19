using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models.Reports;
using PersonalExpenseTracker.Services;

namespace PersonalExpenseTracker.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ReportsApiController(
    IReportService reportService,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ReportViewModel>> Summary(
        int months = 6,
        CancellationToken cancellationToken = default) =>
        Ok(await reportService.CreateAsync(
            userManager.GetUserId(User)!,
            months,
            cancellationToken));
}
