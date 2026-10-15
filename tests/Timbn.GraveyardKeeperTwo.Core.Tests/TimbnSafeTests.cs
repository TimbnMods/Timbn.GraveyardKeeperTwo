using BepInEx.Logging;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSafeTests
{
    private readonly ManualLogSource _logger = new("Tests");
    private readonly List<LogEventArgs> _logged = [];

    public TimbnSafeTests() => _logger.LogEvent += (_, args) => _logged.Add(args);

    [Fact]
    public void ANullActionCountsAsRun()
    {
        Assert.True(TimbnSafe.Run(null, _logger, "Nothing"));
        Assert.Empty(_logged);
    }

    [Fact]
    public void AnActionThatRunsIsTrueAndLogsNothing()
    {
        var ran = false;

        Assert.True(TimbnSafe.Run(() => ran = true, _logger, "Something"));
        Assert.True(ran);
        Assert.Empty(_logged);
    }

    [Fact]
    public void AThrowIsFalseAndLoggedAsAnError()
    {
        var result = TimbnSafe.Run(() => throw new InvalidOperationException("boom"), _logger, "TimbnTests|A handler");

        Assert.False(result);
        var entry = Assert.Single(_logged);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.StartsWith("TimbnTests|A handler threw: ", entry.Data.ToString());
        Assert.Contains("boom", entry.Data.ToString());
    }

    [Fact]
    public void DescribeNamesTheHandlersTypeAndMethod()
    {
        Action handler = Helper;

        Assert.Equal($"{typeof(TimbnSafeTests).FullName}.{nameof(Helper)}", TimbnSafe.Describe(handler));
    }

    private static void Helper()
    {
    }
}
