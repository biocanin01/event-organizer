using FluentValidation;

namespace EventOrganizer.Application.Queries.GetEventById
{
    public sealed class GetEventByIdQueryValidator : AbstractValidator<GetEventByIdQuery>
    {
        public GetEventByIdQueryValidator()
        {
            RuleFor(query => query.EventId)
                .NotEmpty();
        }
    }
}
