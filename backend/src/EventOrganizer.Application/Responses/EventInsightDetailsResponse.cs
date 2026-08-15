namespace EventOrganizer.Application.Responses
{
    public sealed record EventInsightDetailsResponse(
        Guid EventId,
        string EventTitle,
        string Status,
        DateTime StartsAtUtc,
        DateTime EndsAtUtc,
        int Capacity,
        int PendingRegistrationCount,
        int ConfirmedRegistrationCount,
        int RejectedRegistrationCount,
        int CancelledRegistrationCount,
        decimal CapacityFillPercentage,
        double? AverageRating,
        int ReviewCount,
        IReadOnlyList<RatingDistributionResponse> RatingDistribution,
        IReadOnlyList<RecentReviewResponse> RecentReviews);
}
