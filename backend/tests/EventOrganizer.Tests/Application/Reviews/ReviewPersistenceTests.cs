using EventOrganizer.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace EventOrganizer.Tests.Application.Reviews
{
    public sealed class ReviewPersistenceTests : ApplicationTestBase
    {
        [Fact]
        public async Task SaveChanges_WithDuplicateEventAndParticipant_Throws()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("review-participant@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            DbContext.Reviews.Add(Review.Create(
                eventItem.Id,
                participantUserId,
                5,
                "Odlican dogadjaj.",
                DateTime.UtcNow));
            DbContext.Reviews.Add(Review.Create(
                eventItem.Id,
                participantUserId,
                4,
                "Drugi komentar.",
                DateTime.UtcNow));

            await Assert.ThrowsAsync<DbUpdateException>(() =>
                DbContext.SaveChangesAsync());
        }

        [Fact]
        public async Task SaveChanges_WithStaleReviewVersion_ThrowsConcurrencyException()
        {
            var organizerUserId = await CreateOrganizerUserAsync();
            var participantUserId = await CreateOrganizerUserAsync("review-concurrency@example.com");
            var eventItem = await CreateEventAsync(organizerUserId);
            var review = Review.Create(
                eventItem.Id,
                participantUserId,
                5,
                "Odlican dogadjaj.",
                DateTime.UtcNow);
            DbContext.Reviews.Add(review);
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();

            using var secondContext = CreateDbContext();
            var firstCopy = await DbContext.Reviews.SingleAsync(item => item.Id == review.Id);
            var secondCopy = await secondContext.Reviews.SingleAsync(item => item.Id == review.Id);
            firstCopy.Update(4, "Prva izmena.", DateTime.UtcNow);
            secondCopy.Update(3, "Druga izmena.", DateTime.UtcNow);

            await DbContext.SaveChangesAsync();

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                secondContext.SaveChangesAsync());
        }
    }
}
