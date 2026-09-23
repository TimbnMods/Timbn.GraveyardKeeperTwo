using System.Collections.Generic;
using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class TechPointCap
{
    private static readonly string[] _resourceIds = ["tech_red", "tech_green", "tech_blue"];

    private readonly int _cap;
    private readonly Dictionary<GameResSystemDef, LazyExpression> _originals = [];

    public TechPointCap(int cap)
    {
        _cap = cap;
    }

    public void Apply()
    {
        foreach (var id in _resourceIds)
        {
            var definition = GameBalance.Me.GetDataOrNull<GameResSystemDef>(id);
            if (definition == null || _originals.ContainsKey(definition))
                continue;

            var current = definition.max.EvaluateFloat();
            if (current >= _cap)
            {
                Plugin.Logger.LogInfo($"{id} already caps at {current}, leaving it alone.");
                continue;
            }

            _originals[definition] = definition.max;
            definition.max = new LazyExpression(_cap.ToString(CultureInfo.InvariantCulture));
        }

        if (_originals.Count > 0)
            Plugin.Logger.LogInfo($"Tech points now cap at {_cap}.");
    }

    public void Revert()
    {
        foreach (var pair in _originals)
            pair.Key.max = pair.Value;
        _originals.Clear();
    }
}
