using FluentValidation;

namespace EventOrganizer.Application.Queries.GetEventBookingById
{
    public sealed class GetEventBookingByIdQueryValidator
        : AbstractValidator<GetEventBookingByIdQuery>
    {
        public GetEventBookingByIdQueryValidator()
        {
            RuleFor(query => query.BookingId)
                .NotEmpty();
        }
    }
}
