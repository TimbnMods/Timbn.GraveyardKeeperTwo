namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What the player pays for an answer or a quest step, taken when they pick the answer. The game draws each cost as the
/// item's icon or the price with how much the player has, and grays the answer out until they can pay. Something the
/// player only has to have goes in <see cref="TimbnNeeds"/> instead.
/// </summary>
/// <example>
/// <code><![CDATA[
/// .Cost(cost => cost.Money(200))
/// ]]></code>
/// </example>
public sealed class TimbnCost
{
    internal TimbnCost(TimbnWants wants)
    {
        Wants = wants;
    }

    internal TimbnWants Wants { get; }

    /// <summary>Items the player hands over.</summary>
    /// <param name="itemId">The item id, with its quality suffix where it has one, such as grape_wine:1.</param>
    /// <param name="count">How many.</param>
    /// <returns>This cost, for chaining.</returns>
    public TimbnCost Item(string itemId, int count = 1) => With(() => Wants.Item(itemId, count));

    /// <summary>A resource the player pays.</summary>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="amount">How much.</param>
    /// <returns>This cost, for chaining.</returns>
    public TimbnCost Resource(string resourceId, float amount) => With(() => Wants.Resource(resourceId, amount));

    /// <summary>Money in the game's raw units, where 100 is one silver coin and 10,000 one gold coin.</summary>
    /// <param name="amount">How much.</param>
    /// <returns>This cost, for chaining.</returns>
    public TimbnCost Money(int amount) => Resource(LazyConsts.MONEY_KEY, amount);

    private TimbnCost With(Action add)
    {
        add();
        return this;
    }
}
