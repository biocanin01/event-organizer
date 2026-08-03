using FluentValidation;

namespace EventOrganizer.Application.Commands.SuspendUser
{
    public sealed class SuspendUserCommandValidator
        : AbstractValidator<SuspendUserCommand>
    {
        public SuspendUserCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty();
        }
    }
}
