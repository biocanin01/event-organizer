using Microsoft.Extensions.DependencyInjection;

namespace EventOrganizer.Infrastructure.Persistance.Seeding
{
    public static class DemoDataSeederExtensions
    {
        public static async Task SeedDemoDataAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await seeder.SeedAsync();
        }
    }
}
