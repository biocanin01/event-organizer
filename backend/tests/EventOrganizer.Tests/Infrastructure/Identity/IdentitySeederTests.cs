using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventOrganizer.Tests.Infrastructure.Identity
{
    public sealed class IdentitySeederTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        private ServiceProvider _serviceProvider = default!;

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            services.AddDataProtection();

            services
                .AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IdentitySeeder>();

            _serviceProvider = services.BuildServiceProvider();

            using var scope = _serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task SeedRolesAsync_WhenRolesDoNotExist_CreatesRequiredRoles()
        {
            using var scope = _serviceProvider.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            await seeder.SeedRolesAsync();

            Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Admin));
            Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Organizer));
            Assert.True(await roleManager.RoleExistsAsync(ApplicationRoles.Participant));
        }

        [Fact]
        public async Task SeedRolesAsync_WhenCalledMultipleTimes_DoesNotCreateDuplicateRoles()
        {
            using var scope = _serviceProvider.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await seeder.SeedRolesAsync();
            await seeder.SeedRolesAsync();

            var rolesCount = await dbContext.Roles.CountAsync();

            Assert.Equal(3, rolesCount);
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}