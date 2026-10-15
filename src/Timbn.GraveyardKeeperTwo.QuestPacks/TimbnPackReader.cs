using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Timbn.GraveyardKeeperTwo.QuestPacks;

/// <summary>
/// Reads the quest files of one pack into quest builders and recorded talks, with every quest, talk and sprite id the
/// pack names prefixed by the pack's id. A mistake skips the quest, talk or file it is in and goes on the problem list,
/// so the rest of the pack still loads. It reads no game state, so it can be tested.
/// </summary>
internal sealed class TimbnPackReader
{
    private static readonly Regex _idPattern = new("^[a-z0-9_]+$");

    private static readonly string[] _lineKinds = ["say", "playerSay", "sayAs", "give", "take", "wait", "cinematic", "when", "ask", "do", "run"];

    private static readonly string[] _stepParts = ["phase", "description", "done", "icon", "npc", "needs", "cost", "talk", "reward", "readyOn", "readyWhen", "then", "onDone", "runOnStart", "runOnDone"];

    private readonly string _packId;
    private readonly HashSet<string> _sprites;
    private readonly HashSet<string> _questIds = [];
    private readonly HashSet<string> _talkIds = [];
    private readonly Dictionary<string, string> _endIds = [];
    private readonly TimbnPackRules _rules;
    private readonly TimbnPackContent _content = new();
    private string _where = "";

    internal TimbnPackReader(string packId, IEnumerable<string> spriteNames)
    {
        _packId = packId;
        _sprites = [.. spriteNames];
        _rules = new TimbnPackRules(QuestId, EndId);
    }

    internal static TimbnPackInfo ReadInfo(string text)
    {
        var root = JObject.Parse(text);
        string? id = null;
        string? name = null;
        Color? color = null;
        foreach (var property in root.Properties())
        {
            switch (property.Name)
            {
                case "$schema":
                    break;
                case "id":
                    id = Text(property.Value, "id");
                    break;
                case "name":
                    name = Text(property.Value, "name");
                    break;
                case "bubbleColor":
                    color = ParseColor(property.Value);
                    break;
                default:
                    throw TimbnPackException.At(property, $"\"{property.Name}\" is not a pack.json field. Use id, name and bubbleColor.");
            }
        }

        if (id is null)
            throw TimbnPackException.At(root, "pack.json needs an \"id\", such as \"larrys_wine\".");

        if (!_idPattern.IsMatch(id))
            throw TimbnPackException.At(root["id"]!, $"The pack id \"{id}\" can only hold lowercase letters, digits and underscores.");

        return new TimbnPackInfo(id, name ?? id, color);
    }

    internal static Color ParseColor(JToken token)
    {
        var text = Text(token, "bubbleColor").TrimStart('#');
        if (text.Length is 6 or 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
        {
            if (text.Length == 6)
                hex = (hex << 8) | 0xFF;

            return new Color(((hex >> 24) & 0xFF) / 255f, ((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f);
        }

        throw TimbnPackException.At(token, $"\"{text}\" is not a color. Write it like \"#9ad0ff\".");
    }

    internal TimbnPackContent Read(IEnumerable<(string Name, string Text)> files)
    {
        List<(string File, string Id, JObject Quest)> quests = [];
        List<(string File, string Id, JObject Talk)> talks = [];
        foreach ((var file, var text) in files)
        {
            JObject root;
            try
            {
                root = JObject.Parse(text);
            }
            catch (JsonException ex)
            {
                Problem($"{file}: {ex.Message} The whole file was skipped.");
                continue;
            }

            _where = file;
            foreach (var property in root.Properties())
            {
                switch (property.Name)
                {
                    case "$schema":
                        break;
                    case "quests":
                        Collect(file, property.Value, "quest", _questIds, quests);
                        break;
                    case "talks":
                        Collect(file, property.Value, "talk", _talkIds, talks);
                        break;
                    default:
                        Unknown(property);
                        break;
                }
            }
        }

        foreach ((_, var id, var quest) in quests)
        {
            if (LastStage(quest) is { } stage)
                _endIds[id] = $"{Full(id)}_{stage}";
        }

        foreach ((var file, var id, var quest) in quests)
        {
            _where = $"{file}, quest '{id}'";
            Try(() => _content.Quests.Add(BuildQuest(id, quest)));
        }

        foreach ((var file, var id, var talk) in talks)
        {
            _where = $"{file}, talk '{id}'";
            Try(() => _content.Talks.Add(BuildTalk(id, talk)));
        }

        return _content;
    }

    private void Collect(string file, JToken token, string kind, HashSet<string> ids, List<(string, string, JObject)> into)
    {
        if (token is not JArray list)
        {
            Problem($"{file}: {TimbnPackException.LineOf(token)}\"{kind}s\" should be a list, such as [ {{ ... }} ].");
            return;
        }

        foreach (var entry in list)
        {
            if (entry is not JObject obj || obj["id"] is not JValue { Type: JTokenType.String } idToken)
            {
                Problem($"{file}: {TimbnPackException.LineOf(entry)}each {kind} needs an \"id\", so this one was skipped.");
                continue;
            }

            var id = (string)idToken!;
            if (!_idPattern.IsMatch(id))
                Problem($"{file}: {TimbnPackException.LineOf(idToken)}the {kind} id \"{id}\" can only hold lowercase letters, digits and underscores, so it was skipped.");
            else if (!ids.Add(id))
                Problem($"{file}: {TimbnPackException.LineOf(idToken)}a second {kind} with the id \"{id}\" was skipped.");
            else
                into.Add((file, id, obj));
        }
    }

    private TimbnQuestBuilder BuildQuest(string id, JObject quest)
    {
        var builder = new TimbnQuestBuilder(Full(id), Npc(Required(quest, "giver"), "giver"), Text(Required(quest, "name"), "name"));
        Dictionary<TimbnQuestBuilder, Func<bool>> readyWhen = [];
        foreach (var property in quest.Properties())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "id":
                case "giver":
                case "name":
                    break;
                case "after":
                    foreach (var parent in TextList(value, "after"))
                        builder.OfferAfter(EndId(parent));

                    break;
                case "continues":
                    builder.ContinuesId(ContinuedId(value));
                    break;
                case "offerWhen":
                    builder.OfferWhen(_rules.Condition(value));
                    break;
                case "offer":
                    builder.Offer(talk => Offer(talk, Object(value, "offer")));
                    break;
                case "startsOn":
                    builder.StartsOn(TimbnPackRules.Event(value));
                    break;
                case "stages":
                    Stages(builder, value, readyWhen);
                    break;
                default:
                    if (!StepField(builder, property, readyWhen))
                        Unknown(property);

                    break;
            }
        }

        builder.Finish();
        foreach (var step in builder.Steps)
        {
            if (readyWhen.TryGetValue(step, out var isReady))
                _content.ReadyWhen[step.Id] = isReady;
        }

        return builder;
    }

    private void Stages(TimbnQuestBuilder builder, JToken token, Dictionary<TimbnQuestBuilder, Func<bool>> readyWhen)
    {
        if (token is not JArray stages)
            throw TimbnPackException.At(token, "\"stages\" should be a list of stages, such as [ { \"id\": \"ask\", ... } ].");

        foreach (var entry in stages)
        {
            var stage = Object(entry, "stage");
            var id = Text(Required(stage, "id"), "id");
            builder.AddStage(id, Text(Required(stage, "description"), "description"), added =>
            {
                foreach (var property in stage.Properties())
                {
                    switch (property.Name)
                    {
                        case "id":
                        case "description":
                            break;
                        case "name":
                            added.Name(Text(property.Value, "name"));
                            break;
                        default:
                            if (!StepField(added.Step, property, readyWhen))
                                Unknown(property);

                            break;
                    }
                }
            });
        }
    }

    private bool StepField(TimbnQuestBuilder step, JProperty property, Dictionary<TimbnQuestBuilder, Func<bool>> readyWhen)
    {
        var value = property.Value;
        switch (property.Name)
        {
            case "icon":
                step.Icon(Icon(Text(value, "icon")));
                return true;
            case "phase":
                step.Phase(Int(value, "phase"));
                return true;
            case "description":
                step.Description(Text(value, "description"));
                return true;
            case "done":
                step.DoneDescription(Text(value, "done"));
                return true;
            case "npc":
                step.TalksWith(Npc(value, "npc"));
                return true;
            case "needs":
                step.Needs(needs => Needs(needs, value));
                return true;
            case "cost":
                step.Cost(cost => Cost(cost, value));
                return true;
            case "talk":
                step.Talk(talk => Talk(talk, Object(value, "talk")));
                return true;
            case "reward":
                step.Reward(rewards => Rewards(rewards, value));
                return true;
            case "readyOn":
                step.ReadyOn(TimbnPackRules.Event(value));
                return true;
            case "readyWhen":
                readyWhen[step] = _rules.Condition(value);
                step.ReadyByCode();
                return true;
            case "then":
                step.DoneLines(lines => Lines(lines, value));
                return true;
            case "onDone":
                step.OnDone(_rules.Actions(value));
                return true;
            case "runOnStart":
                foreach (var expression in TextList(value, "runOnStart"))
                    step.OnStartExpression(expression);

                return true;
            case "runOnDone":
                foreach (var expression in TextList(value, "runOnDone"))
                    step.OnDoneExpression(expression);

                return true;
            default:
                return false;
        }
    }

    private TimbnPackTalk BuildTalk(string id, JObject talk)
    {
        Dictionary<string, string> texts = [];
        var lines = new TimbnLines(new TimbnScript(Full(id), texts));
        var npc = Npc(Required(talk, "npc"), "npc");
        Required(talk, "lines");
        Func<bool> when = () => true;
        var once = false;
        Color? color = null;
        foreach (var property in talk.Properties())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "id":
                case "npc":
                    break;
                case "when":
                    when = _rules.Condition(value);
                    break;
                case "once":
                    once = value.Type == JTokenType.Boolean ? value.Value<bool>() : throw TimbnPackException.At(value, "\"once\" should be true or false.");
                    break;
                case "bubbleColor":
                    color = ParseColor(value);
                    break;
                case "lines":
                    Lines(lines, value);
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }

        return new TimbnPackTalk(Full(id), npc, when, once, color, lines, texts);
    }

    private void Offer(TimbnQuestOffer talk, JObject offer)
    {
        if (offer["lines"] is { } lines)
            Lines(talk, lines);

        foreach (var property in offer.Properties())
        {
            switch (property.Name)
            {
                case "lines":
                    break;
                case "accept":
                    var accept = Answer(property.Value, "accept", allowNeeds: true, allowCost: true);
                    talk.Accept(accept.Text, accept.Then, accept.Needs, accept.Cost);
                    break;
                case "decline":
                    var decline = Answer(property.Value, "decline");
                    talk.Decline(decline.Text, decline.Then);
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }
    }

    private void Talk(TimbnQuestTalk talk, JObject spec)
    {
        if (spec["lines"] is { } lines)
            Lines(talk, lines);

        foreach (var property in spec.Properties())
        {
            switch (property.Name)
            {
                case "lines":
                    break;
                case "accept" when property.Value.Type == JTokenType.Boolean:
                    if (!property.Value.Value<bool>())
                        throw TimbnPackException.At(property.Value, "\"accept\" should be the answer's text, { \"text\", \"then\" }, or true to finish once the lines are done.");

                    talk.Accept();
                    break;
                case "accept":
                    var accept = Answer(property.Value, "accept");
                    talk.Accept(accept.Text, accept.Then);
                    break;
                case "decline":
                    var decline = Answer(property.Value, "decline");
                    talk.Decline(decline.Text, decline.Then);
                    break;
                case "resume":
                    talk.Resume(Text(property.Value, "resume"));
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }
    }

    private (string Text, Action<TimbnLines>? Then, Action<TimbnNeeds>? Needs, Action<TimbnCost>? Cost, Action<TimbnRewards>? Reward, string? Key) Answer(JToken token, string field, bool allowNeeds = false, bool allowCost = false, bool allowReward = false, bool allowKey = false)
    {
        if (token is JValue { Type: JTokenType.String })
            return (Text(token, field), null, null, null, null, null);

        var answer = Object(token, field);
        string? text = null;
        Action<TimbnLines>? then = null;
        Action<TimbnNeeds>? needs = null;
        Action<TimbnCost>? cost = null;
        Action<TimbnRewards>? reward = null;
        string? key = null;
        foreach (var property in answer.Properties())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "text":
                    text = Text(value, "text");
                    break;
                case "then":
                    then = lines => Lines(lines, value);
                    break;
                case "needs" when allowNeeds:
                    needs = needed => Needs(needed, value);
                    break;
                case "cost" when allowCost:
                    cost = paid => Cost(paid, value);
                    break;
                case "reward" when allowReward:
                    reward = given => Rewards(given, value);
                    break;
                case "key" when allowKey:
                    key = Text(value, "key");
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }

        return (text ?? throw TimbnPackException.At(answer, $"\"{field}\" needs a \"text\"."), then, needs, cost, reward, key);
    }

    private void Lines<T>(TimbnTalk<T> talk, JToken token)
        where T : TimbnTalk<T>
    {
        if (token is not JArray list)
            throw TimbnPackException.At(token, "Lines should be a list, such as [\"Hello.\", \"Bring me wine.\"].");

        foreach (var line in list)
            Line(talk, line);
    }

    private void Line<T>(TimbnTalk<T> talk, JToken line)
        where T : TimbnTalk<T>
    {
        if (line is JValue { Type: JTokenType.String })
        {
            talk.Say(Text(line, "line"));
            return;
        }

        if (line is not JObject step || step.Properties().FirstOrDefault(property => _lineKinds.Contains(property.Name)) is not { } kind)
            throw TimbnPackException.At(line, $"A line should be text, or an object with one of {string.Join(", ", _lineKinds)}.");

        var value = kind.Value;
        switch (kind.Name)
        {
            case "say":
                Allowed(step, "say", "values", "key");
                talk.Say(Text(value, "say"), Values(step));
                LineKey(talk, step);
                break;
            case "playerSay":
                Allowed(step, "playerSay", "values", "key");
                talk.PlayerSay(Text(value, "playerSay"), Values(step));
                LineKey(talk, step);
                break;
            case "sayAs":
                Allowed(step, "sayAs", "text", "key");
                talk.SayAs(Npc(value, "sayAs"), Text(Required(step, "text"), "text"));
                LineKey(talk, step);
                break;
            case "give":
                Allowed(step, "give", "count");
                talk.Give(Text(value, "give"), step["count"] is { } giveCount ? Int(giveCount, "count") : 1);
                break;
            case "take":
                Allowed(step, "take", "count");
                talk.Take(Text(value, "take"), step["count"] is { } takeCount ? Int(takeCount, "count") : 1);
                break;
            case "wait":
                Allowed(step, "wait");
                talk.WaitRealSeconds(Number(value, "wait"));
                break;
            case "cinematic":
                Allowed(step, "cinematic");
                talk.Cinematic();
                break;
            case "when":
                Allowed(step, "when", "then", "otherwise");
                var condition = _rules.Condition(value);
                var then = step["then"];
                var otherwise = step["otherwise"];
                talk.When(
                    condition,
                    yes =>
                    {
                        if (then is not null)
                            Lines(yes, then);
                    },
                    otherwise is null ? null : no => Lines(no, otherwise));
                break;
            case "ask":
                Allowed(step, "ask");
                if (value is not JArray answers)
                    throw TimbnPackException.At(value, "\"ask\" should be a list of answers.");

                talk.Ask(ask =>
                {
                    foreach (var entry in answers)
                    {
                        var answer = Answer(entry, "answer", allowNeeds: true, allowCost: true, allowReward: true, allowKey: true);
                        ask.Answer(answer.Text, answer.Then, answer.Needs, answer.Cost, answer.Reward, answer.Key);
                    }
                });
                break;
            case "run":
                Allowed(step, "run");
                talk.RunExpression(Text(value, "run"));
                break;
            case "do":
                Allowed(step, "do");
                talk.RunCode(_rules.Actions(value));
                break;
        }
    }

    private static void LineKey<T>(TimbnTalk<T> talk, JObject step)
        where T : TimbnTalk<T>
    {
        if (step["key"] is { } key)
            talk.Named(Text(key, "key"));
    }

    private object[] Values(JObject step) =>
        step["values"] switch
        {
            null => [],
            JArray values => [.. values.Select(_rules.Value)],
            var other => throw TimbnPackException.At(other, "\"values\" should be a list, such as [\"itemCount:flitch\"]."),
        };

    private void Needs(TimbnNeeds needs, JToken token)
    {
        foreach (var property in Object(token, "needs").Properties())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "items":
                    Map(value, "items", (id, count) => needs.Item(id, Int(count, id)));
                    break;
                case "resources":
                    Map(value, "resources", (id, amount) => needs.Resource(id, Number(amount, id)));
                    break;
                case "money":
                    needs.Money(Int(value, "money"));
                    break;
                case "reputation":
                    needs.Reputation(Int(value, "reputation"));
                    break;
                case "happiness":
                    needs.Gratitude(Number(value, "happiness"));
                    break;
                case "onDay":
                    needs.OnWeekday(TimbnPackRules.Weekday(value, Text(value, "onDay")));
                    break;
                case "orderCompleted":
                    needs.OrderCompleted(Text(value, "orderCompleted"));
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }
    }

    private void Cost(TimbnCost cost, JToken token)
    {
        foreach (var property in Object(token, "cost").Properties())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "items":
                    Map(value, "items", (id, count) => cost.Item(id, Int(count, id)));
                    break;
                case "resources":
                    Map(value, "resources", (id, amount) => cost.Resource(id, Number(amount, id)));
                    break;
                case "money":
                    cost.Money(Int(value, "money"));
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }
    }

    private void Rewards(TimbnRewards rewards, JToken token)
    {
        foreach (var property in Object(token, "reward").Properties())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "money":
                    rewards.Money(Int(value, "money"));
                    break;
                case "items":
                    Map(value, "items", (id, count) => rewards.Item(id, Int(count, id)));
                    break;
                case "resources":
                    Map(value, "resources", (id, amount) => rewards.Resource(id, Number(amount, id)));
                    break;
                case "reputation":
                    rewards.Reputation(Int(value, "reputation"));
                    break;
                case "shows":
                    Map(value, "shows", (id, count) => rewards.ShowItem(id, Int(count, id)));
                    break;
                case "showsResources":
                    Map(value, "showsResources", (id, amount) => rewards.ShowResource(id, Number(amount, id)));
                    break;
                default:
                    Unknown(property);
                    break;
            }
        }
    }

    private string Full(string id) => $"{_packId}_{id}";

    private string QuestId(string id) => _questIds.Contains(id) ? Full(id) : id;

    private string EndId(string id) => _endIds.TryGetValue(id, out var end) ? end : QuestId(id);

    private string Icon(string name) => _sprites.Contains(name) ? Full(name) : name;

    private string ContinuedId(JToken token)
    {
        var id = Text(token, "continues");
        return _questIds.Contains(id)
            ? EndId(id)
            : throw TimbnPackException.At(token, $"\"continues\" needs a quest from this pack, and \"{id}\" is not one.");
    }

    private static string? LastStage(JObject quest)
    {
        if (quest["stages"] is not JArray { Count: > 0 } stages || stages[stages.Count - 1] is not JObject last || last["id"] is not JValue { Type: JTokenType.String } id)
            return null;

        var questIsFirst = quest.Properties().Any(property => _stepParts.Contains(property.Name));
        return stages.Count > 1 || questIsFirst ? (string)id! : null;
    }

    private void Try(Action build)
    {
        try
        {
            build();
        }
        catch (Exception ex)
        {
            Problem($"{_where}: {ex.Message} It was skipped.");
        }
    }

    private void Allowed(JObject step, params string[] fields)
    {
        foreach (var property in step.Properties())
        {
            if (!fields.Contains(property.Name))
                Unknown(property);
        }
    }

    private void Unknown(JProperty property) =>
        Problem($"{_where}: {TimbnPackException.LineOf(property)}\"{property.Name}\" doesn't belong here, so it was ignored.");

    private void Problem(string problem) => _content.Problems.Add(problem);

    private static JToken Required(JObject obj, string field) =>
        obj[field] ?? throw TimbnPackException.At(obj, $"needs \"{field}\".");

    private static JObject Object(JToken token, string field) =>
        token as JObject ?? throw TimbnPackException.At(token, $"\"{field}\" should be an object, such as {{ ... }}.");

    private static string Text(JToken token, string field) =>
        token is JValue { Type: JTokenType.String } value && (string)value! is { Length: > 0 } text
            ? text
            : throw TimbnPackException.At(token, $"\"{field}\" should be text in quotes.");

    private static string Npc(JToken token, string field) => TimbnPackRules.Npc(token, Text(token, field));

    private static IEnumerable<string> TextList(JToken token, string field) =>
        token is JArray list ? list.Select(entry => Text(entry, field)).ToList() : [Text(token, field)];

    private static int Int(JToken token, string field) => TimbnPackRules.Whole(token, field);

    private static float Number(JToken token, string field) => TimbnPackRules.Number(token, field);

    private static void Map(JToken token, string field, Action<string, JToken> each)
    {
        foreach (var property in Object(token, field).Properties())
            each(property.Name, property.Value);
    }
}

internal sealed class TimbnPackContent
{
    internal List<TimbnQuestBuilder> Quests { get; } = [];

    internal List<TimbnPackTalk> Talks { get; } = [];

    internal Dictionary<string, Func<bool>> ReadyWhen { get; } = [];

    internal List<string> Problems { get; } = [];
}

internal sealed class TimbnPackTalk
{
    internal TimbnPackTalk(string id, string npc, Func<bool> when, bool once, Color? bubbleColor, TimbnLines lines, Dictionary<string, string> texts)
    {
        Id = id;
        Npc = npc;
        When = when;
        Once = once;
        BubbleColor = bubbleColor;
        Lines = lines;
        Texts = texts;
    }

    internal string Id { get; }

    internal string Npc { get; }

    internal Func<bool> When { get; }

    internal bool Once { get; }

    internal Color? BubbleColor { get; }

    internal TimbnLines Lines { get; }

    internal Dictionary<string, string> Texts { get; }
}

internal sealed class TimbnPackInfo
{
    internal TimbnPackInfo(string id, string name, Color? bubbleColor)
    {
        Id = id;
        Name = name;
        BubbleColor = bubbleColor;
    }

    internal string Id { get; }

    internal string Name { get; }

    internal Color? BubbleColor { get; }
}
