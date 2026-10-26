using BepInEx.Configuration;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnConfigTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TimbnTests", Guid.NewGuid().ToString("N"));

    public TimbnConfigTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string ConfigPath => Path.Combine(_folder, "plugin.cfg");

    [Fact]
    public void CarryOverMovesAnOldValueIntoTheNewEntryAndDropsTheOldKey()
    {
        File.WriteAllText(ConfigPath, "[Carriers]\nShowWhenStuck = true\n\n[Unstuck]\nKey = F9\n");
        var config = new ConfigFile(ConfigPath, false) { SaveOnConfigSet = false };
        var stuck = config.Bind("General", "StuckCarriers", false, "Shows a hint.");
        var key = config.Bind("General", "UnstuckKey", "F8", "The key.");

        TimbnConfig.CarryOver(config, (stuck, "Carriers", "ShowWhenStuck"), (key, "Unstuck", "Key"));

        Assert.True(stuck.Value);
        Assert.Equal("F9", key.Value);
        var written = File.ReadAllText(ConfigPath);
        Assert.DoesNotContain("ShowWhenStuck", written);
        Assert.DoesNotContain("[Unstuck]", written);
        Assert.Contains("StuckCarriers = true", written);
    }

    [Fact]
    public void CarryOverLeavesANewEntryAloneWhenTheOldKeyIsGone()
    {
        File.WriteAllText(ConfigPath, "[General]\nStuckCarriers = false\n");
        var config = new ConfigFile(ConfigPath, false) { SaveOnConfigSet = false };
        var stuck = config.Bind("General", "StuckCarriers", true, "Shows a hint.");
        var before = DateTime.UtcNow.AddHours(-1);
        File.SetLastWriteTimeUtc(ConfigPath, before);

        TimbnConfig.CarryOver(config, (stuck, "Carriers", "ShowWhenStuck"));

        Assert.False(stuck.Value);
        Assert.Equal(before, File.GetLastWriteTimeUtc(ConfigPath));
    }

    [Fact]
    public void CarryOverOnlyUsesAnOldKeyOnce()
    {
        File.WriteAllText(ConfigPath, "[Carriers]\nShowWhenStuck = true\n");
        var config = new ConfigFile(ConfigPath, false) { SaveOnConfigSet = false };
        var stuck = config.Bind("General", "StuckCarriers", false, "Shows a hint.");

        TimbnConfig.CarryOver(config, (stuck, "Carriers", "ShowWhenStuck"));
        stuck.Value = false;
        TimbnConfig.CarryOver(config, (stuck, "Carriers", "ShowWhenStuck"));

        Assert.False(stuck.Value);
    }
}
