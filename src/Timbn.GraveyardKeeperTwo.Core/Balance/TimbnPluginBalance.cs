using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds definitions to the game's balance tables for a mod. The balance is where the game keeps every item, buff,
/// quest, vendor, craft and building. GameBalance.Me.GetData looks them up by id. Use this for a kind of definition
/// that has no easier Core API. Definitions are removed when the mod unloads. Reach it through the Balance property.
/// </summary>
public sealed class TimbnPluginBalance
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginBalance(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a definition to the balance. It goes in straight away if the balance has already loaded, and again
    /// every time the balance loads after that. Core keeps an added item, buff, craft or station out of every save the
    /// game writes, so the save still loads once the mod is gone. The game's lookups built from definitions follow
    /// along, so a build desk offers a new building and a station lists a new craft straight away, and a station
    /// already in the loaded save picks up a new or replaced world object definition.
    /// </summary>
    /// <param name="definition">The definition to add. Its id must not belong to a definition the game ships.</param>
    /// <returns>A handle that removes the definition early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(BalanceBaseObject definition) => _owner.Subscriptions.Add(TimbnBalance.Add(definition));

    /// <summary>
    /// Adds a definition like <see cref="Add(BalanceBaseObject)"/>, and calls prepare with the loaded balance
    /// right before each insert. Use it when the definition needs something from the balance first, such as
    /// values copied from one of the game's items. A definition with the same id that a mod added earlier is
    /// replaced in place. That is how a hot reloaded mod takes over from its old copy. An id that the game ships is
    /// refused.
    /// </summary>
    /// <param name="definition">The definition to add. Its id must not belong to a definition the game ships.</param>
    /// <param name="prepare">Called with the loaded balance just before the definition goes in, on every load. Can be null.</param>
    /// <returns>A handle that removes the definition early when disposed. You don't need to keep it.</returns>
    public IDisposable Add(BalanceBaseObject definition, Action<GameBalance>? prepare) =>
        _owner.Subscriptions.Add(TimbnBalance.Add(definition, prepare));

    /// <summary>
    /// Adds a copy of one of the game's definitions under a new id. Every field is copied from the game's definition
    /// each time the balance loads, then edit runs on the copy, so it starts with everything the game expects and you
    /// change only what differs. For example
    /// <c>Balance.AddCopy&lt;ItemDef&gt;("cook_baked_fish", "timbn_smoked_fish", fish =&gt; TimbnBalance.SetIcon(fish, "timbn_smoked_fish"))</c>.
    /// </summary>
    /// <typeparam name="T">The kind of definition, such as ItemDef, CraftDef, WGODef or BuildingDef.</typeparam>
    /// <param name="templateId">The id of the game's definition to copy.</param>
    /// <param name="id">The new definition's id. It must not belong to a definition the game ships.</param>
    /// <param name="edit">
    /// Changes the copy after its fields are filled in. Can be null. The game reads an item's use effects again
    /// afterward, so a change to onUseExpressions shows.
    /// </param>
    /// <returns>A handle that removes the definition early when disposed. You don't need to keep it.</returns>
    public IDisposable AddCopy<T>(string templateId, string id, Action<T>? edit = null) where T : BalanceBaseObject, new() =>
        _owner.Subscriptions.Add(TimbnBalance.AddCopy(templateId, id, edit));

    /// <summary>
    /// Adds an item copied from one of the game's, with its name and description in the loaded language and its own
    /// icon, in one call. Everything else, price, stack size and what using it does, stays the template's unless edit
    /// changes it. For example
    /// <c>Balance.AddItem("cook_baked_fish", "timbn_smoked_fish", "Smoked Fish", "Golden and smoky.", "timbn_smoked_fish")</c>
    /// is food that restores energy like baked fish. Core keeps the item out of every save the game writes.
    /// </summary>
    /// <example>
    /// A pie worth more than the muffin it is copied from, with its text in the lang files.
    /// <code><![CDATA[
    /// Balance.AddItem("cook_muffin", "timbn_cheese_pie", "", "", "i_cheese_pie", pie => pie.basePrice = 40);
    /// ]]></code>
    /// </example>
    /// <param name="templateItemId">The id of the game's item to copy.</param>
    /// <param name="id">The new item's id. It must not belong to an item the game ships.</param>
    /// <param name="name">The item's name. Empty leaves the text to the mod's lang files.</param>
    /// <param name="description">The item's tooltip description. Empty leaves it to the lang files.</param>
    /// <param name="iconId">The item's icon, any sprite the game has or one added with Sprites.AddEmbedded and the Item kind. Empty keeps the template's.</param>
    /// <param name="edit">
    /// Changes anything else on the copy, such as its price or onUseExpressions, each time the balance loads. Can be
    /// null. It runs after the icon is set, and the game reads the item's use effects again afterward.
    /// </param>
    /// <returns>A handle that removes the item early when disposed. You don't need to keep it.</returns>
    public IDisposable AddItem(string templateItemId, string id, string name, string description, string iconId = "", Action<ItemDef>? edit = null) =>
        _owner.Subscriptions.Add(TimbnBalance.AddItem(templateItemId, id, name, description, iconId, edit));

    /// <summary>
    /// Changes one of the game's own definitions, such as a perk, an item, or a craft, and puts it back when the
    /// mod unloads or the returned handle is disposed. Apply runs straight away if the balance has loaded, and
    /// again on the fresh copy each time the balance loads after that, so the change is never lost to a reload.
    /// Revert only runs on the copy apply changed, and only while that copy is still the loaded one. Keep what
    /// revert needs, such as the old value, from inside apply. Pair it with Settings.While to follow a toggle.
    /// </summary>
    /// <example>
    /// Raise the red tech point cap while the setting is on.
    /// <code><![CDATA[
    /// LazyExpression original = null!;
    /// Settings.While(PluginConfig.TechPointCap, cap => cap > 999, () => Balance.Edit<GameResSystemDef>(
    ///     "tech_red",
    ///     resource => { original = resource.max; resource.max = TimbnBalance.Expression("5000"); },
    ///     resource => resource.max = original));
    /// ]]></code>
    /// </example>
    /// <typeparam name="T">The definition's type, such as PerkDef, ItemDef, or CraftDef.</typeparam>
    /// <param name="id">The definition's id.</param>
    /// <param name="apply">Makes the change. A throw is logged.</param>
    /// <param name="revert">Puts the definition back the way apply found it. A throw is logged.</param>
    /// <returns>A handle that reverts the change early when disposed. You don't need to keep it.</returns>
    public IDisposable Edit<T>(string id, Action<T> apply, Action<T> revert) where T : BalanceBaseObject =>
        _owner.Subscriptions.Add(TimbnBalance.Edit(id, apply, revert, _owner.PluginLogger));
}
