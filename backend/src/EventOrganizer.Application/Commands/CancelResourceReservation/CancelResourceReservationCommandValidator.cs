using FluentValidation;

namespace EventOrganizer.Application.Commands.CancelResourceReservation
{
    public sealed class CancelResourceReservationCommandValidator
        : AbstractValidator<CancelResourceReservationCommand>
    {
        public CancelResourceReservationCommandValidator()
        {
            RuleFor(command => command.ReservationId)
                .NotEmpty();
        }
    }
}
