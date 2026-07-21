using EventOrganizer.Application.Responses;
using MediatR;

namespace EventOrganizer.Application.Queries.GetResourceById
{
    public sealed record GetResourceByIdQuery(Guid ResourceId)
        : IRequest<ResourceResponse?>;
}