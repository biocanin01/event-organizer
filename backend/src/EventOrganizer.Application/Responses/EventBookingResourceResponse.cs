namespace EventOrganizer.Application.Responses
{
    public sealed record EventBookingResourceResponse(
        Guid Id,
        string Name,
        string Type,
        decimal Cost,
        int QualityScore);
}
