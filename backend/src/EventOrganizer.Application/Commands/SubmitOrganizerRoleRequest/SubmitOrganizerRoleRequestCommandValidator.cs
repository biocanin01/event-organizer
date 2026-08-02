using EventOrganizer.Domain.Users;
using FluentValidation;

namespace EventOrganizer.Application.Commands.SubmitOrganizerRoleRequest
{
    public sealed class SubmitOrganizerRoleRequestCommandValidator
        : AbstractValidator<SubmitOrganizerRoleRequestCommand>
    {
        public SubmitOrganizerRoleRequestCommandValidator()
        {
            RuleFor(command => command.Motivation)
                .NotEmpty()
                .MaximumLength(OrganizerRoleRequest.MaxMotivationLength);
        }
    }
}
