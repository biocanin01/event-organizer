using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetUserById
{
    public sealed record GetUserByIdQuery(Guid UserId)
        : IRequest<UserDetailsResponse>;
}
