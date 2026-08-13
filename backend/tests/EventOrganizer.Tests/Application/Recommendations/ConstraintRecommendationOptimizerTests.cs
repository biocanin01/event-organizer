using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Application.Recommendations.Optimization;
using EventOrganizer.Domain.Events;
using EventOrganizer.Domain.Resources;

namespace EventOrganizer.Tests.Application.Recommendations
{
    public sealed class ConstraintRecommendationOptimizerTests
    {
        private readonly ConstraintRecommendationOptimizer _optimizer = new();

        [Fact]
        public void Optimize_WithFeasibleCandidates_ReturnsHighestQualityCombinationWithinBudget()
        {
            var eventItem = CreateEvent(
                budget: 800m,
                requiredSpeakerCount: 2,
                requiresEquipment: true);
            var venue = CreateCandidate("Better Hall", ResourceType.Venue, 350m, qualityScore: 5);
            var firstSpeaker = CreateCandidate("Senior Speaker", ResourceType.Speaker, 200m, qualityScore: 5);
            var secondSpeaker = CreateCandidate("Domain Speaker", ResourceType.Speaker, 150m, qualityScore: 4);
            var lowerQualitySpeaker = CreateCandidate("Junior Speaker", ResourceType.Speaker, 100m, qualityScore: 2);
            var affordableEquipment = CreateCandidate("Projector", ResourceType.EquipmentPackage, 100m, qualityScore: 3);
            var expensiveEquipment = CreateCandidate("Recording Kit", ResourceType.EquipmentPackage, 200m, qualityScore: 4);

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    new[] { venue },
                    new[] { lowerQualitySpeaker, firstSpeaker, secondSpeaker },
                    new[] { expensiveEquipment, affordableEquipment }));

            Assert.True(result.IsSuccessful);
            Assert.Equal(venue.Id, result.Venue?.Id);
            Assert.Equal(new[] { secondSpeaker.Id, firstSpeaker.Id }, result.Speakers.Select(speaker => speaker.Id));
            Assert.Equal(affordableEquipment.Id, result.EquipmentPackage?.Id);
            Assert.Equal(800m, result.TotalCost);
            Assert.Equal(17, result.TotalQualityScore);
        }

        [Fact]
        public void Optimize_WhenQualityIsEqual_ReturnsLowerCostCombination()
        {
            var eventItem = CreateEvent(budget: 1000m, requiredSpeakerCount: 1);
            var expensiveVenue = CreateCandidate("Expensive Hall", ResourceType.Venue, 500m, qualityScore: 5);
            var cheaperVenue = CreateCandidate("Cheaper Hall", ResourceType.Venue, 300m, qualityScore: 5);
            var speaker = CreateCandidate("Architecture Speaker", ResourceType.Speaker, 100m, qualityScore: 4);

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    new[] { expensiveVenue, cheaperVenue },
                    new[] { speaker },
                    Array.Empty<ResourceCandidate>()));

            Assert.True(result.IsSuccessful);
            Assert.Equal(cheaperVenue.Id, result.Venue?.Id);
            Assert.Equal(400m, result.TotalCost);
            Assert.Equal(9, result.TotalQualityScore);
            Assert.Null(result.EquipmentPackage);
        }

        [Fact]
        public void Optimize_WhenEquipmentIsNotRequired_DoesNotSelectEquipmentPackage()
        {
            var eventItem = CreateEvent(budget: 1000m);
            var venue = CreateCandidate("Main Hall", ResourceType.Venue, 300m, qualityScore: 4);
            var speaker = CreateCandidate("Architecture Speaker", ResourceType.Speaker, 100m, qualityScore: 4);
            var equipment = CreateCandidate("Excellent Equipment", ResourceType.EquipmentPackage, 100m, qualityScore: 5);

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    new[] { venue },
                    new[] { speaker },
                    new[] { equipment }));

            Assert.True(result.IsSuccessful);
            Assert.Null(result.EquipmentPackage);
            Assert.Equal(400m, result.TotalCost);
            Assert.Equal(8, result.TotalQualityScore);
        }

        [Fact]
        public void Optimize_WhenEquipmentIsRequiredWithoutCandidates_ReturnsFailure()
        {
            var eventItem = CreateEvent(requiresEquipment: true);

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    new[] { CreateCandidate("Main Hall", ResourceType.Venue) },
                    new[] { CreateCandidate("Architecture Speaker", ResourceType.Speaker) },
                    Array.Empty<ResourceCandidate>()));

            Assert.False(result.IsSuccessful);
            Assert.Contains("No eligible equipment package candidates.", result.FailureReasons);
        }

        [Fact]
        public void Optimize_WhenNoVenueCandidates_ReturnsFailure()
        {
            var eventItem = CreateEvent();

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    Array.Empty<ResourceCandidate>(),
                    new[] { CreateCandidate("Architecture Speaker", ResourceType.Speaker) },
                    Array.Empty<ResourceCandidate>()));

            Assert.False(result.IsSuccessful);
            Assert.Contains("No eligible venue candidates.", result.FailureReasons);
        }

        [Fact]
        public void Optimize_WhenNotEnoughSpeakers_ReturnsFailure()
        {
            var eventItem = CreateEvent(requiredSpeakerCount: 2);

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    new[] { CreateCandidate("Main Hall", ResourceType.Venue) },
                    new[] { CreateCandidate("Architecture Speaker", ResourceType.Speaker) },
                    Array.Empty<ResourceCandidate>()));

            Assert.False(result.IsSuccessful);
            Assert.Contains("Not enough eligible speaker candidates.", result.FailureReasons);
        }

        [Fact]
        public void Optimize_WhenAllRequiredCombinationsExceedBudget_ReturnsFailure()
        {
            var eventItem = CreateEvent(budget: 100m);

            var result = _optimizer.Optimize(
                eventItem,
                new ResourceCandidateSet(
                    new[] { CreateCandidate("Main Hall", ResourceType.Venue, 100m) },
                    new[] { CreateCandidate("Architecture Speaker", ResourceType.Speaker, 100m) },
                    Array.Empty<ResourceCandidate>()));

            Assert.False(result.IsSuccessful);
            Assert.Contains("No feasible recommendation within event budget.", result.FailureReasons);
        }

        private static Event CreateEvent(
            decimal budget = 1000m,
            int requiredSpeakerCount = 1,
            bool requiresEquipment = false)
        {
            var startsAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

            return Event.Create(
                "Software Architecture Seminar",
                "Seminar about modern web architecture.",
                startsAtUtc,
                startsAtUtc.AddHours(4),
                80,
                budget,
                "IT",
                requiredSpeakerCount,
                Guid.NewGuid(),
                new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
                requiresEquipment);
        }

        private static ResourceCandidate CreateCandidate(
            string name,
            ResourceType type,
            decimal cost = 100m,
            int qualityScore = 4)
        {
            return new ResourceCandidate(
                Guid.NewGuid(),
                name,
                type,
                cost,
                type == ResourceType.Venue ? 100 : null,
                type == ResourceType.Speaker ? "IT" : null,
                qualityScore);
        }
    }
}
