using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Users;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.AddSingleton<IOptions<InitialAdminSettings>>(
                Options.Create(new InitialAdminSettings()));

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

        [Fact]
        public async Task SeedInitialAdminAsync_WhenSettingsAreNotConfigured_DoesNotCreateAdminUser()
        {
            using var scope = _serviceProvider.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await seeder.SeedRolesAsync();
            await seeder.SeedInitialAdminAsync();

            var usersCount = await dbContext.Users.CountAsync();

            Assert.Equal(0, usersCount);
        }

        [Fact]
        public async Task SeedInitialAdminAsync_WhenSettingsAreConfigured_CreatesActiveAdminUser()
        {
            using var scope = _serviceProvider.CreateScope();

            var seeder = CreateSeederWithInitialAdminSettings(scope);
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await seeder.SeedRolesAsync();
            await seeder.SeedInitialAdminAsync();

            var adminUser = await userManager.FindByEmailAsync("admin@eventorganizer.local");

            Assert.NotNull(adminUser);
            Assert.Equal("System Administrator", adminUser.FullName);
            Assert.Equal(UserStatus.Active, adminUser.Status);
            Assert.NotNull(adminUser.VerifiedAtUtc);
            Assert.True(await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin));
        }

        [Fact]
        public async Task SeedInitialAdminAsync_WhenCalledMultipleTimes_DoesNotCreateDuplicateAdminUsers()
        {
            using var scope = _serviceProvider.CreateScope();

            var seeder = CreateSeederWithInitialAdminSettings(scope);
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await seeder.SeedRolesAsync();
            await seeder.SeedInitialAdminAsync();
            await seeder.SeedInitialAdminAsync();

            var usersCount = await dbContext.Users.CountAsync();

            Assert.Equal(1, usersCount);
        }

        [Fact]
        public async Task SeedInitialAdminAsync_WhenUserAlreadyExists_AssignsAdminRole()
        {
            using var scope = _serviceProvider.CreateScope();

            var seeder = CreateSeederWithInitialAdminSettings(scope);
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var existingUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin@eventorganizer.local",
                Email = "admin@eventorganizer.local",
                FullName = "Existing User",
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await seeder.SeedRolesAsync();
            await userManager.CreateAsync(existingUser, "Admin12345");

            await seeder.SeedInitialAdminAsync();

            Assert.True(await userManager.IsInRoleAsync(existingUser, ApplicationRoles.Admin));
        }

        private static IdentitySeeder CreateSeederWithInitialAdminSettings(IServiceScope scope)
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var settings = Options.Create(new InitialAdminSettings
            {
                Email = "admin@eventorganizer.local",
                Password = "Admin12345",
                FullName = "System Administrator",
            });

            return new IdentitySeeder(roleManager, userManager, settings);
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
