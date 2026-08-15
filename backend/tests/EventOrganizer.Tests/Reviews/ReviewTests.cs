using EventOrganizer.Domain.Reviews;

namespace EventOrganizer.Tests.Reviews;

public sealed class ReviewTests
{
    [Fact]
    public void Create_WithValidData_CreatesReview()
    {
        var eventId = Guid.NewGuid();
        var participantUserId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc);

        var review = Review.Create(
            eventId,
            participantUserId,
            5,
            " Odlican dogadjaj. ",
            createdAtUtc);

        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(eventId, review.EventId);
        Assert.Equal(participantUserId, review.ParticipantUserId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Odlican dogadjaj.", review.Comment);
        Assert.Equal(1, review.Version);
        Assert.Equal(createdAtUtc, review.CreatedAtUtc);
    }

    [Fact]
    public void Update_WithValidData_ChangesReviewAndIncrementsVersion()
    {
        var review = CreateReview();
        var updatedAtUtc = DateTime.UtcNow;

        review.Update(4, "Dobro organizovano.", updatedAtUtc);

        Assert.Equal(4, review.Rating);
        Assert.Equal("Dobro organizovano.", review.Comment);
        Assert.Equal(updatedAtUtc, review.UpdatedAtUtc);
        Assert.Equal(2, review.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_WithInvalidRating_Throws(int rating)
    {
        var act = () => Review.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            rating,
            "Komentar.",
            DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Create_WithBlankComment_Throws()
    {
        var act = () => Review.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            " ",
            DateTime.UtcNow);

        Assert.Throws<ArgumentException>(act);
    }

    private static Review CreateReview()
    {
        return Review.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            "Odlican dogadjaj.",
            DateTime.UtcNow);
    }
}
