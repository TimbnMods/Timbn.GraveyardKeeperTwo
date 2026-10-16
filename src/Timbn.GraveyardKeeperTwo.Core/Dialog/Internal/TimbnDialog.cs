using LazyBearTechnology;
using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Runs the talks plugins add to NPCs, from the speech icon on the NPC through the conversation to giving control back.</summary>
internal static class TimbnDialog
{
    private const string _iconId = "timbn_speech_bubble";
    private const string _leaveKey = "common_leave";
    private const string _errorAnswer = "Error";
    private const string _gameIconId = "icon_speech_bubble";

    private static readonly List<Talk> _talks = [];
    private static readonly List<MenuAnswer> _menuAnswers = [];
    private static readonly HashSet<string> _noMenu = [];
    private static Func<HashSet<string>>? _seenTalks;
    private static IDisposable? _control;
    private static bool _iconFailed;
    private static bool _menuJoined;

    internal static IDisposable AddTalk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk, Func<Color>? bubbleColor = null, string? onceKey = null) =>
        AddTalk(npcId, () => npcId, eventId, isOffered, onTalk, bubbleColor, onceKey);

    internal static IDisposable AddTalk(Func<string> npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk, Func<Color>? bubbleColor = null) =>
        AddTalk(null, npcId, eventId, isOffered, onTalk, bubbleColor, null);

    private static IDisposable AddTalk(string? fixedNpc, Func<string> npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk, Func<Color>? bubbleColor, string? onceKey)
    {
        if (_talks.Find(t => t.EventId == eventId && (fixedNpc is null || t.FixedNpc != fixedNpc)) is { } other)
            throw new InvalidOperationException($"The talk id '{eventId}' is already used on {other.FixedNpc ?? "another NPC"}. A talk id has to be unique, since it is also the NPC's interaction event and picks the bubble color.");

        var talk = new Talk(fixedNpc, npcId, eventId, isOffered, onTalk, bubbleColor ?? (() => TimbnPluginDialog.DefaultBubbleColor), onceKey);
        var replaced = _talks.FindAll(t => t.EventId == eventId);
        _talks.RemoveAll(t => t.EventId == eventId);
        if (replaced.Count > 0)
            TimbnCorePlugin.Logger?.LogWarning($"{nameof(TimbnDialog)}|The talk '{eventId}' on {fixedNpc ?? "an NPC"} was added again while it was still in, so the new one replaces it.");

        foreach (var old in replaced)
            talk.ShownOn ??= old.ShownOn;

        _talks.Add(talk);
        Refresh();
        return new TimbnUndo(() =>
        {
            if (!_talks.Remove(talk) || _talks.Any(t => t.EventId == eventId))
                return;

            var shownOn = talk.ShownOn ?? (fixedNpc is null ? null : TimbnNpcs.FindData(fixedNpc));
            shownOn?.RemoveInteractionEvent(eventId);
            talk.ShownOn = null;
        });
    }

    internal static IDisposable AddMenuAnswer(string npcId, Func<TimbnAnswer?> answer, Action<TimbnConversation> onPicked) =>
        AddMenuAnswer(() => npcId, answer, onPicked);

    internal static IDisposable AddMenuAnswer(Func<string> npcId, Func<TimbnAnswer?> answer, Action<TimbnConversation> onPicked)
    {
        var added = new MenuAnswer(npcId, answer, onPicked);
        _menuAnswers.Add(added);
        return new TimbnUndo(() => _menuAnswers.Remove(added));
    }

    internal static string IconFor(string eventId, string gameIcon)
    {
        if (gameIcon != _gameIconId || _talks.Find(t => t.EventId == eventId) is not { } talk)
            return gameIcon;

        return EnsureIcon(talk.BubbleColor()) ?? gameIcon;
    }

    internal static void OnEventAdded(WgoData npc, string eventId)
    {
        if (_talks.Count == 0 || IsTalkOf(npc, eventId) || !npc.Events.Any(e => IsTalkOf(npc, e.str)))
            return;

        var queued = npc.Events.Where(e => IsTalkOf(npc, e.str)).Select(e => e.str).ToList();
        foreach (var id in queued)
        {
            npc.RemoveInteractionEvent(id);
            npc.AddInteractionEvent(id);
        }
    }

    internal static bool Say(WgoData npc, string key, object[] values, Action then)
    {
        TimbnLocale.WarnIfMissing(key);
        if (!TryFormat(key, values, out var shown))
            return false;

        void said()
        {
            GlobalEventsSystem.FireTrigger(GlobalEventsSystem.Event.Type.SpeechSaid, key);
            then();
        }

        return Show(key, () => Bubble.Talk(new PhraseData(isPlayer: false, npc, shown, said, null)));
    }

    internal static bool PlayerSay(string key, object[] values, Action then)
    {
        TimbnLocale.WarnIfMissing(key);
        if (!TryFormat(key, values, out var shown))
            return false;

        void said()
        {
            GlobalEventsSystem.FireTrigger(GlobalEventsSystem.Event.Type.SpeechSaid, key);
            then();
        }

        return Show(key, () => Bubble.Talk(new PhraseData(isPlayer: true, null, shown, said, null)));
    }

    internal static bool Wait(float seconds, Action then)
    {
        var until = Time.realtimeSinceStartupAsDouble + seconds;
        return When(() => Time.realtimeSinceStartupAsDouble >= until, then);
    }

    private static bool When(Func<bool> ready, Action then)
    {
        if (TimbnCorePlugin.Instance is not { } core)
            return false;

        IDisposable? frames = null;
        frames = core.SessionSubscriptions.Add(core.Events.Update(() =>
        {
            if (!ready())
                return;

            frames?.Dispose();
            then();
        }));
        return true;
    }

    internal static void UseSeenTalks(Func<HashSet<string>> seenTalks) => _seenTalks = seenTalks;

    internal static bool HasSeen(string onceKey) => _seenTalks?.Invoke().Contains(onceKey) == true;

    internal static void MarkSeen(string onceKey) => _seenTalks?.Invoke().Add(onceKey);

    internal static void ResetSeen(string onceKey)
    {
        if (_seenTalks?.Invoke().Remove(onceKey) == true)
            Refresh();
    }

    internal static AnswerVisualData Answer(string key, IReadOnlyDictionary<string, AnswerData>? prices = null)
    {
        AnswerData? data = null;
        if (prices is not null && prices.TryGetValue(key, out var price))
            data = price;
        else if (GameBalance.Me.questDefByReqPhrase.TryGetValue(key, out var quest))
            data = quest.finishCheck.GetAnswerDataByReqs();

        return new AnswerVisualData { id = key, answerData = data };
    }

    internal static bool Ask(WgoData npc, string[] answers, Action<string> chosen, Action dismissed, IReadOnlyDictionary<string, AnswerData>? prices = null)
    {
        foreach (var answer in answers)
            TimbnLocale.WarnIfMissing(answer);

        var visuals = answers.Select(answer => Answer(answer, prices)).ToList();
        var addedLeave = visuals.All(v => v.answerData is not null);
        if (addedLeave)
            visuals.Add(new AnswerVisualData { id = _leaveKey });

        var picked = false;
        return Show(
            string.Join(", ", answers),
            () => Bubble.ShowMultiAnswer(
                visuals,
                MainGame.PlayerController.BubblePoint,
                npc,
                id =>
                {
                    picked = true;
                    if (id == _errorAnswer || (addedLeave && id == _leaveKey))
                    {
                        dismissed();
                        return;
                    }

                    GlobalEventsSystem.FireTrigger(GlobalEventsSystem.Event.Type.MultiAnswerSay, id);
                    chosen(id);
                },
                () =>
                {
                    if (!picked)
                        dismissed();
                }));
    }

    private static bool TryFormat(string key, object[] values, out string shown)
    {
        shown = key;
        if (values.Length == 0)
            return true;

        try
        {
            var text = LLBase.L(key);
            for (var i = values.Length - 1; i >= 0; i--)
            {
                var value = values[i] is Func<object> compute ? compute() : values[i];
                text = text.Replace($"%{i + 1}", string.Format(CultureInfo.CurrentCulture, "{0}", value));
            }

            shown = TimbnLocale.Scratch(text) ?? text;
            return true;
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnDialog)}|Filling in the line '{key}' threw: {ex}");
            return false;
        }
    }

    private static bool IsSeen(Talk talk) => talk.OnceKey is not null && HasSeen(talk.OnceKey);

    private static void MarkSeen(Talk talk)
    {
        if (talk.OnceKey is not null)
            _seenTalks?.Invoke().Add(talk.OnceKey);
    }

    private static bool Show(string what, Action show) => TimbnSafe.Run(show, $"{nameof(TimbnDialog)}|Showing '{what}'");

    internal static void OnGameStarted()
    {
        ReleaseControl();
        Refresh();
        BuildVoiceOverIndex();
    }

    // The game indexes all of its voice over clips (about 5,300) on the first line anyone says, a 46 ms stall on the
    // session's first talk, so Core builds the index while the save loads. Kept out of a field so the unit tests,
    // which have no audio module, never load VoiceOverPlayer.
    private static void BuildVoiceOverIndex()
    {
        if (VoiceOverSettings.IsEnabled && AccessTools.Method(typeof(VoiceOverPlayer), "EnsureLocationsCache") is { } build)
            build.Invoke(null, null);
    }

    internal static void OnLeftGame()
    {
        ReleaseControl();
        _noMenu.Clear();
        TimbnStoryScene.Reset();
        foreach (var talk in _talks)
        {
            talk.Busy = false;
            talk.ShownOn = null;
        }
    }

    internal static bool TryHandleInteraction(WgoData npc)
    {
        var next = npc.PeekFirstAddedEvent();
        if (next is null)
            return false;

        var talk = _talks.Find(t => t.EventId == next.str && t.NpcNow() == npc.id);
        if (talk is null)
            return false;

        npc.RemoveInteractionEvent(talk.EventId);
        talk.ShownOn = null;
        if (HasQuestToTurnIn(npc))
            return false;

        talk.Busy = true;
        MarkSeen(talk);
        RunConversation(npc, talk.EventId, talk.OnTalk, () =>
        {
            talk.Busy = false;
            Refresh();
        });
        return true;
    }

    internal static bool HasOwnMenu(WgoData npc)
    {
        if (!npc.IsInteractable || npc.Definition?.interactionType != WGODef.InteractionType.Script || _noMenu.Contains(npc.id))
            return false;

        var has = false;
        TimbnSafe.Run(() => has = TimbnNpcMenu.MainMenu(npc) is { } menu && TimbnNpcMenu.Offers(menu, _leaveKey), $"{nameof(TimbnDialog)}|Reading {npc.id}'s menu");
        return has;
    }

    private static void OpenMenu(WgoData npc)
    {
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Opening {npc.id}'s own menu.");
        _menuJoined = false;
        TimbnSafe.Run(() => npc.FireEvent(TimbnNpcMenu.InteractionEvent), $"{nameof(TimbnDialog)}|Opening {npc.id}'s menu");
        if (_menuJoined || !_noMenu.Add(npc.id))
            return;

        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnDialog)}|{npc.id}'s menu did not open with a leave answer, so plugin answers there play as a talk of their own for the rest of this save.");
    }

    internal static void AddToMenu(List<AnswerVisualData> answers, Transform target, WgoData? npc, ref Action<string> onChosen, Action onDisappeared, bool isOverBlackout)
    {
        if (npc is null || _control is not null)
            return;

        var isMain = false;
        if (!TimbnSafe.Run(() => isMain = IsMainMenu(npc, answers), $"{nameof(TimbnDialog)}|Reading {npc.id}'s menu") || !isMain)
            return;

        _menuJoined = true;
        var menu = new GameMenu(npc, [.. answers], onChosen, target, onDisappeared, isOverBlackout);
        var leave = answers.FindIndex(a => a.id == _leaveKey);
        var picks = new Dictionary<string, Action<TimbnConversation>>();
        foreach (var menuAnswer in _menuAnswers.Where(m => m.NpcNow() == npc.id).ToList())
        {
            TimbnAnswer? answer = null;
            if (!TimbnSafe.Run(() => answer = menuAnswer.Answer(), $"{nameof(TimbnDialog)}|A menu answer for {npc.id}") || answer is null)
                continue;

            TimbnLocale.WarnIfMissing(answer.Key);
            answers.Insert(leave++, new AnswerVisualData { id = answer.Id, answerData = answer.ToData() });
            picks[answer.Id] = menuAnswer.OnPicked;
        }

        if (picks.Count == 0)
            return;

        onChosen = id =>
        {
            if (!picks.TryGetValue(id, out var script))
            {
                menu.OnChosen(id);
                return;
            }

            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Menu answer '{id}' picked on {npc.id}.");
            RunConversation(npc, id, script, () => ShowAgain(menu));
        };
    }

    private static bool IsMainMenu(WgoData npc, List<AnswerVisualData> answers) =>
        answers.Exists(a => a.id == _leaveKey)
        && TimbnNpcMenu.MainMenu(npc) is { } menu
        && answers.TrueForAll(a => TimbnNpcMenu.Offers(menu, a.id));

    private static void ShowAgain(GameMenu menu)
    {
        if (!TimbnGame.IsInGame)
            return;

        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Showing {menu.Npc.id}'s menu again.");
        if (!Show($"{menu.Npc.id}'s menu", () => Bubble.ShowMultiAnswer([.. menu.Answers], menu.Target, menu.Npc, menu.OnChosen, menu.OnDisappeared, menu.IsOverBlackout)))
            menu.OnChosen(_leaveKey);
    }

    internal static bool PlayLines(string npcId, string talkId, Action<TimbnConversation> script)
    {
        if (TimbnNpcs.FindData(npcId) is not { } npc)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnDialog)}|Skipped the lines '{talkId}', since {npcId} is not in the world.");
            return false;
        }

        void play() => RunConversation(npc, talkId, script, () => { }, withNpc: false);
        if (IsPlayerFree())
        {
            play();
            return true;
        }

        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|The lines '{talkId}' wait until the player is free.");
        if (!When(IsPlayerFree, play))
            play();

        return true;
    }

    internal static bool Speak(string npcId, string key, Action ended)
    {
        if (_control is not null || TimbnNpcs.FindData(npcId) is not { } npc)
            return false;

        RunConversation(npc, key, conversation => conversation.Say(key, []), ended);
        return true;
    }

    private static bool IsPlayerFree() => _control is null && !TimbnStoryScene.GameHasPlayer();

    private static void RunConversation(WgoData npc, string talkId, Action<TimbnConversation> script, Action ended, bool withNpc = true)
    {
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Talk '{talkId}' started with {npc.id}.");
        TakeControl();
        var talk = withNpc ? TimbnNpcTalk.Begin(npc) : null;
        var conversation = new TimbnConversation(npc, talkId);
        var finished = false;
        void done()
        {
            if (finished)
                return;

            finished = true;
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Talk '{talkId}' ended.");
            if (conversation.TurnedOnBars)
                TimbnStoryScene.HideBars();

            talk?.Dispose();
            ReleaseControl();
            conversation.RunEnded();
            TimbnSafe.Run(ended, $"{nameof(TimbnDialog)}|Ending talk '{talkId}'");
            if (conversation.OpensMenu)
                OpenMenu(npc);
        }

        try
        {
            script(conversation);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnDialog)}|Talk '{talkId}' on {npc.id} threw: {ex}");
            done();
            return;
        }

        conversation.Run(done);
    }

    internal static IDisposable RemoveForSave()
    {
        if (!TimbnGame.IsInGame)
            return TimbnUndo.None;

        foreach (var talk in _talks)
        {
            (talk.ShownOn ?? TimbnNpcs.FindData(talk.NpcNow()))?.RemoveInteractionEvent(talk.EventId);
            talk.ShownOn = null;
        }

        return new TimbnUndo(Refresh);
    }

    private static void TakeControl()
    {
        if (_control is null && TimbnGame.IsInGame)
            _control = TimbnControl.Take();
    }

    private static void ReleaseControl()
    {
        var control = _control;
        _control = null;
        control?.Dispose();
    }

    internal static void Refresh()
    {
        if (!TimbnGame.IsInGame)
            return;

        foreach (var talk in _talks)
        {
            var npc = TimbnNpcs.FindData(talk.NpcNow());
            if (talk.ShownOn is { } old && old != npc)
            {
                old.RemoveInteractionEvent(talk.EventId);
                talk.ShownOn = null;
            }

            if (npc is null)
                continue;

            var shown = npc.Events.Any(e => e.str == talk.EventId);
            var wanted = !talk.Busy && !HasQuestToTurnIn(npc) && !IsSeen(talk) && IsOffered(talk);
            if (wanted && !shown)
                npc.AddInteractionEvent(talk.EventId);
            else if (!wanted && shown)
                npc.RemoveInteractionEvent(talk.EventId);

            talk.ShownOn = wanted ? npc : null;
        }
    }

    private static string? EnsureIcon(Color tint)
    {
        var name = $"{_iconId}_{ColorUtility.ToHtmlStringRGB(tint)}";
        if (TimbnSprites.TryGet(name, out _))
            return name;

        if (_iconFailed)
            return null;

        try
        {
            var source = LazySingletonSO<EasySpritesCollection>.Instance.GetSprite(_gameIconId);
            if (source == null)
                throw new InvalidOperationException($"The game has no {_gameIconId} sprite.");

            TimbnSprites.AddTinted(name, source, tint);
            return name;
        }
        catch (Exception ex)
        {
            _iconFailed = true;
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnDialog)}|Could not tint the {_gameIconId} icon, so mod talks show the game's bubble. {ex.Message}");
            return null;
        }
    }

    private static bool IsTalkOf(WgoData npc, string eventId) =>
        _talks.Any(t => t.EventId == eventId && t.NpcNow() == npc.id);

    private static bool HasQuestToTurnIn(WgoData npc) =>
        MainGame.Instance?.GameSave?.questSystemData?.WgoHasReadyToFinishQuest(npc.id) == true;

    private static bool IsOffered(Talk talk)
    {
        var offered = false;
        return TimbnSafe.Run(() => offered = talk.IsOffered(), $"{nameof(TimbnDialog)}|isOffered for '{talk.EventId}'") && offered;
    }

    private sealed class GameMenu
    {
        public GameMenu(WgoData npc, List<AnswerVisualData> answers, Action<string> onChosen, Transform target, Action onDisappeared, bool isOverBlackout)
        {
            Npc = npc;
            Answers = answers;
            OnChosen = onChosen;
            Target = target;
            OnDisappeared = onDisappeared;
            IsOverBlackout = isOverBlackout;
        }

        public WgoData Npc { get; }

        public List<AnswerVisualData> Answers { get; }

        public Action<string> OnChosen { get; }

        public Transform Target { get; }

        public Action OnDisappeared { get; }

        public bool IsOverBlackout { get; }
    }

    private sealed class MenuAnswer
    {
        private readonly Func<string> _npcId;

        public MenuAnswer(Func<string> npcId, Func<TimbnAnswer?> answer, Action<TimbnConversation> onPicked)
        {
            _npcId = npcId;
            Answer = answer;
            OnPicked = onPicked;
        }

        public string? NpcNow()
        {
            string? id = null;
            TimbnSafe.Run(() => id = _npcId(), $"{nameof(TimbnDialog)}|The npc of a menu answer");
            return id;
        }

        public Func<TimbnAnswer?> Answer { get; }

        public Action<TimbnConversation> OnPicked { get; }
    }

    private sealed class Talk
    {
        private readonly Func<string> _npcId;

        public Talk(string? fixedNpc, Func<string> npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk, Func<Color> bubbleColor, string? onceKey)
        {
            FixedNpc = fixedNpc;
            _npcId = npcId;
            EventId = eventId;
            IsOffered = isOffered;
            OnTalk = onTalk;
            BubbleColor = bubbleColor;
            OnceKey = onceKey;
        }

        public string? FixedNpc { get; }

        public string EventId { get; }

        public WgoData? ShownOn { get; set; }

        public Func<bool> IsOffered { get; }

        public Action<TimbnConversation> OnTalk { get; }

        public Func<Color> BubbleColor { get; }

        public string? OnceKey { get; }

        public bool Busy { get; set; }

        public string? NpcNow()
        {
            string? id = null;
            TimbnSafe.Run(() => id = _npcId(), $"{nameof(TimbnDialog)}|The npc of talk '{EventId}'");
            return id;
        }
    }
}
