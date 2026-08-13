using FluentValidation;

namespace EventOrganizer.Application.Commands.CompleteEvent
{
    public sealed class CompleteEventCommandValidator
        : AbstractValidator<CompleteEventCommand>
    {
        public CompleteEventCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();
        }
    }
}
