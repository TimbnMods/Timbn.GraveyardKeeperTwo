namespace Timbn.GraveyardKeeperTwo.Core;

/// <summary>
/// The loaded Core plugin. Checks the game version and runs the framework pieces that need a live plugin, such as
/// dialog icons.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class TimbnCorePlugin : TimbnFrameworkPlugin<TimbnCorePlugin>
{
    public const string Guid = MyPluginInfo.PLUGIN_GUID;

    /// <summary>Core's own plugin version.</summary>
    public const string Version = MyPluginInfo.PLUGIN_VERSION;

    /// <summary>The game version these mods were last verified against.</summary>
    public const string TestedGameVersion = "1.004.3";

    /// <summary>
    /// The <see cref="GameBuild"/> of <see cref="TestedGameVersion"/>. It tells a silent hotfix apart from the build
    /// these mods were verified on, since a hotfix can ship without a new version string.
    /// </summary>
    public const string TestedGameBuild = "1cacef38030a";

    /// <summary>
    /// A short id for the running game's code, the first twelve hex digits of Assembly-CSharp's module version id.
    /// It changes whenever the game's scripts are rebuilt, for example "1cacef38030a" on 1.004.3.
    /// </summary>
    public static string GameBuild { get; } = typeof(MainGame).Assembly.ManifestModule.ModuleVersionId.ToString("N").Substring(0, 12);

    public static TimbnCorePlugin? Instance { get; private set; }

    protected override void OnAwake()
    {
        Instance = this;
        Subscriptions.Add(TimbnGameEvents.GameStarted(TimbnGame.OnGameStarted));
        Subscriptions.Add(TimbnGameEvents.GoToMainMenu(TimbnGame.OnLeftGame));
        Subscriptions.Add(TimbnGameEvents.GameStarted(TimbnDialog.OnGameStarted));
        Subscriptions.Add(TimbnGameEvents.GameStarted(OnGameStarted));
        Subscriptions.Add(TimbnGameEvents.GoToMainMenu(TimbnDialog.OnLeftGame));
        var started = $"Core {Version} started on Graveyard Keeper 2 {Application.version} (build {GameBuild})";
        if (Application.version != TestedGameVersion)
            Logger.LogWarning($"{started}, but these mods were tested on {TestedGameVersion}. Include this line when reporting a problem.");
        else if (GameBuild != TestedGameBuild)
            Logger.LogWarning($"{started}, but these mods were tested on build {TestedGameBuild} of that version, so this is likely a hotfix. Include this line when reporting a problem.");
        else
            Logger.LogInfo($"{started}.");
    }

    private void OnGameStarted()
    {
        TimbnQuests.OnGameStarted();
        SessionSubscriptions.Add(TimbnGameEvents.QuestStarted(TimbnQuests.OnQuestStarted));
        TimbnPotions.OnGameStarted(SessionSubscriptions);
    }

    protected override void OnUpdate()
    {
        TimbnDialog.OnUpdate();
        TimbnPotions.OnUpdate();
    }

    protected override void OnDestroyed() => Instance = null;
}
