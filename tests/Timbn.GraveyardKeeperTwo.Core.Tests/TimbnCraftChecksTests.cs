namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnCraftChecksTests
{
    private static readonly Action<Func<string, bool>, Exception> _ignore = (_, _) => { };

    [Fact]
    public void ACraftWithNoChecksIsAllowed()
    {
        TimbnCraftChecks<string> checks = new();

        Assert.True(checks.IsEmpty);
        Assert.True(checks.Allows("timbn_lesson", "machine", _ignore));
    }

    [Fact]
    public void EveryCheckForTheCraftMustPass()
    {
        TimbnCraftChecks<string> checks = new();
        checks.Add("timbn_lesson", station => station == "machine");
        checks.Add("timbn_lesson", station => station.Length > 3);

        Assert.True(checks.Allows("timbn_lesson", "machine", _ignore));
        Assert.False(checks.Allows("timbn_lesson", "oven", _ignore));
        Assert.True(checks.Allows("timbn_other", "oven", _ignore));
    }

    [Fact]
    public void RemovingTheLastCheckAllowsTheCraftAgain()
    {
        TimbnCraftChecks<string> checks = new();
        var added = checks.Add("timbn_lesson", _ => false);

        Assert.False(checks.Allows("timbn_lesson", "machine", _ignore));
        added.Dispose();

        Assert.True(checks.IsEmpty);
        Assert.True(checks.Allows("timbn_lesson", "machine", _ignore));
    }

    [Fact]
    public void AThrowingCheckPassesAndIsReportedOnce()
    {
        TimbnCraftChecks<string> checks = new();
        checks.Add("timbn_lesson", _ => throw new InvalidOperationException("broken"));
        var reports = 0;

        Assert.True(checks.Allows("timbn_lesson", "machine", (_, _) => reports++));
        Assert.True(checks.Allows("timbn_lesson", "machine", (_, _) => reports++));

        Assert.Equal(1, reports);
    }

    [Fact]
    public void RequiresNeedsACheck()
    {
        Assert.Throws<ArgumentNullException>(() => new TimbnCraftBuilder("timbn_lesson", "", "").Requires(null!));
    }
}
