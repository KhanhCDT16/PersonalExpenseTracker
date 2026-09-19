using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Services;

public sealed class CurrencyResultFilter(UserManager<ApplicationUser> userManager) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var user = context.HttpContext.Items[typeof(ApplicationUser)] as ApplicationUser
                ?? await userManager.GetUserAsync(context.HttpContext.User);
            if (user is not null)
            {
                CurrencyCulture.ApplyCurrency(user.CurrencyCode);
            }
        }

        await next();
    }
}
