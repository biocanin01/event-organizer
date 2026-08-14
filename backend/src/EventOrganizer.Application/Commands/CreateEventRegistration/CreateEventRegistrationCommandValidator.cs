using FluentValidation;

namespace EventOrganizer.Application.Commands.CreateEventRegistration
{
    public sealed class CreateEventRegistrationCommandValidator
        : AbstractValidator<CreateEventRegistrationCommand>
    {
        public CreateEventRegistrationCommandValidator()
        {
            RuleFor(command => command.EventId).NotEmpty();
        }
    }
}
