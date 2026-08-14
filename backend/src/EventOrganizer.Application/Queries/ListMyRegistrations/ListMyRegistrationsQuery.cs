using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListMyRegistrations
{
    public sealed record ListMyRegistrationsQuery : IRequest<IReadOnlyList<RegistrationResponse>>;
}
