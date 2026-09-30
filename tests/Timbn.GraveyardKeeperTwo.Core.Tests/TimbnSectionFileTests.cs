using BepInEx.Logging;
using Newtonsoft.Json.Linq;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSectionFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TimbnTests", Guid.NewGuid().ToString("N"));
    private readonly ManualLogSource _logger = new("Tests");

    public TimbnSectionFileTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string Main => Path.Combine(_folder, "slot.TimbnSaveData.dat");

    private string Backup => Path.Combine(_folder, "slot.TimbnSaveData.backup.dat");

    [Fact]
    public void WriteThenReadRoundTripsTheSections()
    {
        var sections = new JObject { ["mod.a"] = new JObject { ["Days"] = 3 } };

        TimbnSectionFile.Write(Main, Backup, sections);
        var read = TimbnSectionFile.Read(Main, Backup, _logger);

        Assert.Equal(3, (int)read["mod.a"]!["Days"]!);
        Assert.False(File.Exists(Main + ".new"));
    }

    [Fact]
    public void SecondWriteKeepsThePreviousFileAsBackup()
    {
        TimbnSectionFile.Write(Main, Backup, new JObject { ["mod.a"] = new JObject { ["Days"] = 1 } });
        TimbnSectionFile.Write(Main, Backup, new JObject { ["mod.a"] = new JObject { ["Days"] = 2 } });

        var backup = TimbnSectionFile.Read(Backup, Backup + ".none", _logger);

        Assert.Equal(1, (int)backup["mod.a"]!["Days"]!);
    }

    [Fact]
    public void UnreadableMainFallsBackToTheBackupAndIsSetAside()
    {
        TimbnSectionFile.Write(Main, Backup, new JObject { ["mod.a"] = new JObject { ["Days"] = 1 } });
        TimbnSectionFile.Write(Main, Backup, new JObject { ["mod.a"] = new JObject { ["Days"] = 2 } });
        File.WriteAllText(Main, "{ not json");

        var read = TimbnSectionFile.Read(Main, Backup, _logger);

        Assert.Equal(1, (int)read["mod.a"]!["Days"]!);
        Assert.True(File.Exists(Main + ".bad"));
        Assert.False(File.Exists(Main));
    }

    [Fact]
    public void MissingFilesReadAsEmpty()
    {
        var read = TimbnSectionFile.Read(Main, Backup, _logger);

        Assert.Empty(read);
    }
}
