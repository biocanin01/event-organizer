using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Commands.UpdateReview
{
    public sealed record UpdateReviewCommand(
        Guid ReviewId,
        int Version,
        int Rating,
        string Comment) : IRequest<ReviewResponse>;
}
