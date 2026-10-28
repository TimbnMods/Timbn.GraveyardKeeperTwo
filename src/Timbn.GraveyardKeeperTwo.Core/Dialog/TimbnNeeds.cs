namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What has to be true before the player can go on, checked but never taken. On an answer the game draws each need
/// as a lock and grays the answer out until they are all met. On a quest or stage they gate its talk, so the NPC only
/// offers it once they are met. To take something from the player, use <see cref="TimbnCost"/>.
/// </summary>
/// <example>
/// <code><![CDATA[
/// .Needs(need => need.Reputation(30).OnWeekday(TimbnWeekday.Pride).Item("shovel"))
/// ]]></code>
/// </example>
public sealed class TimbnNeeds
{
    internal TimbnNeeds(TimbnWants wants)
    {
        Wants = wants;
    }

    internal TimbnWants Wants { get; }

    /// <summary>Items the player has to carry but keeps.</summary>
    /// <param name="itemId">The item id, with its quality suffix where it has one, such as grape_wine:1.</param>
    /// <param name="count">How many they need.</param>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds Item(string itemId, int count = 1) => With(() => Wants.HasItem(itemId, count));

    /// <summary>A resource the player has to have but keeps.</summary>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="amount">How much they need.</param>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds Resource(string resourceId, float amount) => With(() => Wants.HasResource(resourceId, amount));

    /// <summary>Money the player has to have but keeps, in the game's raw units, where 100 is one silver coin.</summary>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds Money(int amount) => Resource(LazyConsts.MONEY_KEY, amount);

    /// <summary>
    /// Reputation with the NPC the player is talking to, such as the favor a vendor asks for before a job. Herm and the
    /// village head share one reputation.
    /// </summary>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds Reputation(int amount) => With(() => Wants.Reputation(amount));

    /// <summary>Gratitude, which the town vendors ask for.</summary>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds Gratitude(float amount) => With(() => Wants.Happiness(amount));

    /// <summary>Only on one day of the week, shown with the day's icon. One day per answer.</summary>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds OnWeekday(TimbnWeekday day) => With(() => Wants.OnDay(day));

    /// <summary>One of the town vendors' orders finished this week, such as quest_order_iron. One order per answer.</summary>
    /// <param name="orderId">The order id.</param>
    /// <returns>These needs, for chaining.</returns>
    public TimbnNeeds OrderCompleted(string orderId) => With(() => Wants.OrderCompleted(orderId));

    private TimbnNeeds With(Action add)
    {
        add();
        return this;
    }
}
