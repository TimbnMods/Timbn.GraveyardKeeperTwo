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

    [Fact]
    public void LoadEntryFillsTheEntryFromItsSection()
    {
        var sections = new JObject { ["mod.a"] = new JObject { ["Days"] = 5 } };
        var data = new TimbnSaveData<Sample>("mod.a", null);

        TimbnSectionFile.LoadEntry(sections, data, Main, "Tests");

        Assert.Equal(5, data.Current.Days);
    }

    [Fact]
    public void LoadEntryWithNoSectionStartsFresh()
    {
        var data = new TimbnSaveData<Sample>("mod.b", null);
        data.Current.Days = 9;

        TimbnSectionFile.LoadEntry([], data, Main, "Tests");

        Assert.Equal(0, data.Current.Days);
    }

    [Fact]
    public void KeepEntryWritesTheEntrysSection()
    {
        var sections = new JObject { ["mod.other"] = new JObject { ["Days"] = 1 } };
        var data = new TimbnSaveData<Sample>("mod.a", null);
        data.Current.Days = 7;

        TimbnSectionFile.KeepEntry(sections, data, "Tests");

        Assert.Equal(7, (int)sections["mod.a"]!["Days"]!);
        Assert.Equal(1, (int)sections["mod.other"]!["Days"]!);
    }

    [Fact]
    public void KeepEntryReplacesAnOlderSection()
    {
        var sections = new JObject { ["mod.a"] = new JObject { ["Days"] = 1 } };
        var data = new TimbnSaveData<Sample>("mod.a", null);
        data.Current.Days = 2;

        TimbnSectionFile.KeepEntry(sections, data, "Tests");

        Assert.Equal(2, (int)sections["mod.a"]!["Days"]!);
    }

    [Fact]
    public void AFileWithNoModsSectionReadsAsEmpty()
    {
        File.WriteAllText(Main, "{ \"formatVersion\": 1 }");

        Assert.Empty(TimbnSectionFile.Read(Main, Backup, _logger));
    }

    [Fact]
    public void WriteKeepsTheFormatVersion()
    {
        TimbnSectionFile.Write(Main, Backup, new JObject { ["mod.a"] = new JObject() });

        var root = JObject.Parse(File.ReadAllText(Main));

        Assert.Equal(1, (int)root["formatVersion"]!);
        Assert.NotNull(root["mods"]!["mod.a"]);
    }

    public sealed class Sample
    {
        public int Days { get; set; }
    }
}
