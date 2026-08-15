using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListEventReviews
{
    public sealed record ListEventReviewsQuery(Guid EventId) : IRequest<IReadOnlyList<ReviewResponse>>;
}
