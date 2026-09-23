using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnPotionBalance
{
    private const string _potionDrinkerInspiration = "AddInspiration(\"insp_potion_drinker\", 1)";

    private static readonly Action<GameBalanceBase> _createIdsCache =
        AccessTools.MethodDelegate<Action<GameBalanceBase>>(AccessTools.Method(typeof(GameBalanceBase), "CreateIDsCache"));

    private static readonly AccessTools.FieldRef<ItemDef, string> _itemCustomIcon = AccessTools.FieldRefAccess<ItemDef, string>("customIcon");
    private static readonly AccessTools.FieldRef<PerkDef, string> _perkCustomIcon = AccessTools.FieldRefAccess<PerkDef, string>("customIcon");

    private static readonly string[] _alchemyStations = ["alchemy_mix", "alchemy_mix_2"];

    private static readonly List<ItemDef> _items = [];
    private static readonly List<PerkDef> _perks = [];
    private static readonly List<AlchemyFormulaDef> _formulas = [];
    private static readonly List<AlchemyMixSourceDef> _mixes = [];
    private static GameBalance? _balance;

    public static void Apply(IReadOnlyList<TimbnPotion> potions)
    {
        Remove();
        if (TimbnBalance.Loaded is not { } balance || potions.Count == 0)
            return;

        foreach (var potion in potions)
        {
            if (!TryAdd(balance, potion, out var reason))
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnPotions)}|Skipped {potion.Id}. {reason}");
        }

        _balance = balance;
        _createIdsCache(balance);
        foreach (var item in _items)
            AddToGroupCache(balance, item);

        foreach (var mix in _mixes)
            balance.alchemyMixSourcesByIdCache[mix.mixId] = mix;

        LLBase.AddAliases(_mixes.Select(m => m.mixId).ToList(), _mixes.Select(m => m.formulaId).ToList());
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnPotions)}|Added {_items.Count} potions with {_mixes.Count} ingredient mixes.");
    }

    public static void RefreshBuffs(IReadOnlyList<TimbnPotion> potions)
    {
        foreach (var perk in _perks)
        {
            var potion = potions.FirstOrDefault(p => p.BuffId == perk.id);
            if (potion != null)
                ConfigureBuff(perk, potion.Buff);
        }
    }

    private static void Remove()
    {
        var balance = _balance;
        _balance = null;
        if (balance == null)
            return;

        foreach (var item in _items)
        {
            balance.itemDefs.Remove(item);
            foreach (var group in item.itemGroupIds)
            {
                if (balance.groupItemsCache.TryGetValue(group, out var members))
                    members.Remove(item);
            }
        }

        foreach (var perk in _perks)
            balance.perkDefs.Remove(perk);

        foreach (var formula in _formulas)
            balance.alchemyFormulaDefs.Remove(formula);

        foreach (var mix in _mixes)
        {
            balance.alchemyMixSourceDefs.Remove(mix);
            balance.alchemyMixSourcesByIdCache.Remove(mix.mixId);
            balance.alchemyMixDefsCache.Remove(mix.mixId);
            balance.runtimeCraftDefsCacheAlchemy.Remove(mix.mixId);
        }

        _createIdsCache(balance);
        _items.Clear();
        _perks.Clear();
        _formulas.Clear();
        _mixes.Clear();
    }

    private static bool TryAdd(GameBalance balance, TimbnPotion potion, out string reason)
    {
        var template = balance.itemDefs.Find(i => i.id == potion.TemplateItemId);
        if (template == null)
        {
            reason = $"Template item {potion.TemplateItemId} does not exist.";
            return false;
        }

        if (balance.itemDefs.Exists(i => i.id == potion.Id))
        {
            reason = "An item with that id already exists.";
            return false;
        }

        var clash = balance.alchemyFormulaDefs.Find(f => f.GetRunesAsVector3Int() == potion.Runes);
        if (clash != null)
        {
            reason = $"Runes {potion.Runes} already belong to {clash.id}, so no mix would brew it.";
            return false;
        }

        var templatePerk = balance.perkDefs.Find(p => p.worldFxPrefabId?.StartsWith("buff_char_") == true);
        if (templatePerk == null)
        {
            reason = "No existing buff with a glow to copy.";
            return false;
        }

        var perk = Copy(templatePerk);
        perk.id = potion.BuffId;
        ConfigureBuff(perk, potion.Buff);
        balance.perkDefs.Add(perk);
        _perks.Add(perk);

        var item = Copy(template);
        item.id = potion.Id;
        if (!string.IsNullOrEmpty(potion.IconId))
        {
            item.iconId = potion.IconId;
            _itemCustomIcon(item) = potion.IconId;
        }

        item.basePrice = potion.Price;
        item.canBeUsedInAlchemy = false;
        item.onUseExpressions = [new LazyExpression(_potionDrinkerInspiration), new LazyExpression($"AddPerk(\"{perk.id}\")")];
        item.sortOrder = balance.itemDefs.Count;
        balance.itemDefs.Add(item);
        _items.Add(item);

        var templateFormula = balance.alchemyFormulaDefs.Find(f => f.id == potion.TemplateItemId);
        var formula = new AlchemyFormulaDef
        {
            id = potion.Id,
            runesRed = potion.Runes.x,
            runesGreen = potion.Runes.y,
            runesBlue = potion.Runes.z,
            tab = potion.Tab,
            hiddenAtStart = potion.HiddenAtStart,
            craftsIn = [.. _alchemyStations],
            onCraftEndExpressions = templateFormula != null ? Copy(templateFormula).onCraftEndExpressions : [],
        };
        balance.alchemyFormulaDefs.Add(formula);
        _formulas.Add(formula);

        AddMixes(balance, formula);
        reason = "";
        return true;
    }

    private static void ConfigureBuff(PerkDef perk, TimbnPotionBuff buff)
    {
        var color = buff.GlowColor();
        _perkCustomIcon(perk) = buff.IconId;
        perk.duration = buff.Seconds();
        perk.worldFxPrefabId = $"buff_char_{color}";
        perk.hudFxPrefabId = $"buff_hud_{color}";
        perk.onAddExpressions = [.. buff.OnAddExpressions.Select(e => new LazyExpression(e))];
        perk.onRemoveExpressions = [.. buff.OnRemoveExpressions.Select(e => new LazyExpression(e))];
        perk.setGameResOnAdd = new();
        perk.addGameResOnAdd = new();
        perk.setGameResOnRemove = new();
        perk.addGameResOnRemove = new();
        perk.addGameResPerTick = new();
    }

    private static void AddMixes(GameBalance balance, AlchemyFormulaDef formula)
    {
        var target = formula.GetRunesAsVector3Int();
        var ingredients = balance.itemDefs.Where(i => i.canBeUsedInAlchemy && i.GetRunesAsVector3Int() != Vector3Int.zero).ToList();
        var boosts = balance.craftDefs.Where(c => c.id.EndsWith("_boost")).ToList();
        var runes = ingredients.Select(i => i.GetRunesAsVector3Int()).ToList();
        var boostRunes = boosts.Select(b => b.GetBoostRunesAsVector3Int()).ToList();

        void Consider(Vector3Int sum, params ItemDef[] parts)
        {
            if (sum == target)
                TryAddMix(balance, formula, parts, null);

            for (var b = 0; b < boosts.Count; b++)
            {
                if (sum + boostRunes[b] == target)
                    TryAddMix(balance, formula, parts, boosts[b]);
            }
        }

        for (var i = 0; i < ingredients.Count; i++)
        {
            Consider(runes[i], ingredients[i]);
            for (var j = 0; j < ingredients.Count; j++)
            {
                var pair = runes[i] + runes[j];
                Consider(pair, ingredients[i], ingredients[j]);
                for (var k = 0; k < ingredients.Count; k++)
                    Consider(pair + runes[k], ingredients[i], ingredients[j], ingredients[k]);
            }
        }
    }

    private static void TryAddMix(GameBalance balance, AlchemyFormulaDef formula, ItemDef[] parts, CraftDef? boost)
    {
        var mixId = AlchemyMixDef.MixId(parts.Select(p => p.id).ToArray(), boost);
        if (balance.alchemyMixSourcesByIdCache.ContainsKey(mixId) || _mixes.Exists(m => m.mixId == mixId))
            return;

        var mix = new AlchemyMixSourceDef
        {
            mixId = mixId,
            formulaId = formula.id,
            ingredient1 = parts.Length > 0 ? parts[0].id : "",
            ingredient2 = parts.Length > 1 ? parts[1].id : "",
            ingredient3 = parts.Length > 2 ? parts[2].id : "",
        };
        balance.alchemyMixSourceDefs.Add(mix);
        _mixes.Add(mix);
    }

    private static void AddToGroupCache(GameBalance balance, ItemDef item)
    {
        foreach (var group in item.itemGroupIds)
        {
            if (!balance.groupItemsCache.TryGetValue(group, out var members))
                balance.groupItemsCache[group] = members = [];

            if (!members.Contains(item))
                members.Add(item);
        }
    }

    private static T Copy<T>(T source) => JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
}
