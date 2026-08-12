using FluentValidation;

namespace EventOrganizer.Application.Commands.RejectEventBooking
{
    public sealed class RejectEventBookingCommandValidator
        : AbstractValidator<RejectEventBookingCommand>
    {
        public RejectEventBookingCommandValidator()
        {
            RuleFor(command => command.BookingId)
                .NotEmpty();

            RuleFor(command => command.DecisionReason)
                .MaximumLength(500);

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
