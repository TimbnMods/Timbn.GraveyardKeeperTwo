using Newtonsoft.Json.Linq;
using System.Globalization;

namespace Timbn.GraveyardKeeperTwo.QuestPacks;

/// <summary>
/// Turns the conditions, actions, values and events written in a quest pack into the checks and steps Core runs. Each
/// one is a short "name:argument" string, or an object such as { "carries": "grape_wine:1", "count": 3 } when it
/// needs a count. Parsing reads no game state, and the checks read it only when they run.
/// </summary>
internal sealed class TimbnPackRules
{
    private static readonly string[] _numberFields = ["count", "amount"];

    private readonly Func<string, string> _questId;
    private readonly Func<string, string> _endId;

    internal TimbnPackRules(Func<string, string> questId, Func<string, string>? endId = null)
    {
        _questId = questId;
        _endId = endId ?? questId;
    }

    internal Func<bool> Condition(JToken token)
    {
        if (token is JObject { Count: 1 } single && single.Properties().First() is { Name: "all" or "any" or "not" } property)
        {
            if (property.Name == "not")
            {
                var inner = Condition(property.Value);
                return () => !inner();
            }

            if (property.Value is not JArray { Count: > 0 } list)
                throw TimbnPackException.At(property, $"\"{property.Name}\" needs a list of conditions.");

            var parts = list.Select(Condition).ToList();
            return property.Name == "all" ? () => parts.All(part => part()) : () => parts.Any(part => part());
        }

        (var name, var argument, var number) = Split(token, "condition");
        switch (name)
        {
            case "questStarted":
                {
                    var id = _questId(Required(token, name, argument));
                    return () => TimbnQuests.StatusOf(id) is QuestStatus.InProgress or QuestStatus.Completed;
                }

            case "questCompleted":
                {
                    var id = _endId(Required(token, name, argument));
                    return () => TimbnQuests.StatusOf(id) == QuestStatus.Completed;
                }

            case "questNotStarted":
                {
                    var id = _questId(Required(token, name, argument));
                    return () => TimbnQuests.StatusOf(id) is not (QuestStatus.InProgress or QuestStatus.Completed);
                }

            case "carries":
                {
                    var item = Required(token, name, argument);
                    var count = Count(number);
                    return () => TimbnItems.CountOnPlayer(item) >= count;
                }

            case "owns":
                {
                    var item = Required(token, name, argument);
                    var count = Count(number);
                    return () => TimbnItems.CountAnywhere(item, count) >= count;
                }

            case "hasTech":
                {
                    var tech = Required(token, name, argument);
                    return () => TimbnPlayer.HasTech(tech);
                }

            case "resource":
                {
                    (var resource, var atLeast) = WithNumber(token, name, argument, number, "resource:money:500");
                    return () => TimbnGame.IsInGame && TimbnPlayer.GetResource(resource) >= atLeast;
                }

            case "reputation":
                {
                    (var npc, var atLeast) = WithNumber(token, name, argument, number, $"reputation:{TimbnGameIds.Npcs.Larry}:20");
                    Npc(token, npc);
                    return () => TimbnGame.IsInGame && TimbnNpcs.ReputationOf(npc) is { } reputation && TimbnPlayer.GetResource(reputation) >= atLeast;
                }

            case "day":
                {
                    var day = Weekday(token, Required(token, name, argument));
                    return () => TimbnClock.Weekday == day;
                }

            case "expr":
                {
                    var expression = new TimbnExpression(Required(token, name, argument));
                    return expression.IsTrue;
                }

            case "time":
                {
                    (var from, var to) = Hours(token, Required(token, name, argument));
                    return () =>
                    {
                        var hour = TimbnClock.TimeOfDay * 24f;
                        return from <= to ? hour >= from && hour < to : hour >= from || hour < to;
                    };
                }

            default:
                throw TimbnPackException.At(token, $"\"{name}\" is not a condition. Use questStarted, questCompleted, questNotStarted, carries, owns, hasTech, resource, reputation, day, time or expr.");
        }
    }

    internal Action Actions(JToken token)
    {
        if (token is JArray list)
        {
            var actions = list.Select(Action).ToList();
            return () =>
            {
                foreach (var action in actions)
                    action();
            };
        }

        return Action(token);
    }

    internal Action Action(JToken token)
    {
        (var name, var argument, var number) = Split(token, "action");
        switch (name)
        {
            case "startQuest":
                {
                    var id = _questId(Required(token, name, argument));
                    return () =>
                    {
                        if (!TimbnQuests.StartModQuest(id, "a quest pack started it"))
                            Plugin.Logger.LogWarning($"{nameof(TimbnQuestPacks)}|startQuest:{id} did nothing, since no mod quest with that id can start now.");
                    };
                }

            case "give":
                {
                    var item = Required(token, name, argument);
                    var count = Count(number);
                    return () => TimbnItems.GiveToPlayer(new Item(item, count));
                }

            case "run":
                {
                    var expression = new TimbnExpression(Required(token, name, argument));
                    return expression.Run;
                }

            case "take":
                {
                    var item = Required(token, name, argument);
                    var count = Count(number);
                    return () => TimbnItems.TakeFromPlayer(item, count);
                }

            case "addResource":
                {
                    (var resource, var amount) = WithNumber(token, name, argument, number, "addResource:money:100");
                    return () => TimbnPlayer.AddResource(resource, amount);
                }

            case "reputation":
                {
                    (var npc, var amount) = WithNumber(token, name, argument, number, $"reputation:{TimbnGameIds.Npcs.Larry}:5");
                    Npc(token, npc);
                    return () => new TimbnRewards().Reputation((int)amount).PayReputation(TimbnNpcs.ReputationOf(npc));
                }

            default:
                throw TimbnPackException.At(token, $"\"{name}\" is not an action. Use startQuest, give, take, addResource, reputation or run.");
        }
    }

    internal object Value(JToken token)
    {
        if (token is not JValue { Type: JTokenType.String } value)
            return token is JValue plain ? plain.Value ?? "" : throw TimbnPackException.At(token, "A value should be text or a number.");

        var text = (string)value!;
        var split = text.IndexOf(':');
        var argument = split > 0 ? text[(split + 1)..] : "";
        return (split > 0 ? text[..split] : text) switch
        {
            "itemCount" when argument.Length > 0 => (Func<object>)(() => TimbnItems.CountOnPlayer(argument)),
            "ownedCount" when argument.Length > 0 => (Func<object>)(() => TimbnItems.CountAnywhere(argument)),
            "resource" when argument.Length > 0 => (Func<object>)(() => TimbnPlayer.GetResource(argument)),
            _ => text,
        };
    }

    internal static TimbnQuestEvent Event(JToken token)
    {
        if (token is not JValue { Type: JTokenType.String } value)
            throw TimbnPackException.At(token, "An event should be text, such as \"slept\" or \"crafted:candle\".");

        var text = (string)value!;
        var split = text.IndexOf(':');
        var name = split > 0 ? text[..split] : text;
        var argument = split > 0 ? text[(split + 1)..] : "";
        if (name == "slept")
            return TimbnQuestEvent.Slept();

        if (argument.Length == 0)
            throw TimbnPackException.At(token, $"\"{text}\" is not an event. Use slept, crafted:<craft>, built:<object>, enteredZone:<tag>, walkedInto:<zone>, wonFight:<fight> or usedItem:<item>.");

        return name switch
        {
            "crafted" => TimbnQuestEvent.Crafted(argument),
            "built" => TimbnQuestEvent.Built(argument),
            "enteredZone" => TimbnQuestEvent.EnteredZone(argument),
            "walkedInto" => TimbnQuestEvent.EnteredWorldZone(argument),
            "wonFight" => TimbnQuestEvent.WonFight(argument),
            "usedItem" => TimbnQuestEvent.UsedItem(argument),
            _ => throw TimbnPackException.At(token, $"\"{name}\" is not an event. Use slept, crafted, built, enteredZone, walkedInto, wonFight or usedItem."),
        };
    }

    internal static string Npc(JToken token, string id) =>
        TimbnGameIds.Npcs.All.Contains(id)
            ? id
            : throw TimbnPackException.At(token, $"\"{id}\" is not a person in the game. Use their id, such as {TimbnGameIds.Npcs.Larry} or {TimbnGameIds.Npcs.Herm}.");

    internal static TimbnWeekday Weekday(JToken token, string text) =>
        Enum.TryParse<TimbnWeekday>(text, ignoreCase: true, out var day) && Enum.IsDefined(typeof(TimbnWeekday), day)
            ? day
            : throw TimbnPackException.At(token, $"\"{text}\" is not a day. Use {string.Join(", ", Enum.GetNames(typeof(TimbnWeekday)))}.");

    internal static int Whole(JToken token, string field)
    {
        if (token is JValue { Type: JTokenType.Integer } value && value.Value is long or int)
        {
            var whole = Convert.ToInt64(value.Value, CultureInfo.InvariantCulture);
            if (whole is >= int.MinValue and <= int.MaxValue)
                return (int)whole;
        }

        throw TimbnPackException.At(token, token.Type == JTokenType.Integer
            ? $"\"{field}\" is too big. The most it can be is {int.MaxValue}."
            : $"\"{field}\" should be a whole number.");
    }

    internal static float Number(JToken token, string field)
    {
        if (token is JValue { Type: JTokenType.Integer or JTokenType.Float } value && value.Value is long or int or double or float or decimal)
        {
            var number = Convert.ToDouble(value.Value, CultureInfo.InvariantCulture);
            if (Math.Abs(number) <= float.MaxValue)
                return (float)number;
        }

        throw TimbnPackException.At(token, token.Type is JTokenType.Integer or JTokenType.Float
            ? $"\"{field}\" is too big a number."
            : $"\"{field}\" should be a number.");
    }

    private static (string Name, string Argument, float? Number) Split(JToken token, string what)
    {
        if (token is JValue { Type: JTokenType.String } value)
        {
            var text = (string)value!;
            var split = text.IndexOf(':');
            return split > 0 ? (text[..split], text[(split + 1)..], null) : (text, "", null);
        }

        if (token is not JObject named)
            throw TimbnPackException.At(token, $"A {what} should be text such as \"hasTech:wood_basic\", or an object such as {{ \"carries\": \"flitch\", \"count\": 2 }}.");

        var names = named.Properties().Where(property => !_numberFields.Contains(property.Name)).ToList();
        if (names.Count != 1 || names[0].Value is not JValue { Type: JTokenType.String } argument)
            throw TimbnPackException.At(token, $"A {what} object needs one name with text, such as {{ \"carries\": \"flitch\", \"count\": 2 }}.");

        float? number = null;
        foreach (var field in _numberFields)
        {
            if (named[field] is { } given)
                number = Number(given, field);
        }

        return (names[0].Name, (string)argument!, number);
    }

    private static string Required(JToken token, string name, string argument) =>
        argument.Length > 0 ? argument : throw TimbnPackException.At(token, $"\"{name}\" needs something after the colon, such as {name}:<id>.");

    private static int Count(float? number) => number is { } count ? Math.Max(1, (int)count) : 1;

    private static (string Id, float Number) WithNumber(JToken token, string name, string argument, float? number, string example)
    {
        if (number is { } given)
            return (Required(token, name, argument), given);

        var split = argument.LastIndexOf(':');
        if (split > 0 && float.TryParse(argument[(split + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return (argument[..split], parsed);

        throw TimbnPackException.At(token, $"\"{name}\" needs an id and a number, such as {example}.");
    }

    private static (float From, float To) Hours(JToken token, string text)
    {
        var parts = text.Split('-');
        if (parts.Length == 2
            && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var from)
            && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var to)
            && from is >= 0 and <= 24
            && to is >= 0 and <= 24)
        {
            return (from, to);
        }

        throw TimbnPackException.At(token, $"\"time:{text}\" should be two game hours from 0 to 24, such as time:18-24.");
    }
}
