using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace OldenEra.MapHints;

internal sealed class UiOcclusionBounds
{
    private readonly Il2CppSystem.Collections.Generic.List<CanvasGroup> _groups = new();
    private readonly Il2CppStructArray<Vector3> _corners = new(4);

    internal bool TryRead(RectTransform rect, out ScreenBounds bounds, float alpha = 1)
    {
        bounds = default;
        if (!rect || !rect.gameObject.activeInHierarchy) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        if (!canvas) return false;
        var rootCanvas = canvas.rootCanvas;
        bool canvasEnabled = canvas.isActiveAndEnabled && rootCanvas && rootCanvas.isActiveAndEnabled;
        _groups.Clear();
        rect.GetComponentsInParent<CanvasGroup>(false, _groups);
        foreach (var group in _groups)
        {
            if (!group || !group.enabled) continue;
            alpha *= group.alpha;
            if (group.ignoreParentGroups) break;
        }
        if (!TooltipOcclusionRules.IsVisible(true, canvasEnabled, alpha)) return false;
        Camera camera = null;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera ? canvas.worldCamera : rootCanvas.worldCamera;
            if (!camera) return false;
        }
        rect.GetWorldCorners(_corners);
        float left = float.PositiveInfinity, top = float.PositiveInfinity;
        float right = float.NegativeInfinity, bottom = float.NegativeInfinity;
        for (int index = 0; index < 4; index++)
        {
            var point = RectTransformUtility.WorldToScreenPoint(camera, _corners[index]);
            float y = Screen.height - point.y;
            left = Math.Min(left, point.x); right = Math.Max(right, point.x);
            top = Math.Min(top, y); bottom = Math.Max(bottom, y);
        }
        bounds = new ScreenBounds(left, top, right, bottom);
        return bounds.IsValid;
    }
}
