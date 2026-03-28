using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ThesisWebApp.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "User");

        await EnsureUserAsync(
            userManager,
            roleManager,
            email: "admin@test.com",
            password: "Admin123!",
            role: "Admin");

        await EnsureUserAsync(
            userManager,
            roleManager,
            email: "user@test.com",
            password: "User123!",
            role: "User");
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing == null)
        {
            existing = new IdentityUser
            {
                Email = email,
                UserName = email
            };

            var createResult = await userManager.CreateAsync(existing, password);
            if (!createResult.Succeeded)
            {
                // Ignore errors here to avoid breaking app startup if credentials already exist partially.
                // Console logging can be added later if needed.
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(existing, role))
        {
            // Ensure role exists just in case.
            await EnsureRoleAsync(roleManager, role);
            await userManager.AddToRoleAsync(existing, role);
        }
    }
}

