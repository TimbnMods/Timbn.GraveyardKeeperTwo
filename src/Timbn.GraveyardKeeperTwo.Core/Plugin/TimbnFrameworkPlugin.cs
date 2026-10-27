using BepInEx.Bootstrap;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Holds what every Timbn mod shares. It handles config, the Enabled gate, Harmony patching and teardown. A mod
/// derives from <see cref="TimbnFrameworkPlugin{T}"/> instead.
/// </summary>
public abstract class TimbnFrameworkPlugin : BaseUnityPlugin
{
    private static readonly AccessTools.FieldRef<BaseUnityPlugin, PluginInfo?> _infoField =
        AccessTools.FieldRefAccess<BaseUnityPlugin, PluginInfo?>("<Info>k__BackingField");

    private static readonly List<TimbnFrameworkPlugin> _running = [];

    private static int _harmonyInstances;

    private readonly List<Harmony> _harmonies = [];
    private readonly HashSet<string> _brokenFeatures = [];
    private bool _started;

    /// <summary>Reads the mod's metadata and creates its facade properties, like Events and Settings.</summary>
    protected TimbnFrameworkPlugin()
    {
        Metadata = MetadataHelper.GetMetadata(this);
        RepairPluginInfo();
        Subscriptions = new(Logger);
        SessionSubscriptions = new(Logger);
        Events = new(this);
        Potions = new(this);
        Quests = new(this);
        Dialog = new(this);
        Text = new(this);
        Sprites = new(this);
        Balance = new(this);
        MainMenu = new(this);
        Settings = new(this);
        Player = new(this);
        Clock = new(this);
        UI = new(this);
        Interactions = new(this);
        ModEvents = new(this);
        Saves = new(this);
        Npcs = new(this);
        Audio = new(this);
        Models = new(this);
        Stations = new(this);
        Crafts = new(this);
        Techs = new(this);
    }

    /// <summary>Holds the mod's BepInPlugin attribute. Use it instead of Info.</summary>
    protected internal BepInPlugin Metadata { get; }

    private void RepairPluginInfo()
    {
        if (Info != null)
            return;

        var info = new PluginInfo();
        var traverse = Traverse.Create(info);
        traverse.Property<BepInPlugin>(nameof(PluginInfo.Metadata)).Value = Metadata;
        traverse.Property<BaseUnityPlugin>(nameof(PluginInfo.Instance)).Value = this;
        traverse.Property<IEnumerable<BepInDependency>>(nameof(PluginInfo.Dependencies)).Value = MetadataHelper.GetDependencies(GetType());
        traverse.Property<IEnumerable<BepInProcess>>(nameof(PluginInfo.Processes)).Value = MetadataHelper.GetAttributes<BepInProcess>(GetType());
        traverse.Property<IEnumerable<BepInIncompatibility>>(nameof(PluginInfo.Incompatibilities)).Value = MetadataHelper.GetAttributes<BepInIncompatibility>(GetType());
        traverse.Property<string>(nameof(PluginInfo.Location)).Value = GetType().Assembly.Location;
        Chainloader.PluginInfos[Metadata.GUID] = info;
        _infoField(this) = info;
    }

    /// <summary>
    /// Subscribes the mod to the game's events. Every subscription is removed when the mod unloads. Events that
    /// belong to a save follow every save the player loads.
    /// </summary>
    public TimbnPluginEvents Events { get; }

    /// <summary>Adds potions that are removed when the mod unloads.</summary>
    public TimbnPluginPotions Potions { get; }

    /// <summary>Adds quests that are removed when the mod unloads.</summary>
    public TimbnPluginQuests Quests { get; }

    /// <summary>Gives NPCs conversations that are removed when the mod unloads.</summary>
    public TimbnPluginDialog Dialog { get; }

    /// <summary>Adds or overrides the game's text. The change is undone when the mod unloads.</summary>
    public TimbnPluginText Text { get; }

    /// <summary>Adds sprites like item and buff icons. They are removed when the mod unloads.</summary>
    public TimbnPluginSprites Sprites { get; }

    /// <summary>Adds definitions to the game's balance tables that are removed when the mod unloads.</summary>
    public TimbnPluginBalance Balance { get; }

    /// <summary>Shows lines of text and popups on the main menu. They are removed when the mod unloads.</summary>
    public TimbnPluginMainMenu MainMenu { get; }

    /// <summary>Keeps the mod's changes to the game in step with its config as settings change.</summary>
    public TimbnPluginSettings Settings { get; }

    /// <summary>
    /// Acts on the player character, like holding them still. Each change is undone when the mod
    /// unloads.
    /// </summary>
    public TimbnPluginPlayer Player { get; }

    /// <summary>Changes how fast the game's time runs. The change is undone when the mod unloads.</summary>
    public TimbnPluginClock Clock { get; }

    /// <summary>Puts hints and IMGUI on screen. They are taken down when the mod unloads.</summary>
    public TimbnPluginUI UI { get; }

    /// <summary>
    /// Adds actions to the game's objects, like a second key on a chest. They go away when the mod
    /// unloads.
    /// </summary>
    public TimbnPluginInteractions Interactions { get; }

    /// <summary>
    /// Lets mods tell each other things through Core with no reference to each other. Everything it adds goes away
    /// when the mod unloads.
    /// </summary>
    public TimbnPluginModEvents ModEvents { get; }

    /// <summary>Keeps the mod's data with each save. It goes in a file next to the game's save.</summary>
    public TimbnPluginSaves Saves { get; }

    /// <summary>
    /// Walks and adds people and gives them a day on the game clock. Every walk, claim and added person is undone
    /// when the mod unloads.
    /// </summary>
    public TimbnPluginNpcs Npcs { get; }

    /// <summary>Plays the game's music for the mod. The music goes back to what it was when the mod is done.</summary>
    public TimbnPluginAudio Audio { get; }

    /// <summary>
    /// Builds 3D models from the mod's PNGs and uses them in the world. They are freed when the mod
    /// unloads.
    /// </summary>
    public TimbnPluginModels Models { get; }

    /// <summary>
    /// Adds workstations and bench extensions the player builds at a build desk. They are removed when the mod
    /// unloads.
    /// </summary>
    public TimbnPluginStations Stations { get; }

    /// <summary>Adds crafts at the game's stations or the mod's own. They are removed when the mod unloads.</summary>
    public TimbnPluginCrafts Crafts { get; }

    /// <summary>Adds technologies to the tech tree that cost tech points. They go away when the mod unloads.</summary>
    public TimbnPluginTechs Techs { get; }

    /// <summary>
    /// Whether a feature's patches failed to apply, which happens when a game update renamed or removed something
    /// they target. The feature is named by the <see cref="TimbnFeatureAttribute"/> on its patch classes, and its
    /// config toggle reads as off through Settings.Toggle and Settings.While while it is broken. Check it by hand
    /// for code that reads the toggle directly.
    /// </summary>
    /// <param name="feature">The feature's name, the key of its config entry.</param>
    /// <returns>True when the feature's patches are not applied.</returns>
    public bool IsFeatureBroken(string feature) => _brokenFeatures.Contains(feature);

    internal static IReadOnlyList<TimbnFrameworkPlugin> Running => _running;

    internal TimbnSubscriptions Subscriptions { get; }

    internal TimbnSubscriptions SessionSubscriptions { get; }

    private IDisposable? _modInfo;
    // Core's entry is bound like every plugin's, so its static Enabled is never null, but nothing reads it and the Mods
    // window shows no switch for it, since every Timbn mod and the window itself stop without Core.
    internal ConfigEntry<bool> EnabledSetting { get; private set; } = null!;

    internal ManualLogSource PluginLogger => Logger;

    internal bool IsPluginEnabled =>
        this is TimbnCorePlugin || (EnabledSetting.Value && TimbnCorePlugin.Instance != null);

    internal bool IsRunning => _started && IsPluginEnabled;

    internal string Folder
    {
        get
        {
            var location = GetType().Assembly.Location;
            if (!string.IsNullOrEmpty(location))
                return Path.GetDirectoryName(location)!;

            var dll = Regex.Replace(GetType().Assembly.GetName().Name, @"-\d+$", "") + ".dll";
            foreach (var root in new[] { Path.Combine(Paths.BepInExRootPath, "scripts"), Paths.PluginPath })
            {
                if (!Directory.Exists(root))
                    continue;

                var match = Directory.GetFiles(root, dll, SearchOption.AllDirectories).FirstOrDefault();
                if (match != null)
                    return Path.GetDirectoryName(match)!;
            }

            return Path.Combine(Paths.PluginPath, Metadata.GUID);
        }
    }

    internal static event Action<TimbnFrameworkPlugin>? PluginStarted;

    private void Awake()
    {
        // BepInEx rewrites the whole .cfg on every Bind, so write it once after all of them.
        var saveOnSet = Config.SaveOnConfigSet;
        Config.SaveOnConfigSet = false;
        try
        {
            Initialize();
        }
        finally
        {
            Config.SaveOnConfigSet = saveOnSet;
            Config.Save();
        }
    }

    private void Initialize()
    {
        EnabledSetting = this is TimbnCorePlugin
            ? Config.Bind("General", "Enabled", true, new ConfigDescription("Timbn Core is always on, and this setting does nothing.", null, new TimbnSettingTags { Browsable = false }))
            : Config.Bind("General", "Enabled", true, $"Master toggle for {Metadata.Name}.");
        BindConfig(Config);
        if (this is not TimbnCorePlugin && TimbnCorePlugin.Instance is null)
        {
            Logger.LogWarning($"Plugin {Metadata.GUID} not started because Timbn Core is not running, since it failed to start. See Core's log lines above.");
            return;
        }

        // Before the Enabled gate, so a mod that is turned off keeps its About card and its place in its group.
        RegisterModInfo();
        if (!IsPluginEnabled)
        {
            Logger.LogInfo($"Plugin {Metadata.GUID} is disabled in config. Skipping.");
            return;
        }

        var harmonyId = $"{Metadata.GUID}.{++_harmonyInstances}";
        if (!TryPatchAll(harmonyId))
            return;

        _started = true;
        OnStarted();
        DontDestroyOnLoad(gameObject);
        Subscriptions.Add(TimbnGameEvents.GameStarted(Events.AttachToSave));
        Subscriptions.Add(TimbnGameEvents.GoToMainMenu(SessionSubscriptions.Dispose));
        Logger.LogMessage($"Plugin {Metadata.GUID} v{Metadata.Version} loaded, patches under '{harmonyId}'.");
        Text.AddLanguageFilesIfPresent();
        if (!TimbnSafe.Run(OnAwake, Logger, nameof(OnAwake)))
        {
            Stop();
            Logger.LogError($"Plugin {Metadata.GUID} v{Metadata.Version} not started, its {nameof(OnAwake)} threw. Nothing was left patched or registered.");
            TimbnLoadErrors.AddStartFailure($"{Metadata.Name} {Metadata.Version}");
            return;
        }

        _running.Add(this);
        TimbnSafe.Run(() => PluginStarted?.Invoke(this), Logger, $"{nameof(TimbnFrameworkPlugin)}|{nameof(PluginStarted)}");
    }

    private bool TryPatchAll(string harmonyId)
    {
        var byFeature = AccessTools.GetTypesFromAssembly(GetType().Assembly)
            .GroupBy(type => type.GetCustomAttributes(typeof(TimbnFeatureAttribute), false).OfType<TimbnFeatureAttribute>().FirstOrDefault()?.Name)
            .OrderBy(group => group.Key is null ? 0 : 1)
            .ToList();

        foreach (var group in byFeature)
        {
            var harmony = new Harmony(group.Key is null ? harmonyId : $"{harmonyId}.{group.Key}");
            var failures = Patch(harmony, group);
            if (failures.Count == 0)
            {
                _harmonies.Add(harmony);
                continue;
            }

            harmony.UnpatchSelf();
            var details = $"{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", failures)}";
            if (group.Key is { } feature)
            {
                _brokenFeatures.Add(feature);
                Logger.LogError(
                    $"Plugin {Metadata.GUID} v{Metadata.Version} feature {feature} is off. {failures.Count} patch class(es) failed on game build " +
                    $"{Application.version}, most likely a renamed or removed target. The rest of the plugin runs.{details}");

                TimbnLoadErrors.AddFeatureFailure(Metadata.Name, feature);
                continue;
            }

            foreach (var patched in _harmonies)
                patched.UnpatchSelf();

            _harmonies.Clear();
            _brokenFeatures.Clear();
            Logger.LogError(
                $"Plugin {Metadata.GUID} v{Metadata.Version} not started. {failures.Count} patch class(es) failed on game build {Application.version}, " +
                $"most likely a renamed or removed target. Nothing was left patched.{details}");

            TimbnLoadErrors.AddStartFailure($"{Metadata.Name} {Metadata.Version}");
            return false;
        }

        return true;
    }

    private static List<string> Patch(Harmony harmony, IEnumerable<Type> types)
    {
        List<string> failures = [];
        foreach (var type in types)
        {
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                failures.Add($"{type.FullName}: {(ex.InnerException ?? ex).Message}");
            }
        }

        return failures;
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        Events.RunFrameHandlers();
        UI.Tick();
        OnUpdate();
    }

    private void LateUpdate()
    {
        if (IsRunning)
            Events.RunLateHandlers();
    }

    private void FixedUpdate()
    {
        if (IsRunning)
            Events.RunFixedHandlers();
    }

    private void OnDestroy()
    {
        _modInfo?.Dispose();
        _modInfo = null;
        if (!_started)
            return;

        Stop();
        Logger.LogMessage($"Plugin {Metadata.GUID} unloaded, subscriptions and patches removed.");
    }

    private void Stop()
    {
        _running.Remove(this);
        TimbnSafe.Run(OnUnload, Logger, nameof(OnUnload));
        Events.CloseSave();
        SessionSubscriptions.Dispose();
        Subscriptions.Dispose();
        foreach (var harmony in _harmonies)
            harmony.UnpatchSelf();

        _harmonies.Clear();
        _started = false;
        OnStopped();
    }

    private protected virtual void OnStarted() { }

    private protected virtual void OnStopped() { }

    /// <summary>Binds the mod's settings. It runs before OnAwake, whether or not the mod is enabled.</summary>
    protected virtual void BindConfig(ConfigFile config) { }

    /// <summary>
    /// Describes the mod for the Mods window. It is optional. The mod's Overview opens with an About card that holds
    /// the description and a button for each link. The author shows beside the version. Mods that name the same group
    /// share one entry in the mod list. That entry opens a page listing them. Core reads this once as the mod wakes
    /// and before it checks Enabled. So a mod that is turned off keeps its card and its place in its group. Links that
    /// are not https pages on the site they are named for are left out with a warning. Null is the default and gives a
    /// card saying the mod gave no information.
    /// </summary>
    /// <example>
    /// <code>
    /// protected override TimbnModInfo ModInfo => new()
    /// {
    ///     ModGroupName = "My Mods",
    ///     ModGroupId = "0b5c7e52-3d1a-4f8e-9c6b-2a7d41e0f9a3",
    ///     Description = "Labels for your chests.",
    ///     NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/12",
    /// };
    /// </code>
    /// </example>
    protected virtual TimbnModInfo? ModInfo => null;

    private void RegisterModInfo() => TimbnSafe.Run(() =>
    {
        if (ModInfo is not { } info)
            return;

        TimbnModLinks.For(info, PluginLogger.LogWarning);
        _modInfo = TimbnModInfos.Add(Metadata.GUID, info);
    }, Logger, nameof(ModInfo));

    /// <summary>
    /// Runs once at startup if the mod is enabled. It runs after config is bound and patches are
    /// applied.
    /// </summary>
    protected virtual void OnAwake() { }

    /// <summary>
    /// Runs every frame while the mod is running. It also runs on the main menu. Use Events.Update for work that
    /// should only run in a save.
    /// </summary>
    protected virtual void OnUpdate() { }

    /// <summary>
    /// Runs when the mod unloads, before Core removes everything the mod registered and its Harmony patches.
    /// Put back anything the mod changed in the game directly, such as a value it set or a GameObject it
    /// created. Anything registered through this class's properties (Events, Settings, Interactions,
    /// ModEvents and the rest) is cleaned up for you. Cleanup that belongs to a save is better placed in
    /// Events.SaveClosed, which also runs on the way to the main menu.
    /// </summary>
    protected virtual void OnUnload() { }
}

/// <summary>
/// Is the class every Timbn mod derives from. Pass the mod's class as T to give it a static Logger, Enabled entry
/// and Instance.
/// </summary>
[SuppressMessage("BepInEx", "BepInEx001:BaseUnityPlugin should have a BepInPlugin attribute", Justification = "Abstract base; concrete plugins carry [BepInPlugin]")]
public abstract class TimbnFrameworkPlugin<T> : TimbnFrameworkPlugin where T : TimbnFrameworkPlugin<T>
{
    /// <summary>
    /// Writes to the log under the mod's name. It hides the BepInEx instance Logger so a static Harmony patch or
    /// helper can log the same way the mod does.
    /// </summary>
    public static new ManualLogSource Logger { get; private set; } = null!;

    /// <summary>
    /// Holds the mod's General/Enabled toggle. It is null until the mod has started. Core's is always on whatever it
    /// says, since Core cannot be turned off.
    /// </summary>
    public static ConfigEntry<bool> Enabled { get; private set; } = null!;

    /// <summary>
    /// Holds the running mod or null while it is not running. A Harmony patch has to be static, so this is how it
    /// reaches the mod's state without a static of its own. It is cleared when the mod unloads, so a hot reload never
    /// leaves a patch talking to the old copy.
    /// </summary>
    /// <example>
    /// <code>
    /// private static void GetDropPosPostFix(WgoData __instance, ref Vector3 __result)
    /// {
    ///     if (Plugin.Instance?.Dismantle.TryGetDropPosition(__instance, out var position) == true)
    ///         __result = position;
    /// }
    /// </code>
    /// </example>
    public static T? Instance { get; private set; }

    /// <summary>
    /// Says whether the mod's Enabled toggle is on while Core runs. Check it in a patch or a static helper that has to
    /// stand down the moment a player turns the mod off. Use Settings.IsEnabled for one feature's toggle. Core's is true
    /// whenever Core runs.
    /// </summary>
    public static bool IsEnabled => TimbnCorePlugin.Instance != null && (typeof(T) == typeof(TimbnCorePlugin) || Enabled?.Value == true);

    /// <summary>Sets the static Logger so a patch or helper can log as soon as the mod is created.</summary>
    protected TimbnFrameworkPlugin()
    {
        Logger = base.Logger;
    }

    private protected sealed override void OnStarted()
    {
        Enabled = EnabledSetting;
        Instance = (T)this;
    }

    private protected sealed override void OnStopped()
    {
        Instance = null;
        Enabled = null!;
    }
}
