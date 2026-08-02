using EventOrganizer.Domain.Users;
using FluentValidation;

namespace EventOrganizer.Application.Queries.ListOrganizerRoleRequests
{
    public sealed class ListOrganizerRoleRequestsQueryValidator
        : AbstractValidator<ListOrganizerRoleRequestsQuery>
    {
        public ListOrganizerRoleRequestsQueryValidator()
        {
            RuleFor(query => query.Status)
                .IsInEnum()
                .When(query => query.Status.HasValue);
        }
    }
}
