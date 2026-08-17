using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Behaviors;
using EventOrganizer.Application.Common.Options;
using EventOrganizer.Application.Notifications;
using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Application.Recommendations.Optimization;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EventOrganizer.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddOptions<BookingOptions>()
                .Validate(options => options.HoldDurationHours > 0);

            services.AddMediatR(configuration =>
                configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddScoped<EventAuthorizationService>();

            services.AddScoped<INotificationService, NotificationService>();

            services.AddScoped<IResourceCandidateProvider, ResourceCandidateProvider>();

            services.AddScoped<IRecommendationOptimizer, ConstraintRecommendationOptimizer>();

            return services;
        }
    }
}
