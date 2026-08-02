using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Application.Recommendations.Optimization
{
    public interface IRecommendationOptimizer
    {
        RecommendationResult Optimize(
            Event eventItem,
            ResourceCandidateSet candidates);
    }
}
