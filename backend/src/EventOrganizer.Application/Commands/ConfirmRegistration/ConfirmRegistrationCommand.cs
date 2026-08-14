using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.ConfirmRegistration
{
    public sealed record ConfirmRegistrationCommand(Guid RegistrationId, int Version)
        : IRequest<RegistrationResponse>;
}
