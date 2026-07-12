using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;

namespace EventOrganizer.Application.Commands.LoginUser
{
    public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly IClientContextService _clientContextService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ITokenService _tokenService;

        public LoginUserCommandHandler(
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

        public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.FindByEmailAsync(
                request.Email,
                cancellationToken);

            if (user is null)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            if (user.Status != UserStatus.Active)
            {
                throw new UnauthorizedException("User account is not active.");
            }

            var isPasswordValid = await _identityService.CheckPasswordAsync(
                user.UserId,
                request.Password,
                cancellationToken);

            if (!isPasswordValid)
            {
                throw new UnauthorizedException("Invalid email or password.");
            }

            var roles = await _identityService.GetRolesAsync(
                user.UserId,
                cancellationToken);

            var accessToken = _tokenService.CreateAccessToken(
                user.UserId,
                user.Email,
                user.FullName,
                roles);

            var refreshToken = _refreshTokenService.CreateRefreshToken();

            await _refreshTokenStore.StoreAsync(
                user.UserId,
                refreshToken.TokenHash,
                refreshToken.ExpiresAtUtc,
                _clientContextService.IpAddress,
                cancellationToken);

            return new AuthResponse(
                user.UserId,
                user.FullName,
                user.Email,
                roles,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                refreshToken.Token,
                refreshToken.ExpiresAtUtc);
        }
    }
}
