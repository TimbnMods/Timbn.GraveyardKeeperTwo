using BepInEx;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLoadErrorsTests
{
    private static readonly Dictionary<string, PluginInfo> _noPlugins = [];

    [Fact]
    public void MissingDependencyNamesTheGuidAndVersion()
    {
        var line = TimbnLoadErrors.Describe(
            "Could not load [Timbn Vanilla Tweaks 1.8.0] because it has missing dependencies: Timbn.GraveyardKeeperTwo.Core (v1.5.0 or newer)",
            _noPlugins);

        Assert.Equal("Timbn Vanilla Tweaks 1.8.0 needs Timbn.GraveyardKeeperTwo.Core 1.5.0 or newer, which is missing.", line);
    }

    [Fact]
    public void MissingDependencyUsesTheInstalledPluginName()
    {
        var core = new PluginInfo();
        typeof(PluginInfo).GetProperty(nameof(PluginInfo.Metadata))!.SetValue(core, new BepInPlugin("Timbn.GraveyardKeeperTwo.Core", "Timbn Core", "1.4.0"));
        var plugins = new Dictionary<string, PluginInfo> { ["Timbn.GraveyardKeeperTwo.Core"] = core };

        var line = TimbnLoadErrors.Describe(
            "Could not load [Timbn Vanilla Tweaks 1.8.0] because it has missing dependencies: Timbn.GraveyardKeeperTwo.Core (v1.5.0 or newer)",
            plugins);

        Assert.Equal("Timbn Vanilla Tweaks 1.8.0 needs Timbn Core 1.5.0 or newer (you have 1.4.0).", line);
    }
}
