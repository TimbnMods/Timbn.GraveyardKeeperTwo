using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// An answer choice for <see cref="TimbnConversation.Ask(TimbnAnswer[])"/>, with the game's own extras. An answer
/// can cost items or a resource, need something the player keeps (or a day of the week or a vendor order), and hand out or show a reward, and the game draws each
/// on the answer the way it does in its own conversations. A cost shows the item's icon with how many the player
/// carries out of how many are needed, for example 0/3. A need shows a lock. Either greys the answer out until
/// the player has enough. When the answer is picked the game takes the cost and gives the reward itself. A plain
/// string converts to an answer with no extras, so the two can be mixed.
/// </summary>
/// <example>
/// Three choices, showing a price, a need that is not spent, and a price with a reward.
/// <code><![CDATA[
/// talk.Ask(
///         new TimbnAnswer("larry_pay").Costs("flitch", 3),
///         new TimbnAnswer("larry_rich").RequiresResource("money", 100),
///         new TimbnAnswer("larry_trade").Costs("flitch", 1).RewardsResource("money", 5),
///         "larry_nothing")
///     .If("larry_pay", then => then.Say("larry_thanks"));
/// ]]></code>
/// </example>
public sealed class TimbnAnswer
{
    private readonly AnswerData _data = new();
    private bool _hasExtras;

    /// <summary>Makes an answer with no extras.</summary>
    /// <param name="key">The answer's localisation key. It is also the id <see cref="TimbnConversation.If"/> matches on.</param>
    public TimbnAnswer(string key)
    {
        Key = key;
    }

    /// <summary>The answer's localisation key.</summary>
    public string Key { get; }

    /// <summary>Lets a plain answer key stand in for an answer with no extras.</summary>
    /// <param name="key">The answer's localisation key.</param>
    public static implicit operator TimbnAnswer(string key) => new(key);

    /// <summary>Makes picking the answer cost items. The game takes them when it is picked.</summary>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many it costs.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer Costs(string itemId, int count = 1)
    {
        _data.AddCostRes(Items(itemId, count));
        _hasExtras = true;
        return this;
    }

    /// <summary>Makes picking the answer cost a resource, such as money. The game takes it when the answer is picked.</summary>
    /// <param name="resourceId">The resource id, such as money.</param>
    /// <param name="amount">How much it costs.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer CostsResource(string resourceId, float amount)
    {
        _data.AddCostRes(Resource(resourceId, amount));
        _hasExtras = true;
        return this;
    }

    /// <summary>Makes the answer need items the player keeps. It shows a lock and is greyed out until they carry enough.</summary>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many the player needs to carry.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer Requires(string itemId, int count = 1)
    {
        _data.AddLockRes(Items(itemId, count));
        _hasExtras = true;
        return this;
    }

    /// <summary>Makes the answer need a resource the player keeps. It shows a lock and is greyed out until they have enough.</summary>
    /// <param name="resourceId">The resource id, such as money.</param>
    /// <param name="amount">How much the player needs to have.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer RequiresResource(string resourceId, float amount)
    {
        _data.AddLockRes(Resource(resourceId, amount));
        _hasExtras = true;
        return this;
    }

    /// <summary>
    /// Makes the answer only available on one day of the week, the way a vendor's option is. It shows the day's
    /// icon with a lock and is greyed out on every other day. An answer can need one day.
    /// </summary>
    /// <param name="day">The day of the week the answer is available on.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer RequiresDay(TimbnWeekday day)
    {
        _data.AddDay(TimbnClock.WeekdayId(day));
        _hasExtras = true;
        return this;
    }

    /// <summary>
    /// Makes the answer need happiness, which is what the game's town vendors call gratitude and show as a smiley.
    /// It shows a lock with the amount and is greyed out until the player has that much. Nothing is spent. This is
    /// <see cref="RequiresResource"/> for the happiness resource.
    /// </summary>
    /// <param name="amount">How much happiness the player needs to have.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer RequiresHappiness(float amount) => RequiresResource("happiness", amount);

    /// <summary>
    /// Makes the answer need one of the town vendors' orders to be finished this week, the way a vendor's option
    /// is. It shows the ordered item's icon and is greyed out until the order is done. Nothing is spent. An answer
    /// can need one order.
    /// </summary>
    /// <param name="orderId">The order's id, from the game's vendor orders, such as quest_order_iron.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer RequiresOrder(string orderId)
    {
        _data.AddOrder(orderId);
        _hasExtras = true;
        return this;
    }

    /// <summary>Makes picking the answer give items. The game puts them in the player's inventory and drops any that do not fit.</summary>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many it gives.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer Rewards(string itemId, int count = 1)
    {
        _data.AddRewardRes(Items(itemId, count));
        _hasExtras = true;
        return this;
    }

    /// <summary>Makes picking the answer give a resource, such as money.</summary>
    /// <param name="resourceId">The resource id, such as money.</param>
    /// <param name="amount">How much it gives.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer RewardsResource(string resourceId, float amount)
    {
        _data.AddRewardRes(Resource(resourceId, amount));
        _hasExtras = true;
        return this;
    }

    /// <summary>
    /// Shows a reward item on the answer without giving it, for a reward your own code hands out, such as a
    /// <see cref="TimbnConversation.Give"/> step after the answer. It looks the same as <see cref="Rewards"/>.
    /// </summary>
    /// <param name="itemId">The item id, such as flitch.</param>
    /// <param name="count">How many to show.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer ShowsReward(string itemId, int count = 1)
    {
        AddShownReward(Items(itemId, count));
        return this;
    }

    /// <summary>Shows a reward resource, such as money, on the answer without giving it. See <see cref="ShowsReward"/>.</summary>
    /// <param name="resourceId">The resource id, such as money.</param>
    /// <param name="amount">How much to show.</param>
    /// <returns>This answer, for chaining.</returns>
    public TimbnAnswer ShowsRewardResource(string resourceId, float amount)
    {
        AddShownReward(Resource(resourceId, amount));
        return this;
    }

    internal AnswerData? ToData() => _hasExtras ? _data : null;

    private void AddShownReward(SmartRes reward)
    {
        if (_data.fakeRewardRes is null)
        {
            _data.fakeRewardRes = reward;
        }
        else
        {
            _data.fakeRewardRes.items.AddRange(reward.items);
            _data.fakeRewardRes.gameRes.Add(reward.gameRes);
        }

        _hasExtras = true;
    }

    private static SmartRes Items(string itemId, int count) =>
        new() { items = [new ItemCount { itemId = itemId, count = count }] };

    private static SmartRes Resource(string resourceId, float amount) =>
        new() { gameRes = new GameRes(resourceId, amount) };
}
