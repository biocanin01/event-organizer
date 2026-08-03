using FluentValidation;

namespace EventOrganizer.Application.Commands.LogoutUser
{
    public sealed class LogoutUserCommandValidator
        : AbstractValidator<LogoutUserCommand>
    {
        public LogoutUserCommandValidator()
        {
            RuleFor(command => command.RefreshToken)
                .NotEmpty();
        }
    }
}
