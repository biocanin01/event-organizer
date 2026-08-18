using EventOrganizer.Domain.Bookings;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Notifications;
using EventOrganizer.Domain.Registrations;
using EventOrganizer.Domain.Resources;
using EventOrganizer.Domain.Users;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Application.Recommendations.Optimization;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using EventOrganizer.Infrastructure.Persistance.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventOrganizer.Tests.Infrastructure.Persistance
{
    public sealed class DemoDataSeederTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        private ServiceProvider _serviceProvider = default!;

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());
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
            services.AddSingleton<IOptions<DemoDataSettings>>(
                Options.Create(new DemoDataSettings
                {
                    Enabled = true,
                    Password = "Demo12345",
                }));
            services.AddScoped<IdentitySeeder>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<DemoDataSeeder>();

            _serviceProvider = services.BuildServiceProvider();

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        [Fact]
        public async Task SeedAsync_WhenEnabled_CreatesCompleteIdempotentDemoScenario()
        {
            using var scope = _serviceProvider.CreateScope();
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            var demoDataSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await identitySeeder.SeedRolesAsync();
            await demoDataSeeder.SeedAsync();
            await demoDataSeeder.SeedAsync();

            Assert.Equal(6, await dbContext.Users.CountAsync());
            Assert.Equal(10, await dbContext.Resources.CountAsync());
            Assert.Equal(5, await dbContext.Events.CountAsync());
            Assert.Equal(5, await dbContext.EventResourceBookings.CountAsync());
            Assert.Equal(9, await dbContext.Registrations.CountAsync());
            Assert.Equal(2, await dbContext.Reviews.CountAsync());
            Assert.Equal(2, await dbContext.OrganizerRoleRequests.CountAsync());
            Assert.Equal(10, await dbContext.Notifications.CountAsync());

            var admin = await userManager.FindByEmailAsync(DemoDataSeeder.AdminEmail);
            var organizer = await userManager.FindByEmailAsync(DemoDataSeeder.OrganizerEmail);
            Assert.NotNull(admin);
            Assert.NotNull(organizer);
            Assert.True(await userManager.IsInRoleAsync(admin, "Admin"));
            Assert.True(await userManager.IsInRoleAsync(organizer, "Organizer"));
            Assert.True(await userManager.IsInRoleAsync(organizer, "Participant"));

            Assert.Equal(1, await dbContext.Events.CountAsync(
                eventItem => eventItem.Status == EventStatus.Published));
            Assert.Equal(1, await dbContext.Events.CountAsync(
                eventItem => eventItem.Status == EventStatus.Completed));
            Assert.Equal(1, await dbContext.Events.CountAsync(
                eventItem => eventItem.Status == EventStatus.Cancelled));
            Assert.Equal(1, await dbContext.EventResourceBookings.CountAsync(
                booking => booking.Status == EventResourceBookingStatus.Submitted));
            Assert.Equal(2, await dbContext.EventResourceBookings.CountAsync(
                booking => booking.Status == EventResourceBookingStatus.Approved));

            Assert.Contains(
                RegistrationStatus.Pending,
                await dbContext.Registrations.Select(item => item.Status).ToListAsync());
            Assert.Contains(
                RegistrationStatus.Confirmed,
                await dbContext.Registrations.Select(item => item.Status).ToListAsync());
            Assert.Contains(
                RegistrationStatus.Rejected,
                await dbContext.Registrations.Select(item => item.Status).ToListAsync());
            Assert.Contains(
                RegistrationStatus.Cancelled,
                await dbContext.Registrations.Select(item => item.Status).ToListAsync());

            var notificationTypes = await dbContext.Notifications
                .Select(notification => notification.Type)
                .Distinct()
                .ToListAsync();
            Assert.Contains(NotificationType.OrganizerRoleRequestApproved, notificationTypes);
            Assert.Contains(NotificationType.OrganizerRoleRequestRejected, notificationTypes);
            Assert.Contains(NotificationType.BookingApproved, notificationTypes);
            Assert.Contains(NotificationType.RegistrationConfirmed, notificationTypes);
            Assert.Contains(NotificationType.RegistrationRejected, notificationTypes);
            Assert.Contains(NotificationType.RegistrationCancelled, notificationTypes);
            Assert.Contains(NotificationType.EventCancelled, notificationTypes);
            Assert.Contains(NotificationType.ReviewAvailable, notificationTypes);
            Assert.Equal(5, await dbContext.Notifications.CountAsync(item => item.ReadAtUtc == null));
            Assert.Equal(5, await dbContext.Notifications.CountAsync(item => item.ReadAtUtc != null));
            Assert.All(
                await dbContext.Notifications.ToListAsync(),
                notification =>
                {
                    Assert.NotNull(notification.RelatedEntityType);
                    Assert.NotNull(notification.RelatedEntityId);
                });

            var planningEvent = await dbContext.Events.SingleAsync(
                eventItem => eventItem.Title == "AI i razvoj modernih aplikacija");
            Assert.True(planningEvent.RequiresEquipment);
            Assert.Equal(2, planningEvent.RequiredSpeakerCount);
            Assert.Equal(260000m, planningEvent.Budget);
            Assert.True(await dbContext.Resources.OfType<Venue>().AnyAsync(
                venue => venue.Status == ResourceStatus.Available
                    && venue.Capacity >= planningEvent.Capacity));
            Assert.True(await dbContext.Resources.OfType<EquipmentPackage>().AnyAsync(
                package => package.Status == ResourceStatus.Available
                    && package.ServiceArea == planningEvent.Area
                    && package.SupportedCapacity >= planningEvent.Capacity));
            Assert.True(await dbContext.Resources.OfType<Speaker>().CountAsync(
                speaker => speaker.Status == ResourceStatus.Available
                    && speaker.ExpertiseArea == planningEvent.Area) >= 2);

            var candidateProvider = new ResourceCandidateProvider(dbContext);
            var candidates = await candidateProvider.GetCandidatesAsync(
                planningEvent,
                CancellationToken.None);
            var recommendation = new ConstraintRecommendationOptimizer().Optimize(
                planningEvent,
                candidates);

            Assert.True(recommendation.IsSuccessful);
            Assert.NotNull(recommendation.Venue);
            Assert.Equal(2, recommendation.Speakers.Count);
            Assert.NotNull(recommendation.EquipmentPackage);
            Assert.Equal(238000m, recommendation.TotalCost);
            Assert.True(recommendation.TotalCost <= planningEvent.Budget);
        }

        public async Task DisposeAsync()
        {
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
