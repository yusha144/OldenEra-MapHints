using Hex.Session.Data;
using UnityEngine;

namespace OldenEra.MapHints;

internal sealed class MarkerGlyph
{
    internal Sprite Icon;
    internal string Fallback;
    internal string Badge;
    internal int AlternativeGroup = -1;
}

internal sealed class ResourceIcons
{
    private readonly Dictionary<string, Sprite> _sprites = new();
    private readonly HashSet<string> _warned = new();

    internal void Clear() { _sprites.Clear(); _warned.Clear(); }

    internal MarkerGlyph[] Read(DataObject data)
    {
        try
        {
            // Read the instantiated reward only. Never roll a variant or generate a reward.
            var rewards = data.rewardSet?.rewards;
            var ids = new List<string>();
            if (rewards != null)
                foreach (var reward in rewards)
                {
                    if (reward == null || reward.rewardType != ERewardType.SideResReward) continue;
                    var parameters = reward.parameters;
                    if (parameters == null || parameters.Count < 2) continue;
                    if (!int.TryParse(parameters[1], out int amount) || amount <= 0) continue;
                    ids.Add(parameters[0]);
                }
            var resolved = ResourceIconRules.Resolve(ids, data.sidConfig);
            if (resolved.Length == 0) return null;
            return resolved.Select(id => new MarkerGlyph { Icon = GetSprite(id), Fallback = ResourceIconRules.Fallback(id) }).ToArray();
        }
        catch (Exception error)
        {
            if (_warned.Add("read")) Plugin.Instance.Log.LogWarning($"Resource icons unavailable: {error.Message}");
            return null;
        }
    }

    private Sprite GetSprite(string id)
    {
        if (_sprites.TryGetValue(id, out var sprite) && sprite) return sprite;
        // resources_info.json maps these seven resource IDs directly to sprite IDs.
        // cbws is the lookup used by cnh.tak; direct access avoids its generic missing-icon fallback.
        var sprites = cnh.cbwr?.cbws;
        if (sprites != null && sprites.TryGetValue(id, out sprite) && sprite)
        {
            _sprites[id] = sprite;
            return sprite;
        }
        // Missing assets are retried on refresh (scene loading can populate the catalog later).
        if (_warned.Add(id)) Plugin.Instance.Log.LogWarning($"Native resource sprite not ready: {id}");
        return null;
    }
}
