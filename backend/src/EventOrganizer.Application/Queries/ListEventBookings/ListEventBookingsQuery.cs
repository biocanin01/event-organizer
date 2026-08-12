using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Bookings;
using MediatR;

namespace EventOrganizer.Application.Queries.ListEventBookings
{
    public sealed record ListEventBookingsQuery(EventResourceBookingStatus? Status)
        : IRequest<IReadOnlyList<EventResourceBookingResponse>>;
}
