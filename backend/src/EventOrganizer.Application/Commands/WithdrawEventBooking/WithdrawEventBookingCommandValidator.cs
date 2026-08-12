using FluentValidation;

namespace EventOrganizer.Application.Commands.WithdrawEventBooking
{
    public sealed class WithdrawEventBookingCommandValidator
        : AbstractValidator<WithdrawEventBookingCommand>
    {
        public WithdrawEventBookingCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);
        }
    }
}
