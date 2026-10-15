using LazyBearTechnology;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Turns a tech builder into a TechDef in the game's balance and joins it to its parents on the tree.</summary>
internal static class TimbnTechs
{
    private static readonly AccessTools.FieldRef<TechDef, float> _x = AccessTools.FieldRefAccess<TechDef, float>("posX");
    private static readonly AccessTools.FieldRef<TechDef, float> _y = AccessTools.FieldRefAccess<TechDef, float>("posY");
    private static readonly AccessTools.FieldRef<TechDef, int> _red = AccessTools.FieldRefAccess<TechDef, int>("redSpheresPrice");
    private static readonly AccessTools.FieldRef<TechDef, int> _green = AccessTools.FieldRefAccess<TechDef, int>("greenSpheresPrice");
    private static readonly AccessTools.FieldRef<TechDef, int> _blue = AccessTools.FieldRefAccess<TechDef, int>("blueSpheresPrice");
    private static readonly MethodInfo _link = AccessTools.Method(typeof(TechDef), "InitTechDef");

    private static readonly Dictionary<string, IReadOnlyList<string>> _rows = [];

    internal static IReadOnlyList<string>? RowsOf(string techId) => _rows.GetValueOrDefault(techId);

    internal static IDisposable Add(TimbnTechBuilder tech)
    {
        if (!tech.HasCell)
            throw new InvalidOperationException($"Tech {tech.Id} needs a cell. Call At on the builder.");

        if (tech.ParentIds.Count == 0)
            throw new InvalidOperationException($"Tech {tech.Id} needs a tech before it. Call After on the builder.");

        List<IDisposable> undo = [];
        if (tech.Name.Length > 0)
            undo.Add(TimbnLocale.Add(tech.Id, tech.Name));

        if (tech.RowIds is { } rows)
        {
            _rows[tech.Id] = [.. rows];
            undo.Add(new TimbnUndo(() => _rows.Remove(tech.Id)));
        }

        TechDef? added = null;
        // Copied from the first parent so every field the game reads has a real value, then emptied of what isn't ours.
        undo.Add(TimbnBalance.AddCopy<TechDef>(_ => tech.ParentIds[0], tech.Id, (_, def) =>
        {
            added = def;
            def.tab = tech.TabId;
            def.parents = [.. tech.ParentIds];
            _x(def) = tech.X;
            _y(def) = tech.Y;
            _red(def) = tech.Red;
            _green(def) = tech.Green;
            _blue(def) = tech.Blue;
            def.customIconId = "";
            def.availableAtStart = false;
            def.hiddenAtStart = false;
            def.techDefType = TechDefType.Common;
            def.techLockType = TechLockType.All;
            def.wgoRepLock = new GameRes();
            def.districtReputationLock = new GameRes();
            def.craftsAfterUnlock = [];
            def.alchemyFormulasAfterUnlock = [];
            def.buildingsAfterUnlock = [];
            def.townBuildingsAfterUnlock = [];
            def.perksAfterUnlock = [];
            def.addGameResAfterUnlock = new GameRes();
            def.setGameResAfterUnlock = new GameRes();
            def.expressionsAfterUnlock = [];
            Link(def);
        }, balance => SpotIsFree(balance, tech)));
        undo.Add(new TimbnUndo(() =>
        {
            if (added != null)
                Unlink(added);
        }));
        return TimbnUndo.All(undo);
    }

    // The tree never moves a tech out of the way, so a tech on top of another would hide it and take its clicks.
    private static bool SpotIsFree(GameBalance balance, TimbnTechBuilder tech)
    {
        var taken = balance.techDefs.FirstOrDefault(other =>
            other.id != tech.Id && other.tab == tech.TabId && TimbnTechRules.Overlaps(tech.X, tech.Y, _x(other), _y(other)));
        if (taken == null)
            return true;

        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnTechs)}|Tech '{tech.Id}' was not added. Its spot ({tech.X}, {tech.Y}) on the {tech.TabId} tab is taken by '{taken.id}' at ({_x(taken)}, {_y(taken)}). Pick a free spot with At.");
        return false;
    }

    // The game joins every tech to its parents once, when the techs first initialize, so a tech added after that (a
    // script reload) has to be joined by hand. Before then the game's own pass picks it up.
    private static void Link(TechDef tech)
    {
        Unlink(tech);
        if (!TimbnTechGrants.TreeBuilt)
            return;

        tech.linkedEntityWidgetDatas.Clear();
        _link.Invoke(tech, null);
    }

    private static void Unlink(TechDef tech)
    {
        foreach (var parent in tech.parentDefinitionList.ToList())
            parent.childDefinitionList.Remove(tech);

        tech.parentDefinitionList.Clear();
    }
}
