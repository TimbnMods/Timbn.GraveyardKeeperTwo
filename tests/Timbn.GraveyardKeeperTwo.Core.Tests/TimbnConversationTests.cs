namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnConversationTests
{
    private readonly List<string> _log = [];

    private static TimbnConversation Talk() => new(null!, "test_talk");

    private Action Note(string text) => () => _log.Add(text);

    [Fact]
    public void WhenRunsTheChosenBranchThenCarriesOn()
    {
        var talk = Talk()
            .When(() => true, then => then.Do(Note("yes")), otherwise => otherwise.Do(Note("no")))
            .When(() => false, then => then.Do(Note("yes2")), otherwise => otherwise.Do(Note("no2")))
            .When(() => false, then => then.Do(Note("skipped")))
            .Do(Note("after"));

        talk.Run(Note("done"));

        Assert.Equal(["yes", "no2", "after", "done"], _log);
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
}
