using BepInEx.Configuration;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnModsDraftTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TimbnTests", Guid.NewGuid().ToString("N"));

    public TimbnModsDraftTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private ConfigFile NewConfig() => new(Path.Combine(_folder, "plugin.cfg"), true);

    // Written to the file with one decimal, the way a Color is written with 8 bits a channel, so a value read back can
    // differ from the default in code by less than the file keeps.
    private readonly struct Rounded
    {
        public Rounded(float value) => Value = value;

        public float Value { get; }
    }

    [Fact]
    public void AValueTheFileWritesTheSameAsTheDefaultIsTheDefault()
    {
        if (!TomlTypeConverter.CanConvert(typeof(Rounded)))
        {
            TomlTypeConverter.AddConverter(typeof(Rounded), new TypeConverter
            {
                ConvertToString = (value, _) => ((Rounded)value).Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                ConvertToObject = (text, _) => new Rounded(float.Parse(text, System.Globalization.CultureInfo.InvariantCulture)),
            });
        }

        var entry = NewConfig().Bind("General", "Tint", new Rounded(0.5f));

        Assert.True(TimbnModsValues.IsDefault(entry, new Rounded(128f / 255f)));
        Assert.False(TimbnModsValues.IsDefault(entry, new Rounded(0.7f)));
    }
}
