namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds a quest added with Quests.Add, all its stages included. Keep it to start the quest, mark it ready or complete
/// it from your own code. Pass it to another quest's OfferAfter or ContinuesFrom to chain them.
/// </summary>
/// <example>
/// <code><![CDATA[
/// var bats = Quests.Add("my_cellar_bats", TimbnGameIds.Npcs.Larry, "Bats in the Cellar", quest => quest
///     .Description("Smoke the bats out of the cellar.")
///     .ReadyByCode());
///
/// Events.PlayerItemUsed(item => { if (item.id == "smoke_bomb") bats.MarkReady(); });
/// ]]></code>
/// </example>
public sealed class TimbnQuest
{
    private readonly IReadOnlyList<TimbnQuestRuntime> _steps;
    private readonly IReadOnlyList<string> _stageNames;
    private readonly bool _added;
    private readonly TimbnQuestCycle? _cycle;

    internal TimbnQuest(IReadOnlyList<TimbnQuestRuntime> steps, IReadOnlyList<string> stageNames, bool added = true, TimbnQuestCycle? cycle = null, bool repeats = false)
    {
        _steps = steps;
        _stageNames = stageNames;
        _added = added;
        _cycle = cycle;
        Repeats = repeats;
    }

    internal bool Repeats { get; }

    /// <summary>
    /// The current run of a quest with a Setup, or null when none is rolled, no save is loaded, or the quest was
    /// skipped because another mod adds one with the same id.
    /// </summary>
    public TimbnQuestRun? CurrentRun => _added ? _cycle?.Run : null;

    /// <summary>
    /// Rolls a new run of a quest with a Setup, such as on a lucky new day. Setup fills it, then the giver offers the
    /// quest, or it starts straight away when it has no Offer or StartsOn. It does nothing while a run is going, while
    /// the quest is in progress or done or until OfferAfter and OfferWhen allow it. It throws for a quest with no Setup.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// Events.NewDayStarted(_ =>
    /// {
    ///     if (UnityEngine.Random.Range(0, 7) == 0)
    ///         craving.NewRun();
    /// });
    /// ]]></code>
    /// </example>
    /// <returns>True when a run was rolled.</returns>
    public bool NewRun() =>
        _cycle is { HasSetup: true }
            ? _added && _cycle.Roll()
            : throw new InvalidOperationException($"Quest {Id} has no Setup, so there is nothing to roll.");

    /// <summary>
    /// Ends the current run early, whether or not the player took the quest on. Every stage goes back to not started and
    /// leaves the log, and a quest with a Setup waits for the next NewRun. It does nothing once a quest that is not
    /// repeatable is done and at the main menu.
    /// </summary>
    /// <returns>True when a run or a quest in progress was canceled.</returns>
    public bool Cancel() => _added && _cycle is not null && _cycle.Cancel("by the mod's code");

    /// <summary>The quest id, which is also its first stage's id. Saves keep progress under it.</summary>
    public string Id => _steps[0].Id;

    /// <summary>
    /// The quest's status in the loaded save, or null when no save is loaded or the quest was skipped because another
    /// mod adds one with the same id. A quest with stages is in progress from its first stage starting until its last
    /// stage is completed, the moment between two stages included, and completed once its last stage is.
    /// </summary>
    public QuestStatus? Status => _added ? Read([.. _steps.Select(step => step.Status)]).Status : null;

    /// <summary>
    /// The name of the stage the player is on, or null for a quest without stages or one not in progress. Between two
    /// stages, once one is done and before the next starts, it is the next stage. When the quest's own calls are its
    /// first stage, that stage's name is the quest id.
    /// </summary>
    public string? Stage
    {
        get
        {
            if (!_added || _stageNames.Count == 0)
                return null;

            var at = Read([.. _steps.Select(step => step.Status)]).Stage;
            return at >= 0 ? _stageNames[at] : null;
        }
    }

    internal string EndId => _steps[^1].Id;

    private TimbnQuestRuntime? Current
    {
        get
        {
            if (!_added)
                return null;

            foreach (var step in _steps)
            {
                if (step.Status == QuestStatus.InProgress)
                    return step;
            }

            return null;
        }
    }

    internal static (QuestStatus? Status, int Stage) Read(IReadOnlyList<QuestStatus?> steps)
    {
        if (steps[^1] == QuestStatus.Completed)
            return (QuestStatus.Completed, -1);

        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i] == QuestStatus.InProgress)
                return (QuestStatus.InProgress, i);
        }

        for (var i = steps.Count - 2; i >= 0; i--)
        {
            if (steps[i] == QuestStatus.Completed)
                return (QuestStatus.InProgress, i + 1);
        }

        return (steps[0], -1);
    }

    /// <summary>
    /// Starts the quest in the loaded save, for a quest with no offer or one your own code hands out. It does nothing
    /// once the quest has started or finished.
    /// </summary>
    /// <returns>True when the quest started.</returns>
    public bool Start() => _added && _steps[0].Start("code");

    /// <summary>
    /// Puts the quest, every stage included, back to not started in the loaded save, as if the player had never taken
    /// it, so it can be played again while testing. Items and money already handed over stay spent.
    /// </summary>
    /// <returns>True when the quest was reset. False when no save is loaded or the quest was skipped.</returns>
    public bool Reset()
    {
        if (!_added)
            return false;

        var reset = false;
        for (var i = _steps.Count - 1; i >= 0; i--)
            reset |= _steps[i].Reset();

        return reset;
    }

    /// <summary>
    /// Marks the stage the player is on ready, for a quest or stage that waits on ReadyByCode. It does nothing unless
    /// that stage has started, so it is safe to call whenever the thing happens.
    /// </summary>
    /// <returns>True when it was marked.</returns>
    public bool MarkReady() => Current?.Ready() == true;

    /// <summary>Marks one stage ready, and only while the player is on that stage.</summary>
    /// <param name="stage">The stage's name.</param>
    /// <returns>True when it was marked.</returns>
    public bool MarkReady(string stage) => Current is { } current && Stage == stage && current.Ready();

    /// <summary>
    /// Completes the stage the player is on straight away, or the quest when it has no stages, for something your own
    /// code decides is done, with or without a talk. It pays the rewards, plays the DoneLines and runs OnDone. It does
    /// nothing unless that stage has started.
    /// </summary>
    /// <returns>True when it was completed.</returns>
    public bool Complete() => Current?.Complete() == true;
}
