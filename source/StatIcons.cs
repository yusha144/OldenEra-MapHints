using Hex.Session.Data;
using UnityEngine;

namespace OldenEra.MapHints;

internal sealed class StatIcons
{
    private readonly Dictionary<string, Sprite> _sprites = new();
    private readonly HashSet<string> _warned = new();
    internal void Clear() { _sprites.Clear(); _warned.Clear(); }

    internal MarkerGlyph[] Read(DataObject data)
    {
        try
        {
            var set = data.rewardSet;
            var rewards = set?.rewards;
            if (rewards == null) return null;
            bool choice = set.applyType == ERewardSetApplyType.OnlySelected && rewards.Count > 1;
            var icons = new List<MarkerGlyph>();
            for (int index = 0; index < rewards.Count; index++)
            {
                var reward = rewards[index];
                if (reward == null || reward.rewardType != ERewardType.HeroStatsReward || reward.parameters == null) continue;
                var parameters = new string[reward.parameters.Count];
                for (int p = 0; p < parameters.Length; p++) parameters[p] = reward.parameters[p];
                foreach (var stat in StatIconRules.ReadPairs(parameters))
                    icons.Add(new MarkerGlyph {
                        Icon = GetSprite(stat.Id), Fallback = StatIconRules.Fallback(stat.Id),
                        Badge = "+" + stat.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        AlternativeGroup = choice ? index : -1
                    });
            }
            return icons.Count > 0 ? icons.ToArray() : null;
        }
        catch (Exception error)
        {
            if (_warned.Add("read")) Plugin.Instance.Log.LogWarning($"Stat icons unavailable: {error.Message}");
            return null;
        }
    }

    private Sprite GetSprite(string id)
    {
        if (_sprites.TryGetValue(id, out var sprite) && sprite) return sprite;
        var sprites = cnh.cbwr?.cbws;
        if (sprites != null && sprites.TryGetValue(id, out sprite) && sprite)
        {
            _sprites[id] = sprite;
            return sprite;
        }
        if (_warned.Add(id)) Plugin.Instance.Log.LogWarning($"Native stat sprite not ready: {id}");
        return null;
    }
}
