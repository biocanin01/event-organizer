using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.CreateReview
{
    public sealed record CreateReviewCommand(
        Guid EventId,
        int Rating,
        string Comment) : IRequest<ReviewResponse>;
}
