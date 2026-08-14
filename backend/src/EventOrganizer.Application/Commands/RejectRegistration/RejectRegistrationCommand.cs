using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.RejectRegistration
{
    public sealed record RejectRegistrationCommand(
        Guid RegistrationId,
        string Reason,
        int Version) : IRequest<RegistrationResponse>;
}
