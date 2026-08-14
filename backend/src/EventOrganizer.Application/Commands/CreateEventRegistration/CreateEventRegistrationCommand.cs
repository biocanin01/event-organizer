using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateEventRegistration
{
    public sealed record CreateEventRegistrationCommand(Guid EventId)
        : IRequest<RegistrationResponse>;
}
