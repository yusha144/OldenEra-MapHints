using OldenEra.MapHints;
int checks = 0;
void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
var altarA = new List<int>();
var altarB = new List<int>();
Check(VisitRules.CanVisit(2, altarA, 101, 1) == true, "Hero 1 sees fresh altar");
altarA.Add(101);
Check(VisitRules.CanVisit(2, altarA, 101, 1) == false, "Hero 1 does not see consumed reward");
Check(VisitRules.CanVisit(2, altarA, 102, 1) == true, "Hero 2 still sees same altar");
Check(VisitRules.CanVisit(2, altarB, 101, 1) == true, "Same type at another location stays available");
int[] loadedVisitors = [101];
Check(VisitRules.CanVisit(2, loadedVisitors, 101, 1) == false, "Loaded save restores visit without mod history");
var weekly = new List<int> { 101 };
Check(VisitRules.CanVisit(0, weekly, 102, 1) == false, "Weekly shared stock is depleted for everyone");
Check(VisitRules.CanVisit(0, weekly, 303, 2) == false, "Shared stock is depleted for other sides");
weekly.Clear();
Check(VisitRules.CanVisit(0, weekly, 102, 1) == true, "Game resets weekly list and reward becomes available");
var stables = new List<int> { 101 };
Check(VisitRules.CanVisit(2, stables, 102, 1) == true, "Weekly hero buff is per hero");
stables.Clear();
Check(VisitRules.CanVisit(2, stables, 101, 1) == true, "Weekly hero buff resets for original hero");
Check(VisitRules.CanVisit(1, new[] { 1 }, 102, 1) == false, "Side-specific visits are not hero-specific");
Check(VisitRules.CanVisit(1, new[] { 1 }, 303, 2) == true, "Other side can visit side-specific object");
Check(VisitRules.CanVisit(2, altarA, null, 1) is null, "No selected hero means unknown");
Check(VisitRules.CanVisit(55, [], 101, 1) is null, "Unknown visit rule fails closed");
Check(VisitRules.CanVisit(2, null, 101, 1) is null, "Missing state fails closed");
Check(VisitRules.Classify(true, false) == HintState.Requirements, "Unaffordable/ineligible reward is distinct from used");
Check(VisitRules.Classify(false, false) == HintState.Used, "Used state takes precedence");
Check(!VisitRules.ShouldDisplay(HintState.Unknown, DisplayMode.All), "Unknown state is never shown as available");
Check(!VisitRules.ShouldDisplay(HintState.Used, DisplayMode.Available), "Available-only mode hides used");
Check(VisitRules.ShouldDisplay(HintState.Used, DisplayMode.All), "All mode permits dimmed used icons");
foreach (HintState state in Enum.GetValues<HintState>()) Check(!VisitRules.ShouldDisplay(state, DisplayMode.Off), "Off hides every state");
Check(VisitRules.NeedsMana(15, 80), "Mana well is useful to a depleted hero");
Check(!VisitRules.NeedsMana(80, 80), "Full mana stops well pulse");
Check(!VisitRules.NeedsMana(100, 80), "Over-cap mana is not treated as depleted");
Check(!VisitRules.NeedsMana(0, 0), "Zero mana capacity does not pulse");
Check(VisitRules.ShouldDisplayWell(DisplayMode.Available), "Well remains discoverable in available-only mode even when full");
Check(VisitRules.ShouldDisplayWell(DisplayMode.All), "Well remains visible in all mode");
Check(!VisitRules.ShouldDisplayWell(DisplayMode.Off), "Off hides the special well beacon");
foreach (string id in new[] { "university", "infernal_cirque", "wise_owl", "circus" })
    Check(VisitRules.IsSkillBuilding(id), $"{id} uses hover-only skills and never a permanent marker");
Check(!VisitRules.IsSkillBuilding("learning_stone"), "XP altar keeps its existing marker behavior");
Check(!VisitRules.IsSkillBuilding("altar_of_magic_1"), "Spell altar is not confused with a skill vendor");
// Regression: the map and hero HUD remain active behind the magic book and menus.
foreach (string window in new[] { "BhMagicBook", "ScEscMenu", "ScSettings", "BhHeroProfileWindow", "BhItemMarketUISession", "NewDialog" })
    Check(InterfaceVisibilityRules.BlocksMap(window, true, true, false, 0f, false), $"{window} hides every overlay even before its fade-in");
foreach (string hud in new[] { "BhHeroHUDPanel", "BhHUDSession", "BhResPanel", "BhSideHeroesUISession", "BhSideMinimapSession" })
    Check(!InterfaceVisibilityRules.BlocksMap(hud, true, true, true, 1f, false), $"Normal {hud} does not hide all map hints");
// User log: these persistent/local overlays were mistaken for modal windows.
foreach (string panel in new[] { "BhMapExtraWindow", "BhNotification" })
{
    Check(!InterfaceVisibilityRules.BlocksMap(panel, true, false, true, 1f, false), $"{panel}'s active canvas does not hide the entire adventure map");
    Check(!InterfaceVisibilityRules.BlocksMap(panel, true, true, true, 1f, false), $"Showing local {panel} still leaves unobscured map hints visible");
}
Check(InterfaceVisibilityRules.BlocksMap("ScEscMenu", true, false, true, 0.4f, false), "Closing animation still covers the map");
Check(!InterfaceVisibilityRules.BlocksMap("ScEscMenu", true, false, false, 1f, false), "Hidden pooled window with disabled canvas does not block");
Check(!InterfaceVisibilityRules.BlocksMap("ScEscMenu", false, true, true, 1f, false), "Inactive window cannot block despite stale flags");
Check(!InterfaceVisibilityRules.BlocksMap("ScEscMenu", true, false, true, 0f, false), "Fully transparent closed window does not block");
Check(InterfaceVisibilityRules.BlocksMap("BhMagicBook", true, false, false, 0f, true), "Book's separate view covers its close transition");
Check(!InterfaceVisibilityRules.BlocksMap("BhMagicBook", true, false, false, 0f, false), "Closed book permits the overlay again");
bool cover(bool book, bool menu) => InterfaceVisibilityRules.BlocksMap("BhMagicBook", true, book, false, 0f, false)
    || InterfaceVisibilityRules.BlocksMap("ScEscMenu", true, menu, false, 0f, false);
Check(!cover(false, false), "Bare map permits rendering");
Check(cover(true, false), "Window opened after a map update blocks the next render");
Check(cover(true, true), "Stacked book and menu block rendering");
Check(cover(true, false), "Closing only the top window must not restore markers");
Check(!cover(false, false), "Closing the last window restores map rendering");
Check(ResourceIconRules.Resolve(new[] { "wood", "ore" }, "village").SequenceEqual(new[] { "wood", "ore" }), "Village keeps both offered resource types");
Check(ResourceIconRules.Resolve(new[] { "gemstones", "mercury", "crystals" }, "gingerbread_house").Length == 3, "Choice of three rare resources does not collapse to one coin");
foreach (string id in new[] { "windmill", "gardener" })
    foreach (string rolledResource in new[] { "gold", "wood", "ore", "gemstones", "crystals", "mercury" })
        Check(ResourceIconRules.Resolve(new[] { rolledResource }, id).Length == 0,
            $"{id} never exposes {rolledResource}, including after a weekly reroll");
Check(ResourceIconRules.Resolve(new[] { "gold", "gold", "hero_exp" }, "storage_gold").SequenceEqual(new[] { "gold" }), "Repeated fixed resource entries do not add duplicate icons; non-resources are excluded");
Check(ResourceIconRules.Resolve(null, "storage_ore").Single() == "ore", "Consumed ore storage retains its correct identity");
Check(ResourceIconRules.Resolve(null, "windmill").Length == 0, "Missing random reward never guesses gold or another resource");
Check(ResourceIconRules.Resolve(new[] { "unrecognized" }, "storage_unrecognized").Length == 0, "Unsupported resource falls back without a false resource icon");
Check(RewardKnowledgeRules.KnowsSkills(new[] { 1 }, 1), "One player's acquaintance is shared by all of their heroes");
Check(!RewardKnowledgeRules.KnowsSkills(new[] { 1 }, 2), "Another player's visit does not reveal skills");
Check(!RewardKnowledgeRules.KnowsSkills(Array.Empty<int>(), 1), "Visible but unvisited building keeps skills hidden");
Check(!RewardKnowledgeRules.KnowsSkills(null, 1), "Missing acquaintance data fails closed");
Check(!RewardKnowledgeRules.KnowsSkills(new[] { -1 }, -1), "Missing player identity fails closed");
int[] savedAcquaintance = System.Text.Json.JsonSerializer.Deserialize<int[]>("[1,3]")!;
Check(RewardKnowledgeRules.KnowsSkills(savedAcquaintance, 1) && RewardKnowledgeRules.KnowsSkills(savedAcquaintance, 3), "Loaded native knowledge restores each acquainted player's offers");
Check(!RewardKnowledgeRules.KnowsSkills(new int[0], 1), "Loading an earlier save or new match cannot inherit prior knowledge");
Check(!RewardKnowledgeRules.IsRandomResource("village") && !RewardKnowledgeRules.IsRandomResource("gingerbread_house"), "Player choices remain distinct from random outcomes");
// Approximate bounds from the two user screenshots: text panels cover multiple
// objects while nearby map markers must stay visible. Raycastability is irrelevant.
var goldDescription = new ScreenBounds(58, 42, 540, 344);
Check(goldDescription.Overlaps(new ScreenBounds(131, 166, 165, 200)), "Gold marker under description text is hidden");
Check(!goldDescription.Overlaps(new ScreenBounds(284, 403, 320, 440)), "Gold marker below description remains visible");
var stoneDescription = new ScreenBounds(16, 24, 497, 358);
Check(stoneDescription.Overlaps(new ScreenBounds(401, 141, 439, 180)), "Buff marker under the stone description is hidden");
Check(stoneDescription.Overlaps(new ScreenBounds(326, 302, 362, 339)), "Unrelated XP marker under the same description is also hidden");
Check(!stoneDescription.Overlaps(new ScreenBounds(241, 417, 280, 456)), "Buff marker on the unobscured map remains visible");
Check(goldDescription.Overlaps(new ScreenBounds(530, 170, 550, 190)), "Partial edge overlap is enough to hide a marker");
Check(goldDescription.Overlaps(new ScreenBounds(1, 180, 590, 183)), "Thin crossing overlap is detected even when point probes miss it");
Check(goldDescription.Expand(8).Overlaps(new ScreenBounds(540, 170, 544, 174)), "Ornamental border is protected too");
Check(!new ScreenBounds(0, 0, 0, 100).Overlaps(goldDescription), "Unlaid-out pooled tooltip cannot hide markers");
Check(!new ScreenBounds(float.NaN, 0, 100, 100).Overlaps(goldDescription), "Invalid screen geometry is ignored");
Check(TooltipOcclusionRules.IsVisible(true, true, 0.3f), "Tooltip fade-out still protects text");
Check(!TooltipOcclusionRules.IsVisible(true, true, 0f), "Invisible pooled tooltip does not suppress the map");
Check(!TooltipOcclusionRules.IsVisible(false, true, 1f), "Inactive tooltip is ignored despite stale opacity");
Check(!TooltipOcclusionRules.IsVisible(true, false, 1f), "Disabled canvas does not hide markers");
Check(Math.Abs(MarkerSizing.Upgrade(0.7f, 0) / 0.7f - 1.2f) < 0.0001f, "Upgrade increases current size by exactly twenty percent");
Check(MarkerSizing.Upgrade(0.84f, 1) == 0.84f, "Restart does not enlarge markers again");
Check(MarkerSizing.Upgrade(0.7f, 1) == 0.7f, "User may choose a smaller size after migration");
Check(MarkerSizing.Upgrade(1f, 0) == 1f, "Custom marker size is preserved");
Check(MarkerSizing.Upgrade(MarkerSizing.DefaultScale, 0) == MarkerSizing.DefaultScale, "New installation uses the requested size directly");
// Beer fountain: visitors reset daily, LeftNew luck bonus lasts until next week.
BuffOffer[] beerReward = [new("beer_fountain_buff", 1, [1])];
var heroOneBuffs = new List<ActiveHeroBuff>();
var beerVisitors = new List<int>();
HintState BeerState(int heroId, IReadOnlyList<ActiveHeroBuff> activeBuffs) => VisitRules.Classify(
    VisitRules.CanVisit(2, beerVisitors, heroId, 1), true,
    BuffHintRules.AllRewardsAlreadyActive(beerReward, activeBuffs, false));
Check(BeerState(101, heroOneBuffs) == HintState.Available, "Fresh fountain is available to hero one");
beerVisitors.Add(101);
heroOneBuffs.Add(new("beer_fountain_buff", [1]));
Check(BeerState(101, heroOneBuffs) == HintState.Used, "Visited fountain loses its ready marker");
beerVisitors.Clear();
Check(BeerState(101, heroOneBuffs) == HintState.Used, "Next day does not relight the fountain while its weekly buff is active");
Check(BeerState(102, Array.Empty<ActiveHeroBuff>()) == HintState.Available, "Second hero without the bonus still sees the same fountain");
Check(BeerState(101, new[] { new ActiveHeroBuff("beer_fountain_buff", [1]) }) == HintState.Used, "Loaded buff state suppresses the fountain without mod visit history");
Check(!VisitRules.ShouldDisplay(BeerState(101, heroOneBuffs), DisplayMode.Available), "Available-only mode hides redundant weekly buff");
Check(VisitRules.ShouldDisplay(BeerState(101, heroOneBuffs), DisplayMode.All), "All mode still shows redundant buff dimmed");
heroOneBuffs.Clear();
Check(BeerState(101, heroOneBuffs) == HintState.Available, "Buff expiration at week change restores readiness");
Check(!BuffHintRules.AllRewardsAlreadyActive(beerReward, new[] { new ActiveHeroBuff("quixs_path_buff", [1]) }, false), "A different bonus never suppresses the fountain");
Check(!BuffHintRules.AllRewardsAlreadyActive(beerReward, new[] { new ActiveHeroBuff("beer_fountain_buff", [3]) }, false), "Replacing battle-limited buff with weekly duration can still help");
Check(!BuffHintRules.AllRewardsAlreadyActive(beerReward, new[] { new ActiveHeroBuff("beer_fountain_buff", [1, 3]) }, false), "An extra expiry condition may make a refresh useful");
var hasBeerBuff = new[] { new ActiveHeroBuff("beer_fountain_buff", [1]) };
Check(!BuffHintRules.AllRewardsAlreadyActive(beerReward, hasBeerBuff, true), "A matching buff cannot hide additional mana, movement or resource rewards");
foreach (int stacking in new[] { 2, 3, 4 })
    Check(!BuffHintRules.AllRewardsAlreadyActive(new[] { new BuffOffer("beer_fountain_buff", stacking, [1]) }, hasBeerBuff, false), "Stacking effects or duration remain useful");
Check(!BuffHintRules.AllRewardsAlreadyActive(new[] { new BuffOffer("timed_buff", 1, [4]) }, new[] { new ActiveHeroBuff("timed_buff", [4]) }, false), "Several-day replacement can extend duration");
Check(!BuffHintRules.AllRewardsAlreadyActive(new[] { new BuffOffer("timed_buff", 1, [5]) }, new[] { new ActiveHeroBuff("timed_buff", [5]) }, false), "Several-battle replacement can extend duration");
Check(!BuffHintRules.AllRewardsAlreadyActive(new[] { beerReward[0], new BuffOffer("quixs_path_buff", 1, [1]) }, hasBeerBuff, false), "All offered buffs must already be active before hiding");
Check(!BuffHintRules.AllRewardsAlreadyActive([], hasBeerBuff, false), "Empty reward information must not hide a marker");
Check(!BuffHintRules.AllRewardsAlreadyActive(beerReward, null, false), "Missing hero buff data preserves native availability");
Check(!BuffHintRules.AllRewardsAlreadyActive(new[] { new BuffOffer("beer_fountain_buff", 99, [1]) }, hasBeerBuff, false), "Unknown stacking rule is not assumed redundant");
Check(VisitRules.Classify(null, true, true) == HintState.Unknown, "Buff knowledge does not bypass unknown visit rules");
// These match actual permanent reward parameter layouts in the installed Core.zip.
var swordStats = StatIconRules.ReadPairs(new[] { "offence", "1" });
Check(swordStats.Length == 1 && swordStats[0].Id == "offence" && swordStats[0].Amount == 1, "Sword displays its attack bonus");
var defenceStats = StatIconRules.ReadPairs(new[] { "defence", "2" });
Check(defenceStats.Length == 1 && defenceStats[0].Id == "defence" && defenceStats[0].Amount == 2, "Fort's defence offer is +2, not +1");
var allStats = StatIconRules.ReadPairs(new[] { "offence", "2", "defence", "2", "spellPower", "2", "intelligence", "2" });
Check(allStats.Length == 4 && allStats.All(stat => stat.Amount == 2), "College keeps all four simultaneous bonuses");
Check(allStats.Select(stat => stat.Id).SequenceEqual(new[] { "offence", "defence", "spellPower", "intelligence" }), "Distinct native stat identities and reward order are preserved");
Check(StatIconRules.ReadPairs(new[] { "spellPower", "4" }).Single().Amount == 4, "Trial scales retain the actual +4 amount");
Check(StatIconRules.ReadPairs(new[] { "unknownStat", "5" }).Length == 0, "Unknown stats never borrow an unrelated icon");
Check(StatIconRules.ReadPairs(new[] { "offence", "random" }).Length == 0, "Unresolved stat amounts are not invented");
Check(StatIconRules.ReadPairs(new[] { "offence", "1", "defence" }).Length == 0, "Truncated reward pairs are ignored");
Check(StatIconRules.ReadPairs(new[] { "offence", "0", "defence", "-1" }).Length == 0, "Nonpositive changes are not presented as permanent gains");
Check(StatIconRules.ReadPairs(null).Length == 0, "Missing parameters keep generic fallback");
Check(InterfaceVisibilityRules.IsNotificationLayer("ScSaveNotification"), "Saving toast participates in passive graphic occlusion");
Check(InterfaceVisibilityRules.IsNotificationLayer("BhNotification"), "Day and movement notices participate in passive graphic occlusion");
Check(!InterfaceVisibilityRules.IsNotificationLayer("BhHUDSession"), "Notification occlusion does not mask the entire map HUD");
Check(!InterfaceVisibilityRules.BlocksMap("ScSaveNotification", true, true, true, 1, false), "Saving toast does not hide all map hints");
Check(!TooltipOcclusionRules.IsVisible(true, true, 0), "A transparent notification cannot leave a persistent hole in the overlay");
Console.WriteLine($"PASS: {checks} checks covering visits, reward knowledge, hidden random rewards, active buffs, windows, resource and stat identity, passive UI occlusion and size migration.");
