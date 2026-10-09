namespace OldenEra.MapHints;

public static class InterfaceVisibilityRules
{
    public static bool IsNotificationLayer(string typeName) => typeName is
        "ScSaveNotification" or "BhNotification" or "BhRewardsNotificationView"
        or "BhRewardNotificationHeroStat";

    // Persistent map HUD and local notification layers are allowed. Their occupied
    // area is handled by per-marker UI occlusion, not by hiding the entire map.
    // All other registered screens block the overlay,
    // including future dialogs that are not named here.
    public static bool IsMapHud(string typeName) => typeName is
        "BhHUDSession" or "BhHeroHUDPanel" or "BhResPanel" or "BhSideTimers"
        or "BhSideCitiesUISession" or "BhSideHeroesUISession" or "BhSideMinimapSession"
        or "BhWorldCursorPosition" or "BhQuestMarkersUI" or "BhWinConditionDayTimer"
        or "BhDemoIconTxtUI" or "BhChat" or "BhRewardsNotificationView"
        or "BhRewardNotificationHeroStat" or "ScSaveNotification"
        // Map toggle panel keeps its BhScreen canvas alive even when folded away.
        or "BhMapExtraWindow"
        // Day/astrology/movement notifications animate their own child views.
        or "BhNotification";

    public static bool BlocksMap(string typeName, bool active, bool open,
        bool canvasEnabled, float alpha, bool additionalViewVisible)
        => !IsMapHud(typeName) && active
            && (open || (canvasEnabled && alpha > 0.01f) || additionalViewVisible);
}
