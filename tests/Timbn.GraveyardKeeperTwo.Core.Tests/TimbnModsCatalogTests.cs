using BepInEx.Configuration;
using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnModsCatalogTests : IDisposable
{
    private enum Weather
    {
        Clear,
        Rain,
        Fog,
    }

    private sealed class Tags
    {
        public bool? Browsable;
        public string? DispName;
        public int? Order;
        public bool? ReadOnly;
        public bool? IsAdvanced;
        public Action<ConfigEntryBase>? CustomDrawer;
        public Func<object, string>? ObjToStr;
        public Func<string, object>? StrToObj;
    }

    private static readonly Func<object, string> _vectorToText = value => ((Vector2)value).x + "x" + ((Vector2)value).y;

    private static readonly Func<string, object> _textToVector = text =>
    {
        var parts = text.Split('x');
        return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
    };

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "TimbnTests", Guid.NewGuid().ToString("N"));

    public TimbnModsCatalogTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private ConfigFile NewConfig(string name = "plugin") =>
        new(Path.Combine(_folder, name + ".cfg"), false) { SaveOnConfigSet = false };

    private static TimbnModsMod Build(ConfigFile config, bool isTimbn = false, Func<string, bool>? broken = null) =>
        TimbnModsCatalog.BuildMod(new TimbnModsPlugin("Test", "1.0.0", "test.plugin", config, isTimbn, broken));

    private static TimbnModsRow Row(TimbnModsMod mod, string key) =>
        mod.Sections.SelectMany(section => section.Rows).Single(row => row.Entry.Definition.Key == key);

    [Fact]
    public void EachTypePicksItsRowKind()
    {
        var config = NewConfig();
        config.Bind("Main", "Toggle", true, "A switch.");
        config.Bind("Main", "Sky", Weather.Rain, "An enum.");
        config.Bind("Main", "Mode", "fast", new ConfigDescription("A list.", new AcceptableValueList<string>("fast", "slow")));
        config.BindRange("Main", "Speed", 7f, "A range.", 1f, 50f);
        config.Bind("Main", "Count", 3, "A plain number.");
        config.Bind("Main", "Title", "Keeper", "A string.");
        config.Bind("Main", "Key", new KeyboardShortcut(KeyCode.U, KeyCode.LeftControl), "A key.");
        config.Bind("Main", "Tint", Color.red, "Anything else.");

        var mod = Build(config);

        Assert.Equal(TimbnModsRowKind.Switch, Row(mod, "Toggle").Kind);
        Assert.Equal(TimbnModsRowKind.Switch, Row(mod, "Sky").Kind);
        Assert.Equal(TimbnModsRowKind.Switch, Row(mod, "Mode").Kind);
        Assert.Equal(TimbnModsRowKind.Slider, Row(mod, "Speed").Kind);
        Assert.Equal(TimbnModsRowKind.Text, Row(mod, "Count").Kind);
        Assert.Equal(TimbnModsRowKind.Text, Row(mod, "Title").Kind);
        Assert.Equal(TimbnModsRowKind.Key, Row(mod, "Key").Kind);
        Assert.Equal(TimbnModsRowKind.Readonly, Row(mod, "Tint").Kind);
        Assert.Null(Row(mod, "Toggle").Options);
        Assert.Equal(["Clear", "Rain", "Fog"], Row(mod, "Sky").Options);
        Assert.Equal(["fast", "slow"], Row(mod, "Mode").Options);
        Assert.Equal(1f, Row(mod, "Speed").Min);
        Assert.Equal(50f, Row(mod, "Speed").Max);
    }

    [Fact]
    public void HiddenEntriesStayOut()
    {
        var config = NewConfig();
        config.Bind("Main", "Shown", true, "Shown.");
        config.BindHidden("Debug", "WriteDebugLog", false, "Hidden by Core's helper.");
        config.Bind("Main", "Tagged", true, new ConfigDescription("Hidden by a tag.", null, new Tags { Browsable = false }));

        var mod = Build(config);

        var section = Assert.Single(mod.Sections);
        Assert.Equal("Shown", Assert.Single(section.Rows).Entry.Definition.Key);
    }

    [Fact]
    public void DisplayNameAndOrderTagsAreUsed()
    {
        var config = NewConfig();
        config.Bind("Main", "Low", true, new ConfigDescription("Low.", null, new Tags { Order = 1 }));
        config.Bind("Main", "High", true, new ConfigDescription("High.", null, new Tags { Order = 5, DispName = "Top setting" }));
        config.Bind("Main", "Plain", true, "No tags.");

        var rows = Assert.Single(Build(config).Sections).Rows;

        Assert.Equal(["Top setting", "Low", "Plain"], rows.Select(row => row.Label));
    }

    [Fact]
    public void ReadOnlySettingsKeepTheirControlAndCustomDrawerOnesOnlyShowTheirValue()
    {
        var config = NewConfig();
        config.Bind("Main", "Locked", true, new ConfigDescription("Read only.", null, new Tags { ReadOnly = true }));
        config.Bind("Main", "Drawn", "text", new ConfigDescription("Drawn by the mod.", null, new Tags { CustomDrawer = _ => { } }));
        config.Bind("Main", "Open", true, new ConfigDescription("Not read only.", null, new Tags { ReadOnly = false }));

        var mod = Build(config);

        Assert.Equal(TimbnModsRowKind.Switch, Row(mod, "Locked").Kind);
        Assert.True(Row(mod, "Locked").IsReadOnly);
        Assert.Equal(["Read Only"], Row(mod, "Locked").Tags);
        Assert.Equal(TimbnModsRowKind.Readonly, Row(mod, "Drawn").Kind);
        Assert.True(Row(mod, "Drawn").IsReadOnly);
        Assert.Equal(TimbnModsRowKind.Switch, Row(mod, "Open").Kind);
        Assert.False(Row(mod, "Open").IsReadOnly);
    }

    [Fact]
    public void ATypeWithBothConvertersIsTyped()
    {
        var config = NewConfig();
        config.Bind("Main", "Both", Vector2.one, new ConfigDescription("Both.", null, new Tags { ObjToStr = _vectorToText, StrToObj = _textToVector }));
        config.Bind("Main", "OnlyOne", Vector2.one, new ConfigDescription("One.", null, new Tags { ObjToStr = _vectorToText }));
        config.Bind("Main", "Text", "keeper", new ConfigDescription("A string.", null, new Tags { ObjToStr = _ => "shown" }));

        var mod = Build(config);

        Assert.Equal(TimbnModsRowKind.Text, Row(mod, "Both").Kind);
        Assert.Same(_textToVector, Row(mod, "Both").FromText);
        Assert.Equal(TimbnModsRowKind.Readonly, Row(mod, "OnlyOne").Kind);
        Assert.Equal(TimbnModsRowKind.Text, Row(mod, "Text").Kind);
    }

    [Fact]
    public void AdvancedSettingsStayInTheirSection()
    {
        var config = NewConfig();
        config.Bind("Tweaks", "One", true, "Shown.");
        config.Bind("Tweaks", "Two", true, new ConfigDescription("Advanced.", null, new Tags { IsAdvanced = true }));
        config.Bind("Debug", "Three", true, new ConfigDescription("Advanced.", null, new Tags { IsAdvanced = true }));

        var mod = Build(config);

        Assert.True(mod.HasAdvanced);
        Assert.Equal(["Tweaks", "Debug"], mod.Sections.Select(section => section.Name));
        Assert.False(Row(mod, "One").IsAdvanced);
        Assert.True(Row(mod, "Two").IsAdvanced);
    }
}
