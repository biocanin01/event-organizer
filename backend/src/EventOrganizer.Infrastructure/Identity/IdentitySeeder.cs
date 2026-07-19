using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EventOrganizer.Infrastructure.Identity
{
    public sealed class IdentitySeeder
    {
        private static readonly string[] Roles =
        [
            ApplicationRoles.Admin,
            ApplicationRoles.Organizer,
            ApplicationRoles.Participant,
        ];

        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InitialAdminSettings _initialAdminSettings;

        public IdentitySeeder(
            RoleManager<IdentityRole<Guid>> roleManager,
            UserManager<ApplicationUser> userManager,
            IOptions<InitialAdminSettings> initialAdminSettings)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _initialAdminSettings = initialAdminSettings.Value;
        }

        public async Task SeedRolesAsync()
        {
            foreach (var role in Roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed role '{role}'. Errors: {FormatErrors(result)}");
                }
            }
        }

        public async Task SeedInitialAdminAsync()
        {
            if (!IsInitialAdminConfigured())
            {
                return;
            }

            var existingUser = await _userManager.FindByEmailAsync(_initialAdminSettings.Email);

            if (existingUser is not null)
            {
                await EnsureAdminRoleAsync(existingUser);
                return;
            }

            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = _initialAdminSettings.Email,
                Email = _initialAdminSettings.Email,
                FullName = _initialAdminSettings.FullName,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                VerifiedAtUtc = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(
                adminUser,
                _initialAdminSettings.Password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed initial admin user. Errors: {FormatErrors(createResult)}");
            }

            await EnsureAdminRoleAsync(adminUser);
        }

        private bool IsInitialAdminConfigured()
        {
            return !string.IsNullOrWhiteSpace(_initialAdminSettings.Email) &&
                   !string.IsNullOrWhiteSpace(_initialAdminSettings.Password) &&
                   !string.IsNullOrWhiteSpace(_initialAdminSettings.FullName);
        }

        private async Task EnsureAdminRoleAsync(ApplicationUser user)
        {
            if (await _userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
            {
                return;
            }

            var result = await _userManager.AddToRoleAsync(user, ApplicationRoles.Admin);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign Admin role to initial admin user. Errors: {FormatErrors(result)}");
            }
        }

        private static string FormatErrors(IdentityResult result)
        {
            return string.Join("; ", result.Errors.Select(error => error.Description));
        }
    }
}