using FluentValidation;

namespace EventOrganizer.Application.Commands.UpdateEvent
{
    public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
    {
        public UpdateEventCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.Description)
                .MaximumLength(2000);

            RuleFor(command => command.EndsAtUtc)
                .GreaterThan(command => command.StartsAtUtc);

            RuleFor(command => command.Capacity)
                .GreaterThan(0);

            RuleFor(command => command.Budget)
                .GreaterThan(0);

            RuleFor(command => command.Area)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(command => command.RequiredSpeakerCount)
                .GreaterThan(0);
        }
    }
}
