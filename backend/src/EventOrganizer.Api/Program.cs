using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Middleware;
using EventOrganizer.Application;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Options;
using EventOrganizer.Infrastructure;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
const string frontendCorsPolicy = "Frontend";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? [];

    options.AddPolicy(
        frontendCorsPolicy,
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT access token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = [],
    });
});

builder.Services.AddApplication();
builder.Services.Configure<BookingOptions>(
    builder.Configuration.GetSection(BookingOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.CanCreateEvents,
        policy => policy.RequireRole(
            ApplicationRoles.Organizer,
            ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanManageEvents,
        policy => policy.RequireRole(
            ApplicationRoles.Organizer,
            ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanManageResources,
        policy => policy.RequireRole(ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanBrowseResources,
        policy => policy.RequireRole(
            ApplicationRoles.Organizer,
            ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanRequestOrganizerRole,
        policy => policy.RequireRole(
            ApplicationRoles.Participant,
            ApplicationRoles.Organizer,
            ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanManageOrganizerRoleRequests,
        policy => policy.RequireRole(ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanManageBookings,
        policy => policy.RequireRole(ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanViewInsights,
        policy => policy.RequireRole(
            ApplicationRoles.Organizer,
            ApplicationRoles.Admin));

    options.AddPolicy(
        AuthorizationPolicies.CanManageUsers,
        policy => policy.RequireRole(ApplicationRoles.Admin));
});

var app = builder.Build();

await app.Services.SeedIdentityAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(frontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
