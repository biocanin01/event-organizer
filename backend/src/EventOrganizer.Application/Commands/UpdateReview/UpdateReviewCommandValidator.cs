using FluentValidation;

namespace EventOrganizer.Application.Commands.UpdateReview
{
    public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
    {
        public UpdateReviewCommandValidator()
        {
            RuleFor(command => command.ReviewId)
                .NotEmpty();

            RuleFor(command => command.Version)
                .GreaterThan(0);

            RuleFor(command => command.Rating)
                .InclusiveBetween(1, 5);

            RuleFor(command => command.Comment)
                .NotEmpty()
                .MaximumLength(2000);
        }
    }
}
