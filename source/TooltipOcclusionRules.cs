namespace OldenEra.MapHints;

internal readonly record struct ScreenBounds(float Left, float Top, float Right, float Bottom)
{
    internal bool IsValid => float.IsFinite(Left) && float.IsFinite(Top)
        && float.IsFinite(Right) && float.IsFinite(Bottom) && Right > Left && Bottom > Top;

    internal bool Overlaps(ScreenBounds other) => IsValid && other.IsValid
        && Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    internal ScreenBounds Expand(float padding) => new(Left - padding, Top - padding, Right + padding, Bottom + padding);
}

internal static class TooltipOcclusionRules
{
    // Neither raycastTarget nor blocksRaycasts determines whether text is visible.
    internal static bool IsVisible(bool active, bool canvasEnabled, float alpha)
        => active && canvasEnabled && alpha > 0.01f;
}
