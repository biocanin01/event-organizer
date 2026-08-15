using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListMyReviews
{
    public sealed record ListMyReviewsQuery : IRequest<IReadOnlyList<ReviewResponse>>;
}
