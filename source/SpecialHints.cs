using UnityEngine;
using UnityEngine.EventSystems;

namespace OldenEra.MapHints;

public sealed partial class HintOverlay
{
    private Texture2D _drop;
    private Texture2D _randomCube;

    private static IntPtr ReadHover(fbu world)
    {
        if (!world || !Plugin.Instance.SkillHover.Value || Plugin.Instance.Mode.Value == DisplayMode.Off) return IntPtr.Zero;
        try
        {
            var events = EventSystem.current;
            if (events && events.IsPointerOverGameObject()) return IntPtr.Zero;
            // fln.corz is the game's current raycast target; reading it does not rerun picking.
            return world.cnlr?.couf?.corz?.Pointer ?? IntPtr.Zero;
        }
        catch { return IntPtr.Zero; } // Optional preview must not disable ordinary markers.
    }

    private void LogPreviewError(Exception error)
    {
        if (Time.unscaledTime < _nextErrorLog) return;
        Plugin.Instance.Log.LogWarning($"Reward hover preview unavailable: {error.GetType().Name}: {error.Message}");
        _nextErrorLog = Time.unscaledTime + 15f;
    }

    private void DrawManaWell(Rect rect, bool needed, float scale)
    {
        if (!_drop)
        {
            _drop = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float nx = (x + 0.5f) / 64f - 0.5f, ny = (y + 0.5f) / 64f;
                float circle = 0.29f - MathF.Sqrt(nx * nx + (ny - 0.36f) * (ny - 0.36f));
                float triangle = ny >= 0.36f ? MathF.Min((0.94f - ny) * 0.50f - MathF.Abs(nx), ny - 0.36f) : -1;
                float alpha = Math.Clamp(MathF.Max(circle, triangle) * 64f, 0, 1);
                _drop.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            _drop.Apply();
        }
        float wave = needed && Plugin.Instance.WellPulse.Value ? 0.5f + 0.5f * MathF.Sin(Time.unscaledTime * 2.4f) : 0;
        float halo = (6 + wave * 3) * scale;
        GUI.color = new Color(0.12f, 0.65f, 1f, needed ? 0.17f + wave * 0.13f : 0.12f);
        GUI.DrawTexture(new Rect(rect.x - halo, rect.y - halo, rect.width + halo * 2, rect.height + halo * 2), _disc);
        GUI.color = needed ? new Color(0.28f, 0.84f, 1f) : new Color(0.20f, 0.57f, 0.88f);
        GUI.DrawTexture(rect, _disc);
        float ring = 2.5f * scale;
        GUI.color = new Color(0.025f, 0.085f, 0.18f, 0.97f);
        GUI.DrawTexture(new Rect(rect.x + ring, rect.y + ring, rect.width - ring * 2, rect.height - ring * 2), _disc);
        GUI.color = needed ? new Color(0.63f, 0.95f, 1f) : new Color(0.35f, 0.68f, 0.91f);
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.17f, rect.y + rect.height * 0.10f, rect.width * 0.66f, rect.height * 0.73f), _drop);
        GUI.color = new Color(0.025f, 0.06f, 0.12f, 0.95f);
        var label = new Rect(rect.center.x - 27 * scale, rect.yMax - 2 * scale, 54 * scale, 17 * scale);
        GUI.DrawTexture(label, Texture2D.whiteTexture);
        GUI.color = Color.white;
        _iconStyle.fontSize = Mathf.RoundToInt(11 * scale);
        _iconStyle.normal.textColor = new Color(0.64f, 0.91f, 1f);
        GUI.Label(label, "МАНА", _iconStyle);
    }

    private void DrawRewardPreview(RewardPreview preview, Vector2 mouse, float scale)
    {
        if (preview == null || preview.Offers.Count == 0) return;
        int count = Math.Min(8, preview.Offers.Count);
        float width = Math.Min(365 * scale, Screen.width - 16 * scale);
        _tooltipStyle.fontSize = Mathf.RoundToInt(11 * scale);
        bool hasNote = !string.IsNullOrEmpty(preview.Note);
        float noteHeight = hasNote
            ? Math.Max(34 * scale, _tooltipStyle.CalcHeight(new GUIContent(preview.Note), width - 24 * scale))
            : 0;
        float height = (40 + count * 48 + (hasNote ? 8 : 0)) * scale + noteHeight;
        float x = mouse.x - width - 20 * scale;
        if (x < 8 * scale) x = mouse.x + 22 * scale;
        x = Math.Clamp(x, 8 * scale, Math.Max(8 * scale, Screen.width - width - 8 * scale));
        float y = Math.Clamp(mouse.y + 22 * scale, 8 * scale, Math.Max(8 * scale, Screen.height - height - 8 * scale));
        var panel = new Rect(x, y, width, height);
        if (OverlapsGameUi(panel)) return;
        GUI.color = new Color(0.57f, 0.43f, 0.78f, 0.92f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.045f, 0.055f, 0.085f, 0.97f);
        GUI.DrawTexture(new Rect(x + scale, y + scale, width - 2 * scale, height - 2 * scale), Texture2D.whiteTexture);
        GUI.color = Color.white;
        _tooltipStyle.fontSize = Mathf.RoundToInt(15 * scale);
        GUI.Label(new Rect(x + 12 * scale, y + 9 * scale, width - 24 * scale, 24 * scale), preview.Title, _tooltipStyle);
        for (int index = 0; index < count; index++)
        {
            var offer = preview.Offers[index];
            float rowY = y + (37 + 48 * index) * scale;
            var iconRect = new Rect(x + 12 * scale, rowY, 40 * scale, 40 * scale);
            if (!DrawSpriteIcon(iconRect, offer.Icon, Color.white))
            {
                _iconStyle.fontSize = Mathf.RoundToInt(20 * scale);
                _iconStyle.normal.textColor = new Color(0.8f, 0.7f, 1f);
                GUI.Label(iconRect, "?", _iconStyle);
            }
            _tooltipStyle.fontSize = Mathf.RoundToInt(13 * scale);
            GUI.Label(new Rect(x + 63 * scale, rowY + 2 * scale, width - 76 * scale, 42 * scale), offer.Name, _tooltipStyle);
        }
        if (hasNote)
        {
            _tooltipStyle.fontSize = Mathf.RoundToInt(11 * scale);
            GUI.color = new Color(0.74f, 0.77f, 0.84f);
            GUI.Label(new Rect(x + 12 * scale, y + (38 + count * 48) * scale, width - 24 * scale, noteHeight), preview.Note, _tooltipStyle);
        }
        GUI.color = Color.white;
    }

    private void DrawRandomCube(Rect rect, Color tint)
    {
        if (!_randomCube)
        {
            _randomCube = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            // A three-faced cube also works when menu-only native sprites are unloaded.
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float nx = (x + 0.5f) / 64f, ny = (y + 0.5f) / 64f;
                float dx = Math.Abs(nx - 0.5f);
                bool top = ny > 0.53f + dx * 0.55f && ny < 0.99f - dx * 0.55f;
                bool side = ny > 0.01f + dx * 0.55f && ny < 0.49f + dx * 0.55f;
                Color color = top ? new Color(0.83f, 0.71f, 0.43f)
                    : side && nx < 0.48f ? new Color(0.70f, 0.57f, 0.30f)
                    : side && nx > 0.52f ? new Color(0.59f, 0.47f, 0.23f) : Color.clear;
                _randomCube.SetPixel(x, y, color);
            }
            _randomCube.Apply();
        }
        GUI.color = tint;
        GUI.DrawTexture(rect, _randomCube);
        GUI.color = Color.white;
    }

    private static bool DrawSpriteIcon(Rect destination, Sprite sprite, Color tint)
    {
        if (!sprite || !sprite.texture) return false;
        var texture = sprite.texture;
        // UVs also work for tightly packed sprites whose textureRect accessor throws.
        var uv = sprite.uv;
        if (uv == null || uv.Length == 0) return false;
        float minX = 1, minY = 1, maxX = 0, maxY = 0;
        foreach (var point in uv)
        { minX = Math.Min(minX, point.x); minY = Math.Min(minY, point.y); maxX = Math.Max(maxX, point.x); maxY = Math.Max(maxY, point.y); }
        // Fit the sprite without stretching wood, ore and other non-square artwork.
        var source = sprite.rect;
        if (source.width <= 0 || source.height <= 0) return false;
        float fit = Math.Min(destination.width / source.width, destination.height / source.height);
        var fitted = new Rect(destination.center.x - source.width * fit / 2, destination.center.y - source.height * fit / 2,
            source.width * fit, source.height * fit);
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(fitted, texture, new Rect(minX, minY, maxX - minX, maxY - minY));
        GUI.color = Color.white;
        return true;
    }
}
