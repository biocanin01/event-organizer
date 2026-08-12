using MediatR;

namespace EventOrganizer.Application.Commands.ExpireEventBookings
{
    public sealed record ExpireEventBookingsCommand : IRequest<int>;
}
