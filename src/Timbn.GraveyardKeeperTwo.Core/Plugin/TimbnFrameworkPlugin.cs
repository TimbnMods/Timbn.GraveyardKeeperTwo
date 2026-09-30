using BepInEx.Bootstrap;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Non generic base of every Timbn plugin. Handles config, the Enabled gate, Harmony patching, and
/// teardown. Plugins derive from <see cref="TimbnFrameworkPlugin{T}"/> instead.
/// </summary>
public abstract class TimbnFrameworkPlugin : BaseUnityPlugin
{
    private static readonly AccessTools.FieldRef<BaseUnityPlugin, PluginInfo?> _infoField =
        AccessTools.FieldRefAccess<BaseUnityPlugin, PluginInfo?>("<Info>k__BackingField");

    private static int _harmonyInstances;

    private readonly List<Harmony> _harmonies = [];
    private readonly HashSet<string> _brokenFeatures = [];
    private bool _started;

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
        Saves = new(this);
    }

    /// <summary>The plugin's own BepInPlugin attribute. Use this instead of Info.</summary>
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
    /// Subscribes this plugin to the game's events. Every subscription is removed when the plugin unloads, and
    /// events that belong to a save follow every save the player loads.
    /// </summary>
    public TimbnPluginEvents Events { get; }

    /// <summary>Adds potions that are removed when this plugin unloads.</summary>
    public TimbnPluginPotions Potions { get; }

    /// <summary>Adds quests that are removed when this plugin unloads.</summary>
    public TimbnPluginQuests Quests { get; }

    /// <summary>Gives NPCs conversations that are removed when this plugin unloads.</summary>
    public TimbnPluginDialog Dialog { get; }

    /// <summary>Adds or overrides the game's text, removed again when this plugin unloads.</summary>
    public TimbnPluginText Text { get; }

    /// <summary>Adds custom sprites, such as item and buff icons, that are removed when this plugin unloads.</summary>
    public TimbnPluginSprites Sprites { get; }

    /// <summary>Adds definitions to the game's balance tables that are removed when this plugin unloads.</summary>
    public TimbnPluginBalance Balance { get; }

    /// <summary>Shows lines of text and popups on the main menu, removed again when this plugin unloads.</summary>
    public TimbnPluginMainMenu MainMenu { get; }

    /// <summary>Keeps the plugin's changes to the game in step with its config as settings change.</summary>
    public TimbnPluginSettings Settings { get; }

    /// <summary>Acts on the player character, such as holding them still, undone when this plugin unloads.</summary>
    public TimbnPluginPlayer Player { get; }

    /// <summary>Changes how fast the game's time runs, undone when this plugin unloads.</summary>
    public TimbnPluginClock Clock { get; }

    /// <summary>Puts the plugin's own hints and IMGUI on screen, taken down when this plugin unloads.</summary>
    public TimbnPluginUI UI { get; }

    /// <summary>Keeps the plugin's own data with each save, in a file next to the game's save.</summary>
    public TimbnPluginSaves Saves { get; }

    /// <summary>
    /// Whether a feature's patches failed to apply, which happens when a game update renamed or removed something
    /// they target. The feature is named by the <see cref="TimbnFeatureAttribute"/> on its patch classes, and its
    /// config toggle reads as off through Settings.Toggle and Settings.While while it is broken. Check it by hand
    /// for code that reads the toggle directly.
    /// </summary>
    /// <param name="feature">The feature's name, the key of its config entry.</param>
    /// <returns>True when the feature's patches are not applied.</returns>
    public bool IsFeatureBroken(string feature) => _brokenFeatures.Contains(feature);

    internal TimbnSubscriptions Subscriptions { get; }

    internal TimbnSubscriptions SessionSubscriptions { get; }

    internal ConfigEntry<bool> EnabledSetting { get; private set; } = null!;

    internal ManualLogSource PluginLogger => Logger;

    internal bool IsEnabled =>
        EnabledSetting.Value && (this is TimbnCorePlugin || TimbnCorePlugin.Instance?.IsEnabled == true);

    internal bool IsRunning => _started && IsEnabled;

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

    private void Awake()
    {
        EnabledSetting = Config.Bind("General", "Enabled", true, $"Master toggle for {Metadata.Name}.");
        BindConfig(Config);
        if (this is not TimbnCorePlugin && TimbnCorePlugin.Instance is null)
        {
            Logger.LogWarning($"Plugin {Metadata.GUID} not started because Timbn Core is not running, either off in its config or failed to start. See Core's log lines above.");
            return;
        }

        if (!IsEnabled)
        {
            Logger.LogInfo($"Plugin {Metadata.GUID} is disabled in config (its own or Core's Enabled toggle). Skipping.");
            return;
        }

        var harmonyId = $"{Metadata.GUID}.{++_harmonyInstances}";
        if (!TryPatchAll(harmonyId))
            return;

        _started = true;
        OnStarted();
        DontDestroyOnLoad(gameObject);
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Subscriptions.Add(TimbnGameEvents.GameStarted(Events.AttachToSave));
        Subscriptions.Add(TimbnGameEvents.GoToMainMenu(SessionSubscriptions.Dispose));
        Subscriptions.Add(TimbnMainMenu.AddLine($"{Metadata.Name}: ", Metadata.Version.ToString()));
        Logger.LogMessage($"Plugin {Metadata.GUID} v{Metadata.Version} loaded, patches under '{harmonyId}'.");
        Text.AddLanguageFilesIfPresent();
        OnAwake();
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
        if (!_started)
            return;

        TimbnSafe.Run(OnDestroyed, Logger, nameof(OnDestroyed));
        Events.CloseSave();
        SessionSubscriptions.Dispose();
        Subscriptions.Dispose();
        foreach (var harmony in _harmonies)
            harmony.UnpatchSelf();

        _harmonies.Clear();
        _started = false;
        OnStopped();
        Logger.LogMessage($"Plugin {Metadata.GUID} unloaded, subscriptions and patches removed.");
    }

    private protected virtual void OnStarted() { }

    private protected virtual void OnStopped() { }

    /// <summary>Binds the plugin's own settings. Runs before OnAwake, whether or not the plugin is enabled.</summary>
    protected virtual void BindConfig(ConfigFile config) { }

    /// <summary>Runs once at startup if the plugin is enabled, after config is bound and patches are applied.</summary>
    protected virtual void OnAwake() { }

    /// <summary>Runs every frame while the plugin is enabled.</summary>
    protected virtual void OnUpdate() { }

    /// <summary>
    /// Runs when the plugin unloads, before Core removes everything the plugin registered and its Harmony
    /// patches. Put back anything the plugin changed in the game directly, such as a value it set or a
    /// GameObject it created. Registrations made through Events, Settings, Potions, Quests, Dialog, Text,
    /// Sprites, Balance, MainMenu, Player, Clock, UI, and Saves are cleaned up for you, and cleanup that belongs
    /// to a save is better placed in Events.SaveClosed, which also runs on the way to the main menu.
    /// </summary>
    protected virtual void OnDestroyed() { }
}

/// <summary>
/// Base class for a Timbn plugin. Pass your plugin type as T to give it its own static Logger, Enabled entry, and
/// Instance.
/// </summary>
[SuppressMessage("BepInEx", "BepInEx001:BaseUnityPlugin should have a BepInPlugin attribute", Justification = "Abstract base; concrete plugins carry [BepInPlugin]")]
public abstract class TimbnFrameworkPlugin<T> : TimbnFrameworkPlugin where T : TimbnFrameworkPlugin<T>
{
    public static new ManualLogSource Logger { get; private set; } = null!;

    /// <summary>The plugin's General/Enabled toggle. Null until the plugin has started.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; } = null!;

    /// <summary>
    /// The running plugin, or null while it is not running. This is how a Harmony patch, which has to be static,
    /// reaches the plugin's state without a static of its own, and it is cleared when the plugin unloads so a hot
    /// reload never leaves a patch talking to the old copy.
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
    /// Whether the plugin's own Enabled toggle and Core's are both on. Check it in a patch or a static helper that
    /// has to stand down the moment a player turns the plugin off in a config manager.
    /// </summary>
    public static bool IsActive => Enabled?.Value == true && TimbnCorePlugin.Enabled?.Value == true;

    protected TimbnFrameworkPlugin()
    {
        Logger = base.Logger;
    }

    private protected sealed override void OnStarted()
    {
        Enabled = EnabledSetting;
        Instance = (T)this;
    }

    private protected sealed override void OnStopped() => Instance = null;
}
