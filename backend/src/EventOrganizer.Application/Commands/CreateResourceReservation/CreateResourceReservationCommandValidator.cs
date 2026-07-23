using FluentValidation;

namespace EventOrganizer.Application.Commands.CreateResourceReservation
{
    public sealed class CreateResourceReservationCommandValidator
        : AbstractValidator<CreateResourceReservationCommand>
    {
        public CreateResourceReservationCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.ResourceId)
                .NotEmpty();

            RuleFor(command => command.StartsAtUtc)
                .NotEmpty();

            RuleFor(command => command.EndsAtUtc)
                .NotEmpty()
                .GreaterThan(command => command.StartsAtUtc);
        }
    }
}