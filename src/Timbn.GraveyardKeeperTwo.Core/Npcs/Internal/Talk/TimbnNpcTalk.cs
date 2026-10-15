namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps track of who the player is talking to and holds that person still for it. Core's own conversations say when
/// they start and end and always hold the person. The game's own talks start when the player interacts with someone,
/// end once the game gives the player's control back, and hold only people a schedule or a run asked for. A
/// conversation pauses the person's walk and turns them toward the player in one counted pause, so it never resumes
/// a walk a story scene still holds.
/// </summary>
internal static class TimbnNpcTalk
{
    private static readonly AccessTools.FieldRef<WgoData, string> _customTrigger = AccessTools.FieldRefAccess<WgoData, string>("customAnimationTrigger");

    private static readonly List<TimbnNpc> _stoppers = [];
    private static readonly List<Talk> _talks = [];
    private static readonly TimbnTalkWatch _gameWatch = new();
    private static Talk? _gameTalk;

    internal static WgoData? Partner => _talks.Count > 0 ? _talks[^1].Npc : null;

    internal static int Session { get; private set; }

    internal static bool IsTalkingTo(WgoData npc) => _talks.Exists(talk => talk.Npc.UniqueId == npc.UniqueId);

    internal static IDisposable StopWhileTalking(TimbnNpc npc)
    {
        _stoppers.Add(npc);
        return new TimbnUndo(() => _stoppers.Remove(npc));
    }

    internal static IDisposable Begin(WgoData npc) => Start(npc, ours: true);

    internal static void GameTalkStarted(WgoData npc)
    {
        _gameWatch.Restart();
        if (_gameTalk is { } talk && talk.Npc.UniqueId == npc.UniqueId)
            return;

        _gameTalk?.Dispose();
        _gameTalk = Start(npc, ours: false);
    }

    internal static void Tick()
    {
        if (_gameTalk is { } talk && _gameWatch.Ended(TimbnStoryScene.GameHasPlayer()))
        {
            _gameTalk = null;
            talk.Dispose();
        }
    }

    internal static void Reset()
    {
        _talks.Clear();
        _gameTalk = null;
        _gameWatch.Restart();
    }

    private static Talk Start(WgoData npc, bool ours)
    {
        if (_talks.Count == 0)
            Session++;

        var talk = new Talk(npc, ours);
        _talks.Add(talk);
        return talk;
    }

    private static IEnumerable<TimbnNpc> StoppersOf(WgoData npc) =>
        _stoppers.Where(stopper => !stopper.IsGone && stopper.Data.UniqueId == npc.UniqueId).ToList();

    private sealed class Talk : IDisposable
    {
        private readonly bool _turnBack;
        private readonly Vector2 _direction;
        private readonly IDisposable? _pause;
        private readonly bool _stoppedWalk;
        private string? _animation;
        private string? _point;
        private NPCPointOfInterestAnimationData? _roll;
        private bool _ended;

        public Talk(WgoData npc, bool ours)
        {
            Npc = npc;
            _direction = npc.MovableDirection;
            var walking = npc.MovementComponent is { IsMoving: true };
            var stoppers = StoppersOf(npc);
            _turnBack = ours && !walking;
            if (!ours && !stoppers.Any())
                return;

            _pause = TimbnWalkPause.Hold(npc);
            if (ours)
                TimbnSafe.Run(() => HoldAnimation(npc), $"{nameof(TimbnNpcTalk)}|Stopping {npc.id}'s animation for the conversation");

            if (ours || walking)
                TimbnSafe.Run(() => TurnToPlayer(npc), $"{nameof(TimbnNpcTalk)}|Turning {npc.id} toward the player");

            _stoppedWalk = walking;
            if (walking)
            {
                foreach (var stopper in stoppers)
                    stopper.Log("stopped for the conversation");
            }
        }

        public WgoData Npc { get; }

        public void Dispose()
        {
            if (_ended)
                return;

            _ended = true;
            _talks.Remove(this);
            if (_gameTalk == this)
                _gameTalk = null;

            TimbnSafe.Run(PutBack, $"{nameof(TimbnNpcTalk)}|Putting {Npc.id} back after a talk");
        }

        private static void TurnToPlayer(WgoData npc)
        {
            var toward = TimbnPlayer.Position - npc.Position;
            if (toward.x != 0f || toward.z != 0f)
                npc.MovableDirection = new Vector2(toward.x, toward.z);
        }

        private void HoldAnimation(WgoData npc)
        {
            var trigger = _customTrigger(npc);
            if (string.IsNullOrEmpty(trigger))
                return;

            _animation = trigger;
            _point = npc.occupiedPointOfInterest;
            var simulator = MainGame.Instance.GameSave.npcLifeSimulatorData;
            _roll = simulator.AnimationDatas.Find(data => data.WgoId.Guid == npc.UniqueId.Guid);
            if (_roll is not null)
                simulator.TryRemoveAnimationData(npc.UniqueId);

            npc.SetCustomAnimationTrigger();
            npc.SetTriggerToAnimator(AnimationComponentBase.RESET_TO_IDLE_TRIGGER);
        }

        private void RestoreAnimation()
        {
            if (_animation is null || !TimbnGame.IsInGame)
                return;

            if (Npc.occupiedPointOfInterest != _point || Npc.MovementComponent is { IsMoving: true } || !string.IsNullOrEmpty(_customTrigger(Npc)))
                return;

            if (_roll is not null)
            {
                MainGame.Instance.GameSave.npcLifeSimulatorData.AddAnimationData(_roll);
                return;
            }

            Npc.SetCustomAnimationTrigger(_animation);
            Npc.SetTriggerToAnimator(_animation);
        }

        private void PutBack()
        {
            if (_pause is null)
                return;

            TimbnSafe.Run(RestoreAnimation, $"{nameof(TimbnNpcTalk)}|Giving {Npc.id} their animation back");
            _pause.Dispose();
            if (_stoppedWalk)
            {
                foreach (var stopper in StoppersOf(Npc))
                    stopper.Log("the conversation ended");
            }

            if (_turnBack && TimbnGame.IsInGame && Npc.MovementComponent is not { IsMoving: true })
                Npc.MovableDirection = _direction;
        }
    }
}
