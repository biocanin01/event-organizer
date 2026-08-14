using FluentValidation;

namespace EventOrganizer.Application.Commands.ConfirmRegistration
{
    public sealed class ConfirmRegistrationCommandValidator : AbstractValidator<ConfirmRegistrationCommand>
    {
        public ConfirmRegistrationCommandValidator()
        {
            RuleFor(command => command.RegistrationId).NotEmpty();
            RuleFor(command => command.Version).GreaterThan(0);
        }
    }
}
