namespace EventOrganizer.Application.Responses
{
    public sealed record RatingDistributionResponse(
        int Rating,
        int Count);
}
