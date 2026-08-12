using FluentValidation;

namespace EventOrganizer.Application.Queries.GetEventBooking
{
    public sealed class GetEventBookingQueryValidator : AbstractValidator<GetEventBookingQuery>
    {
        public GetEventBookingQueryValidator()
        {
            RuleFor(query => query.EventId)
                .NotEmpty();
        }
    }
}
