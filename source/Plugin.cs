using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Configuration;
using System.Security.Cryptography;

namespace OldenEra.MapHints;

[BepInPlugin("local.oldenera.maphints", "Olden Era Map Hints", "0.3.7")]
public sealed class Plugin : BasePlugin
{
    internal static Plugin Instance = null!;
    internal ConfigEntry<bool> Enabled = null!;
    internal ConfigEntry<DisplayMode> Mode = null!;
    internal ConfigEntry<int> Size = null!;
    internal ConfigEntry<float> MarkerScale = null!;
    internal ConfigEntry<KeyboardShortcut> CycleKey = null!;
    internal ConfigEntry<bool> Diagnostics = null!;
    internal ConfigEntry<bool> SkillHover = null!;
    internal ConfigEntry<bool> WellPulse = null!;
    internal const string MetadataSha = "99117DC93F4B427967A2FCA49378E1B39B3202401B15234A98328FAA7D489817";
    internal const string AssemblySha = "D9972B0E7F7DD1C17758808E2F1B80E0A728946416D9A41DEAA99BAD744DE53A";

    public override void Load()
    {
        Instance = this;
        Enabled = Config.Bind("Display", "Enabled", true, "Enable the local map overlay.");
        Mode = Config.Bind("Display", "Mode", DisplayMode.All, "All: show used objects dimmed; Available: ready rewards only; Off: hide. Hotkey: yusha144.hotkeys.cfg.");
        Size = Config.Bind("Display", "IconSize", 32, new ConfigDescription("Icon diameter in pixels at 1080p.", new AcceptableValueRange<int>(20, 64)));
        MarkerScale = Config.Bind("Display", "MarkerScale", MarkerSizing.DefaultScale, new ConfigDescription("Scale map markers, their rings, glow and badges together. 0.84 is 20 percent larger than the previous 0.7 scale.", new AcceptableValueRange<float>(0.35f, 1.5f)));
        var visualRevision = Config.Bind("Internal", "VisualRevision", 0, "Tracks one-time migration of the old marker size.");
        if (visualRevision.Value < 1)
        {
            MarkerScale.Value = MarkerSizing.Upgrade(MarkerScale.Value, visualRevision.Value);
            visualRevision.Value = 1;
        }
        CycleKey = HotkeySettings.Load("MapHints");
        Diagnostics = Config.Bind("Diagnostics", "LogStateChanges", false, "Log local hero and visible marker counts, without player names or save contents.");
        SkillHover = Config.Bind("Display", "SkillHoverPreview", true, "Show offered skills only while hovering over a skill building.");
        WellPulse = Config.Bind("Display", "PulseManaWells", true, "Gently pulse mana wells when the selected hero is missing mana.");
        string root = Paths.GameRootPath;
        if (!Matches(Path.Combine(root, "HeroesOldenEra_Data", "il2cpp_data", "Metadata", "global-metadata.dat"), MetadataSha)
            || !Matches(Path.Combine(root, "GameAssembly.dll"), AssemblySha))
        {
            Log.LogWarning("Map Hints disabled: this version supports Steam build 25672315 only. No game patches applied.");
            return;
        }
        AddComponent<HintOverlay>();
        Log.LogInfo($"Map Hints 0.3.7 by yusha144 loaded. Marker scale={MarkerScale.Value}; map legend removed. {CycleKey.Value} cycles All / Available / Off; configure yusha144.hotkeys.cfg.");
    }

    private static bool Matches(string path, string expected)
    {
        try { using var stream = File.OpenRead(path); using var hash = SHA256.Create(); return Convert.ToHexString(hash.ComputeHash(stream)) == expected; }
        catch { return false; }
    }
}
