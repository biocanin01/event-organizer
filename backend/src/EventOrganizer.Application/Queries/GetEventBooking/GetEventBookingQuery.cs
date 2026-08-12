using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetEventBooking
{
    public sealed record GetEventBookingQuery(Guid EventId)
        : IRequest<EventResourceBookingResponse>;
}
