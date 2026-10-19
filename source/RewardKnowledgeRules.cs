namespace OldenEra.MapHints;

internal static class RewardKnowledgeRules
{
    internal static bool IsRandomResource(string objectId) => objectId is "gardener" or "windmill";

    // The game's saved sideScoutings contains player IDs, not hero IDs.
    // Read it afresh so loading an earlier save or another match cannot inherit knowledge.
    internal static bool KnowsSkills(IEnumerable<int> acquaintedSides, int sideId) =>
        sideId >= 0 && acquaintedSides != null && acquaintedSides.Contains(sideId);
}
