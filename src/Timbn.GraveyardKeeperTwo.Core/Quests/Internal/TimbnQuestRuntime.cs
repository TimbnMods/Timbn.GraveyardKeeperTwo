using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Runs one built quest in the loaded save, with its definition, talks and rewards.</summary>
internal sealed class TimbnQuestRuntime
{
    private readonly TimbnQuestBuilder _quest;
    private readonly Func<Color> _bubbleColor;
    private readonly string _owner;
    private readonly TimbnQuestCycle? _cycle;
    private readonly QuestDef _definition;
    private readonly List<IDisposable> _parts = [];
    private readonly HashSet<string> _warned = [];
    private bool _active;

    internal TimbnQuestRuntime(TimbnQuestBuilder quest, Func<Color> bubbleColor, string owner = "", TimbnQuestCycle? cycle = null)
    {
        _quest = quest;
        _bubbleColor = bubbleColor;
        _owner = owner;
        _cycle = cycle;
        _definition = TimbnQuests.CreateDefinition(quest.Id);
        foreach (var parent in quest.Parents)
            _definition.After(parent);

        _definition.wgoNpcId = quest.GiverId;
        _definition.iconId = quest.IconId;
        _definition.phase = quest.RingPhase;
        _definition.execExpressionsStart = [.. quest.StartExpressions.Select(expression => expression.Game)];
        _definition.execExpressionsFinish = [.. quest.DoneExpressions.Select(expression => expression.Game)];
    }

    internal string Id => _quest.Id;

    internal string? Continued => _quest.Continued;

    internal QuestStatus? Status => TimbnQuests.StatusOf(Id);

    private TimbnQuestRun? Run => _cycle?.Run;

    private string GiverNow => _quest.GiverOf is { } giver ? FromRun(giver, "giver") : _quest.GiverId;

    private string TalkNpcNow =>
        _quest.TalkNpcOf is { } npc ? FromRun(npc, "talk NPC")
        : _quest.TalkNpc.Length > 0 ? _quest.TalkNpc
        : GiverNow;

    internal string? HandInMarkerNpc => _quest.ShowsMarker && _quest.TalkOf is not null && IsTalkReady() ? TalkNpcNow : null;

    internal string? OfferMarkerNpc => _quest.ShowsMarker && _quest.OfferTalk is not null && IsOfferable() ? GiverNow : null;

    private TimbnWants Needs => _quest.NeedsFor(Run);

    private TimbnWants Cost => _quest.CostFor(Run);

    private TimbnRewards Rewards => _quest.RewardsFor(Run);

    internal IDisposable Register()
    {
        try
        {
            _parts.Add(TimbnLocale.Add(_quest.Texts));
            _parts.Add(TimbnQuests.Register(_definition, _owner, continues: _quest.Continued is not null, lineColor: _bubbleColor));
            if (_quest.OfferTalk is not null)
            {
                _parts.Add(_quest.GiverOf is null
                    ? TimbnDialog.AddTalk(_quest.GiverId, $"{Id}_offer_talk", IsOfferable, Offer, _bubbleColor)
                    : TimbnDialog.AddTalk(() => GiverNow, $"{Id}_offer_talk", IsOfferable, Offer, _bubbleColor));
            }

            if (_quest.TalkOf is not null)
            {
                var dynamic = _quest.GiverOf is not null || _quest.TalkNpcOf is not null;
                _parts.Add(dynamic
                    ? TimbnDialog.AddTalk(() => TalkNpcNow, $"{Id}_talk", IsTalkReady, Talk, _bubbleColor)
                    : TimbnDialog.AddTalk(TalkNpcNow, $"{Id}_talk", IsTalkReady, Talk, _bubbleColor));
                _parts.Add(dynamic
                    ? TimbnDialog.AddMenuAnswer(() => TalkNpcNow, MenuAnswer, MenuPicked)
                    : TimbnDialog.AddMenuAnswer(TalkNpcNow, MenuAnswer, MenuPicked));
            }

            _parts.Add(TimbnQuests.Track(this));
        }
        catch
        {
            Unregister();
            throw;
        }

        if (TimbnGame.IsInGame)
        {
            TimbnSafe.Run(Apply, $"{nameof(TimbnQuests)}|Setting up quest '{Id}' for the save");
            TimbnSafe.Run(StartWithContinued, $"{nameof(TimbnQuests)}|Starting quest '{Id}' after the one it continues");
        }

        return new TimbnUndo(Unregister);
    }

    private void Unregister()
    {
        Deactivate();
        for (var i = _parts.Count - 1; i >= 0; i--)
            _parts[i].Dispose();

        _parts.Clear();
    }

    internal void Apply()
    {
        var check = _definition.finishCheck;
        var talkNpc = TalkNpcNow;
        var needs = Needs;
        check.hasTrigger = false;
        check.phrase = _quest.TalkOf is null ? "" : MenuKey;
        check.phraseReqs = [
            .. needs.ToRequirements(ReputationFor(needs, talkNpc)),
            .. Cost.ToRequirements(null)];
        _definition.wgoNpcId = Status == QuestStatus.InProgress ? talkNpc : GiverNow;
        if (_quest.IconOf is { } icon && FromRun(icon, "icon") is { Length: > 0 } iconId)
            _definition.iconId = iconId;

        _definition.repVisualisationRes = RewardLine(talkNpc);
        if (TimbnBalance.Loaded is { } balance)
            TimbnBalance.RefreshQuestCaches(balance);

        if (Status == QuestStatus.InProgress)
            Activate();
    }

    private void Activate()
    {
        if (_active)
            return;

        _active = true;
        TimbnSafe.Run(_quest.WhenStarted, $"{nameof(TimbnQuests)}|OnStart of quest '{Id}'");
    }

    internal void Deactivate()
    {
        if (!_active)
            return;

        _active = false;
        TimbnSafe.Run(_quest.WhenEnded, $"{nameof(TimbnQuests)}|OnEnd of quest '{Id}'");
    }

    internal bool Start(string reason)
    {
        if (Status is not (QuestStatus.Available or QuestStatus.Awaiting))
            return false;

        TimbnQuests.Start(Id);
        if (Status != QuestStatus.InProgress)
        {
            Log($"did not start ({reason}), the game kept it at {Status}.", warning: true);
            return false;
        }

        Log($"started ({reason}).");
        Apply();
        TimbnDialog.Refresh();
        return true;
    }

    internal void StartWithContinued()
    {
        if (_quest.StartsWithContinued && IsOfferable())
            Start($"continues {_quest.Continued}");
    }

    internal void StartRolled()
    {
        if (_quest.OfferTalk is null && _quest.StartEvent is null && _quest.Continued is null)
            Start("rolled");
    }

    internal bool Reset()
    {
        if (!TimbnQuests.ResetToAvailable(Id))
            return false;

        TimbnDialog.ResetSeen(SeenKey);
        Log("was reset to not started.");
        Deactivate();
        Apply();
        TimbnDialog.Refresh();
        return true;
    }

    internal bool Complete()
    {
        if (Status != QuestStatus.InProgress)
            return false;

        Log("completed by the mod's code.");
        return Finish(paidByAnswer: false);
    }

    internal bool Ready()
    {
        if (Status != QuestStatus.InProgress || TimbnQuests.IsMarked(Id))
            return false;

        if (!_quest.WaitsForCode)
        {
            Log("ignored MarkReady, since it does not wait for ReadyByCode.", warning: true);
            return false;
        }

        Log("is ready.");
        Marked();
        return true;
    }

    internal void OnGameEvent(GlobalEventsSystem.Event.Type type, string id)
    {
        var status = Status;
        if (status is QuestStatus.Available or QuestStatus.Awaiting)
        {
            if (_quest.StartEvent?.Matches(type, id) == true && IsOfferable())
                Start($"{type} {id}");

            return;
        }

        if (status == QuestStatus.InProgress && _quest.WaitsForEvent?.Matches(type, id) == true && !TimbnQuests.IsMarked(Id))
        {
            Log($"saw {_quest.WaitsForEvent}.");
            Marked();
        }
    }

    private void Marked()
    {
        TimbnQuests.Mark(Id);
        if (_quest.TalkOf is null && IsReady())
            Finish(paidByAnswer: false);
        else
            TimbnDialog.Refresh();
    }

    internal bool IsFree()
    {
        if (Status is not (QuestStatus.Available or QuestStatus.Awaiting))
            return false;

        if (_quest.Parents.Any(parent => TimbnQuests.StatusOf(parent) != QuestStatus.Completed))
            return false;

        return _quest.OfferCondition?.Invoke() ?? true;
    }

    private bool IsOfferable() => (_cycle is not { HasSetup: true } || Run is not null) && IsFree();

    private bool IsReady() => !_quest.NeedsMark || TimbnQuests.IsMarked(Id);

    private bool IsTalkReady()
    {
        if (Status != QuestStatus.InProgress || !IsReady())
            return false;

        var needs = Needs;
        return needs.IsMet(ReputationFor(needs, TalkNpcNow)) && Cost.IsMet(null);
    }

    private string SeenKey => $"{Id}/talk";

    private string MenuKey => _quest.TalkOf!.IsOnlyAccept ? _quest.TalkOf.AcceptKey! : _quest.TalkOf.Opening?.Value ?? Id;

    private TimbnAnswer? MenuAnswer()
    {
        if (Status != QuestStatus.InProgress || !IsReady())
            return null;

        var answer = _quest.TalkOf!.IsOnlyAccept ? AcceptAnswer() : new TimbnAnswer(MenuKey);
        var needs = Needs;
        needs.ApplyTo(answer, ReputationFor(needs, TalkNpcNow));
        return answer;
    }

    private void Offer(TimbnConversation talk)
    {
        var offer = _quest.OfferTalk!;
        offer.Script.Replay(talk);
        var accept = new TimbnAnswer(offer.AcceptKey!);
        if (offer.AcceptWants is { } wants)
            wants.ApplyTo(accept, ReputationFor(wants, GiverNow));
        TimbnAnswer[] answers = offer.DeclineKey is null ? [accept] : [accept, new TimbnAnswer(offer.DeclineKey)];
        talk.Ask(answers)
            .If(offer.AcceptKey!, then => then.When(() => Accept(offer), started => offer.AcceptThen!.Script.Replay(started)));

        if (offer.DeclineKey is not null)
            talk.If(offer.DeclineKey, then => offer.DeclineThen!.Script.Replay(then));
    }

    private bool Accept(TimbnQuestOffer offer)
    {
        if (Start("accepted"))
            return true;

        if (offer.AcceptWants is { } wants)
        {
            wants.Refund();
            Log("gave back what the accept answer took.");
        }

        return false;
    }

    private void Talk(TimbnConversation talk)
    {
        if (TimbnDialog.HasOwnMenu(talk.Npc))
        {
            talk.ThenOpenMenu();
            return;
        }

        PlayLines(talk, skipOpening: false);
        Answer(talk);
    }

    private void MenuPicked(TimbnConversation talk)
    {
        if (_quest.TalkOf!.IsOnlyAccept)
        {
            AcceptBranch(talk);
            return;
        }

        PlayLines(talk, skipOpening: true);
        Answer(talk);
    }

    private void PlayLines(TimbnConversation talk, bool skipOpening)
    {
        var script = _quest.TalkOf!;
        var opening = script.Opening is null ? 0 : 1;
        if (script.ResumeKey is { } resume && TimbnDialog.HasSeen(SeenKey))
        {
            if (opening == 1 && !skipOpening)
                talk.PlayerSay(script.Opening!.Value, []);

            talk.Say(resume, []);
            return;
        }

        script.Script.Replay(talk, skipOpening ? opening : 0);
        if (script.ResumeKey is not null)
            talk.Do(() => TimbnDialog.MarkSeen(SeenKey));
    }

    private void Answer(TimbnConversation talk)
    {
        var script = _quest.TalkOf!;
        if (script.AcceptKey is null)
        {
            talk.Do(() =>
            {
                if (Finish(paidByAnswer: false, inTalk: true))
                    talk.OnEnded(RunDone);
            });
            return;
        }

        var accept = AcceptAnswer();
        TimbnAnswer[] answers = script.DeclineKey is null ? [accept] : [accept, new TimbnAnswer(script.DeclineKey)];
        talk.Ask(answers).If(accept.Key, AcceptBranch);
        if (script.DeclineKey is not null)
            talk.If(script.DeclineKey, then => script.DeclineThen!.Script.Replay(then));
    }

    private TimbnAnswer AcceptAnswer()
    {
        var script = _quest.TalkOf!;
        var answer = new TimbnAnswer(script.AcceptKey!);
        Cost.ApplyTo(answer, null);
        Rewards.ApplyTo(answer, TimbnNpcs.ReputationOf(TalkNpcNow));
        return answer;
    }

    private void AcceptBranch(TimbnConversation then)
    {
        then.Do(() =>
        {
            if (Finish(paidByAnswer: true))
                then.OnEnded(RunDone);
        });
        _quest.TalkOf!.AcceptThen!.Script.Replay(then);
    }

    private bool Finish(bool paidByAnswer, bool inTalk = false)
    {
        if (Status != QuestStatus.InProgress)
            return false;

        var reputation = TimbnNpcs.ReputationOf(TalkNpcNow);
        if (paidByAnswer)
            Rewards.PayReputation(reputation);
        else
            Rewards.PayAll(reputation);

        Log("completed.");
        TimbnQuests.Complete(Id);
        Deactivate();
        Apply();
        TimbnDialog.Refresh();
        if (paidByAnswer || inTalk)
        {
            StartNextStage();
            return true;
        }

        if (_quest.ThenLines is null)
        {
            RunDone();
            StartNextStage();
            return true;
        }

        var played = TimbnDialog.PlayLines(GiverNow, $"{Id}_done", talk =>
        {
            talk.OnEnded(RunDone);
            _quest.ThenLines.Script.Replay(talk);
        });
        if (!played)
            RunDone();

        StartNextStage();
        return true;
    }

    // The once a second sweep would leave the next stage unstarted for up to a second, and its ReadyOn event unheard.
    private static void StartNextStage() => TimbnQuests.StartContinuedQuests();

    private void RunDone()
    {
        TimbnSafe.Run(_quest.WhenDone, $"{nameof(TimbnQuests)}|OnDone of quest '{Id}'");
        if (_cycle is not null)
            TimbnSafe.Run(() => _cycle.StepDone(this), $"{nameof(TimbnQuests)}|Ending the run of quest '{_cycle.Id}'");
    }

    private GameRes RewardLine(string talkNpc)
    {
        var line = new GameRes();
        var rewards = Rewards;
        if (rewards.MoneyTotal > 0)
            line.Add(LazyConsts.MONEY_KEY, rewards.MoneyTotal);

        if (rewards.ReputationTotal == 0 || talkNpc.Length == 0)
            return line;

        if (TimbnNpcs.ReputationOf(talkNpc) is { } reputation)
            line.Add(reputation, rewards.ReputationTotal);
        else if (_warned.Add($"reward {talkNpc}"))
            Log($"gives reputation with {talkNpc}, who has none in the game, so that reward is skipped.", warning: true);

        return line;
    }

    private string? ReputationFor(TimbnWants wants, string npc)
    {
        if (!wants.WantsReputation || npc.Length == 0)
            return null;

        var reputation = TimbnNpcs.ReputationOf(npc);
        if (reputation is null && _warned.Add($"want {npc}"))
            Log($"needs reputation with {npc}, who has none in the game, so that need is skipped.", warning: true);

        return reputation;
    }

    private string FromRun(Func<TimbnQuestRun, string> read, string what)
    {
        if (Run is not { } run)
            return "";

        var npc = "";
        if (!TimbnSafe.Run(() => npc = read(run), $"{nameof(TimbnQuests)}|Reading the {what} of quest '{Id}' from its run"))
            return "";

        return npc ?? "";
    }

    private void Log(string message, bool warning = false)
    {
        var line = $"{nameof(TimbnQuests)}|Quest '{Id}' {message}";
        if (warning)
            TimbnCorePlugin.Logger.LogWarning(line);
        else
            TimbnCorePlugin.Logger.LogInfo(line);
    }
}
