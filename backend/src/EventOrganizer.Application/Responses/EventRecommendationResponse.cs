namespace EventOrganizer.Application.Responses
{
    public sealed record EventRecommendationResponse(
        bool IsSuccessful,
        RecommendedResourceResponse? Venue,
        IReadOnlyList<RecommendedResourceResponse> Speakers,
        RecommendedResourceResponse? EquipmentPackage,
        decimal TotalCost,
        int TotalQualityScore,
        IReadOnlyList<string> FailureReasons);
}
