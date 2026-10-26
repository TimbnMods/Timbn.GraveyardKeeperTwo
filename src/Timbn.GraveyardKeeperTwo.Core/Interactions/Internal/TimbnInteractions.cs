using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The actions plugins have added to the game's objects, asked by the patches on the interaction handler.</summary>
internal static class TimbnInteractions
{
    private static readonly List<TimbnInteractionTarget> _targets = [];
    private static readonly Dictionary<(Type Type, bool Primary), bool> _owns = [];

    internal static IDisposable Add(TimbnInteractionTarget target)
    {
        _targets.Add(target);
        return target;
    }

    internal static void Remove(TimbnInteractionTarget target) => _targets.Remove(target);

    internal static bool Has(Wgo? wgo, bool primary) => Find(wgo, primary, out _) != null;

    // The key icon is read each time, since it changes when the player switches between keyboard and gamepad. A primary
    // action goes first, where the game puts its own, and a second one after.
    internal static void AddHints(Wgo? wgo, InteractionInfos infos)
    {
        if (Find(wgo, true, out var primary) != null)
            infos.list.Insert(0, new InteractionInfo(ControllerIconLibrary.GetIconId(GameKey.Interaction) + primary));

        if (Find(wgo, false, out var secondary) != null)
            infos.Add(new InteractionInfo(ControllerIconLibrary.GetIconId(GameKey.Action) + secondary));
    }

    internal static bool Interact(Wgo? wgo, bool primary)
    {
        if (wgo == null || Find(wgo, primary, out _) is not { } target)
            return false;

        var action = primary ? target.Primary! : target.Secondary!;
        TimbnSafe.Run(() => action.Run(wgo), target.Logger, $"{nameof(TimbnPluginInteractions)}|{TimbnSafe.Describe(action.Run)}");
        return true;
    }

    // Runs for every object the game draws a hint for, so it allocates nothing and returns at once when no plugin added one.
    private static TimbnInteractionTarget? Find(Wgo? wgo, bool primary, out string label)
    {
        label = string.Empty;
        if (wgo == null || _targets.Count == 0)
            return null;

        foreach (var target in _targets)
        {
            if ((primary ? target.Primary : target.Secondary) is not { } action || !Applies(target, wgo))
                continue;

            if (OwnsKey(wgo, target, primary))
                return null;

            if (action.LabelNow(target.Logger) is not { } text)
                continue;

            label = text;
            return target;
        }

        return null;
    }

    // A plain try and catch rather than TimbnSafe.Run, which would allocate a delegate for every object drawn. The check
    // runs several times a frame for each object, so one that throws is logged once and then left off.
    private static bool Applies(TimbnInteractionTarget target, Wgo wgo)
    {
        if (target.Broken)
            return false;

        try
        {
            return target.AppliesTo(wgo);
        }
        catch (Exception exception)
        {
            target.Broken = true;
            target.Logger.LogError($"{nameof(TimbnPluginInteractions)}|{TimbnSafe.Describe(target.AppliesTo)} threw, so its actions are off until the mod reloads: {exception}");
            return false;
        }
    }

    // A handler that overrides a key's methods has an action of its own on it, which a plugin's never replaces.
    private static bool OwnsKey(Wgo wgo, TimbnInteractionTarget target, bool primary)
    {
        var type = wgo.InteractionHandler?.GetType();
        if (type == null)
            return false;

        if (_owns.TryGetValue((type, primary), out var owns))
            return owns;

        owns = primary
            ? Overrides(type, nameof(WGOInteractionHandlerBase.HasInteraction)) || Overrides(type, nameof(WGOInteractionHandlerBase.Interact))
            : Overrides(type, nameof(WGOInteractionHandlerBase.HasInteraction2)) || Overrides(type, nameof(WGOInteractionHandlerBase.Interact2));
        _owns[(type, primary)] = owns;
        if (owns)
            target.Logger.LogWarning($"{nameof(TimbnPluginInteractions)}|{wgo.Id} uses {type.Name}, which has a {(primary ? "primary" : "second")} action of its own, so the mod's was not added.");

        return owns;
    }

    private static bool Overrides(Type type, string method) =>
        type.GetMethod(method, [typeof(PlayerController)])?.DeclaringType != typeof(WGOInteractionHandlerBase);
}
