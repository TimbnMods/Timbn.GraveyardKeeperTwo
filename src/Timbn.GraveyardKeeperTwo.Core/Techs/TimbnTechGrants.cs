using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts a mod's craft or building on a technology's reward list, so researching the tech unlocks it and the tech's
/// tooltip lists it, and unlocks it in a save that already has the tech.
/// </summary>
internal static class TimbnTechGrants
{
    internal static IDisposable Craft(string techId, string craftId) =>
        Grant(techId, craftId, tech => tech.craftsAfterUnlock, (knowledge, id) => knowledge.UnlockCraft(id));

    internal static IDisposable Building(string techId, string buildingId) =>
        Grant(techId, buildingId, tech => tech.buildingsAfterUnlock, (knowledge, id) => knowledge.UnlockBuilding(id));

    private static readonly FieldInfo _initialized = AccessTools.Field(typeof(TechDef), "isInitialized");

    /// <summary>Whether the game has already joined the techs to each other and built their reward icons, which it does once.</summary>
    internal static bool TreeBuilt => _initialized.GetValue(null) is true;

    // KnowledgeSystem.OnTechUnlocked fires before TechDef.Unlock grants the tech's rewards, so the id goes on the
    // tech's own list instead, and a save that researched the tech before the plugin arrived gets it on load.
    private static IDisposable Grant(string techId, string id, Func<TechDef, List<string>> rewards, Action<KnowledgeSystem, string> unlock)
    {
        var listed = TimbnBalance.Edit<TechDef>(
            techId,
            tech =>
            {
                if (!rewards(tech).Contains(id))
                    rewards(tech).Add(id);
                TimbnTechRows.Refresh(tech);
            },
            tech =>
            {
                rewards(tech).Remove(id);
                TimbnTechRows.Refresh(tech);
            },
            TimbnCorePlugin.Logger);

        void Unlock()
        {
            if (TimbnPlayer.HasTech(techId) && MainGame.Instance.GameSave?.knowledgeSystem is { } knowledge)
                unlock(knowledge, id);
        }

        var granted = TimbnGameEvents.GameStarted(Unlock);
        if (TimbnGame.IsInGame)
            TimbnSafe.Run(Unlock, TimbnCorePlugin.Logger, $"{nameof(TimbnTechGrants)}|Granting '{id}' for '{techId}'");

        return TimbnUndo.All([listed, granted]);
    }
}
