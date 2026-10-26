namespace Timbn.GraveyardKeeperTwo.Core;

/// <summary>
/// The loaded Core plugin. Checks the game version and wires the game's events to the framework's save, talk, quest,
/// potion, NPC and main menu upkeep.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class TimbnCorePlugin : TimbnFrameworkPlugin<TimbnCorePlugin>
{
    /// <summary>Core's plugin guid, for a mod's BepInDependency attribute so BepInEx loads Core first.</summary>
    public const string Guid = MyPluginInfo.PLUGIN_GUID;

    /// <summary>Core's own plugin version.</summary>
    public const string Version = MyPluginInfo.PLUGIN_VERSION;

    /// <summary>The game version these mods were last verified against.</summary>
    public const string TestedGameVersion = "1.010";

    /// <summary>
    /// The <see cref="GameBuild"/> of <see cref="TestedGameVersion"/>. It tells a silent hotfix apart from the build
    /// these mods were verified on, since a hotfix can ship without a new version string.
    /// </summary>
    public const string TestedGameBuild = "5ec8094e4c33";

    /// <summary>
    /// A short id for the running game's code, the first twelve hex digits of Assembly-CSharp's module version id.
    /// It changes whenever the game's scripts are rebuilt, for example "06096414ba3b" on 1.010 (Steam build 25907946).
    /// </summary>
    public static string GameBuild { get; } = typeof(MainGame).Assembly.ManifestModule.ModuleVersionId.ToString("N")[..12];

    private bool _loadErrorsChecked;

    protected override void BindConfig(ConfigFile config) => TimbnModsWindowHost.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        Description = "The shared framework the Timbn mods are built on. It adds nothing to the game by itself.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/84",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3810064587",
        ThunderstoreLink = "https://thunderstore.io/c/graveyard-keeper-2/p/Timbn/TimbnCore/",
        WebsiteLink = "https://timbnmods.github.io/",
    };

    protected override void OnAwake()
    {
        TimbnExpression.CompileNow = true;
        TimbnExpressionFunctions.Register();
        Subscriptions.Add(TimbnGameEvents.GameStarted(() => TimbnSession.Open(SessionSubscriptions)));
        Subscriptions.Add(TimbnGameEvents.GoToMainMenu(TimbnSession.Close));
        Subscriptions.Add(TimbnGameEvents.SaveWriteEnded(TimbnGlobalSaves.Flush));
        Subscriptions.Add(TimbnGameEvents.On(TimbnSaves.OnLoadingExistingSave, h => SaveSystem.OnSaveLoadingStarted += h, h => SaveSystem.OnSaveLoadingStarted -= h));
        Subscriptions.Add(TimbnGameEvents.On(TimbnGlobalSaves.Flush, h => Application.quitting += h, h => Application.quitting -= h));
        Subscriptions.Add(TimbnGameEvents.On<GameBalance>(_ => TimbnModels.OnBalanceLoaded(), h => TimbnPatchedEvents.BalanceLoaded += h, h => TimbnPatchedEvents.BalanceLoaded -= h));
        Events.WindowOpened(TimbnMainMenu.OnWindowOpened);
        TimbnModsWindowHost.Start(this);
        Subscriptions.Add(TimbnMainMenu.AddLine($"{Metadata.Name}: ", Metadata.Version.ToString()));
        Events.EveryRealSeconds(1f, TimbnDialog.Refresh);
        Events.SaveReady(TimbnMoveBuildingsLink.Check);
        Events.EveryRealSeconds(1f, TimbnQuests.StartContinuedQuests);
        Events.EveryRealSeconds(1f, TimbnQuests.CheckReadyWhen);
        Events.EveryRealSeconds(0.5f, TimbnQuestMarker.Tick);
        Events.Update(TimbnPotions.Tick);
        Events.Update(TimbnNpcs.Tick);
        Events.Update(TimbnPlayerGrid.Tick);
        Events.Update(TimbnInteractionSpots.Tick);
        Events.Update(TimbnKeyHints.Tick);
        Events.Update(TimbnPlayerAnimation.Tick);
        Events.FixedUpdate(TimbnPlayerWalk.FixedTick);
        var saved = Saves.Register<TimbnCoreSaveData>();
        TimbnDialog.UseSeenTalks(() => saved.Current.SeenTalks);
        TimbnQuests.UseReadyMarks(() => saved.Current.QuestsReady);
        TimbnQuests.UseRuns(() => saved.Current.QuestRuns);
        Events.NewDayStarted(_ => TimbnQuests.OnNewDay());
        Subscriptions.Add(TimbnGameEvents.On<GlobalEventsSystem.Event.Type, string>(
            TimbnQuests.OnGameEvent,
            h => TimbnPatchedEvents.Triggered += h,
            h => TimbnPatchedEvents.Triggered -= h));

        var timbnCoreGlobalData = Saves.RegisterGlobal<TimbnCoreData>();
        var game = $"{Application.version} {GameBuild}";
        var started = $"Core {Version} started on Graveyard Keeper 2 {Application.version} (build {GameBuild})";
        if (Application.version != TestedGameVersion)
        {
            Logger.LogWarning($"{started}, but these mods were tested on {TestedGameVersion}. Include this line when reporting a problem.");
            WarnOnce(timbnCoreGlobalData, game, "Untested game version", $"Timbn mods were tested on version {TestedGameVersion} and this is {Application.version}. Some mods may misbehave until they are updated.");
        }
        else if (GameBuild != TestedGameBuild)
        {
            Logger.LogWarning($"{started}, but these mods were tested on build {TestedGameBuild} of that version, so this is likely a hotfix. Include this line when reporting a problem.");
            WarnOnce(timbnCoreGlobalData, game, "Untested game build", $"Timbn mods were tested on an earlier build of {Application.version}, so this is likely a hotfix. Some mods may misbehave until they are updated.");
        }
        else
        {
            Logger.LogInfo($"{started}.");
        }
    }

    private void WarnOnce(TimbnGlobalData<TimbnCoreData> timbnCoreGlobalData, string game, string title, string message)
    {
        if (timbnCoreGlobalData.Current.WarnedGameVersion == game)
            return;

        MainMenu.AddPopup(title, message);
        timbnCoreGlobalData.Current.WarnedGameVersion = game;
        timbnCoreGlobalData.Save();
    }

    protected override void OnUpdate()
    {
        if (!_loadErrorsChecked)
        {
            _loadErrorsChecked = true;
            TimbnSafe.Run(() => Subscriptions.Add(TimbnScriptReloads.Install(Logger)), Logger, $"{nameof(TimbnScriptReloads)}|Guarding the F6 reload");
            if (TimbnLoadErrors.Describe() is { } loadErrors)
                MainMenu.AddPopup("Some mods did not load", loadErrors);
        }

        TimbnMainMenu.OnUpdate();
    }

    private void OnGUI() => TimbnGui.Draw();

    protected override void OnUnload()
    {
        TimbnSession.ReleaseHolds();
        TimbnSettingsPages.Clear();
        TimbnModInfos.Clear();
        TimbnGroupPages.Clear();
        TimbnMoveBuildingsLink.Detach();
        TimbnPageWindow.DestroyInstance();
    }
}
