namespace EventOrganizer.Application.Recommendations.Candidates
{
    public sealed record ResourceCandidateSet(
        IReadOnlyList<ResourceCandidate> Venues,
        IReadOnlyList<ResourceCandidate> Speakers,
        IReadOnlyList<ResourceCandidate> Equipment);
}
