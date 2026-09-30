namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Keeps a plugin's own data with each save. The data goes in a file next to the game's save rather than inside
/// it, so removing the plugin never breaks a save, and Steam Cloud syncs it along with the save. Core reads the
/// file when a save loads, writes it each time the game saves, and deletes it with the save. Reach it through the
/// plugin's Saves property.
/// </summary>
public sealed class TimbnPluginSaves
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginSaves(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gives the plugin a data object that is saved and loaded with each save. Call it in OnAwake. Every Timbn
    /// plugin shares one file per save, named after the save's slot and kept next to it, such as
    /// Steam_1.TimbnSaveData.dat beside Steam_1.dat. It holds JSON with a section per plugin GUID. The section is
    /// written with Newtonsoft.Json from T's public properties and fields, so keep T to plain values such as
    /// numbers, strings, lists, dictionaries, and simple classes of your own. Adding a member later is safe, since
    /// an older file leaves it at its default, but renaming one loses what was saved under the old name. A section
    /// that cannot be read is kept in a <c>.bad</c> file and the plugin starts from a new T, and a section whose
    /// plugin is not loaded is kept as it is. On a hot reload the data is carried over from the unloaded copy,
    /// unsaved changes included.
    /// </summary>
    /// <example>
    /// Count the days a save has spent with the mod.
    /// <code><![CDATA[
    /// public sealed class GhostData
    /// {
    ///     public int DaysPassed { get; set; }
    /// }
    ///
    /// private TimbnSaveData<GhostData> _saveData = null!;
    ///
    /// protected override void OnAwake()
    /// {
    ///     _saveData = Saves.Register<GhostData>(data => Logger.LogInfo($"{data.DaysPassed} days so far"));
    ///     Events.NewDayStarted(_ => _saveData.Current.DaysPassed++);
    /// }
    /// ]]></code>
    /// </example>
    /// <typeparam name="T">The plugin's save data type. It needs a public constructor with no parameters.</typeparam>
    /// <param name="loaded">
    /// Optional. Runs with the data each time a save starts, before the plugin's GameStarted handlers, and right
    /// away when the plugin registers while a save is already running.
    /// </param>
    /// <returns>The handle whose Current property holds the data for the loaded save.</returns>
    public TimbnSaveData<T> Register<T>(Action<T>? loaded = null) where T : class, new() => Register(null, loaded);

    /// <summary>
    /// Like <see cref="Register{T}(Action{T})"/>, for a plugin that keeps more than one data object with a save.
    /// Each gets its own section, named after the plugin GUID and the name given here, so the name has to stay
    /// stable.
    /// </summary>
    /// <typeparam name="T">The data type. It needs a public constructor with no parameters.</typeparam>
    /// <param name="name">A short name for this data object, such as Graves. Null or empty uses the plugin's own section.</param>
    /// <param name="loaded">Optional. Runs with the data each time a save starts.</param>
    /// <returns>The handle whose Current property holds the data for the loaded save.</returns>
    public TimbnSaveData<T> Register<T>(string? name, Action<T>? loaded = null) where T : class, new()
    {
        var data = new TimbnSaveData<T>(SectionId(name), loaded);
        _owner.Subscriptions.Add(TimbnSaves.Register(data));
        return data;
    }

    /// <summary>
    /// Gives the plugin a data object that belongs to no save, for things that follow the player across every save
    /// and the main menu, such as a tip the player has already dismissed or a counter over all their games. Call it
    /// in OnAwake. Every Timbn plugin shares one file, TimbnGlobalData.dat, kept with the game's saves and holding
    /// JSON with a section per plugin GUID, written the same way as <see cref="Register{T}(Action{T})"/>. It is
    /// written when the game saves, on the way to the main menu, when the game quits, when the plugin unloads, and
    /// when <see cref="TimbnGlobalData{T}.Save"/> is called, and only when something changed. A crash can lose
    /// changes since the last of those. A previous copy is kept as TimbnGlobalData.backup.dat, and an unreadable
    /// file or section is set aside as a <c>.bad</c> file.
    /// </summary>
    /// <example>
    /// Remember that the player has seen a welcome message, in any save.
    /// <code><![CDATA[
    /// public sealed class WelcomeData
    /// {
    ///     public bool Seen { get; set; }
    /// }
    ///
    /// protected override void OnAwake()
    /// {
    ///     var welcome = Saves.RegisterGlobal<WelcomeData>();
    ///     Events.GameStarted(() =>
    ///     {
    ///         if (welcome.Current.Seen)
    ///             return;
    ///
    ///         welcome.Current.Seen = true;
    ///         welcome.Save();
    ///     });
    /// }
    /// ]]></code>
    /// </example>
    /// <typeparam name="T">The plugin's global data type. It needs a public constructor with no parameters.</typeparam>
    /// <param name="loaded">Optional. Runs with the data as soon as it is read, which is before this method returns.</param>
    /// <returns>The handle whose Current property holds the data.</returns>
    public TimbnGlobalData<T> RegisterGlobal<T>(Action<T>? loaded = null) where T : class, new() => RegisterGlobal(null, loaded);

    /// <summary>
    /// Like <see cref="RegisterGlobal{T}(Action{T})"/>, for a plugin that keeps more than one global data object.
    /// Each gets its own section, named after the plugin GUID and the name given here, so the name has to stay
    /// stable.
    /// </summary>
    /// <typeparam name="T">The data type. It needs a public constructor with no parameters.</typeparam>
    /// <param name="name">A short name for this data object, such as Tips. Null or empty uses the plugin's own section.</param>
    /// <param name="loaded">Optional. Runs with the data as soon as it is read.</param>
    /// <returns>The handle whose Current property holds the data.</returns>
    public TimbnGlobalData<T> RegisterGlobal<T>(string? name, Action<T>? loaded = null) where T : class, new()
    {
        var data = new TimbnGlobalData<T>(SectionId(name), loaded);
        _owner.Subscriptions.Add(TimbnGlobalSaves.Register(data));
        return data;
    }

    private string SectionId(string? name) =>
        string.IsNullOrEmpty(name) ? _owner.Metadata.GUID : $"{_owner.Metadata.GUID}.{name}";
}
