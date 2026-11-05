using Hex.Session.Data;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace OldenEra.MapHints;

internal static class BuffAvailability
{
    private static float _nextErrorLog;

    internal static bool AllRewardsAlreadyActive(fpj bank, Hero hero)
    {
        try
        {
            var rewards = bank.crii?.cqvr;
            var active = hero.buffs?.list;
            if (rewards == null || rewards.Length == 0 || active == null || active.Count == 0) return false;
            var offers = new List<BuffOffer>();
            bool hasOtherRewards = false;
            foreach (var reward in rewards)
            {
                var buffReward = reward?.TryCast<flf>();
                if (buffReward == null) { hasOtherRewards = true; continue; }
                var config = buffReward.cqvz;
                if (config == null) return false;
                offers.Add(new BuffOffer(buffReward.cqvx, (int)config.cduu, ReadDurations(buffReward.cqvy)));
            }
            if (hasOtherRewards || offers.Count == 0) return false;
            var buffs = new List<ActiveHeroBuff>();
            foreach (var buff in active)
                if (buff != null) buffs.Add(new ActiveHeroBuff(buff.sid, ReadDurations(buff.durations)));
            // The game removes expired/dispelled buffs from this saved list. Reading
            // it handles day/week changes, hero switching and loading without history.
            return BuffHintRules.AllRewardsAlreadyActive(offers, buffs, hasOtherRewards);
        }
        catch (Exception error)
        {
            // An optional usefulness check must not disable the rest of the map.
            if (Time.unscaledTime >= _nextErrorLog)
            {
                Plugin.Instance.Log.LogWarning($"Active buff check unavailable: {error.Message}");
                _nextErrorLog = Time.unscaledTime + 15;
            }
            return false;
        }
    }

    private static int[] ReadDurations(Il2CppReferenceArray<BuffDuration> durations)
    {
        if (durations == null) return Array.Empty<int>();
        var types = new int[durations.Length];
        for (int index = 0; index < types.Length; index++)
            types[index] = durations[index] == null ? -1 : (int)durations[index].cwss;
        return types;
    }
}
