using EventOrganizer.Api.Authorization;
using EventOrganizer.Api.Middleware;
using EventOrganizer.Application;
using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Infrastructure;
using EventOrganizer.Infrastructure.Identity;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
