using FluentValidation;

namespace EventOrganizer.Application.Commands.RejectResourceReservation
{
    public sealed class RejectResourceReservationCommandValidator
        : AbstractValidator<RejectResourceReservationCommand>
    {
        public RejectResourceReservationCommandValidator()
        {
            RuleFor(command => command.ReservationId)
                .NotEmpty();
        }
    }
}
