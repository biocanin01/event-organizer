using FluentValidation;

namespace EventOrganizer.Application.Commands.RejectRegistration
{
    public sealed class RejectRegistrationCommandValidator : AbstractValidator<RejectRegistrationCommand>
    {
        public RejectRegistrationCommandValidator()
        {
            RuleFor(command => command.RegistrationId).NotEmpty();
            RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
            RuleFor(command => command.Version).GreaterThan(0);
        }
    }
}
