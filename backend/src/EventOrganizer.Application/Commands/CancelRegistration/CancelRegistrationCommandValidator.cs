using FluentValidation;

namespace EventOrganizer.Application.Commands.CancelRegistration
{
    public sealed class CancelRegistrationCommandValidator : AbstractValidator<CancelRegistrationCommand>
    {
        public CancelRegistrationCommandValidator()
        {
            RuleFor(command => command.RegistrationId).NotEmpty();
            RuleFor(command => command.Version).GreaterThan(0);
        }
    }
}
