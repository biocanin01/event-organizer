using EventOrganizer.Domain.Events;

namespace EventOrganizer.Application.Recommendations.Candidates
{
    public interface IResourceCandidateProvider
    {
        Task<ResourceCandidateSet> GetCandidatesAsync(
            Event eventItem,
            CancellationToken cancellationToken);
    }
}
