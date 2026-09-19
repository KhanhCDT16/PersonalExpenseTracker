using Microsoft.AspNetCore.Identity;
using PersonalExpenseTracker.Data.Entities;

namespace PersonalExpenseTracker.Data;

public static class AdminBootstrapper
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email.Trim(), Email = email.Trim(), DisplayName = "Administrator",
                EmailConfirmed = true, IsActive = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the bootstrap administrator: " +
                    string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not grant the Admin role: " +
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }
}
