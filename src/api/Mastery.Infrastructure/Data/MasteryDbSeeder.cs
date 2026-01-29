using Mastery.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Data;

public class MasteryDbSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MasteryDbSeeder> _logger;

    public MasteryDbSeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<MasteryDbSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedSuperUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {Role}", role);
                }
                else
                {
                    _logger.LogError("Failed to create role {Role}: {Errors}",
                        role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedSuperUserAsync()
    {
        const string superEmail = "hunter@hunterwebapps.com";
        var superPassword = _configuration["SuperUserPassword"]
            ?? throw new InvalidOperationException("SuperUserPassword not configured");

        var existingUser = await _userManager.FindByEmailAsync(superEmail);
        if (existingUser != null)
        {
            // Ensure super user has Super role
            if (!await _userManager.IsInRoleAsync(existingUser, AppRoles.Super))
            {
                await _userManager.AddToRoleAsync(existingUser, AppRoles.Super);
                _logger.LogInformation("Added Super role to existing user: {Email}", superEmail);
            }
            return;
        }

        var superUser = new ApplicationUser
        {
            UserName = superEmail,
            Email = superEmail,
            EmailConfirmed = true,
            DisplayName = "Hunter",
            AuthProvider = "Email",
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(superUser, superPassword);
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(superUser, AppRoles.Super);
            _logger.LogInformation("Created super user: {Email}", superEmail);
        }
        else
        {
            _logger.LogError("Failed to create super user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }
}
