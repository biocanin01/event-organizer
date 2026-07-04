using FluentValidation;

namespace EventOrganizer.Application.Commands.CreateEvent
{
    public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
    {
        public CreateEventCommandValidator() 
        {
            RuleFor(command => command.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(command => command.Description)
                .MaximumLength(2000);

            RuleFor(command => command.EndsAtUtc)
                .GreaterThan(command => command.StartsAtUtc);

            RuleFor(command => command.Capacity)
                .GreaterThan(0);

            RuleFor(command => command.OrganizerUserId)
                .NotEmpty();
        }
    }
}
