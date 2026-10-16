using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps one quest's run and repeats for every step of it. It rolls a new run, fills the run's values into the quest's
/// text, expires and cancels runs, and puts a repeatable quest back to not started once its last step is done.
/// </summary>
internal sealed class TimbnQuestCycle
{
    private readonly TimbnQuestBuilder _quest;
    private readonly List<TimbnQuestRuntime> _steps = [];
    private readonly List<string> _keys;
    private TimbnQuestRun? _run;

    internal TimbnQuestCycle(TimbnQuestBuilder quest)
    {
        _quest = quest;
        _keys = quest.SetupOf is null ? [] : [.. quest.Steps.SelectMany(step => step.Texts.Keys).Distinct()];
    }

    internal string Id => _quest.Id;

    internal bool HasSetup => _quest.SetupOf is not null;

    internal bool IsFinished => _steps.Count > 0 && _steps[^1].Status == QuestStatus.Completed;

    internal TimbnQuestRun? Run
    {
        get
        {
            var data = TimbnQuests.RunOf(Id);
            if (data is null)
                return _run = null;

            if (_run?.Data != data)
                _run = new TimbnQuestRun(data);

            return _run;
        }
    }

    internal void Use(IEnumerable<TimbnQuestRuntime> steps) => _steps.AddRange(steps);

    internal IDisposable Register()
    {
        var fill = _keys.Count == 0 ? TimbnUndo.None : TimbnLocale.Fill(_keys, Fill);
        var tracked = TimbnQuests.Track(this);
        if (TimbnGame.IsInGame)
            TimbnSafe.Run(OnGameStarted, $"{nameof(TimbnQuests)}|Setting up the run of quest '{Id}' for the save");

        return new TimbnUndo(() =>
        {
            tracked.Dispose();
            fill.Dispose();
        });
    }

    internal bool Roll()
    {
        if (_quest.SetupOf is not { } setup)
            throw new InvalidOperationException($"Quest {Id} has no Setup, so there is nothing to roll.");

        if (!TimbnGame.IsInGame)
            return false;

        if (Run is not null)
        {
            Log("did not roll, since a run is still going.");
            return false;
        }

        if (!_steps[0].IsFree())
        {
            Log($"did not roll, since it is {_steps[0].Status} or its OfferAfter and OfferWhen don't allow it yet.");
            return false;
        }

        var data = new TimbnQuestRunData { Day = TimbnClock.Day };
        var made = false;
        if (!TimbnSafe.Run(() => made = setup(new TimbnQuestRun(data)), $"{nameof(TimbnQuests)}|Setup of quest '{Id}'"))
            return false;

        if (!made)
        {
            Log("did not roll, since its Setup found nothing to use.");
            return false;
        }

        foreach (var npc in data.Npcs.Where(pair => IsShown(pair.Key) && !LLBase.HasL(pair.Value)))
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnQuests)}|Quest '{Id}' shows {{{npc.Key}}}, but {npc.Value} has no name in the game, so the text shows the id.");

        TimbnQuests.SetRun(Id, data);
        Log($"rolled a run with {Describe(data)}.");
        Changed();
        _steps[0].StartRolled();
        return true;
    }

    internal bool Cancel(string reason)
    {
        if (!TimbnGame.IsInGame || _steps[^1].Status == QuestStatus.Completed)
            return false;

        var had = Run is not null || _steps.Exists(step => step.Status == QuestStatus.InProgress);
        if (!had)
            return false;

        Log($"was canceled ({reason}).");
        if (_quest.WhenCanceled is { } canceled)
        {
            var run = Run;
            TimbnSafe.Run(() => canceled(run), $"{nameof(TimbnQuests)}|OnCancel of quest '{Id}'");
        }

        using var batch = TimbnQuestRuntime.Batch();
        TimbnQuests.SetRun(Id, null);
        ResetSteps();
        Changed();
        return true;
    }

    internal void StepDone(TimbnQuestRuntime step)
    {
        if (step != _steps[^1] || !_quest.Repeats)
            return;

        Log("is done and starts over.");
        using var batch = TimbnQuestRuntime.Batch();
        TimbnQuests.SetRun(Id, null);
        ResetSteps();
        Changed();
    }

    internal void OnGameStarted()
    {
        if (Restore())
            Changed();
    }

    // Puts the run back for the loaded save. False when it had to cancel, which already rewrote everything.
    internal bool Restore()
    {
        if (_quest.Repeats && _steps[^1].Status == QuestStatus.Completed)
        {
            TimbnQuests.SetRun(Id, null);
            ResetSteps();
        }

        // A backup slot or an older Timbn file can leave a step in progress with no run, which nothing could ever end.
        if (HasSetup && Run is null && _steps.Exists(step => step.Status == QuestStatus.InProgress))
        {
            Cancel("its run data is missing from the Timbn save file");
            return false;
        }

        return true;
    }

    internal void RewriteTexts() => TimbnLocale.Rewrite(_keys);

    internal void OnNewDay()
    {
        if (_quest.ExpireDays > 0 && Run is { } run && TimbnClock.Day - run.Day >= _quest.ExpireDays)
            Cancel($"ran out after {_quest.ExpireDays} days");
    }

    // Each step's reset rewrites its quest and nothing is in progress after, so the cache is rebuilt once at the end.
    private void ResetSteps()
    {
        using var batch = TimbnQuestRuntime.Batch();
        for (var i = _steps.Count - 1; i >= 0; i--)
            _steps[i].Reset();
    }

    private void Changed()
    {
        RewriteTexts();
        foreach (var step in _steps)
            step.Write();

        TimbnQuestRuntime.RefreshCaches();
        foreach (var step in _steps)
            step.ActivateIfStarted();

        TimbnDialog.Refresh();
    }

    private bool IsShown(string name) => _quest.Steps.Exists(step => step.Texts.Values.Any(text => text.Contains($"{{{name}}}")));

    private string Fill(string text)
    {
        var run = Run;
        return run is null ? text : TimbnQuestBuilder.Placeholder.Replace(text, match => run.Shown(match.Groups[1].Value) ?? match.Value);
    }

    private static string Describe(TimbnQuestRunData data) =>
        string.Join(", ", data.Npcs.Select(pair => $"{pair.Key} {pair.Value}")
            .Concat(data.Items.Select(pair => $"{pair.Key} {pair.Value}"))
            .Concat(data.Numbers.Select(pair => $"{pair.Key} {pair.Value}"))
            .Concat(data.Texts.Select(pair => $"{pair.Key} {pair.Value}")));

    private void Log(string message) => TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnQuests)}|Quest '{Id}' {message}");
}
