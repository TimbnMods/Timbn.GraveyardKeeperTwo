using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One answer choice with the game's own extras, built from a talk's or quest's wants and rewards. The game draws a
/// cost as the item's icon with how many the player carries out of how many are needed and a need as a lock, grays
/// the answer out until the player has enough, and takes the cost and gives the reward itself when it is picked.
/// </summary>
internal sealed class TimbnAnswer
{
    private readonly AnswerData _data = new();
    private bool _hasExtras;

    internal TimbnAnswer(string key, string? id = null)
    {
        Key = key;
        Id = id ?? key;
    }

    internal string Key { get; }

    // What the game hands back when the answer is picked. A menu answer carries its plugin in it, so two plugins
    // using the same answer id on one NPC stay apart, while the key stays the plain locale key a lang file names.
    internal string Id { get; }

    internal TimbnAnswer Costs(string itemId, int count = 1) => Extra(() => _data.AddCostRes(Items(itemId, count)));

    internal TimbnAnswer CostsResource(string resourceId, float amount) => Extra(() => _data.AddCostRes(Resource(resourceId, amount)));

    internal TimbnAnswer Requires(string itemId, int count = 1) => Extra(() => _data.AddLockRes(Items(itemId, count)));

    internal TimbnAnswer RequiresResource(string resourceId, float amount) => Extra(() => _data.AddLockRes(Resource(resourceId, amount)));

    internal TimbnAnswer RequiresDay(TimbnWeekday day) => Extra(() => _data.AddDay(TimbnClock.WeekdayId(day)));

    internal TimbnAnswer RequiresOrder(string orderId) => Extra(() => _data.AddOrder(orderId));

    internal TimbnAnswer Rewards(string itemId, int count = 1) => Extra(() => _data.AddRewardRes(Items(itemId, count)));

    internal TimbnAnswer RewardsResource(string resourceId, float amount) => Extra(() => _data.AddRewardRes(Resource(resourceId, amount)));

    internal TimbnAnswer ShowsReward(string itemId, int count = 1) => Extra(() => AddShown(Items(itemId, count)));

    internal TimbnAnswer ShowsRewardResource(string resourceId, float amount) => Extra(() => AddShown(Resource(resourceId, amount)));

    internal AnswerData? ToData() => _hasExtras ? _data : null;

    private TimbnAnswer Extra(Action add)
    {
        add();
        _hasExtras = true;
        return this;
    }

    private void AddShown(SmartRes reward)
    {
        if (_data.fakeRewardRes is null)
        {
            _data.fakeRewardRes = reward;
            return;
        }

        _data.fakeRewardRes.items.AddRange(reward.items);
        _data.fakeRewardRes.gameRes.Add(reward.gameRes);
    }

    private static SmartRes Items(string itemId, int count) =>
        new() { items = [new ItemCount { itemId = itemId, count = count }] };

    private static SmartRes Resource(string resourceId, float amount) =>
        new() { gameRes = new GameRes(resourceId, amount) };
}
