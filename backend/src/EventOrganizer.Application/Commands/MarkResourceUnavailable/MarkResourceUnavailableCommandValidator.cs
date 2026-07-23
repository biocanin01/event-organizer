using FluentValidation;

namespace EventOrganizer.Application.Commands.MarkResourceUnavailable
{
    public sealed class MarkResourceUnavailableCommandValidator
        : AbstractValidator<MarkResourceUnavailableCommand>
    {
        public MarkResourceUnavailableCommandValidator()
        {
            RuleFor(command => command.ResourceId)
                .NotEmpty();
        }
    }
}
