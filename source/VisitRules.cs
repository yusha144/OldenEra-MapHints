namespace OldenEra.MapHints;

public enum HintState { Available, Used, Requirements, Unknown }
public enum DisplayMode { All, Available, Off }

public static class VisitRules
{
    // Values come from EVisitType. Never infer an unknown value as available.
    public static bool? CanVisit(int visitType, IReadOnlyCollection<int> visitors, int? heroId, int? sideId)
    {
        if (visitors is null) return null;
        return visitType switch {
            0 => visitors.Count == 0,
            1 when sideId.HasValue => !visitors.Contains(sideId.Value),
            2 when heroId.HasValue => !visitors.Contains(heroId.Value),
            3 => true,
            _ => null
        };
    }

    public static HintState Classify(bool? visitAvailable, bool? rewardAvailable, bool rewardAlreadyActive = false)
        => visitAvailable switch { false => HintState.Used, true when rewardAlreadyActive => HintState.Used, true => rewardAvailable switch {
            true => HintState.Available, false => HintState.Requirements, _ => HintState.Unknown
        }, _ => HintState.Unknown };

    public static bool ShouldDisplay(HintState state, DisplayMode mode)
        => mode != DisplayMode.Off && state != HintState.Unknown
            && (mode == DisplayMode.All || state == HintState.Available);

    public static bool ShouldDisplayWell(DisplayMode mode) => mode != DisplayMode.Off;

    public static bool NeedsMana(int current, int maximum) => maximum > 0 && current < maximum;

    public static bool IsSkillBuilding(string sid) => sid is "university" or "infernal_cirque" or "wise_owl" or "circus";
}
