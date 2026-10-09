using System.IO.Compression;
using System.Text.Json;
using BepInEx;
using UnityEngine;

namespace OldenEra.MapHints;

// Display metadata only. Offered skills are read from the existing reward controllers;
// this code never generates rewards, advances randomness or opens an interaction.
internal sealed class SkillPreview
{
    internal sealed class Offer
    {
        public string Name;
        public Sprite Icon;
    }
    internal string Title;
    internal string Note;
    internal IntPtr HoverPointer;
    internal readonly List<Offer> Offers = new();
}

internal sealed class SkillCatalog
{
    private sealed class Metadata
    {
        public string Name;
        public string[] Icons;
        public string[] LevelNames;
    }
    private readonly Dictionary<string, Metadata> _skills = new();
    private readonly Dictionary<string, Sprite> _icons = new();

    internal void Load()
    {
        using var zip = ZipFile.OpenRead(Path.Combine(Paths.GameRootPath, "HeroesOldenEra_Data", "StreamingAssets", "Core.zip"));
        // These are base definitions. No static roll tables are used as an offer list.
        foreach (var file in new[] { "skills.json", "pseudo_skills.json" })
        {
            var entry = zip.GetEntry("DB/heroes_skills/skills/" + file);
            if (entry == null) continue;
            using var stream = entry.Open();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            foreach (var row in doc.RootElement.GetProperty("array").EnumerateArray())
            {
                if (!row.TryGetProperty("parametersPerLevel", out var levels)) continue;
                string Text(JsonElement value, string key) => value.TryGetProperty(key, out var item) ? item.GetString() ?? "" : "";
                _skills[Text(row, "id")] = new Metadata {
                    Name = Text(row, "name"),
                    Icons = levels.EnumerateArray().Select(level => Text(level, "icon")).ToArray(),
                    LevelNames = levels.EnumerateArray().Select(level => Text(level, "name")).ToArray()
                };
            }
        }
    }

    internal SkillPreview Read(fmt bank)
    {
        var sid = bank.cowc.sidConfig;
        var preview = new SkillPreview {
            Title = sid switch { "university" => "Университет", "infernal_cirque" => "Зловещий театр", "wise_owl" => "Мудрая сова", _ => "Цирк" },
            HoverPointer = bank.cozc?.Pointer ?? IntPtr.Zero
        };
        var rewards = bank.cowe?.cojj;
        int unresolved = 0;
        if (rewards != null)
        {
            foreach (var reward in rewards)
            {
                var skill = reward?.TryCast<fja>();
                if (skill == null) { unresolved++; continue; }
                string id = skill.cokb;
                if (string.IsNullOrEmpty(id) || !_skills.TryGetValue(id, out var metadata) || metadata.Icons.Length == 0)
                { unresolved++; continue; }
                // Native fja.blfs uses level 3 for Max, level 1 otherwise.
                int level = (int)skill.cokc == 2 ? Math.Min(2, metadata.Icons.Length - 1) : 0;
                string iconId = metadata.Icons[level];
                if (!_icons.TryGetValue(iconId, out var sprite))
                {
                    sprite = cnh.taf()?.tak(iconId);
                    _icons[iconId] = sprite;
                }
                string nameKey = metadata.LevelNames[level];
                string name = string.IsNullOrEmpty(nameKey) ? metadata.Name : nameKey;
                name = it.iap(name);
                preview.Offers.Add(new SkillPreview.Offer { Name = name, Icon = sprite });
            }
        }
        preview.Note = preview.Offers.Count > 0 ? "Предложения здания · условия покупки — при посещении"
            : sid == "circus" ? "Улучшение навыка определяется при посещении"
            : "Конкретные навыки пока не определены игрой";
        if (unresolved > 0 && preview.Offers.Count > 0) preview.Note = "Часть предложений пока не определена игрой";
        return preview;
    }
}
