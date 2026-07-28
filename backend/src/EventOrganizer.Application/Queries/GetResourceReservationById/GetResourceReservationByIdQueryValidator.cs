using FluentValidation;

namespace EventOrganizer.Application.Queries.GetResourceReservationById
{
    public sealed class GetResourceReservationByIdQueryValidator
        : AbstractValidator<GetResourceReservationByIdQuery>
    {
        public GetResourceReservationByIdQueryValidator()
        {
            RuleFor(query => query.ReservationId)
                .NotEmpty();
        }
    }
}
