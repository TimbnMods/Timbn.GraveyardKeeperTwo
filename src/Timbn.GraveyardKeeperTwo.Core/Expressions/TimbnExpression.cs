using Expressive;
using LazyBearTechnology;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One of the game's script expressions written by a mod, such as AddPerk("perk_x") or $money &gt;= 500. In game it is
/// compiled as soon as it is written, so a typo fails when the mod loads rather than when the expression runs.
/// </summary>
internal sealed class TimbnExpression
{
    private LazyExpression? _game;

    internal TimbnExpression(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("An expression can't be empty.");

        Text = text;
        if (CompileNow)
            _game = Compile(text);
    }

    internal static bool CompileNow { get; set; }

    internal string Text { get; }

    internal LazyExpression Game => _game ??= Compile(Text);

    internal void Run() => Game.Evaluate();

    internal bool IsTrue() => Game.EvaluateBool();

    internal void RunOn(WgoData target) => Game.Evaluate(target);

    internal bool IsTrueFor(WgoData target) => Game.EvaluateBool(target);

    /// <summary>
    /// A game expression holding a plain text value, for a field that names an id but is typed as an expression. The
    /// game stores those as pure string values, and parsing the id as a formula would evaluate to nothing.
    /// </summary>
    internal static LazyExpression PureText(string value)
    {
        var expression = new LazyExpression();
        expression.FromString(value, PureValueType.String);
        return expression;
    }

    /// <summary>A game expression holding one number, the way the balance stores an item count.</summary>
    internal static LazyExpression Number(int value) => Formula(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>A game expression parsed the way the balance parses its own, so #perk and $resource references work.</summary>
    internal static LazyExpression Formula(string text) =>
        LazyExpressionBase.ParseExpression<LazyExpression>(text) ?? throw new ArgumentException("An expression can't be empty.");

    private static LazyExpression Compile(string text)
    {
        var expression = LazyExpressionBase.ParseExpression<LazyExpression>(text)
            ?? throw new ArgumentException("An expression can't be empty.");
        try
        {
            GameParser.Parse(expression);
        }
        catch (Exception ex)
        {
            var reason = (ex is TargetInvocationException { InnerException: { } inner } ? inner : ex).Message;
            throw new ArgumentException($"The expression \"{text}\" doesn't work. {reason}");
        }

        return expression;
    }

    private static class GameParser
    {
        private static readonly MethodInfo _init = AccessTools.Method(typeof(LazyExpressionBase), "CheckExpressionInit");
        private static readonly AccessTools.FieldRef<LazyExpressionBase, Expression?> _parsed = AccessTools.FieldRefAccess<LazyExpressionBase, Expression?>("expression");

        internal static void Parse(LazyExpression expression)
        {
            _init.Invoke(expression, null);
            _ = _parsed(expression)?.ReferencedVariables;
        }
    }
}
