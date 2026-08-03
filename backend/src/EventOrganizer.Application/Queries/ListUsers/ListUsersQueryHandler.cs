using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Common.Mapping;
using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListUsers
{
    public sealed class ListUsersQueryHandler
        : IRequestHandler<ListUsersQuery, IReadOnlyList<UserResponse>>
    {
        private readonly IUserManagementService _userManagementService;

        public ListUsersQueryHandler(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        public async Task<IReadOnlyList<UserResponse>> Handle(
            ListUsersQuery request,
            CancellationToken cancellationToken)
        {
            var users = await _userManagementService.ListUsersAsync(
                new UserListFilter(
                    request.Search,
                    request.Status,
                    request.Role),
                cancellationToken);

            return users
                .Select(UserResponseMapper.ToResponse)
                .ToArray();
        }
    }
}
