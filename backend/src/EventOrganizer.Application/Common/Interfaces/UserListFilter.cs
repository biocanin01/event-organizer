using EventOrganizer.Domain.Users;

namespace EventOrganizer.Application.Common.Interfaces
{
    public sealed record UserListFilter(
        string? Search,
        UserStatus? Status,
        string? Role);
}
