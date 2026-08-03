using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Infrastructure.Authentication;
using EventOrganizer.Infrastructure.Identity;
using EventOrganizer.Infrastructure.Persistance;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace EventOrganizer.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<AppDbContext>());

            services.AddHttpContextAccessor();

            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            var jwtSettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JWT settings are not configured.");

            services.Configure<InitialAdminSettings>(
                configuration.GetSection(InitialAdminSettings.SectionName));

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.EventsType = typeof(ActiveAccountJwtBearerEvents);

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role,
                    };
                });

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

            services.AddScoped<IdentitySeeder>();

            services.AddScoped<IClientContextService, ClientContextService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IdentityService>();
            services.AddScoped<IIdentityService>(provider =>
                provider.GetRequiredService<IdentityService>());
            services.AddScoped<IUserManagementService>(provider =>
                provider.GetRequiredService<IdentityService>());
            services.AddScoped<RefreshTokenStore>();
            services.AddScoped<IRefreshTokenStore>(provider =>
                provider.GetRequiredService<RefreshTokenStore>());
            services.AddScoped<IRefreshTokenRevocationService>(provider =>
                provider.GetRequiredService<RefreshTokenStore>());
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<ActiveAccountJwtBearerEvents>();

            return services;
        }
    }
}
