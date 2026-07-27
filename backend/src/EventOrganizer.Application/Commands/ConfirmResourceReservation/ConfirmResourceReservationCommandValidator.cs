using FluentValidation;

namespace EventOrganizer.Application.Commands.ConfirmResourceReservation
{
    public sealed class ConfirmResourceReservationCommandValidator
        : AbstractValidator<ConfirmResourceReservationCommand>
    {
        public ConfirmResourceReservationCommandValidator()
        {
            RuleFor(command => command.ReservationId)
                .NotEmpty();
        }
    }
}
