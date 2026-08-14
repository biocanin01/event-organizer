using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Registrations;
using MediatR;

namespace EventOrganizer.Application.Queries.ListEventRegistrations
{
    public sealed record ListEventRegistrationsQuery(Guid EventId, RegistrationStatus? Status)
        : IRequest<IReadOnlyList<RegistrationResponse>>;
}
