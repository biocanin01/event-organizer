using FluentValidation;

namespace EventOrganizer.Application.Commands.UpdateEventBookingDraft
{
    public sealed class UpdateEventBookingDraftCommandValidator
        : AbstractValidator<UpdateEventBookingDraftCommand>
    {
        public UpdateEventBookingDraftCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);

            RuleForEach(command => command.SpeakerIds)
                .NotEmpty();

            RuleFor(command => command.SpeakerIds)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Speaker resources must be distinct.");
        }
    }
}
