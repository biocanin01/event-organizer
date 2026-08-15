using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListManagedReviews
{
    public sealed record ListManagedReviewsQuery(Guid? EventId) : IRequest<IReadOnlyList<ReviewResponse>>;
}
