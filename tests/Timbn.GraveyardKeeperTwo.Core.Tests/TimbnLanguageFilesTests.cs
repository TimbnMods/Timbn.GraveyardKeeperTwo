using BepInEx.Logging;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLanguageFilesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TimbnTests", Guid.NewGuid().ToString("N"));
    private readonly ManualLogSource _logger = new("Tests");
    private readonly List<string> _warnings = [];

    public TimbnLanguageFilesTests()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "lang"));
        _logger.LogEvent += (_, args) =>
        {
            if (args.Level == LogLevel.Warning)
                _warnings.Add(args.Data?.ToString() ?? "");
        };
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void ReadsKeysPerLanguageWithCommentsAndLineBreaks()
    {
        Write("en.txt", "# a comment", "", "greeting = Hello\\nthere", "farewell = Bye ");
        Write("de.txt", "greeting = Hallo");

        var texts = TimbnLanguageFiles.Read(_folder, _logger);

        Assert.Equal("Hello\nthere", texts["greeting"]["en"]);
        Assert.Equal("Hallo", texts["greeting"]["de"]);
        Assert.Equal("Bye", texts["farewell"]["en"]);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void WarnsAboutKeysWithoutEnglishAndBadLines()
    {
        Write("en.txt", "not a pair");
        Write("de.txt", "only_german = Nur Deutsch");

        var texts = TimbnLanguageFiles.Read(_folder, _logger);

        Assert.Single(texts);
        Assert.Equal(2, _warnings.Count);
        Assert.Contains(_warnings, warning => warning.Contains("line 1"));
        Assert.Contains(_warnings, warning => warning.Contains("only_german"));
    }

    [Fact]
    public void ExistsLooksForTheLangFolder()
    {
        Assert.True(TimbnLanguageFiles.Exists(_folder));
        Assert.False(TimbnLanguageFiles.Exists(Path.Combine(_folder, "lang")));
    }

    private void Write(string file, params string[] lines) =>
        File.WriteAllLines(Path.Combine(_folder, "lang", file), lines);
}
