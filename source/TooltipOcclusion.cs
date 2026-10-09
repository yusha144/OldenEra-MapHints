using Hex.UI.Tooltips;
using UnityEngine;

namespace OldenEra.MapHints;

internal sealed class TooltipOcclusion
{
    private readonly List<ScreenBounds> _bounds = new();
    private readonly HashSet<IntPtr> _seen = new();
    private readonly Il2CppSystem.Collections.Generic.List<BhTooltip> _tooltips = new();
    private readonly UiOcclusionBounds _geometry = new();
    private float _nextErrorLog;

    internal void Refresh(float scale)
    {
        _bounds.Clear();
        _seen.Clear();
        try
        {
            // Use existing UI objects. The game pools tooltips, including multiple
            // simultaneous descriptions; no show/hide or layout methods are invoked.
            var manager = BhTooltips.cdsk;
            if (!manager || !manager.gameObject.activeInHierarchy) return;
            ReadParent(manager.baseParent, scale);
            ReadParent(manager.overlayParent, scale);
            ReadParent(manager.raycastParent, scale);
        }
        catch (Exception error)
        {
            if (Time.unscaledTime >= _nextErrorLog)
            {
                Plugin.Instance.Log.LogWarning($"Tooltip bounds unavailable: {error.Message}");
                _nextErrorLog = Time.unscaledTime + 15;
            }
        }
    }

    private void ReadParent(RectTransform parent, float scale)
    {
        if (!parent || !parent.gameObject.activeInHierarchy) return;
        _tooltips.Clear();
        parent.GetComponentsInChildren<BhTooltip>(false, _tooltips);
        foreach (var tooltip in _tooltips)
        {
            if (!tooltip || !_seen.Add(tooltip.Pointer)) continue;
            var rect = tooltip.tooltipRect_;
            if (tooltip.cg && tooltip.cg.alpha <= 0.01f) continue;
            // Include the ornamental frame beyond the content rectangle.
            if (_geometry.TryRead(rect, out var bounds)) _bounds.Add(bounds.Expand(8 * scale));
        }
    }

    internal bool Overlaps(Rect rect)
    {
        var bounds = new ScreenBounds(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
        foreach (var tooltip in _bounds)
            if (tooltip.Overlaps(bounds)) return true;
        return false;
    }
}
