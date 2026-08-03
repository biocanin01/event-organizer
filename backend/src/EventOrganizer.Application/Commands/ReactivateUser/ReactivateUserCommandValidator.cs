using FluentValidation;

namespace EventOrganizer.Application.Commands.ReactivateUser
{
    public sealed class ReactivateUserCommandValidator
        : AbstractValidator<ReactivateUserCommand>
    {
        public ReactivateUserCommandValidator()
        {
            RuleFor(command => command.UserId)
                .NotEmpty();
        }
    }
}
