using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Patches;

[HarmonyPatch]
internal static class SermonResultDataPatch
{
    private const int _snapDecimals = 4;

    private static readonly MethodInfo _gameRound =
        AccessTools.Method(typeof(Math), nameof(Math.Round), [typeof(double), typeof(MidpointRounding)]);

    private static readonly MethodInfo _faithRound = AccessTools.Method(typeof(SermonResultDataPatch), nameof(RoundFaith));

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Constructor(typeof(SermonResultData),
            [typeof(string), typeof(string), typeof(int), typeof(bool), typeof(int), typeof(int)]);
        yield return AccessTools.PropertyGetter(typeof(SermonResultData), nameof(SermonResultData.FaithOnlyParishioners));
        yield return AccessTools.PropertyGetter(typeof(SermonResultData), nameof(SermonResultData.FaithOnlyBonus));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(_gameRound))
                instruction.operand = _faithRound;

            yield return instruction;
        }
    }

    private static double RoundFaith(double value, MidpointRounding mode) =>
        Math.Round(PluginConfig.SermonFaithRounding.Value ? Math.Round(value, _snapDecimals) : value, mode);
}
