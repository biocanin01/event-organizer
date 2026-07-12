using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.RegisterUser
{
    public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly IClientContextService _clientContextService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ITokenService _tokenService;

        public RegisterUserCommandHandler(
            IIdentityService identityService,
            IClientContextService clientContextService,
            IRefreshTokenService refreshTokenService,
            IRefreshTokenStore refreshTokenStore,
            ITokenService tokenService)
        {
            _identityService = identityService;
            _clientContextService = clientContextService;
            _refreshTokenService = refreshTokenService;
            _refreshTokenStore = refreshTokenStore;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _identityService.FindByEmailAsync(
                request.Email,
                cancellationToken);

            if (existingUser is not null)
            {
                throw new ConflictException(
                    $"A user with email '{request.Email}' already exists.");
            }

            var createdUser = await _identityService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.Password,
                cancellationToken);

            await _identityService.AddToRoleAsync(
                createdUser.UserId,
                ApplicationRoles.Participant,
                cancellationToken);

            var roles = await _identityService.GetRolesAsync(
                createdUser.UserId,
                cancellationToken);

            var accessToken = _tokenService.CreateAccessToken(
                createdUser.UserId,
                createdUser.Email,
                createdUser.FullName,
                roles);

            var refreshToken = _refreshTokenService.CreateRefreshToken();

            await _refreshTokenStore.StoreAsync(
                createdUser.UserId,
                refreshToken.TokenHash,
                refreshToken.ExpiresAtUtc,
                _clientContextService.IpAddress,
                cancellationToken);

            return new AuthResponse(
                createdUser.UserId,
                createdUser.FullName,
                createdUser.Email,
                roles,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                refreshToken.Token,
                refreshToken.ExpiresAtUtc);
        }
    }
}
