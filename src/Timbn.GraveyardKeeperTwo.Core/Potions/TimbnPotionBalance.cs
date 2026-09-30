using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal sealed class TimbnPotionBalance : IDisposable
{
    private const string _potionDrinkerInspiration = "AddInspiration(\"insp_potion_drinker\", 1)";
    private const string _glowPrefix = "buff_char_";

    private static readonly AccessTools.FieldRef<PerkDef, string> _perkCustomIcon = AccessTools.FieldRefAccess<PerkDef, string>("customIcon");

    private static readonly string[] _alchemyStations = ["alchemy_mix", "alchemy_mix_2"];

    private readonly TimbnPotion _potion;
    private readonly PerkDef _perk;
    private readonly ItemDef _item;
    private readonly AlchemyFormulaDef _formula;
    private readonly List<AlchemyMixSourceDef> _mixes = [];
    private readonly List<IDisposable> _entries = [];

    private TimbnPotionBalance(TimbnPotion potion)
    {
        _potion = potion;
        _perk = new PerkDef { id = potion.BuffId };
        _item = new ItemDef { id = potion.Id };
        _formula = new AlchemyFormulaDef { id = potion.Id };
    }

    internal static bool TryAdd(TimbnPotion potion, out TimbnPotionBalance added, out string reason)
    {
        added = null!;
        if (TimbnBalance.Loaded is { } balance && !Validate(balance, potion, out reason))
            return false;

        reason = "";
        added = new TimbnPotionBalance(potion);
        added._entries.Add(TimbnBalance.Add(added._perk, added.PreparePerk, null));
        added._entries.Add(TimbnBalance.Add(added._item, added.PrepareItem, null));
        added._entries.Add(TimbnBalance.Add(added._formula, added.PrepareFormula, added.RemoveMixes));
        return true;
    }

    internal void RefreshBuff()
    {
        if (TimbnBalance.Loaded?.GetDataOrNull<PerkDef>(_perk.id) == _perk)
            ConfigureBuff(_perk, _potion.Buff);
    }

    public void Dispose()
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
            _entries[i].Dispose();

        _entries.Clear();
    }

    private static bool Validate(GameBalance balance, TimbnPotion potion, out string reason)
    {
        if (balance.GetDataOrNull<ItemDef>(potion.TemplateItemId) == null)
        {
            reason = $"Template item {potion.TemplateItemId} does not exist.";
            return false;
        }

        if (balance.GetDataOrNull<ItemDef>(potion.Id) != null)
        {
            reason = "An item with that id already exists.";
            return false;
        }

        if (FindClash(balance, potion) is { } clash)
        {
            reason = $"Runes {potion.Runes} already belong to {clash.id}, so no mix would brew it.";
            return false;
        }

        if (FindTemplatePerk(balance) == null)
        {
            reason = "No existing buff with a glow to copy.";
            return false;
        }

        reason = "";
        return true;
    }

    private static AlchemyFormulaDef? FindClash(GameBalance balance, TimbnPotion potion) =>
        balance.alchemyFormulaDefs.Find(f => f.id != potion.Id && f.GetRunesAsVector3Int() == potion.Runes);

    private static PerkDef? FindTemplatePerk(GameBalance balance) =>
        balance.perkDefs.Find(p => p.worldFxPrefabId?.StartsWith(_glowPrefix) == true);

    private bool PreparePerk(GameBalance balance)
    {
        var template = FindTemplatePerk(balance);
        if (template == null)
        {
            Skip("No existing buff with a glow to copy.");
            return false;
        }

        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), _perk);
        _perk.id = _potion.BuffId;
        ConfigureBuff(_perk, _potion.Buff);
        return true;
    }

    private bool PrepareItem(GameBalance balance)
    {
        var template = balance.GetDataOrNull<ItemDef>(_potion.TemplateItemId);
        if (template == null)
        {
            Skip($"Template item {_potion.TemplateItemId} does not exist.");
            return false;
        }

        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), _item);
        _item.id = _potion.Id;
        if (!string.IsNullOrEmpty(_potion.IconId))
            TimbnBalance.SetIcon(_item, _potion.IconId);

        _item.basePrice = _potion.Price;
        _item.canBeUsedInAlchemy = false;
        _item.onUseExpressions = [new LazyExpression(_potionDrinkerInspiration), new LazyExpression($"AddPerk(\"{_perk.id}\")")];
        _item.sortOrder = balance.itemDefs.Count;
        TimbnBalance.ForgetPerks(_item);
        return true;
    }

    private bool PrepareFormula(GameBalance balance)
    {
        if (FindClash(balance, _potion) is { } clash)
        {
            Skip($"Runes {_potion.Runes} already belong to {clash.id}, so no mix would brew it.");
            return false;
        }

        var template = balance.alchemyFormulaDefs.Find(f => f.id == _potion.TemplateItemId);
        _formula.runesRed = _potion.Runes.x;
        _formula.runesGreen = _potion.Runes.y;
        _formula.runesBlue = _potion.Runes.z;
        _formula.tab = _potion.Tab;
        _formula.hiddenAtStart = _potion.HiddenAtStart;
        _formula.craftsIn = [.. _alchemyStations];
        _formula.onCraftEndExpressions = template != null ? [.. template.onCraftEndExpressions] : [];
        AddMixes(balance);
        return true;
    }

    private void Skip(string reason) =>
        TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnPotions)}|Skipped {_potion.Id}. {reason}");

    private static void ConfigureBuff(PerkDef perk, TimbnPotionBuff buff)
    {
        var color = buff.GlowColor();
        _perkCustomIcon(perk) = buff.IconId;
        perk.duration = buff.Seconds();
        perk.worldFxPrefabId = $"{_glowPrefix}{color}";
        perk.hudFxPrefabId = $"buff_hud_{color}";
        perk.onAddExpressions = [.. buff.OnAddExpressions.Select(e => new LazyExpression(e))];
        perk.onRemoveExpressions = [.. buff.OnRemoveExpressions.Select(e => new LazyExpression(e))];
        perk.setGameResOnAdd = new();
        perk.addGameResOnAdd = new();
        perk.setGameResOnRemove = new();
        perk.addGameResOnRemove = new();
        perk.addGameResPerTick = new();
    }

    private void AddMixes(GameBalance balance)
    {
        _mixes.Clear();
        var target = _formula.GetRunesAsVector3Int();
        var ingredients = balance.itemDefs.Where(i => i.canBeUsedInAlchemy && i.GetRunesAsVector3Int() != Vector3Int.zero).ToList();
        var boosts = balance.craftDefs.Where(c => c.id.EndsWith("_boost")).ToList();
        var runes = ingredients.Select(i => i.GetRunesAsVector3Int()).ToList();
        var boostRunes = boosts.Select(b => b.GetBoostRunesAsVector3Int()).ToList();

        void Consider(Vector3Int sum, params ItemDef[] parts)
        {
            if (sum == target)
                TryAddMix(balance, parts, null);

            for (var b = 0; b < boosts.Count; b++)
            {
                if (sum + boostRunes[b] == target)
                    TryAddMix(balance, parts, boosts[b]);
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

        LLBase.AddAliases(_mixes.Select(m => m.mixId).ToList(), _mixes.Select(m => m.formulaId).ToList());
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnPotions)}|Added {_potion.Id} with {_mixes.Count} ingredient mixes.");
    }

    private void TryAddMix(GameBalance balance, ItemDef[] parts, CraftDef? boost)
    {
        var mixId = AlchemyMixDef.MixId(parts.Select(p => p.id).ToArray(), boost);
        if (balance.alchemyMixSourcesByIdCache.ContainsKey(mixId))
            return;

        var mix = new AlchemyMixSourceDef
        {
            mixId = mixId,
            formulaId = _formula.id,
            ingredient1 = parts.Length > 0 ? parts[0].id : "",
            ingredient2 = parts.Length > 1 ? parts[1].id : "",
            ingredient3 = parts.Length > 2 ? parts[2].id : "",
        };
        balance.alchemyMixSourceDefs.Add(mix);
        balance.alchemyMixSourcesByIdCache[mixId] = mix;
        _mixes.Add(mix);
    }

    private void RemoveMixes(GameBalance balance)
    {
        foreach (var mix in _mixes)
        {
            balance.alchemyMixSourceDefs.Remove(mix);
            balance.alchemyMixSourcesByIdCache.Remove(mix.mixId);
            balance.alchemyMixDefsCache.Remove(mix.mixId);
            balance.runtimeCraftDefsCacheAlchemy.Remove(mix.mixId);
        }

        _mixes.Clear();
    }
}
