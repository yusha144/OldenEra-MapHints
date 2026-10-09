using System.Reflection;
using System.Text.Json;
using Hex;
using Hex.Session.Hud;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OldenEra.MapHints;

public sealed partial class HintOverlay : MonoBehaviour
{
    public HintOverlay(IntPtr pointer) : base(pointer) { }
    private sealed class Definition
    {
        public string Category { get; set; } = "Reward";
        public string Reset { get; set; } = "None";
        public string Visit { get; set; } = "";
    }
    private sealed class Marker
    {
        public readonly int Id;
        public readonly Vector3 Position;
        public readonly string Category, Reset;
        public readonly HintState State;
        public readonly MarkerGlyph[] Glyphs;
        public Marker(int id, Vector3 position, string category, string reset, HintState state, MarkerGlyph[] glyphs = null)
        { Id=id; Position=position; Category=category; Reset=reset; State=state; Glyphs=glyphs; }
    }
    private readonly List<Marker> _markers = new();
    private Dictionary<string, Definition> _catalog = new();
    private Camera _camera;
    private IntPtr _world;
    private float _nextRefresh;
    private float _nextErrorLog;
    private int _lastHero = -1;
    private string _lastSummary = "";
    private Texture2D _disc;
    private GUIStyle _iconStyle, _smallStyle, _tooltipStyle, _choiceStyle;
    private bool _mapActive;
    private readonly SkillCatalog _skills = new();
    private readonly ResourceIcons _resourceIcons = new();
    private readonly BuffIcons _buffIcons = new();
    private readonly StatIcons _statIcons = new();
    private SkillPreview _skillPreview;
    private IntPtr _hoverPointer;
    private float _hoverSince;

    public void Awake()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("MapHints.object-catalog.json")
            ?? asm.GetManifestResourceStream("OldenEra.MapHints.object-catalog.json")
            ?? asm.GetManifestResourceStream(asm.GetManifestResourceNames().Single(n => n.EndsWith("object-catalog.json")))!;
        _catalog = JsonSerializer.Deserialize<Dictionary<string, Definition>>(stream)!;
        try { _skills.Load(); }
        catch (Exception error) { Plugin.Instance.Log.LogWarning($"Skill preview metadata unavailable: {error.Message}"); }
        Plugin.Instance.Log.LogInfo($"Catalog ready: {_catalog.Count} object types. Waiting for an adventure map.");
    }

    public void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                var mode = Plugin.Instance.Mode;
                mode.Value = (DisplayMode)(((int)mode.Value + 1) % 3);
                _nextRefresh = 0;
            }
            if (!Plugin.Instance.Enabled.Value) { Clear(); return; }
            var current = fbu.me;
            if (!current || !current.gameObject.activeInHierarchy) { Clear(); return; }
            if (current.Pointer != _world) { Clear(); _screenTypes.Clear(); _resourceIcons.Clear(); _buffIcons.Clear(); _statIcons.Clear(); _world = current.Pointer; _nextRefresh = 0; }
            if (IsMapCovered()) { Clear(); _nextRefresh = 0; return; }
            var hoverPointer = ReadHover(current);
            if (hoverPointer != _hoverPointer)
            {
                _hoverPointer = hoverPointer;
                _hoverSince = Time.unscaledTime;
                _skillPreview = null;
                _nextRefresh = 0;
            }
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.20f;
            Refresh(current);
        }
        catch (Exception error)
        {
            Clear();
            if (Time.unscaledTime >= _nextErrorLog)
            {
                Plugin.Instance.Log.LogWarning($"Map overlay paused: {error.GetType().Name}: {error.Message}");
                _nextErrorLog = Time.unscaledTime + 15f;
            }
        }
    }

    private void Clear()
    {
        _markers.Clear();
        _camera = null;
        _mapActive = false;
        _lastHero = -1;
        _skillPreview = null;
        _hoverPointer = IntPtr.Zero;
    }

    private void Refresh(fbu world)
    {
        _markers.Clear();
        _mapActive = false;
        _skillPreview = null;
        var worldCamera = BhWorldCamera.btbj;
        var hud = BhHeroHUDPanel.me;
        if (!worldCamera || !hud || !hud.gameObject.activeInHierarchy) return;
        if (hud.cg && hud.cg.alpha < 0.05f) return;
        var hudHero = hud.cghv;
        if (hudHero == null || hudHero.cnsk == null) return;
        // cpak is the hero used by the game's own per-hero tooltip query.
        var hero = hudHero.cnsk.cpak;
        if (hero == null || hero.Pointer != hudHero.Pointer) return;
        var dataHero = hero.cuaa;
        if (dataHero == null || dataHero.inBattle_) return;
        var side = hero.cnsk.cudy;
        if (side == null || side.index != dataHero.sideId) return;
        var visibility = side.fogData?.visibilityMap;
        if (visibility == null) return;
        _camera = worldCamera.targetCamera_;
        if (!_camera || !_camera.enabled) return;
        _mapActive = true;
        _lastHero = dataHero.id;
        if (Plugin.Instance.Mode.Value == DisplayMode.Off) return;
        var registry = world.cnmf?.cozn;
        if (registry == null) return;
        int available = 0, used = 0, requirements = 0;
        foreach (var pair in registry)
        {
            var bank = pair.Value?.TryCast<fmt>();
            if (bank == null) continue;
            var data = bank.cowc;
            if (data == null || data.released || data.inBattle) continue;
            bool skillBuilding = VisitRules.IsSkillBuilding(data.sidConfig);
            if (!_catalog.TryGetValue(data.sidConfig, out var definition) && !skillBuilding) continue;
            var mapObject = bank.coyw;
            if (mapObject == null) continue;
            var node = mapObject.bzrc;
            if (node < 0 || node >= visibility.Length || !visibility[node]) continue;
            var behaviour = mapObject.bzro;
            if (!behaviour || !behaviour.gameObject.activeInHierarchy) continue;
            var anchor = behaviour.lookPoint ? behaviour.lookPoint : behaviour.transform;
            Vector3 position = anchor.position;
            var viewport = _camera.WorldToViewportPoint(position);
            if (viewport.z <= 0 || viewport.x < -0.05f || viewport.x > 1.05f || viewport.y < -0.05f || viewport.y > 1.05f) continue;
            if (skillBuilding)
            {
                if (Plugin.Instance.SkillHover.Value && _hoverPointer != IntPtr.Zero && bank.cozc?.Pointer == _hoverPointer)
                {
                    try { _skillPreview = _skills.Read(bank); }
                    catch (Exception error) { LogPreviewError(error); }
                }
                continue; // No permanent icons for skill buildings, in any display mode.
            }
            if (definition.Category == "Mana")
            {
                int currentMana = dataHero.mana;
                int maxMana = hero.cuab.bjgh();
                bool needsMana = VisitRules.NeedsMana(currentMana, maxMana);
                if (VisitRules.ShouldDisplayWell(Plugin.Instance.Mode.Value))
                    _markers.Add(new Marker(data.idMapObject, position, "Mana", "None",
                        needsMana ? HintState.Available : HintState.Used));
                continue;
            }
            // Both methods are read-only eligibility queries verified in build 25672315.
            // The game owns visitor lists and resets them on day/week change and save load.
            var nativeVisitors = data.visitorsList;
            if (nativeVisitors == null) continue;
            var visitors = new int[nativeVisitors.Count];
            for (int i = 0; i < visitors.Length; i++) visitors[i] = nativeVisitors[i];
            bool? policyResult = VisitRules.CanVisit((int)bank.cowd.cbvd, visitors, dataHero.id, side.index);
            if (!policyResult.HasValue || policyResult.Value != bank.bmdw(hero)) continue;
            bool visitAvailable = policyResult.Value;
            bool rewardAvailable = visitAvailable && bank.bmbk(hero);
            bool rewardAlreadyActive = visitAvailable && definition.Category == "Buff"
                && BuffAvailability.AllRewardsAlreadyActive(bank, dataHero);
            var state = VisitRules.Classify(visitAvailable, rewardAvailable, rewardAlreadyActive);
            if (state == HintState.Available) available++;
            else if (state == HintState.Used) used++;
            else requirements++;
            if (VisitRules.ShouldDisplay(state, Plugin.Instance.Mode.Value))
                _markers.Add(new Marker(data.idMapObject, position, definition.Category, definition.Reset, state,
                    definition.Category switch {
                        "Resources" => _resourceIcons.Read(data),
                        "Buff" => _buffIcons.Read(bank),
                        "Stats" => _statIcons.Read(data),
                        _ => null
                    }));
        }
        string summary = $"Hero {_lastHero}: available={available}, used={used}, requirements={requirements}, drawn={_markers.Count}";
        if (summary != _lastSummary && Plugin.Instance.Diagnostics.Value)
            Plugin.Instance.Log.LogInfo(summary);
        _lastSummary = summary;
    }

    public void OnGUI()
    {
        if (!_mapActive || !_camera || !_camera.enabled || !Plugin.Instance.Enabled.Value) return;
        if (Event.current == null || Event.current.type != EventType.Repaint) return;
        if (IsMapCovered()) { Clear(); _nextRefresh = 0; return; }
        try
        {
            EnsureStyles();
            float scale = Math.Clamp(Screen.height / 1080f, 0.8f, 2f);
            // Read current tooltip geometry at repaint, not from the 0.2 s map cache.
            _tooltipOcclusion.Refresh(scale);
            _notificationOcclusion.Refresh(scale);
            float markerScale = scale * Plugin.Instance.MarkerScale.Value;
            float size = Plugin.Instance.Size.Value * markerScale;
            var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            foreach (var marker in _markers)
            {
                var screen = _camera.WorldToScreenPoint(marker.Position);
                if (screen.z <= 0) continue;
                float markerSize = marker.Category == "Mana" ? size * 1.35f : size;
                int count = Math.Max(1, marker.Glyphs?.Length ?? 0);
                bool alternatives = count > 1 && marker.Glyphs.Any(glyph => glyph.AlternativeGroup >= 0);
                float gap = alternatives ? markerSize * 0.5f : 4 * markerScale;
                float width = count * markerSize + (count - 1) * gap;
                var rect = new Rect(screen.x - width / 2, Screen.height - screen.y - markerSize - 7 * markerScale, width, markerSize);
                if (rect.xMax < 0 || rect.x > Screen.width || rect.yMax < 0 || rect.y > Screen.height) continue;
                float margin = (marker.Category == "Mana" ? 10 : 4) * markerScale;
                var footprint = new Rect(rect.x - margin, rect.y - margin, rect.width + 2 * margin,
                    rect.height + margin + (marker.Category == "Mana" ? 17 : 4) * markerScale);
                if (OverlapsGameUi(footprint)) continue;
                if (marker.Category == "Mana") DrawManaWell(rect, marker.State == HintState.Available, markerScale);
                else
                {
                    for (int index = 0; index < count; index++)
                    {
                        var iconRect = new Rect(rect.x + index * (markerSize + gap), rect.y, markerSize, markerSize);
                        DrawMarker(iconRect, marker, markerScale, marker.Glyphs?[index], index == count - 1);
                        if (alternatives && index > 0 && marker.Glyphs[index].AlternativeGroup != marker.Glyphs[index - 1].AlternativeGroup)
                            DrawChoiceSeparator(new Rect(iconRect.x - gap, rect.y, gap, markerSize), marker.State, markerScale);
                    }
                }
            }
            if (_skillPreview != null && Plugin.Instance.Mode.Value != DisplayMode.Off
                && Time.unscaledTime - _hoverSince >= 0.18f && ReadHover(fbu.me) == _skillPreview.HoverPointer)
                DrawSkillPreview(_skillPreview, mouse, scale);
            GUI.color = Color.white;
            if (Plugin.Instance.Legend.Value)
            {
                string mode = Plugin.Instance.Mode.Value switch { DisplayMode.All => "все", DisplayMode.Available => "доступные", _ => "выкл." };
                _smallStyle!.fontSize = Mathf.RoundToInt(12 * scale);
                var legendRect = new Rect(16 * scale, 90 * scale, 290 * scale, 24 * scale);
                if (!OverlapsGameUi(legendRect)) GUI.Label(legendRect, $"Подсветка: {mode}   ·   F8", _smallStyle);
            }
            GUI.color = Color.white;
        }
        catch (Exception error)
        {
            if (Time.unscaledTime >= _nextErrorLog)
            {
                Plugin.Instance.Log.LogWarning($"Map drawing skipped: {error.Message}");
                _nextErrorLog = Time.unscaledTime + 15f;
            }
        }
    }

    private void EnsureStyles()
    {
        if (_disc) return;
        _disc = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float distance = MathF.Sqrt(MathF.Pow(x - 31.5f, 2) + MathF.Pow(y - 31.5f, 2));
            _disc.SetPixel(x, y, new Color(1, 1, 1, Math.Clamp(31.5f - distance, 0, 1)));
        }
        _disc.Apply();
        _iconStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, padding = new RectOffset { left=0, right=0, top=0, bottom=0 } };
        _choiceStyle = new GUIStyle(_iconStyle);
        _smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
        _smallStyle.normal.textColor = new Color(0.92f, 0.91f, 0.84f);
        _tooltipStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true };
        _tooltipStyle.normal.textColor = Color.white;
    }

    private void DrawChoiceSeparator(Rect rect, HintState state, float scale)
    {
        // A full-height, outlined gold slash stays legible over detailed terrain.
        // Its own style prevents the +1/+2 badge font from shrinking the separator.
        _choiceStyle.fontSize = Mathf.RoundToInt(rect.height * 0.85f);
        float outline = Math.Max(1f, 1.1f * scale);
        GUI.color = Color.white;
        _choiceStyle.normal.textColor = new Color(0.035f, 0.045f, 0.055f, state == HintState.Used ? 0.7f : 0.98f);
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (x != 0 || y != 0)
                    GUI.Label(new Rect(rect.x + x * outline, rect.y + y * outline, rect.width, rect.height), "/", _choiceStyle);
        _choiceStyle.normal.textColor = state switch {
            HintState.Used => new Color(0.64f, 0.65f, 0.67f, 0.65f),
            HintState.Requirements => new Color(0.92f, 0.54f, 0.25f, 0.95f),
            _ => new Color(1f, 0.83f, 0.37f)
        };
        GUI.Label(rect, "/", _choiceStyle);
    }

    private void DrawMarker(Rect rect, Marker marker, float scale, MarkerGlyph glyph, bool showReset)
    {
        Color accent = marker.Category switch {
            "Experience" => new Color(0.49f,0.81f,1f), "Stats" => new Color(1f,0.83f,0.37f),
            "Magic" => new Color(0.81f,0.63f,1f), "Buff" => new Color(0.40f,0.91f,0.70f),
            _ => new Color(1f,0.79f,0.39f)
        };
        if (marker.State == HintState.Used) accent = new Color(0.54f,0.56f,0.59f,0.58f);
        else if (marker.State == HintState.Requirements) accent = new Color(0.92f,0.54f,0.25f,0.85f);
        float ring = 2f * scale;
        if (marker.State == HintState.Available)
        {
            GUI.color = new Color(accent.r,accent.g,accent.b,0.17f);
            GUI.DrawTexture(new Rect(rect.x - ring * 2,rect.y - ring * 2,rect.width + ring * 4,rect.height + ring * 4), _disc!);
        }
        GUI.color = accent;
        GUI.DrawTexture(rect, _disc!);
        GUI.color = new Color(0.075f,0.10f,0.13f, marker.State == HintState.Used ? 0.68f : 0.97f);
        GUI.DrawTexture(new Rect(rect.x+ring,rect.y+ring,rect.width-ring*2,rect.height-ring*2), _disc!);
        float inset = 3 * scale;
        var iconArea = new Rect(rect.x + inset, rect.y + inset, rect.width - 2 * inset, rect.height - 2 * inset);
        Color tint = marker.State == HintState.Used ? new Color(0.65f, 0.65f, 0.65f, 0.58f) : Color.white;
        if (glyph == null || !DrawSpriteIcon(iconArea, glyph.Icon, tint))
        {
            string symbol = glyph?.Fallback ?? (marker.Category switch { "Experience" => "XP", "Stats" => "↑", "Magic" => "S", "Buff" => "+", "Resources" => "?", _ => "!" });
            _iconStyle!.fontSize = Mathf.RoundToInt(rect.height * 0.41f);
            _iconStyle.normal.textColor = accent;
            GUI.color = Color.white;
            GUI.Label(rect, symbol, _iconStyle);
        }
        if (!string.IsNullOrEmpty(glyph?.Badge))
        {
            var badge = new Rect(rect.xMax - 17 * scale, rect.yMax - 10 * scale, 21 * scale, 14 * scale);
            GUI.color = new Color(0.075f, 0.10f, 0.13f, 0.96f);
            GUI.DrawTexture(badge, Texture2D.whiteTexture);
            GUI.color = Color.white;
            _iconStyle.fontSize = Mathf.RoundToInt(10 * scale);
            _iconStyle.normal.textColor = accent;
            GUI.Label(badge, glyph.Badge, _iconStyle);
        }
        if (showReset && marker.Reset is "OnStartWeek" or "OnStartDay")
        {
            var badge = new Rect(rect.xMax - 10*scale,rect.yMax - 10*scale,14*scale,14*scale);
            GUI.color = new Color(0.1f,0.13f,0.16f,0.95f);
            GUI.DrawTexture(badge, _disc!);
            GUI.color = Color.white;
            _iconStyle.fontSize = Mathf.RoundToInt(10 * scale);
            GUI.Label(badge, marker.Reset == "OnStartWeek" ? "7" : "1", _iconStyle);
        }
    }

    public void OnDestroy() { if (_disc) UnityEngine.Object.Destroy(_disc); if (_drop) UnityEngine.Object.Destroy(_drop); }
}
