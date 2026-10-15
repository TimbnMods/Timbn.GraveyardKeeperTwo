using HarmonyLib;
using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnGameMovesTests
{
    private readonly WgoData _herm = Npc();
    private readonly WgoData _linda = Npc();
    private readonly List<(string What, bool Walks)> _told = [];

    private static WgoData Npc()
    {
        var data = (WgoData)RuntimeHelpers.GetUninitializedObject(typeof(WgoData));
        AccessTools.Field(typeof(WgoData), "uniqueId").SetValue(data, new SGuid());
        return data;
    }

    private IDisposable Watch(WgoData npc) => TimbnGameMoves.Add(() => npc, (what, walks) => _told.Add((what, walks)));

    [Fact]
    public void AScriptEventOfTheirsIsReported()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.OnEvent(_herm, "go_home");

        Assert.Equal([("the game fired their go_home event", false)], _told);
    }

    [Fact]
    public void SomeoneElsesEventIsNotTheirs()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.OnEvent(_linda, "go_home");

        Assert.Empty(_told);
    }

    [Fact]
    public void TheInteractionEventIsNoMove()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.OnEvent(_herm, "interaction");

        Assert.Empty(_told);
    }

    [Fact]
    public void EventsDuringAnInteractionAreNoMove()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.InteractionStarted();
        TimbnGameMoves.OnEvent(_herm, "go_home");
        TimbnGameMoves.InteractionEnded();
        TimbnGameMoves.OnEvent(_herm, "go_home");

        Assert.Single(_told);
    }

    [Fact]
    public void AnExtraInteractionEndDoesNotGoBelowZero()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.InteractionEnded();
        TimbnGameMoves.InteractionStarted();
        TimbnGameMoves.OnEvent(_herm, "go_home");
        TimbnGameMoves.InteractionEnded();

        Assert.Empty(_told);
    }

    [Fact]
    public void CoresOwnMovesAreNotReported()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.Ours(() => TimbnGameMoves.OnEvent(_herm, "go_home"));
        var result = TimbnGameMoves.Ours(() =>
        {
            TimbnGameMoves.OnTeleport(_herm);
            return 5;
        });

        Assert.Equal(5, result);
        Assert.Empty(_told);
    }

    [Fact]
    public void OursIsLiftedAgainAfterAThrow()
    {
        using var watch = Watch(_herm);

        Assert.Throws<InvalidOperationException>(() => TimbnGameMoves.Ours(() => throw new InvalidOperationException()));
        TimbnGameMoves.OnEvent(_herm, "go_home");

        Assert.Single(_told);
    }

    [Fact]
    public void NestedOursStaysOursUntilTheOutermostEnds()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.Ours(() =>
        {
            TimbnGameMoves.Ours(() => { });
            TimbnGameMoves.OnEvent(_herm, "go_home");
        });

        Assert.Empty(_told);
    }

    [Fact]
    public void AStoryTeleportIsReportedAsNoWalk()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.OnTeleport(null);
        TimbnGameMoves.OnTeleport(_herm);

        Assert.Equal([("a story graph teleported them", false)], _told);
    }

    [Fact]
    public void MovesAfterAnEventCoreFiredAreNotReported()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.OurEvent(_herm);
        TimbnGameMoves.OnEvent(_herm, "go_home");
        TimbnGameMoves.OnTeleport(_herm);

        Assert.Empty(_told);
        TimbnGameMoves.Reset();
    }

    [Fact]
    public void SettlingIsPerPerson()
    {
        using var watch = Watch(_linda);

        TimbnGameMoves.OurEvent(_herm);
        TimbnGameMoves.OnEvent(_linda, "go_home");

        Assert.Single(_told);
        TimbnGameMoves.Reset();
    }

    [Fact]
    public void ResetEndsEverySettle()
    {
        using var watch = Watch(_herm);

        TimbnGameMoves.OurEvent(_herm);
        TimbnGameMoves.Reset();
        TimbnGameMoves.OnEvent(_herm, "go_home");

        Assert.Single(_told);
    }

    [Fact]
    public void ADisposedWatchHearsNothing()
    {
        var watch = Watch(_herm);
        watch.Dispose();

        TimbnGameMoves.OnEvent(_herm, "go_home");

        Assert.Empty(_told);
    }

    [Fact]
    public void AWatchWithNoPersonYetHearsNothing()
    {
        using var watch = TimbnGameMoves.Add(() => null, (what, walks) => _told.Add((what, walks)));

        TimbnGameMoves.OnEvent(_herm, "go_home");

        Assert.Empty(_told);
    }
}
