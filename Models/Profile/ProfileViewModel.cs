using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PersonalExpenseTracker.Models.Profile;

public sealed class ProfileViewModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, StringLength(3, MinimumLength = 3)]
    [RegularExpression("^[A-Z]{3}$")]
    [Display(Name = "Currency")]
    public string CurrencyCode { get; set; } = "USD";

    [Required, StringLength(100)]
    [Display(Name = "Time zone")]
    public string TimeZoneId { get; set; } = "UTC";

    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<SelectListItem> TimeZones { get; set; } = [];
    public IReadOnlyList<SelectListItem> Currencies { get; set; } = [];
}
