using FluentValidation;

namespace EventOrganizer.Application.Commands.WithdrawOrganizerRoleRequest
{
    public sealed class WithdrawOrganizerRoleRequestCommandValidator
        : AbstractValidator<WithdrawOrganizerRoleRequestCommand>
    {
        public WithdrawOrganizerRoleRequestCommandValidator()
        {
            RuleFor(command => command.RequestId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
