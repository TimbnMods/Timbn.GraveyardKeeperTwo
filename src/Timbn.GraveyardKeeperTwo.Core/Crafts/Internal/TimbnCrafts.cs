namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Turns a craft builder into its CraftDef and text through Core's balance entries, so it goes in on every balance load and comes out with the owning plugin.</summary>
internal static class TimbnCrafts
{
    private static readonly AccessTools.FieldRef<CraftDef, string> _description = AccessTools.FieldRefAccess<CraftDef, string>("description");

    internal static IDisposable Add(TimbnCraftBuilder craft)
    {
        List<IDisposable> undo = [];
        var texts = TimbnCraftRules.Texts(craft.Id, craft.Name, craft.Description);
        if (texts.Count > 0)
            undo.Add(TimbnLocale.Add(texts));

        foreach (var handler in craft.StartedHandlers)
            undo.Add(TimbnCraftEvents.OnStarted(craft.Id, handler));

        foreach (var handler in craft.FinishedHandlers)
            undo.Add(TimbnCraftEvents.OnFinished(craft.Id, handler));

        foreach (var check in craft.Checks)
            undo.Add(TimbnCraftRequirements.Add(craft.Id, check));

        undo.Add(TimbnBalance.AddCopy<CraftDef>(_ => craft.TemplateId, craft.Id, (balance, def) => Fill(balance, craft, def)));
        // After the craft is in the balance, so the tech's reward icons can find it.
        if (craft.TechId is { } tech)
            undo.Add(TimbnTechGrants.Craft(tech, craft.Id));

        return TimbnUndo.All(undo);
    }

    private static void Fill(GameBalance balance, TimbnCraftBuilder craft, CraftDef def)
    {
        _description(def) = TimbnStationRules.DescriptionKey(craft.Id);
        if (craft.StationIds is { } stations)
            def.craftsIn = [.. stations];

        if (craft.NeedItems is { } needs)
            def.needItems = [.. needs.Select(need => new NeedItemData(need.Item, TimbnExpression.Formula(need.Count)))];

        if (craft.Outputs is { } outputs)
        {
            def.outputItems.chanceOutputItems = [.. outputs.Select(output => new ChanceOutputItem { id = output.Item, count = TimbnExpression.Formula(output.Count) })];
            def.outputItems.groupChanceOutputItems.Clear();
        }

        def.isNeedsUnlock = craft.MustBeUnlocked;
        if (craft.MasteryLevel is { } mastery)
            def.talentLock = mastery;

        if (craft.ExtensionId is { } extension)
            def.extensionNeedId = extension;

        if (craft.IconId is { } icon)
        {
            def.iconId = icon;
            def.customCraftResultIcon = TimbnExpression.PureText(icon);
        }

        if (craft.WorkTicks is { } ticks)
            def.duration = TimbnExpression.Number(ticks);

        if (craft.EnergyEachTick is { } energy)
            def.energyPerTick = TimbnExpression.Number(energy);

        if (craft.FinishesItself)
            def.autoFinishAutoCraft = true;

        def.isAuto = TimbnCraftRules.IsAuto(StationRunsItself(balance, def.craftsIn), def.isAuto);
        craft.EditCraft?.Invoke(def);
    }

    // A station added by a mod is only in the balance once its own entry went in, so the mod registers it before its crafts.
    private static bool? StationRunsItself(GameBalance balance, List<string> stationIds)
    {
        foreach (var stationId in stationIds)
        {
            if (balance.GetDataOrNull<WGODef>(stationId) is { } station)
                return station.isAutoCrafter;
        }

        return null;
    }
}
