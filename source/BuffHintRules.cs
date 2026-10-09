namespace OldenEra.MapHints;

internal sealed class BuffOffer
{
    internal readonly string Id;
    internal readonly int AdditionType;
    internal readonly int[] DurationTypes;

    internal BuffOffer(string id, int additionType, int[] durationTypes)
    {
        Id = id;
        AdditionType = additionType;
        DurationTypes = durationTypes;
    }
}

internal sealed class ActiveHeroBuff
{
    internal readonly string Id;
    internal readonly int[] DurationTypes;

    internal ActiveHeroBuff(string id, int[] durationTypes)
    {
        Id = id;
        DurationTypes = durationTypes;
    }
}

internal static class BuffHintRules
{
    internal static bool AllRewardsAlreadyActive(IReadOnlyList<BuffOffer> rewards,
        IReadOnlyList<ActiveHeroBuff> active, bool hasOtherRewards)
    {
        if (hasOtherRewards || rewards == null || rewards.Count == 0 || active == null) return false;
        return rewards.All(offer => offer != null && active.Any(buff => IsRedundant(offer, buff)));
    }

    private static bool IsRedundant(BuffOffer offer, ActiveHeroBuff active)
    {
        if (active == null || string.IsNullOrEmpty(offer.Id) || offer.Id != active.Id) return false;
        // EBuffAdditionType.LeftOld: an existing copy always wins.
        if (offer.AdditionType == 0) return true;
        // LeftNew may extend a timed buff. Only suppress replacement when its
        // single event-based expiry is unchanged (or the existing buff is infinite).
        // CombineEffect, CombineDuration and CombineAll remain useful.
        if (offer.AdditionType != 1 || offer.DurationTypes?.Length != 1 || active.DurationTypes?.Length != 1) return false;
        int next = offer.DurationTypes[0], current = active.DurationTypes[0];
        return next is >= 0 and <= 3 && (current == next || current == 0);
    }
}
