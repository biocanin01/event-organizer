using EventOrganizer.Application.Recommendations.Candidates;
using EventOrganizer.Domain.Events;

namespace EventOrganizer.Application.Recommendations.Optimization
{
    public sealed class ConstraintRecommendationOptimizer : IRecommendationOptimizer
    {
        private const string NoVenueFailure = "No eligible venue candidates.";
        private const string NotEnoughSpeakersFailure = "Not enough eligible speaker candidates.";
        private const string NoEquipmentPackageFailure = "No eligible equipment package candidates.";
        private const string NoFeasibleRecommendationFailure = "No feasible recommendation within event budget.";

        public RecommendationResult Optimize(
            Event eventItem,
            ResourceCandidateSet candidates)
        {
            ArgumentNullException.ThrowIfNull(eventItem);
            ArgumentNullException.ThrowIfNull(candidates);

            if (candidates.Venues.Count == 0)
            {
                return RecommendationResult.Failure(NoVenueFailure);
            }

            if (candidates.Speakers.Count < eventItem.RequiredSpeakerCount)
            {
                return RecommendationResult.Failure(NotEnoughSpeakersFailure);
            }

            if (eventItem.RequiresEquipment && candidates.EquipmentPackages.Count == 0)
            {
                return RecommendationResult.Failure(NoEquipmentPackageFailure);
            }

            RecommendationResult? bestResult = null;

            foreach (var venue in OrderCandidates(candidates.Venues))
            {
                foreach (var speakers in GetCombinations(
                    OrderCandidates(candidates.Speakers),
                    eventItem.RequiredSpeakerCount))
                {
                    foreach (var equipmentPackage in GetEquipmentPackageOptions(
                        eventItem,
                        OrderCandidates(candidates.EquipmentPackages)))
                    {
                        var selectedResources = new[] { venue }
                            .Concat(speakers)
                            .Concat(equipmentPackage is null
                                ? Array.Empty<ResourceCandidate>()
                                : new[] { equipmentPackage })
                            .ToArray();

                        var totalCost = selectedResources.Sum(resource => resource.Cost);

                        if (totalCost > eventItem.Budget)
                        {
                            continue;
                        }

                        var totalQualityScore = selectedResources.Sum(resource => resource.QualityScore);
                        var result = RecommendationResult.Success(
                            venue,
                            speakers.ToArray(),
                            equipmentPackage,
                            totalCost,
                            totalQualityScore);

                        if (IsBetter(result, bestResult))
                        {
                            bestResult = result;
                        }
                    }
                }
            }

            return bestResult
                ?? RecommendationResult.Failure(NoFeasibleRecommendationFailure);
        }

        private static bool IsBetter(
            RecommendationResult candidate,
            RecommendationResult? currentBest)
        {
            if (currentBest is null)
            {
                return true;
            }

            if (candidate.TotalQualityScore != currentBest.TotalQualityScore)
            {
                return candidate.TotalQualityScore > currentBest.TotalQualityScore;
            }

            if (candidate.TotalCost != currentBest.TotalCost)
            {
                return candidate.TotalCost < currentBest.TotalCost;
            }

            return GetDeterministicKey(candidate)
                .CompareTo(GetDeterministicKey(currentBest)) < 0;
        }

        private static string GetDeterministicKey(RecommendationResult result)
        {
            var resourceKeys = new[] { result.Venue }
                .Where(resource => resource is not null)
                .Cast<ResourceCandidate>()
                .Concat(result.Speakers)
                .Concat(result.EquipmentPackage is null
                    ? Array.Empty<ResourceCandidate>()
                    : new[] { result.EquipmentPackage })
                .Select(resource => $"{resource.Name}|{resource.Id:N}")
                .OrderBy(value => value, StringComparer.Ordinal);

            return string.Join(";", resourceKeys);
        }

        private static IReadOnlyList<ResourceCandidate> OrderCandidates(
            IReadOnlyList<ResourceCandidate> candidates)
        {
            return candidates
                .OrderBy(candidate => candidate.Name, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Id)
                .ToArray();
        }

        private static IEnumerable<IReadOnlyList<ResourceCandidate>> GetCombinations(
            IReadOnlyList<ResourceCandidate> candidates,
            int size)
        {
            if (size == 0)
            {
                yield return Array.Empty<ResourceCandidate>();
                yield break;
            }

            for (var index = 0; index <= candidates.Count - size; index++)
            {
                var current = candidates[index];

                foreach (var combination in GetCombinations(
                    candidates.Skip(index + 1).ToArray(),
                    size - 1))
                {
                    yield return new[] { current }
                        .Concat(combination)
                        .ToArray();
                }
            }
        }

        private static IEnumerable<ResourceCandidate?> GetEquipmentPackageOptions(
            Event eventItem,
            IReadOnlyList<ResourceCandidate> candidates)
        {
            if (!eventItem.RequiresEquipment)
            {
                yield return null;
                yield break;
            }

            foreach (var candidate in candidates)
            {
                yield return candidate;
            }
        }
    }
}
