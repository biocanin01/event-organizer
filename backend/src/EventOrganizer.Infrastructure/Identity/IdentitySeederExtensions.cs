using Microsoft.Extensions.DependencyInjection;

namespace EventOrganizer.Infrastructure.Identity
{
    public static class IdentitySeederExtensions
    {
        public static async Task SeedIdentityAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();

            await seeder.SeedRolesAsync();
            await seeder.SeedInitialAdminAsync();
        }
    }
}