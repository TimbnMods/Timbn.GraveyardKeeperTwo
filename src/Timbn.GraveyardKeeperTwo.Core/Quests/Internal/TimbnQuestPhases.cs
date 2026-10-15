namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Picks the ring each step of a mod quest chain shows in the quest tree, the way the game's own storylines do, where
/// the green grows around the ring as the story goes on. Steps spread across the five rings by their place in the
/// chain. A step whose author set a phase keeps it, and a quest that is no chain's step keeps its own.
/// </summary>
internal static class TimbnQuestPhases
{
    internal static Dictionary<string, int> Assign(IReadOnlyList<Step> quests)
    {
        var byId = new Dictionary<string, Step>();
        foreach (var quest in quests)
            byId[quest.Id] = quest;

        var next = new Dictionary<string, string>();
        foreach (var quest in quests)
        {
            if (quest.Continued is { } parent && byId.ContainsKey(parent) && !next.ContainsKey(parent))
                next[parent] = quest.Id;
        }

        var phases = new Dictionary<string, int>();
        foreach (var quest in quests)
            phases[quest.Id] = quest.OwnPhase;

        foreach (var head in quests.Where(q => next.ContainsKey(q.Id) && !IsNextStep(q, next)))
        {
            List<string> chain = [head.Id];
            while (next.TryGetValue(chain[^1], out var step) && !chain.Contains(step))
                chain.Add(step);

            var spread = Spread(chain.Count);
            for (var i = 0; i < chain.Count; i++)
            {
                if (byId[chain[i]].OwnPhase == 0)
                    phases[chain[i]] = spread[i];
            }
        }

        return phases;
    }

    internal static int[] Spread(int count) => count switch
    {
        2 => [2, 4],
        3 => [1, 3, 5],
        4 => [1, 2, 3, 4],
        _ => [.. Enumerable.Range(0, count).Select(i => 1 + (i * 5 / count))],
    };

    private static bool IsNextStep(Step quest, Dictionary<string, string> next) =>
        quest.Continued is { } parent && next.TryGetValue(parent, out var step) && step == quest.Id;

    internal sealed class Step
    {
        internal Step(string id, string? continued, int ownPhase)
        {
            Id = id;
            Continued = continued;
            OwnPhase = ownPhase;
        }

        internal string Id { get; }

        internal string? Continued { get; }

        internal int OwnPhase { get; }
    }
}
