using FluentValidation;

namespace EventOrganizer.Application.Queries.ListEventReviews
{
    public sealed class ListEventReviewsQueryValidator : AbstractValidator<ListEventReviewsQuery>
    {
        public ListEventReviewsQueryValidator()
        {
            RuleFor(query => query.EventId)
                .NotEmpty();
        }
    }
}
