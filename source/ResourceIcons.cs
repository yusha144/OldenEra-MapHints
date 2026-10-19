using Hex.Session.Data;
using UnityEngine;

namespace OldenEra.MapHints;

internal sealed class MarkerGlyph
{
    internal Sprite Icon;
    internal string Fallback;
    internal string Badge;
    internal int AlternativeGroup = -1;
    internal bool RandomResource;
}

internal sealed class ResourceIcons
{
    private readonly Dictionary<string, Sprite> _sprites = new();
    private readonly HashSet<string> _warned = new();
    private Sprite _randomIcon;
    private bool _searchedRandomIcon;

    internal void Clear() { _sprites.Clear(); _warned.Clear(); _randomIcon = null; _searchedRandomIcon = false; }

    internal MarkerGlyph[] Read(DataObject data)
    {
        try
        {
            // Show only the type of interaction, without even reading the rolled reward.
            if (RewardKnowledgeRules.IsRandomResource(data.sidConfig))
                return new[] { new MarkerGlyph { Icon = GetRandomIcon(), RandomResource = true } };
            // Read the instantiated reward only. Never roll a variant or generate a reward.
            var set = data.rewardSet;
            var rewards = set?.rewards;
            bool choice = set != null && set.applyType == ERewardSetApplyType.OnlySelected && rewards != null && rewards.Count > 1;
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
            return resolved.Select((id, index) => new MarkerGlyph {
                Icon = GetSprite(id), Fallback = ResourceIconRules.Fallback(id),
                AlternativeGroup = choice ? index : -1
            }).ToArray();
        }
        catch (Exception error)
        {
            if (_warned.Add("read")) Plugin.Instance.Log.LogWarning($"Resource icons unavailable: {error.Message}");
            return null;
        }
    }

    private Sprite GetRandomIcon()
    {
        if (_randomIcon) return _randomIcon;
        if (_searchedRandomIcon) return null;
        _searchedRandomIcon = true;
        // These native dice are used by the game's random hero/faction selection.
        // Some scenes unload them; DrawRandomCube supplies a vector fallback then.
        try
        {
            foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                if (sprite && sprite.name is "random_icon_medium" or "random_icon_large" or "random_Icon")
                { _randomIcon = sprite; break; }
        }
        catch (Exception error)
        {
            if (_warned.Add("random-icon")) Plugin.Instance.Log.LogWarning($"Using the fallback random reward cube: {error.Message}");
        }
        return _randomIcon;
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
