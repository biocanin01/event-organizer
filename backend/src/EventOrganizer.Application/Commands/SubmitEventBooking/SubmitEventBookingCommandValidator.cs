using FluentValidation;

namespace EventOrganizer.Application.Commands.SubmitEventBooking
{
    public sealed class SubmitEventBookingCommandValidator
        : AbstractValidator<SubmitEventBookingCommand>
    {
        public SubmitEventBookingCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
