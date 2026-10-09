using UnityEngine;

namespace OldenEra.MapHints;

internal sealed class BuffIcons
{
    private readonly Dictionary<string, Sprite> _sprites = new();
    private readonly HashSet<string> _warned = new();

    internal void Clear() { _sprites.Clear(); _warned.Clear(); }

    internal MarkerGlyph[] Read(fmt bank)
    {
        try
        {
            // Read only the existing offer, never generate or apply an effect.
            var rewards = bank.cowe?.cojj;
            if (rewards == null) return null;
            var icons = new List<MarkerGlyph>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reward in rewards)
            {
                var config = reward?.TryCast<fis>()?.cojr;
                if (config == null || config.hidden) continue;
                // BhHeroBuffView.bdah uses this exact LogicBuffConfig icon ID
                // with cnh.tak when drawing effects in the hero's inventory.
                string iconId = config.cswj;
                if (string.IsNullOrEmpty(iconId) || !seen.Add(iconId)) continue;
                icons.Add(new MarkerGlyph { Icon = GetSprite(iconId), Fallback = "+" });
            }
            return icons.Count > 0 ? icons.ToArray() : null;
        }
        catch (Exception error)
        {
            if (_warned.Add("read")) Plugin.Instance.Log.LogWarning($"Buff icons unavailable: {error.Message}");
            return null;
        }
    }

    private Sprite GetSprite(string id)
    {
        if (_sprites.TryGetValue(id, out var sprite) && sprite) return sprite;
        // Same catalog as the hero profile. Avoid its generic missing-icon
        // replacement: keep '+' until the real sprite has finished loading.
        var sprites = cnh.cbwr?.cbws;
        if (sprites != null && sprites.TryGetValue(id, out sprite) && sprite)
        {
            _sprites[id] = sprite;
            return sprite;
        }
        if (_warned.Add(id)) Plugin.Instance.Log.LogWarning($"Native buff sprite not ready: {id}");
        return null;
    }
}
