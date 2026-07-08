using Microsoft.AspNetCore.Identity;

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

        public IdentitySeeder(RoleManager<IdentityRole<Guid>> roleManager)
        {
            _roleManager = roleManager;
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
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(error => error.Description));

                    throw new InvalidOperationException(
                        $"Failed to seed role '{role}'. Errors: {errors}");
                }
            }
        }
    }
}