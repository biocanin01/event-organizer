using EventOrganizer.Application.Common.Constants;
using EventOrganizer.Domain.Users;
using FluentValidation;

namespace EventOrganizer.Application.Queries.ListUsers
{
    public sealed class ListUsersQueryValidator
        : AbstractValidator<ListUsersQuery>
    {
        private static readonly string[] SupportedRoles =
        [
            ApplicationRoles.Admin,
            ApplicationRoles.Organizer,
            ApplicationRoles.Participant,
        ];

        public ListUsersQueryValidator()
        {
            RuleFor(query => query.Search)
                .MaximumLength(100);

            RuleFor(query => query.Status)
                .IsInEnum()
                .When(query => query.Status.HasValue);

            RuleFor(query => query.Role)
                .Must(role => SupportedRoles.Contains(role))
                .When(query => !string.IsNullOrWhiteSpace(query.Role))
                .WithMessage("Unsupported role filter.");
        }
    }
}
