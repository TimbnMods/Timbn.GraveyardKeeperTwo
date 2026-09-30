using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal static class TechPointCap
{
    private const int _gameCap = 999;

    private static readonly string[] _resourceIds = ["tech_red", "tech_green", "tech_blue"];

    public static void Register(TimbnFrameworkPlugin plugin)
    {
        foreach (var id in _resourceIds)
            plugin.Settings.While(PluginConfig.TechPointCap, cap => cap > _gameCap, () => Edit(plugin, id));
    }

    private static IDisposable Edit(TimbnFrameworkPlugin plugin, string id)
    {
        LazyExpression? original = null;
        return plugin.Balance.Edit<GameResSystemDef>(
            id,
            definition =>
            {
                original = null;
                var cap = PluginConfig.TechPointCap.Value;
                var current = definition.max.EvaluateFloat();
                if (current >= cap)
                {
                    Plugin.Logger.LogInfo($"{id} already caps at {current}, leaving it alone.");
                    return;
                }

                original = definition.max;
                definition.max = new LazyExpression(cap.ToString(CultureInfo.InvariantCulture));
                Plugin.Logger.LogInfo($"{id} now caps at {cap}.");
            },
            definition =>
            {
                if (original is null)
                    return;

                definition.max = original;
                original = null;
            });
    }
}
