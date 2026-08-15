namespace EventOrganizer.Api.Contracts.Reviews
{
    public sealed record UpdateReviewRequest(
        int Version,
        int Rating,
        string Comment);
}
