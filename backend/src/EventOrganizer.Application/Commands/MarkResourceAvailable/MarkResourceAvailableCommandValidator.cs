using FluentValidation;

namespace EventOrganizer.Application.Commands.MarkResourceAvailable
{
    public sealed class MarkResourceAvailableCommandValidator
        : AbstractValidator<MarkResourceAvailableCommand>
    {
        public MarkResourceAvailableCommandValidator()
        {
            RuleFor(command => command.ResourceId)
                .NotEmpty();
        }
    }
}
