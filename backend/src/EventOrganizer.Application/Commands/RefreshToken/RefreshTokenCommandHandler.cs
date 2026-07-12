using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;

namespace EventOrganizer.Application.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandHandler
        : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly IClientContextService _clientContextService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ITokenService _tokenService;

        public RefreshTokenCommandHandler(
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

        public async Task<AuthResponse> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

            var storedRefreshToken = await _refreshTokenStore.FindByTokenHashAsync(
                tokenHash,
                cancellationToken);

            if (storedRefreshToken is null || !storedRefreshToken.IsActive)
            {
                throw new UnauthorizedException("Invalid refresh token.");
            }

            var user = await _identityService.FindByIdAsync(
                storedRefreshToken.UserId,
                cancellationToken);

            if (user is null || user.Status != UserStatus.Active)
            {
                throw new UnauthorizedException("Invalid refresh token.");
            }

            var roles = await _identityService.GetRolesAsync(
                user.UserId,
                cancellationToken);

            var accessToken = _tokenService.CreateAccessToken(
                user.UserId,
                user.Email,
                user.FullName,
                roles);

            var newRefreshToken = _refreshTokenService.CreateRefreshToken();

            await _refreshTokenStore.RotateAsync(
                storedRefreshToken.Id,
                user.UserId,
                newRefreshToken.TokenHash,
                newRefreshToken.ExpiresAtUtc,
                _clientContextService.IpAddress,
                cancellationToken);

            return new AuthResponse(
                user.UserId,
                user.FullName,
                user.Email,
                roles,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                newRefreshToken.Token,
                newRefreshToken.ExpiresAtUtc);
        }
    }
}