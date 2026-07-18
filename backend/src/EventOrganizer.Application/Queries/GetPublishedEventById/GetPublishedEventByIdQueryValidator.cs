using FluentValidation;

namespace EventOrganizer.Application.Queries.GetPublishedEventById
{
    public sealed class GetPublishedEventByIdQueryValidator
        : AbstractValidator<GetPublishedEventByIdQuery>
    {
        public GetPublishedEventByIdQueryValidator()
        {
            RuleFor(query => query.EventId)
                .NotEmpty();
        }
    }
}