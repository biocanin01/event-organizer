using FluentValidation;

namespace EventOrganizer.Application.Commands.ReviseEventBooking
{
    public sealed class ReviseEventBookingCommandValidator
        : AbstractValidator<ReviseEventBookingCommand>
    {
        public ReviseEventBookingCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
