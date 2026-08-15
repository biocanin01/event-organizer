using FluentValidation;

namespace EventOrganizer.Application.Commands.CreateReview
{
    public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
    {
        public CreateReviewCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty();

            RuleFor(command => command.Rating)
                .InclusiveBetween(1, 5);

            RuleFor(command => command.Comment)
                .NotEmpty()
                .MaximumLength(2000);
        }
    }
}
