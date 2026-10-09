namespace OldenEra.MapHints;

internal static class MarkerSizing
{
    internal const float DefaultScale = 0.84f;

    // Upgrade the previously shipped 0.7 scale once. Keep custom values, and allow
    // the user to choose 0.7 again after migration without silently resetting it.
    internal static float Upgrade(float scale, int revision)
        => revision < 1 && Math.Abs(scale - 0.7f) < 0.0001f ? DefaultScale : scale;
}
