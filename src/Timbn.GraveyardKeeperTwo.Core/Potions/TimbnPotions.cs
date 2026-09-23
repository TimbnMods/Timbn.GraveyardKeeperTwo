namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds potions as real items, buffs, and alchemy formulas, and runs their buff hooks. Disposing the result
/// removes the potion.
/// </summary>
public static class TimbnPotions
{
    private static readonly List<TimbnPotion> _potions = [];
    private static readonly List<TimbnPotion> _active = [];

    internal static IDisposable Register(TimbnPotion potion)
    {
        if (_potions.Find(p => p.Id == potion.Id) is { } previous)
            Unregister(previous);

        _potions.Add(potion);
        var text = TimbnLocale.Add(new Dictionary<string, string>
        {
            [potion.Id] = potion.Name,
            [potion.Id + "_d"] = potion.Description,
            [potion.BuffId] = potion.Buff.Name,
            [potion.BuffId + "_d"] = potion.Buff.Description,
        });
        TimbnPotionBalance.Apply(_potions);
        if (TimbnGame.IsInGame && MainGame.Instance.GameSave?.perkSystemData?.HasPerk(potion.BuffId) == true)
            Start(potion);

        return new TimbnUndo(() =>
        {
            Unregister(potion);
            text.Dispose();
        });
    }

    internal static void OnBalanceLoaded() => TimbnPotionBalance.Apply(_potions);

    internal static void OnGameStarted(TimbnSubscriptions session)
    {
        TimbnPotionBalance.RefreshBuffs(_potions);
        var perks = MainGame.Instance.GameSave.perkSystemData;
        session.Add(TimbnGameEvents.On<PerkData>(OnPerkAdded, h => perks.OnPerkAdded += h, h => perks.OnPerkAdded -= h));
        session.Add(TimbnGameEvents.On<PerkData>(OnPerkRemoved, h => perks.OnPerkRemoved += h, h => perks.OnPerkRemoved -= h));
        session.Add(EndAll);

        foreach (var perk in perks.activePerks)
            OnPerkAdded(perk);
    }

    internal static void OnUpdate()
    {
        for (var i = 0; i < _active.Count; i++)
            Run(_active[i], nameof(TimbnPotionBuff.WhileActive), _active[i].Buff.WhileActive);
    }

    private static void Unregister(TimbnPotion potion)
    {
        if (!_potions.Remove(potion))
            return;

        End(potion);
        TimbnPotionBalance.Apply(_potions);
    }

    private static void OnPerkAdded(PerkData perk)
    {
        if (_potions.Find(p => p.BuffId == perk.id) is { } potion)
            Start(potion);
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

    private static void Run(TimbnPotion potion, string hook, Action? action)
    {
        if (action == null)
            return;

        try
        {
            action();
        }
        catch (Exception ex)
        {
            TimbnCorePlugin.Logger.LogError($"{nameof(TimbnPotions)}|{hook} for {potion.Id} threw: {ex}");
        }
    }
}
