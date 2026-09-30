using LazyBearTechnology;
using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnDialog
{
    private const string _iconId = "timbn_speech_bubble";
    private const string _leaveKey = "common_leave";
    private const int _maxRememberedLines = 256;
    private const string _gameIconId = "icon_speech_bubble";
    private static readonly List<Talk> _talks = [];
    private static readonly HashSet<string> _warnedKeys = [];
    private static readonly HashSet<string> _modLines = [];
    private static Func<HashSet<string>>? _seenTalks;
    private static float _nextRefresh;
    private static IDisposable? _control;
    private static bool _iconFailed;

    internal static IDisposable AddTalk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk, Func<Color>? bubbleColor = null, string? onceKey = null)
    {
        if (_talks.Find(t => t.EventId == eventId && t.NpcId != npcId) is { } other)
            throw new InvalidOperationException($"The talk id '{eventId}' is already used on {other.NpcId}. A talk id has to be unique, since it is also the NPC's interaction event and picks the bubble color.");

        var talk = new Talk(npcId, eventId, isOffered, onTalk, bubbleColor ?? (() => TimbnPluginDialog.DefaultBubbleColor), onceKey);
        _talks.RemoveAll(t => t.NpcId == npcId && t.EventId == eventId);
        _talks.Add(talk);
        Refresh();
        return new TimbnUndo(() =>
        {
            if (!_talks.Remove(talk) || _talks.Any(t => t.NpcId == npcId && t.EventId == eventId))
                return;

            if (TimbnGame.IsInGame && MainGame.WorldData.GetWgoData(npcId) is { } npc)
                npc.RemoveInteractionEvent(eventId);
        });
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
        WarnIfMissing(key);
        return TryFormat(key, values, out var text)
            && Show(key, () => Bubble.Talk(new PhraseData(isPlayer: false, npc, text, then, null)));
    }

    internal static bool PlayerSay(string key, object[] values, Action then)
    {
        WarnIfMissing(key);
        return TryFormat(key, values, out var text)
            && Show(key, () => Bubble.Talk(new PhraseData(isPlayer: true, null, text, then, null)));
    }

    internal static bool AllowVoice(VoiceOverPlayer player, string id)
    {
        if (!_modLines.Contains(id) || !_talks.Any(t => t.Busy))
            return true;

        player.Stop();
        return false;
    }

    internal static void UseSeenTalks(Func<HashSet<string>> seenTalks) => _seenTalks = seenTalks;

    internal static bool HasSeen(string onceKey) => _seenTalks?.Invoke().Contains(onceKey) == true;

    internal static void ResetSeen(string onceKey)
    {
        if (_seenTalks?.Invoke().Remove(onceKey) == true)
            Refresh();
    }

    internal static AnswerVisualData Answer(string key, IReadOnlyDictionary<string, AnswerData>? prices = null)
    {
        AnswerData? data = null;
        if (prices is not null && prices.TryGetValue(key, out var price))
        {
            data = price;
        }
        else if (GameBalance.Me.questDefByReqPhrase.TryGetValue(key, out var quest))
        {
            data = quest.finishCheck.GetAnswerDataByReqs();
            var money = TimbnQuests.MoneyRewardOf(quest);
            if (money > 0)
                (data ??= new AnswerData()).AddRewardRes(new SmartRes { gameRes = new GameRes(LazyConsts.MONEY_KEY, money) });
        }

        return new AnswerVisualData { id = key, answerData = data };
    }

    internal static bool Ask(WgoData npc, string[] answers, Action<string> chosen, Action dismissed, IReadOnlyDictionary<string, AnswerData>? prices = null)
    {
        foreach (var answer in answers)
            WarnIfMissing(answer);

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
                    if (addedLeave && id == _leaveKey)
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

    private static bool TryFormat(string key, object[] values, out string text)
    {
        text = key;
        if (values.Length == 0)
        {
            RememberLine(text);
            return true;
        }

        try
        {
            text = LLBase.L(key);
            for (var i = values.Length - 1; i >= 0; i--)
            {
                var value = values[i] is Func<object> compute ? compute() : values[i];
                text = text.Replace($"%{i + 1}", string.Format(CultureInfo.CurrentCulture, "{0}", value));
            }

            RememberLine(text);
            return true;
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnDialog)}|Filling in the line '{key}' threw: {ex}");
            return false;
        }
    }

    private static void RememberLine(string text)
    {
        if (_modLines.Count >= _maxRememberedLines)
            _modLines.Clear();

        _modLines.Add(text);
    }

    private static bool IsSeen(Talk talk) => talk.OnceKey is not null && HasSeen(talk.OnceKey);

    private static void MarkSeen(Talk talk)
    {
        if (talk.OnceKey is not null)
            _seenTalks?.Invoke().Add(talk.OnceKey);
    }

    private static void WarnIfMissing(string key)
    {
        if (!LLBase.IsCurrentLangLoaded || LLBase.HasL(key) || !_warnedKeys.Add(key))
            return;

        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnDialog)}|The line '{key}' has no text in the current language, so the game shows the key itself. Add it with Text.Add or a lang file.");
    }

    private static bool Show(string what, Action show)
    {
        try
        {
            show();
            return true;
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnDialog)}|Showing '{what}' threw: {ex}");
            return false;
        }
    }

    internal static void OnGameStarted()
    {
        ReleaseControl();
        Refresh();
    }

    internal static void OnLeftGame()
    {
        ReleaseControl();
        foreach (var talk in _talks)
            talk.Busy = false;
    }

    internal static void OnUpdate()
    {
        if (!TimbnGame.IsInGame || Time.unscaledTime < _nextRefresh)
            return;

        _nextRefresh = Time.unscaledTime + 1f;
        Refresh();
    }

    internal static bool TryHandleInteraction(WgoData npc)
    {
        var next = npc.PeekFirstAddedEvent();
        if (next is null)
            return false;

        var talk = _talks.Find(t => t.NpcId == npc.id && t.EventId == next.str);
        if (talk is null)
            return false;

        npc.RemoveInteractionEvent(talk.EventId);
        if (HasQuestToTurnIn(npc))
            return false;

        talk.Busy = true;
        MarkSeen(talk);
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Talk '{talk.EventId}' started with {npc.id}.");
        TakeControl();
        void done()
        {
            if (!talk.Busy)
                return;

            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnDialog)}|Talk '{talk.EventId}' ended.");
            talk.Busy = false;
            ReleaseControl();
            Refresh();
        }

        var conversation = new TimbnConversation(npc, talk.EventId);
        try
        {
            talk.OnTalk(conversation);
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnDialog)}|Talk '{talk.EventId}' on {talk.NpcId} threw: {ex}");
            done();
            return true;
        }

        conversation.Run(done);

        return true;
    }

    internal static IDisposable RemoveForSave()
    {
        if (!TimbnGame.IsInGame)
            return new TimbnUndo(() => { });

        foreach (var talk in _talks)
            MainGame.WorldData.GetWgoData(talk.NpcId)?.RemoveInteractionEvent(talk.EventId);

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

    private static void Refresh()
    {
        if (!TimbnGame.IsInGame)
            return;

        foreach (var talk in _talks)
        {
            if (MainGame.WorldData.GetWgoData(talk.NpcId) is not { } npc)
                continue;

            var shown = npc.Events.Any(e => e.str == talk.EventId);
            var wanted = !talk.Busy && !HasQuestToTurnIn(npc) && !IsSeen(talk) && IsOffered(talk);
            if (wanted && !shown)
                npc.AddInteractionEvent(talk.EventId);
            else if (!wanted && shown)
                npc.RemoveInteractionEvent(talk.EventId);
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
        _talks.Any(t => t.NpcId == npc.id && t.EventId == eventId);

    private static bool HasQuestToTurnIn(WgoData npc) =>
        MainGame.Instance?.GameSave?.questSystemData?.WgoHasReadyToFinishQuest(npc.id) == true;

    private static bool IsOffered(Talk talk)
    {
        try
        {
            return talk.IsOffered();
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnDialog)}|isOffered for '{talk.EventId}' threw: {ex}");
            return false;
        }
    }

    private sealed class Talk
    {
        public Talk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk, Func<Color> bubbleColor, string? onceKey)
        {
            NpcId = npcId;
            EventId = eventId;
            IsOffered = isOffered;
            OnTalk = onTalk;
            BubbleColor = bubbleColor;
            OnceKey = onceKey;
        }

        public string NpcId { get; }

        public string EventId { get; }

        public Func<bool> IsOffered { get; }

        public Action<TimbnConversation> OnTalk { get; }

        public Func<Color> BubbleColor { get; }

        public string? OnceKey { get; }

        public bool Busy { get; set; }
    }
}
