using Hex.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OldenEra.MapHints;

internal sealed class NotificationOcclusion
{
    private readonly List<ScreenBounds> _bounds = new();
    private readonly HashSet<IntPtr> _seen = new();
    private readonly Il2CppSystem.Collections.Generic.List<Graphic> _graphics = new();
    private readonly UiOcclusionBounds _geometry = new();
    private float _nextErrorLog;

    internal void Refresh(float scale)
    {
        // Rebuild each repaint, including while a notification fades/moves away.
        _bounds.Clear();
        _seen.Clear();
        try
        {
            var screens = cpo.ccog;
            if (screens == null) return;
            if (screens.ccoh != null)
                foreach (var entry in screens.ccoh) ReadScreen(entry.Value, scale);
            if (screens.ccoi != null)
                foreach (var screen in screens.ccoi) ReadScreen(screen, scale);
        }
        catch (Exception error)
        {
            if (Time.unscaledTime >= _nextErrorLog)
            {
                Plugin.Instance.Log.LogWarning($"Notification bounds unavailable: {error.Message}");
                _nextErrorLog = Time.unscaledTime + 15;
            }
        }
    }

    private void ReadScreen(BhScreen screen, float scale)
    {
        if (!screen || !screen.gameObject.activeInHierarchy || !_seen.Add(screen.Pointer)) return;
        if (!InterfaceVisibilityRules.IsNotificationLayer(screen.GetIl2CppType().Name)) return;
        _graphics.Clear();
        screen.GetComponentsInChildren<Graphic>(false, _graphics);
        foreach (var graphic in _graphics)
        {
            if (!graphic || !graphic.isActiveAndEnabled) continue;
            var renderer = graphic.canvasRenderer;
            if (!renderer || renderer.cull) continue;
            // Passive text/images are visible even with raycastTarget=false.
            // Scan artwork, not the persistent full-screen notification container.
            float alpha = graphic.color.a * renderer.GetAlpha();
            if (_geometry.TryRead(graphic.rectTransform, out var bounds, alpha))
                _bounds.Add(bounds.Expand(8 * scale));
        }
    }

    internal bool Overlaps(Rect rect)
    {
        var bounds = new ScreenBounds(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
        return _bounds.Any(notification => notification.Overlaps(bounds));
    }
}
