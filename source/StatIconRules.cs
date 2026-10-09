using System.Globalization;

namespace OldenEra.MapHints;

internal sealed class StatBonus
{
    internal readonly string Id;
    internal readonly int Amount;
    internal StatBonus(string id, int amount) { Id = id; Amount = amount; }
}

internal static class StatIconRules
{
    // DB/stats/stats_info.json uses each of these IDs as its native sprite ID.
    internal static StatBonus[] ReadPairs(IReadOnlyList<string> parameters)
    {
        if (parameters == null || parameters.Count % 2 != 0) return Array.Empty<StatBonus>();
        var bonuses = new List<StatBonus>();
        for (int index = 0; index < parameters.Count; index += 2)
        {
            string id = parameters[index];
            if (id is not ("offence" or "defence" or "spellPower" or "intelligence" or "luck" or "moral")) continue;
            if (int.TryParse(parameters[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0)
                bonuses.Add(new StatBonus(id, value));
        }
        return bonuses.ToArray();
    }

    internal static string Fallback(string id) => id switch {
        "offence" => "А", "defence" => "З", "spellPower" => "М", "intelligence" => "Зн",
        "luck" => "У", "moral" => "Д", _ => "↑"
    };
}
