using FluentValidation;

namespace EventOrganizer.Application.Queries.GetEventRecommendation
{
    public sealed class GetEventRecommendationQueryValidator
        : AbstractValidator<GetEventRecommendationQuery>
    {
        public GetEventRecommendationQueryValidator()
        {
            RuleFor(query => query.EventId)
                .NotEmpty();
        }
    }
}
