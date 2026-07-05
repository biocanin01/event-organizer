using FluentValidation;

namespace EventOrganizer.Application.Commands.CancelEvent
{
    public sealed class CancelEventCommandValidator : AbstractValidator<CancelEventCommand>
    {
        public CancelEventCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();
        }
    }
}
