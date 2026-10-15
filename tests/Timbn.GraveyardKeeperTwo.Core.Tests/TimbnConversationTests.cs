using System.Runtime.CompilerServices;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnConversationTests
{
    private readonly List<string> _log = [];

    private static TimbnConversation Talk() => new(null!, "test_talk");

    private Action Note(string text) => () => _log.Add(text);

    [Fact]
    public void StepsRunInOrderAndThenDoneIsCalled()
    {
        var talk = Talk().Do(Note("one")).Do(Note("two"));

        talk.Run(Note("done"));

        Assert.Equal(["one", "two", "done"], _log);
    }

    [Fact]
    public void AnEmptyConversationFinishesAtOnce()
    {
        Talk().Run(Note("done"));

        Assert.Equal(["done"], _log);
    }

    [Fact]
    public void WhenRunsTheChosenBranchThenCarriesOn()
    {
        var talk = Talk()
            .When(() => true, then => then.Do(Note("yes")), otherwise => otherwise.Do(Note("no")))
            .When(() => false, then => then.Do(Note("yes2")), otherwise => otherwise.Do(Note("no2")))
            .Do(Note("after"));

        talk.Run(Note("done"));

        Assert.Equal(["yes", "no2", "after", "done"], _log);
    }

    [Fact]
    public void WhenWithNoOtherwiseSkipsAhead()
    {
        var talk = Talk().When(() => false, then => then.Do(Note("yes"))).Do(Note("after"));

        talk.Run(Note("done"));

        Assert.Equal(["after", "done"], _log);
    }

    [Fact]
    public void BranchesNestAndEveryStepOfThemRuns()
    {
        var talk = Talk().When(
            () => true,
            then => then
                .Do(Note("a"))
                .When(() => true, inner => inner.Do(Note("b")).Do(Note("c")))
                .Do(Note("d")));

        talk.Run(Note("done"));

        Assert.Equal(["a", "b", "c", "d", "done"], _log);
    }

    [Fact]
    public void AskWithNoAnswersIsRefused() =>
        Assert.Throws<ArgumentException>(() => Talk().Ask());

    [Fact]
    public void IfHasToFollowAsk()
    {
        Assert.Throws<InvalidOperationException>(() => Talk().If("a", _ => { }));
        Assert.Throws<InvalidOperationException>(() => Talk().Ask(new TimbnAnswer("a")).Do(() => { }).If("a", _ => { }));
    }

    [Fact]
    public void SeveralIfsMayFollowOneAsk()
    {
        var talk = Talk().Ask(new TimbnAnswer("a"), new TimbnAnswer("b"));

        talk.If("a", _ => { }).If("b", _ => { });
    }

    [Fact]
    public void EndedActionsRunWhenAskedAndOnlyFromTheRoot()
    {
        var talk = Talk();
        talk.OnEnded(Note("ended root"));
        talk.When(() => true, branch => branch.OnEnded(Note("ended branch")));

        talk.Run(Note("done"));
        talk.RunEnded();

        Assert.Equal(["done", "ended root", "ended branch"], _log);
    }

    [Fact]
    public void RunEndedWithNoHandlersDoesNothing()
    {
        Talk().RunEnded();

        Assert.Empty(_log);
    }

    [Fact]
    public void OpeningTheMenuFromABranchMarksTheRoot()
    {
        var talk = Talk();
        talk.When(() => true, branch => branch.ThenOpenMenu());

        Assert.True(talk.OpensMenu);
    }

    [Fact]
    public void FlagsStartOff()
    {
        var talk = Talk();

        Assert.False(talk.OpensMenu);
        Assert.False(talk.TurnedOnBars);
    }

    [Fact]
    public void TheNpcIsSharedWithBranches()
    {
        var npc = (WgoData)RuntimeHelpers.GetUninitializedObject(typeof(WgoData));
        var talk = new TimbnConversation(npc, "t");
        WgoData? seen = null;
        talk.When(() => true, branch => seen = branch.Npc);

        Assert.Same(npc, seen);
    }
}
