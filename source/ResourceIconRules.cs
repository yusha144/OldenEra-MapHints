namespace OldenEra.MapHints;

internal static class ResourceIconRules
{
    internal static bool IsResource(string id) => id is "gold" or "wood" or "ore" or "gemstones" or "crystals" or "mercury" or "dust";

    internal static string[] Resolve(IEnumerable<string> rewards, string objectId)
    {
        var ids = (rewards ?? Array.Empty<string>()).Where(IsResource).Distinct().ToArray();
        if (ids.Length != 0) return ids;
        // Fixed storage type remains meaningful even if a consumed reward set was cleared.
        if (objectId != null && objectId.StartsWith("storage_", StringComparison.Ordinal))
        {
            string id = objectId.Substring("storage_".Length);
            if (IsResource(id)) return new[] { id };
        }
        // Random and choice rewards have no guessed resource when their data is unavailable.
        return Array.Empty<string>();
    }

    internal static string Fallback(string id) => id switch
    {
        "gold" => "З", "wood" => "Д", "ore" => "Р", "gemstones" => "С",
        "crystals" => "К", "mercury" => "М", "dust" => "П", _ => "?"
    };
}
