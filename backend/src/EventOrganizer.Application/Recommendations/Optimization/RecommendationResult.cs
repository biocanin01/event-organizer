using EventOrganizer.Application.Recommendations.Candidates;

namespace EventOrganizer.Application.Recommendations.Optimization
{
    public sealed record RecommendationResult(
        bool IsSuccessful,
        ResourceCandidate? Venue,
        IReadOnlyList<ResourceCandidate> Speakers,
        IReadOnlyList<ResourceCandidate> Equipment,
        decimal TotalCost,
        int TotalQualityScore,
        IReadOnlyList<string> FailureReasons)
    {
        public static RecommendationResult Success(
            ResourceCandidate venue,
            IReadOnlyList<ResourceCandidate> speakers,
            IReadOnlyList<ResourceCandidate> equipment,
            decimal totalCost,
            int totalQualityScore)
        {
            return new RecommendationResult(
                true,
                venue,
                speakers,
                equipment,
                totalCost,
                totalQualityScore,
                Array.Empty<string>());
        }

        public static RecommendationResult Failure(params string[] failureReasons)
        {
            return new RecommendationResult(
                false,
                null,
                Array.Empty<ResourceCandidate>(),
                Array.Empty<ResourceCandidate>(),
                0m,
                0,
                failureReasons);
        }
    }
}
