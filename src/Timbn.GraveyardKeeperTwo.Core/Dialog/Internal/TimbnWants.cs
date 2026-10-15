using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// What an answer asks of the player, filled through <see cref="TimbnNeeds"/> and <see cref="TimbnCost"/>. The game
/// draws a need as a lock and a cost as a price, grays the answer out until the player has it all, and takes the
/// cost when the answer is picked.
/// </summary>
internal sealed class TimbnWants
{
    private readonly List<Action<TimbnAnswer>> _answer = [];
    private readonly List<Func<QuestPhraseRequirement>> _requirements = [];
    private readonly List<ItemCount> _costItems = [];
    private readonly List<GameResAtom> _costResources = [];
    private int _reputation;

    internal TimbnWants()
    {
    }

    internal static TimbnWants? From(Action<TimbnNeeds>? needs, Action<TimbnCost>? cost)
    {
        if (needs is null && cost is null)
            return null;

        var wants = new TimbnWants();
        needs?.Invoke(new TimbnNeeds(wants));
        cost?.Invoke(new TimbnCost(wants));
        return wants;
    }

    internal bool IsEmpty => _answer.Count == 0 && _reputation == 0;

    internal bool WantsReputation => _reputation > 0;

    public TimbnWants Reputation(int amount)
    {
        _reputation += amount;
        return this;
    }

    public TimbnWants Item(string itemId, int count = 1)
    {
        _costItems.Add(new ItemCount { itemId = itemId, count = count });
        return Add(a => a.Costs(itemId, count), () => ItemRequirement(QuestPhraseRequirement.Requirement.Price, itemId, count));
    }

    public TimbnWants Resource(string resourceId, float amount)
    {
        _costResources.Add(new GameResAtom(resourceId, amount));
        return Add(a => a.CostsResource(resourceId, amount), () => ResourceRequirement(QuestPhraseRequirement.Requirement.Price, resourceId, amount));
    }

    public TimbnWants HasItem(string itemId, int count = 1) =>
        Add(a => a.Requires(itemId, count), () => ItemRequirement(QuestPhraseRequirement.Requirement.Lock, itemId, count));

    public TimbnWants HasResource(string resourceId, float amount) =>
        Add(a => a.RequiresResource(resourceId, amount), () => ResourceRequirement(QuestPhraseRequirement.Requirement.Lock, resourceId, amount));

    public TimbnWants Happiness(float amount) => HasResource("happiness", amount);

    public TimbnWants OnDay(TimbnWeekday day) =>
        Add(a => a.RequiresDay(day), () => new QuestPhraseRequirement
        {
            requirement = QuestPhraseRequirement.Requirement.Day,
            entity = QuestPhraseRequirement.Entity.Day,
            dayNumber = TimbnClock.WeekdayId(day),
        });

    public TimbnWants OrderCompleted(string orderId) =>
        Add(a => a.RequiresOrder(orderId), () => new QuestPhraseRequirement
        {
            requirement = QuestPhraseRequirement.Requirement.Order,
            entity = QuestPhraseRequirement.Entity.Order,
            order = orderId,
        });

    internal void ApplyTo(TimbnAnswer answer, string? reputationId)
    {
        foreach (var add in _answer)
            add(answer);

        if (_reputation > 0 && reputationId is not null)
            answer.RequiresResource(reputationId, _reputation);
    }

    internal void Refund()
    {
        foreach (var item in _costItems)
            TimbnItems.GiveToPlayer(new Item(item.itemId, item.count));

        foreach (var resource in _costResources)
            MainGame.PlayerData.AddRes(resource.type, resource.value);
    }

    internal bool IsMet(string? reputationId) =>
        IsEmpty || new QuestFinishCheck { phraseReqs = ToRequirements(reputationId) }.IsReadyToFinish();

    internal List<QuestPhraseRequirement> ToRequirements(string? reputationId)
    {
        var requirements = _requirements.Select(make => make()).ToList();
        if (_reputation > 0 && reputationId is not null)
            requirements.Add(ResourceRequirement(QuestPhraseRequirement.Requirement.Lock, reputationId, _reputation));

        return requirements;
    }

    private TimbnWants Add(Action<TimbnAnswer> answer, Func<QuestPhraseRequirement> requirement)
    {
        _answer.Add(answer);
        _requirements.Add(requirement);
        return this;
    }

    private static QuestPhraseRequirement ItemRequirement(QuestPhraseRequirement.Requirement kind, string itemId, int count) => new()
    {
        requirement = kind,
        entity = QuestPhraseRequirement.Entity.Item,
        itemCount = new ItemCount { itemId = itemId, count = count },
    };

    private static QuestPhraseRequirement ResourceRequirement(QuestPhraseRequirement.Requirement kind, string resourceId, float amount) => new()
    {
        requirement = kind,
        entity = QuestPhraseRequirement.Entity.GameResAtom,
        gameResAtom = new GameResAtom(resourceId, amount),
    };
}
