using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Users;
using MediatR;

namespace EventOrganizer.Application.Queries.ListUsers
{
    public sealed record ListUsersQuery(
        string? Search,
        UserStatus? Status,
        string? Role)
        : IRequest<IReadOnlyList<UserResponse>>;
}
