using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.CancelRegistration
{
    public sealed record CancelRegistrationCommand(Guid RegistrationId, int Version)
        : IRequest<RegistrationResponse>;
}
