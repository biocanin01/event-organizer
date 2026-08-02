namespace EventOrganizer.Application.Responses
{
    public sealed record EventRecommendationResponse(
        bool IsSuccessful,
        RecommendedResourceResponse? Venue,
        IReadOnlyList<RecommendedResourceResponse> Speakers,
        IReadOnlyList<RecommendedResourceResponse> Equipment,
        decimal TotalCost,
        int TotalQualityScore,
        IReadOnlyList<string> FailureReasons);
}
