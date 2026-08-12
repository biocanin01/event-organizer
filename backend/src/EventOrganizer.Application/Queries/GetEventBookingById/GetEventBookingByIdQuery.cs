using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetEventBookingById
{
    public sealed record GetEventBookingByIdQuery(Guid BookingId)
        : IRequest<EventResourceBookingResponse>;
}
