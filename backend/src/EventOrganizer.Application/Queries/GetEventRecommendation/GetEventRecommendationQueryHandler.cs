using EventOrganizer.Application.Common.Authorization;
using EventOrganizer.Application.Common.Exceptions;
using EventOrganizer.Application.Common.Interfaces;
using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Application.Recommendations.Optimization;
using EventOrganizer.Application.Responses;
using EventOrganizer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Application.Queries.GetEventRecommendation
{
    public sealed class GetEventRecommendationQueryHandler
        : IRequestHandler<GetEventRecommendationQuery, EventRecommendationResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly EventAuthorizationService _eventAuthorizationService;
        private readonly IResourceCandidateProvider _candidateProvider;
        private readonly IRecommendationOptimizer _optimizer;

        public GetEventRecommendationQueryHandler(
            IApplicationDbContext dbContext,
            EventAuthorizationService eventAuthorizationService,
            IResourceCandidateProvider candidateProvider,
            IRecommendationOptimizer optimizer)
        {
            _dbContext = dbContext;
            _eventAuthorizationService = eventAuthorizationService;
            _candidateProvider = candidateProvider;
            _optimizer = optimizer;
        }

        public async Task<EventRecommendationResponse> Handle(
            GetEventRecommendationQuery request,
            CancellationToken cancellationToken)
        {
            var eventItem = await _dbContext.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    eventItem => eventItem.Id == request.EventId,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(nameof(Event), request.EventId);
            }

            _eventAuthorizationService.EnsureCanManage(eventItem);

            var candidates = await _candidateProvider.GetCandidatesAsync(
                eventItem,
                cancellationToken);

            var recommendation = _optimizer.Optimize(eventItem, candidates);

            return MapRecommendation(recommendation);
        }

        private static EventRecommendationResponse MapRecommendation(
            RecommendationResult recommendation)
        {
            return new EventRecommendationResponse(
                recommendation.IsSuccessful,
                recommendation.Venue is null
                    ? null
                    : MapResource(recommendation.Venue),
                recommendation.Speakers.Select(MapResource).ToArray(),
                recommendation.EquipmentPackage is null
                    ? null
                    : MapResource(recommendation.EquipmentPackage),
                recommendation.TotalCost,
                recommendation.TotalQualityScore,
                recommendation.FailureReasons);
        }

        private static RecommendedResourceResponse MapResource(
            ResourceCandidate resource)
        {
            return new RecommendedResourceResponse(
                resource.Id,
                resource.Name,
                resource.Type.ToString(),
                resource.Cost,
                resource.Capacity,
                resource.Area,
                resource.QualityScore);
        }
    }
}
