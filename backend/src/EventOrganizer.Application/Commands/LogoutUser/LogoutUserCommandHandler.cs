using EventOrganizer.Application.Common.Interfaces;
using MediatR;

namespace EventOrganizer.Application.Commands.LogoutUser
{
    public sealed class LogoutUserCommandHandler
        : IRequestHandler<LogoutUserCommand>
    {
        private readonly IClientContextService _clientContextService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IRefreshTokenRevocationService _refreshTokenRevocationService;

        public LogoutUserCommandHandler(
            IClientContextService clientContextService,
            IRefreshTokenService refreshTokenService,
            IRefreshTokenRevocationService refreshTokenRevocationService)
        {
            _clientContextService = clientContextService;
            _refreshTokenService = refreshTokenService;
            _refreshTokenRevocationService = refreshTokenRevocationService;
        }

        public async Task Handle(
            LogoutUserCommand request,
            CancellationToken cancellationToken)
        {
            var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);

            await _refreshTokenRevocationService.RevokeAsync(
                tokenHash,
                _clientContextService.IpAddress,
                cancellationToken);
        }
    }
}
