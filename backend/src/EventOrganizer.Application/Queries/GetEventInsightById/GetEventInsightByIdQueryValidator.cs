using FluentValidation;

namespace EventOrganizer.Application.Queries.GetEventInsightById
{
    public sealed class GetEventInsightByIdQueryValidator : AbstractValidator<GetEventInsightByIdQuery>
    {
        public GetEventInsightByIdQueryValidator()
        {
            RuleFor(query => query.EventId)
                .NotEmpty();
        }
    }
}
