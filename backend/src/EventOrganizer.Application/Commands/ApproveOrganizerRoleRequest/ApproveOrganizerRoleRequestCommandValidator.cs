using FluentValidation;

namespace EventOrganizer.Application.Commands.ApproveOrganizerRoleRequest
{
    public sealed class ApproveOrganizerRoleRequestCommandValidator
        : AbstractValidator<ApproveOrganizerRoleRequestCommand>
    {
        public ApproveOrganizerRoleRequestCommandValidator()
        {
            RuleFor(command => command.RequestId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
