using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken);

            if (user is null) 
            {
                throw new InvalidOperationException($"User with id '{userId}' was not found.");
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to assign role '{role}'. Errors: {errors}");
            }
        }

        public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken);

            if (user is null)
            {
                return false;
            }

            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<AuthUserResult> CreateUserAsync(string fullName, string email, string password, CancellationToken cancellationToken)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FullName = fullName,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                VerifiedAtUtc = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));

                throw new InvalidOperationException($"Failed to create user. Errors: {errors}");
            }

            return new AuthUserResult(
                user.Id,
                user.FullName,
                user.Email!,
                user.Status);
        }

        public async Task<AuthUserResult?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(currentUser => currentUser.Email == email, cancellationToken);

            return user is null ? null : new AuthUserResult(user.Id, user.FullName, user.Email!, user.Status);
        }

        public async Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(currentUser => currentUser.Id == userId, cancellationToken);

            if (user is null)
            {
                return Array.Empty<string>();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToArray();
        }
    }
}
