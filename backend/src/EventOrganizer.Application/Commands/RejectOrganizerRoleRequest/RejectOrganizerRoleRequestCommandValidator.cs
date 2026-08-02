using EventOrganizer.Domain.Users;
using FluentValidation;

namespace EventOrganizer.Application.Commands.RejectOrganizerRoleRequest
{
    public sealed class RejectOrganizerRoleRequestCommandValidator
        : AbstractValidator<RejectOrganizerRoleRequestCommand>
    {
        public RejectOrganizerRoleRequestCommandValidator()
        {
            RuleFor(command => command.RequestId)
                .NotEmpty();

            RuleFor(command => command.DecisionReason)
                .NotEmpty()
                .MaximumLength(OrganizerRoleRequest.MaxDecisionReasonLength);

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
