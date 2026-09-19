using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PersonalExpenseTracker.Data.Entities;
using PersonalExpenseTracker.Models.Profile;

namespace PersonalExpenseTracker.Controllers;

[Authorize]
public sealed class ProfileController(UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(CreateModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!TimeZoneInfo.GetSystemTimeZones().Any(zone => zone.Id == model.TimeZoneId))
        {
            ModelState.AddModelError(nameof(model.TimeZoneId), "Choose a valid time zone.");
        }

        if (!SupportedCurrencies.Contains(model.CurrencyCode))
        {
            ModelState.AddModelError(nameof(model.CurrencyCode), "Choose a supported currency.");
        }

        if (!ModelState.IsValid)
        {
            model = AddOptions(model);
            return View(model);
        }

        user.DisplayName = model.DisplayName.Trim();
        user.CurrencyCode = model.CurrencyCode;
        user.TimeZoneId = model.TimeZoneId;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(AddOptions(model));
        }

        TempData["StatusMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private ProfileViewModel CreateModel(ApplicationUser user) => AddOptions(new ProfileViewModel
    {
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        CurrencyCode = user.CurrencyCode,
        TimeZoneId = user.TimeZoneId
    });

    private static ProfileViewModel AddOptions(ProfileViewModel model)
    {
        model.Currencies = SupportedCurrencies.Select(code => new SelectListItem(code, code)).ToList();
        model.TimeZones = TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => new SelectListItem(zone.DisplayName, zone.Id))
            .ToList();
        return model;
    }

    private static readonly string[] SupportedCurrencies = ["USD", "EUR", "VND", "GBP", "JPY", "AUD", "CAD"];
}
