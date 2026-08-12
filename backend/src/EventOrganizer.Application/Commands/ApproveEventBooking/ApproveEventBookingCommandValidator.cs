using FluentValidation;

namespace EventOrganizer.Application.Commands.ApproveEventBooking
{
    public sealed class ApproveEventBookingCommandValidator
        : AbstractValidator<ApproveEventBookingCommand>
    {
        public ApproveEventBookingCommandValidator()
        {
            RuleFor(command => command.BookingId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
