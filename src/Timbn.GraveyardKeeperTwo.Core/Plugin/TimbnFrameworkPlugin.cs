using BepInEx.Bootstrap;
using System.Diagnostics.CodeAnalysis;

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

    private Harmony? _harmony;

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
    }

    /// <summary>The plugin's own BepInPlugin attribute. Use this instead of Info.</summary>
    protected BepInPlugin Metadata { get; }

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

    internal TimbnSubscriptions Subscriptions { get; }

    internal TimbnSubscriptions SessionSubscriptions { get; }

    private void Awake()
    {
        InitConfig();
        if (!IsEnabled)
        {
            Logger.LogInfo($"Plugin {Metadata.GUID} is disabled in config (its own or Core's Enabled toggle); skipping.");
            return;
        }

        if (this is not TimbnCorePlugin && TimbnCorePlugin.Instance is null)
        {
            Logger.LogWarning($"Plugin {Metadata.GUID} not started because Timbn Core did not start. See Core's log lines above.");
            return;
        }

        var harmonyId = $"{Metadata.GUID}.{++_harmonyInstances}";
        var harmony = new Harmony(harmonyId);
        if (!TryPatchAll(harmony))
            return;

        _harmony = harmony;
        DontDestroyOnLoad(gameObject);
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        Subscriptions.Add(TimbnGameEvents.GameStarted(Events.AttachToSave));
        Subscriptions.Add(TimbnGameEvents.GoToMainMenu(SessionSubscriptions.Dispose));
        Logger.LogMessage($"Plugin {Metadata.GUID} v{Metadata.Version} loaded, patches under '{harmonyId}'.");
        OnAwake();
    }

    private bool TryPatchAll(Harmony harmony)
    {
        List<string> failures = [];
        foreach (var type in AccessTools.GetTypesFromAssembly(GetType().Assembly))
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

        if (failures.Count == 0)
            return true;

        harmony.UnpatchSelf();
        Logger.LogError(
            $"Plugin {Metadata.GUID} v{Metadata.Version} not started. {failures.Count} patch class(es) failed on game build {Application.version}, " +
            $"most likely a renamed or removed target. Nothing was left patched.{Environment.NewLine}  " +
            string.Join($"{Environment.NewLine}  ", failures));
        return false;
    }

    private void Update()
    {
        if (_harmony is null || !IsEnabled)
            return;

        OnUpdate();
    }

    private void OnDestroy()
    {
        if (_harmony is null)
            return;

        try
        {
            OnDestroyed();
        }
        catch (Exception ex)
        {
            Logger.LogError($"{nameof(OnDestroyed)} threw: {ex}");
        }

        SessionSubscriptions.Dispose();
        Subscriptions.Dispose();
        _harmony.UnpatchSelf();
        _harmony = null;
        Logger.LogMessage($"Plugin {Metadata.GUID} unloaded, subscriptions and patches removed.");
    }

    private protected virtual void InitConfig() { }

    private protected virtual bool IsEnabled => true;

    /// <summary>Runs once at startup if the plugin is enabled, after config is bound and patches are applied.</summary>
    protected virtual void OnAwake() { }

    /// <summary>Runs every frame while the plugin is enabled.</summary>
    protected virtual void OnUpdate() { }

    /// <summary>
    /// Runs when the plugin unloads, before Core removes everything the plugin registered and its Harmony
    /// patches. Put back anything the plugin changed in the game directly, such as a value it set or a
    /// GameObject it created. Registrations made through Events, Potions, Quests, Dialog, Text, Sprites, and
    /// Balance are cleaned up for you.
    /// </summary>
    protected virtual void OnDestroyed() { }
}

/// <summary>
/// Base class for a Timbn plugin. Pass your plugin type as T to give it its own static Logger and Enabled entry.
/// </summary>
[SuppressMessage("BepInEx", "BepInEx001:BaseUnityPlugin should have a BepInPlugin attribute", Justification = "Abstract base; concrete plugins carry [BepInPlugin]")]
public abstract class TimbnFrameworkPlugin<T> : TimbnFrameworkPlugin where T : TimbnFrameworkPlugin<T>
{
    public static new ManualLogSource Logger { get; private set; } = null!;
    public static ConfigEntry<bool> Enabled { get; private set; } = null!;

    protected TimbnFrameworkPlugin()
    {
        Logger = base.Logger;
    }

    private protected sealed override void InitConfig()
    {
        Enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            $"Master toggle for {Metadata.Name}.");

        BindConfig(Config);
    }

    private protected sealed override bool IsEnabled => Enabled.Value && TimbnCorePlugin.Enabled.Value;

    /// <summary>Binds the plugin's own settings. Runs before OnAwake.</summary>
    protected virtual void BindConfig(ConfigFile config) { }
}
