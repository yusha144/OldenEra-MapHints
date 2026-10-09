using Hex.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OldenEra.MapHints;

public sealed partial class HintOverlay
{
    private readonly Dictionary<IntPtr, string> _screenTypes = new();
    private PointerEventData _uiProbe;
    private EventSystem _uiEvents;
    private readonly Il2CppSystem.Collections.Generic.List<RaycastResult> _uiHits = new();
    private string _lastBlockingScreen = "";
    private readonly TooltipOcclusion _tooltipOcclusion = new();
    private readonly NotificationOcclusion _notificationOcclusion = new();

    // Called again from OnGUI: a dialog may open after Update in the same frame.
    // Never rely on the map camera being disabled when a window is shown.
    private bool IsMapCovered()
    {
        try
        {
            var screens = cpo.ccog;
            if (screens == null) return true;
            if (screens.ccoh != null)
                foreach (var entry in screens.ccoh)
                    if (IsBlockingScreen(entry.Value)) return true;
            // Includes temporary/duplicate screens that need not be dictionary entries.
            if (screens.ccoi != null)
                foreach (var screen in screens.ccoi)
                    if (IsBlockingScreen(screen)) return true;
            if (_lastBlockingScreen.Length != 0)
                Plugin.Instance.Log.LogInfo("Overlay returned to adventure map.");
            _lastBlockingScreen = "";
            return false;
        }
        catch (Exception error)
        {
            if (Time.unscaledTime >= _nextErrorLog)
            {
                Plugin.Instance.Log.LogWarning($"Overlay hidden while reading game windows: {error.GetType().Name}: {error.Message}");
                _nextErrorLog = Time.unscaledTime + 15f;
            }
            return true;
        }
    }

    private bool IsBlockingScreen(BhScreen screen)
    {
        if (!screen || !screen.gameObject.activeInHierarchy) return false;
        if (!_screenTypes.TryGetValue(screen.Pointer, out string type))
        {
            // Managed wrappers from the registry are BhScreen; inspect the native type.
            type = screen.GetIl2CppType().Name;
            _screenTypes[screen.Pointer] = type;
        }
        if (InterfaceVisibilityRules.IsMapHud(type)) return false;
        bool bookView = false;
        if (type == "BhMagicBook")
        {
            var book = screen.TryCast<Hex.Session.UI.BhMagicBook>();
            var controller = book?.uiController;
            bookView = controller && controller.gameObject.activeInHierarchy
                && controller.canvasGroup && controller.canvasGroup.alpha > 0.01f;
        }
        bool covered = InterfaceVisibilityRules.BlocksMap(type, true, screen.cszv,
            screen.canvas && screen.canvas.isActiveAndEnabled, screen.cg ? screen.cg.alpha : 1f, bookView);
        if (covered && type != _lastBlockingScreen)
        {
            // One line per change makes user-reported false positives diagnosable.
            Plugin.Instance.Log.LogInfo($"Overlay hidden by game window: {type}");
            _lastBlockingScreen = type;
        }
        return covered;
    }

    private bool OverlapsGameUi(Rect guiRect)
    {
        // Probe the badge's footprint, not the user's mouse: HUD panels cover map
        // objects even when the cursor is elsewhere. These queries dispatch no clicks.
        return _tooltipOcclusion.Overlaps(guiRect) || _notificationOcclusion.Overlaps(guiRect) || HitsGameUi(guiRect.center)
            || HitsGameUi(new Vector2(guiRect.xMin, guiRect.yMin))
            || HitsGameUi(new Vector2(guiRect.xMax, guiRect.yMin))
            || HitsGameUi(new Vector2(guiRect.xMin, guiRect.yMax))
            || HitsGameUi(new Vector2(guiRect.xMax, guiRect.yMax));
    }

    private bool HitsGameUi(Vector2 guiPoint)
    {
        var events = EventSystem.current;
        if (!events) return false;
        if (!_uiEvents || _uiEvents.Pointer != events.Pointer)
        {
            _uiEvents = events;
            _uiProbe = new PointerEventData(events);
        }
        _uiProbe.position = new Vector2(guiPoint.x, Screen.height - guiPoint.y);
        _uiHits.Clear();
        events.RaycastAll(_uiProbe, _uiHits);
        foreach (var hit in _uiHits)
            if (hit.module != null && hit.module.TryCast<GraphicRaycaster>() != null) return true;
        return false;
    }
}
