using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.ListResources
{
    public sealed record ListResourcesQuery
        : IRequest<IReadOnlyList<ResourceResponse>>;
}