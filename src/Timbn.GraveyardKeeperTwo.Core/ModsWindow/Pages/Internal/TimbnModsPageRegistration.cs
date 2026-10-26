namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>A tab a mod added to its own page or to a group's page in the Mods window.</summary>
internal sealed class TimbnModsPageRegistration
{
    private readonly Action<TimbnPageBuilder> _build;

    public TimbnModsPageRegistration(string title, Action<TimbnPageBuilder> build, ManualLogSource logger)
    {
        Title = title;
        Logger = logger;
        _build = build;
    }

    public string Title { get; }

    public ManualLogSource Logger { get; }

    /// <summary>Hands the mod an empty page in the area. A throw is logged and leaves the page with whatever the mod made before it.</summary>
    public void Build(RectTransform area) =>
        TimbnSafe.Run(() => _build(TimbnPageBuilder.On(area, Logger)), Logger, $"Building the settings page '{Title}'");
}
