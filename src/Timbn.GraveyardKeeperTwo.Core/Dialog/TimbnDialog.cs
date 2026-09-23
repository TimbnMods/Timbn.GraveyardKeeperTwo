using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnDialog
{
    private const string LeaveKey = "common_leave";

    private static readonly List<Talk> _talks = [];
    private static float _nextRefresh;
    private static bool _holdsControl;

    internal static IDisposable AddTalk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk)
    {
        var talk = new Talk(npcId, eventId, isOffered, onTalk);
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

    internal static bool Say(WgoData npc, string key, Action then) =>
        Show(key, () => Bubble.Talk(new PhraseData(isPlayer: false, npc, key, then, null)));

    internal static bool PlayerSay(string key, Action then) =>
        Show(key, () => Bubble.Talk(new PhraseData(isPlayer: true, null, key, then, null)));

    internal static AnswerVisualData Answer(string key)
    {
        AnswerData? data = null;
        if (GameBalance.Me.questDefByReqPhrase.TryGetValue(key, out var quest))
        {
            data = quest.finishCheck.GetAnswerDataByReqs();
            var money = TimbnQuests.MoneyRewardOf(quest);
            if (money > 0)
                (data ??= new AnswerData()).AddRewardRes(new SmartRes { gameRes = new GameRes(LazyConsts.MONEY_KEY, money) });
        }

        return new AnswerVisualData { id = key, answerData = data };
    }

    internal static bool Ask(WgoData npc, string[] answers, Action<string> chosen, Action dismissed)
    {
        var visuals = answers.Select(Answer).ToList();
        var addedLeave = visuals.All(v => v.answerData is not null);
        if (addedLeave)
            visuals.Add(new AnswerVisualData { id = LeaveKey });

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
                    if (addedLeave && id == LeaveKey)
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
        _holdsControl = false;
        Refresh();
    }

    internal static void OnLeftGame()
    {
        _holdsControl = false;
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
        talk.Busy = true;
        TakeControl();
        void done()
        {
            if (!talk.Busy)
                return;

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
        if (_holdsControl || !TimbnGame.IsInGame || MainGame.PlayerController == null)
            return;

        _holdsControl = true;
        MainGame.PlayerController.SetControlTakenType(TakenControlType.ByFlow, isEnabled: false);
    }

    private static void ReleaseControl()
    {
        if (!_holdsControl)
            return;

        _holdsControl = false;
        if (TimbnGame.IsInGame && MainGame.PlayerController != null)
            MainGame.PlayerController.SetControlTakenType(TakenControlType.ByFlow, isEnabled: true);
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
            var wanted = !talk.Busy && IsOffered(talk);
            if (wanted && !shown)
                npc.AddInteractionEvent(talk.EventId);
            else if (!wanted && shown)
                npc.RemoveInteractionEvent(talk.EventId);
        }
    }

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
        public Talk(string npcId, string eventId, Func<bool> isOffered, Action<TimbnConversation> onTalk)
        {
            NpcId = npcId;
            EventId = eventId;
            IsOffered = isOffered;
            OnTalk = onTalk;
        }

        public string NpcId { get; }

        public string EventId { get; }

        public Func<bool> IsOffered { get; }

        public Action<TimbnConversation> OnTalk { get; }

        public bool Busy { get; set; }
    }
}
