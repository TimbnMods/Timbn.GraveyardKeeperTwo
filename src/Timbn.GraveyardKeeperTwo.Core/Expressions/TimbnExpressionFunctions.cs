using Expressive;
using Expressive.Expressions;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Expression functions Core adds to the game's own, registered once into the shared context the game builds its
/// functions into, so balance expressions such as a build limit can call them.
/// </summary>
internal static class TimbnExpressionFunctions
{
    internal static void Register()
    {
        var context = (Context)AccessTools.Property(typeof(LazyExpression), "SharedContext").GetValue(null);
        context.RegisterFunction(TimbnStationLimitRules.CountFunction, Count, force: true);
    }

    // The build pointer's preview never joins the world's lists, but a temp object is left out in case one does.
    private static object Count(IExpression[] parameters, IDictionary<string, object> values)
    {
        if (!TimbnGame.IsInGame)
            return 0;

        var count = 0;
        foreach (var parameter in parameters)
            count += MainGame.WorldData.GetWgoDataList(parameter.EvaluateAsString(values)).Count(data => data != null && !data.isTempObject);

        return count;
    }
}
