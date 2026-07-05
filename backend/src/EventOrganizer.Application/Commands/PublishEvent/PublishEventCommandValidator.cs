using FluentValidation;

namespace EventOrganizer.Application.Commands.PublishEvent
{
    public sealed class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
    {
        public PublishEventCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();
        }
    }
}