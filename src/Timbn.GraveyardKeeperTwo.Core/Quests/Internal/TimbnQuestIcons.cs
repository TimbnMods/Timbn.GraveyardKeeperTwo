namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives a step of a mod quest chain that sets no icon the icon of the step it continues, so the chain's cell in the
/// quest tree keeps one look as the steps take over. A step with its own icon keeps it.
/// </summary>
internal static class TimbnQuestIcons
{
    internal static Dictionary<string, string> Inherit(IReadOnlyList<Step> quests)
    {
        var byId = new Dictionary<string, Step>();
        foreach (var quest in quests)
            byId[quest.Id] = quest;

        var icons = new Dictionary<string, string>();
        foreach (var quest in quests)
        {
            var step = quest;
            HashSet<string> seen = [];
            while (step.OwnIcon.Length == 0
                && seen.Add(step.Id)
                && step.Continued is { } continued
                && byId.TryGetValue(continued, out var before))
            {
                step = before;
            }

            icons[quest.Id] = step.OwnIcon;
        }

        return icons;
    }

    internal sealed class Step
    {
        internal Step(string id, string? continued, string ownIcon)
        {
            Id = id;
            Continued = continued;
            OwnIcon = ownIcon;
        }

        internal string Id { get; }

        internal string? Continued { get; }

        internal string OwnIcon { get; }
    }
}
