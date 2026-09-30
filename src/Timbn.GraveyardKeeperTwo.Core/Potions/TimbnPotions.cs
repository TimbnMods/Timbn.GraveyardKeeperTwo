namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Registers potions, runs their buff hooks while the player has the buff, and removes them again.</summary>
internal static class TimbnPotions
{
    private static readonly List<Registered> _potions = [];
    private static readonly List<TimbnPotion> _active = [];

    internal static IDisposable Register(TimbnPotion potion)
    {
        if (_potions.Find(p => p.Potion.Id == potion.Id) is { } previous)
            Unregister(previous);

        if (!TimbnPotionBalance.TryAdd(potion, out var balance, out var reason))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnPotions)}|Skipped {potion.Id}. {reason}");
            return TimbnUndo.None;
        }

        var text = TimbnLocale.Add(new Dictionary<string, string>
        {
            [potion.Id] = potion.Name,
            [potion.Id + "_d"] = potion.Description,
            [potion.BuffId] = potion.Buff.Name,
            [potion.BuffId + "_d"] = potion.Buff.Description,
        });
        var registered = new Registered(potion, balance, text);
        _potions.Add(registered);
        if (TimbnPlayer.HasPerk(potion.BuffId))
            Start(potion);

        return new TimbnUndo(() => Unregister(registered));
    }

    internal static void OnGameStarted(TimbnSubscriptions session)
    {
        foreach (var registered in _potions)
            registered.Balance.RefreshBuff();

        var perks = MainGame.Instance.GameSave.perkSystemData;
        session.Add(TimbnGameEvents.On<PerkData>(OnPerkAdded, h => perks.OnPerkAdded += h, h => perks.OnPerkAdded -= h));
        session.Add(TimbnGameEvents.On<PerkData>(OnPerkRemoved, h => perks.OnPerkRemoved += h, h => perks.OnPerkRemoved -= h));
        session.Add(EndAll);

        foreach (var perk in perks.activePerks)
            OnPerkAdded(perk);
    }

    internal static void Tick()
    {
        for (var i = 0; i < _active.Count; i++)
            Run(_active[i], nameof(TimbnPotionBuff.WhileActive), _active[i].Buff.WhileActive);
    }

    private static void Unregister(Registered registered)
    {
        if (!_potions.Remove(registered))
            return;

        End(registered.Potion);
        registered.Balance.Dispose();
        registered.Text.Dispose();
    }

    private static void OnPerkAdded(PerkData perk)
    {
        if (_potions.Find(p => p.Potion.BuffId == perk.id) is { } registered)
            Start(registered.Potion);
    }

    private static void OnPerkRemoved(PerkData perk)
    {
        if (_active.Find(p => p.BuffId == perk.id) is { } potion)
            End(potion);
    }

    private static void Start(TimbnPotion potion)
    {
        if (_active.Contains(potion))
            return;

        _active.Add(potion);
        Run(potion, nameof(TimbnPotionBuff.OnStart), potion.Buff.OnStart);
    }

    private static void End(TimbnPotion potion)
    {
        if (_active.Remove(potion))
            Run(potion, nameof(TimbnPotionBuff.OnEnd), potion.Buff.OnEnd);
    }

    private static void EndAll()
    {
        foreach (var potion in _active.ToList())
            End(potion);
    }

    private static void Run(TimbnPotion potion, string hook, Action? action) =>
        TimbnSafe.Run(action, $"{nameof(TimbnPotions)}|{hook} for {potion.Id}");

    private sealed class Registered(TimbnPotion potion, TimbnPotionBalance balance, IDisposable text)
    {
        public TimbnPotion Potion { get; } = potion;

        public TimbnPotionBalance Balance { get; } = balance;

        public IDisposable Text { get; } = text;
    }
}
