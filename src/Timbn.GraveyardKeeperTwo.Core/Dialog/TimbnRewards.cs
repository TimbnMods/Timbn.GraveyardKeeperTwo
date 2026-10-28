using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What the player gets for an answer or a quest step. An answer, a step's Accept answer included, shows its rewards
/// and the game pays them when it is picked. Any other step pays them when it is done, and a quest's money and
/// reputation also show on its reward line in the quest window.
/// </summary>
/// <example>
/// <code><![CDATA[
/// .Reward(reward => reward.Money(300).Item("grape_wine:2").Reputation(10))
/// ]]></code>
/// </example>
public sealed class TimbnRewards
{
    private readonly List<ItemCount> _items = [];
    private readonly List<GameResAtom> _resources = [];
    private readonly List<ItemCount> _shownItems = [];
    private readonly List<GameResAtom> _shownResources = [];

    internal TimbnRewards()
    {
    }

    internal int MoneyTotal { get; private set; }

    internal int ReputationTotal { get; private set; }

    internal bool IsEmpty =>
        MoneyTotal == 0 && ReputationTotal == 0 && _items.Count == 0 && _resources.Count == 0 && _shownItems.Count == 0 && _shownResources.Count == 0;

    /// <summary>Money in the game's raw units, where 100 is one silver coin and 10,000 one gold coin.</summary>
    /// <returns>These rewards, for chaining.</returns>
    public TimbnRewards Money(int amount)
    {
        MoneyTotal += amount;
        return this;
    }

    /// <summary>An item. Rewards that don't fit in the player's inventory drop on the ground by the player.</summary>
    /// <param name="itemId">The item id, with its quality suffix where it has one, such as grape_wine:2.</param>
    /// <param name="count">How many to give.</param>
    /// <returns>These rewards, for chaining.</returns>
    public TimbnRewards Item(string itemId, int count = 1)
    {
        _items.Add(new ItemCount { itemId = itemId, count = count });
        return this;
    }

    /// <summary>Any other resource the player keeps.</summary>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="amount">How much to give.</param>
    /// <returns>These rewards, for chaining.</returns>
    public TimbnRewards Resource(string resourceId, float amount)
    {
        _resources.Add(new GameResAtom(resourceId, amount));
        return this;
    }

    /// <summary>
    /// Reputation with the NPC the player is talking to, which is the NPC of a quest step's Talk, or the quest giver
    /// for a step with no Talk.
    /// </summary>
    /// <returns>These rewards, for chaining.</returns>
    public TimbnRewards Reputation(int amount)
    {
        ReputationTotal += amount;
        return this;
    }

    /// <summary>
    /// Shows an item on the answer without giving it, for a reward your own code hands out, such as a Give line after
    /// the answer. It looks the same as <see cref="Item"/>.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// ask.Answer("Trade a stick for boards.", then => then.Give("flitch", 2), cost: cost => cost.Item("stick"), reward: reward => reward.ShowItem("flitch", 2))
    /// ]]></code>
    /// </example>
    /// <param name="itemId">The item id.</param>
    /// <param name="count">How many to show.</param>
    /// <returns>These rewards, for chaining.</returns>
    public TimbnRewards ShowItem(string itemId, int count = 1)
    {
        _shownItems.Add(new ItemCount { itemId = itemId, count = count });
        return this;
    }

    /// <summary>Shows a resource, such as money, on the answer without giving it. See <see cref="ShowItem"/>.</summary>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="amount">How much to show.</param>
    /// <returns>These rewards, for chaining.</returns>
    public TimbnRewards ShowResource(string resourceId, float amount)
    {
        _shownResources.Add(new GameResAtom(resourceId, amount));
        return this;
    }

    internal void ApplyTo(TimbnAnswer answer, string? reputationId)
    {
        foreach (var item in _shownItems)
            answer.ShowsReward(item.itemId, item.count);

        foreach (var resource in _shownResources)
            answer.ShowsRewardResource(resource.type, resource.value);

        if (MoneyTotal > 0)
            answer.RewardsResource(LazyConsts.MONEY_KEY, MoneyTotal);

        foreach (var item in _items)
            answer.Rewards(item.itemId, item.count);

        foreach (var resource in _resources)
            answer.RewardsResource(resource.type, resource.value);

        if (ReputationTotal != 0 && reputationId is not null)
            answer.ShowsRewardResource(reputationId, ReputationTotal);
    }

    internal void PayAll(string? reputationId)
    {
        if (MoneyTotal > 0)
            MainGame.PlayerData.AddRes(LazyConsts.MONEY_KEY, MoneyTotal);

        foreach (var item in _items)
            TimbnPlayer.GiveItem(new Item(item.itemId, item.count), notify: false);

        foreach (var resource in _resources)
            MainGame.PlayerData.AddRes(resource.type, resource.value);

        PayReputation(reputationId);
    }

    internal void PayReputation(string? reputationId)
    {
        if (ReputationTotal != 0 && reputationId is not null)
            MainGame.PlayerData.AddNPCRep(reputationId, ReputationTotal);
    }
}
