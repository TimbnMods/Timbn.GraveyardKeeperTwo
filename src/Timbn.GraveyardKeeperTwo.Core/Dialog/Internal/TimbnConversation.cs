namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Plays one conversation with an NPC as a list of steps, using the game's own speech bubbles and answer choices.
/// A recorded talk or quest script fills a fresh one each time it plays. Each step waits for the one before it, and
/// the conversation ends by itself when the last step finishes. Lines and answers here are localization keys.
/// </summary>
internal sealed class TimbnConversation
{
    private readonly List<Action<Action, Action>> _steps = [];
    private readonly string _talkId;
    private readonly TimbnConversation _root;
    private Dictionary<string, TimbnConversation>? _branches;
    private List<Action>? _ended;

    internal TimbnConversation(WgoData npc, string talkId)
    {
        Npc = npc;
        _talkId = talkId;
        _root = this;
    }

    private TimbnConversation(TimbnConversation parent)
    {
        Npc = parent.Npc;
        _talkId = parent._talkId;
        _root = parent._root;
    }

    internal WgoData Npc { get; }

    internal bool TurnedOnBars { get; private set; }

    internal bool OpensMenu { get; private set; }

    internal void ThenOpenMenu() => _root.OpensMenu = true;

    internal void OnEnded(Action action) => (_root._ended ??= []).Add(action);

    internal void RunEnded()
    {
        if (_ended is null)
            return;

        foreach (var action in _ended)
            TimbnSafe.Run(action, $"{nameof(TimbnConversation)}|The end of talk '{_talkId}'");
    }

    internal TimbnConversation Say(string key, object[] values) =>
        Add((next, end) =>
        {
            if (TimbnDialog.Say(Npc, key, values, next))
                return;

            end();
        });

    internal TimbnConversation SayAs(string npcId, string key) =>
        Add((next, end) =>
        {
            if (TimbnNpcs.FindData(npcId) is not { } speaker)
            {
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnConversation)}|Talk '{_talkId}' skipped the line '{key}', since {npcId} is not in the world.");
                next();
                return;
            }

            if (TimbnDialog.Say(speaker, key, [], next))
                return;

            end();
        });

    internal TimbnConversation PlayerSay(string key, object[] values) =>
        Add((next, end) =>
        {
            if (TimbnDialog.PlayerSay(key, values, next))
                return;

            end();
        });

    internal TimbnConversation Wait(float seconds) =>
        Add((next, end) =>
        {
            if (!TimbnDialog.Wait(seconds, next))
                end();
        });

    internal TimbnConversation Cinematic() =>
        Add((next, end) =>
        {
            if (_root.TurnedOnBars || !TimbnStoryScene.TryShowBars(next))
            {
                next();
                return;
            }

            _root.TurnedOnBars = true;
        });

    internal TimbnConversation Do(Action action) =>
        Add((next, end) =>
        {
            if (TimbnSafe.Run(action, $"{nameof(TimbnConversation)}|A step in talk '{_talkId}'"))
                next();
            else
                end();
        });

    internal TimbnConversation Give(string itemId, int count) =>
        Do(() =>
        {
            var moved = TimbnItems.GiveToPlayer(new Item(itemId, count));
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnConversation)}|Talk '{_talkId}' gave the player {count} {itemId}, {moved} into their inventory.");
        });

    internal TimbnConversation Take(string itemId, int count) =>
        Do(() =>
        {
            var taken = TimbnItems.TakeFromPlayer(itemId, count);
            TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnConversation)}|Talk '{_talkId}' took {taken} of {count} {itemId} from the player.");
        });

    internal TimbnConversation When(Func<bool> condition, Action<TimbnConversation> then, Action<TimbnConversation>? otherwise = null)
    {
        var yes = Branch(then);
        var no = otherwise is null ? null : Branch(otherwise);
        return Add((next, end) =>
        {
            var chosen = false;
            if (!TimbnSafe.Run(() => chosen = condition(), $"{nameof(TimbnConversation)}|A condition in talk '{_talkId}'"))
            {
                end();
                return;
            }

            var branch = chosen ? yes : no;
            if (branch is null)
                next();
            else
                branch.RunFrom(0, next, end);
        });
    }

    internal TimbnConversation Ask(params TimbnAnswer[] answers)
    {
        if (answers.Length == 0)
            throw new ArgumentException("Ask needs at least one answer.", nameof(answers));

        var keys = answers.Select(answer => answer.Key).ToArray();
        Dictionary<string, AnswerData> extras = [];
        foreach (var answer in answers)
        {
            if (answer.ToData() is { } data)
                extras[answer.Key] = data;
        }

        Dictionary<string, TimbnConversation> branches = [];
        Add((next, end) =>
        {
            var asked = TimbnDialog.Ask(
                Npc,
                keys,
                chosen =>
                {
                    Log($"answer '{chosen}' picked.");
                    if (branches.TryGetValue(chosen, out var branch))
                        branch.RunFrom(0, next, end);
                    else
                        next();
                },
                () =>
                {
                    Log("left at the answers.");
                    end();
                },
                extras);
            if (!asked)
                end();
        });
        _branches = branches;
        return this;
    }

    internal TimbnConversation If(string answer, Action<TimbnConversation> then)
    {
        if (_branches is null)
            throw new InvalidOperationException("If has to follow Ask.");

        _branches[answer] = Branch(then);
        return this;
    }

    internal void Run(Action done) => RunFrom(0, done, done);

    private void Log(string message) =>
        TimbnCorePlugin.Logger.LogInfo($"{nameof(TimbnConversation)}|Talk '{_talkId}' {message}");

    private TimbnConversation Add(Action<Action, Action> step)
    {
        _branches = null;
        _steps.Add(step);
        return this;
    }

    private TimbnConversation Branch(Action<TimbnConversation> script)
    {
        var branch = new TimbnConversation(this);
        script(branch);
        return branch;
    }

    private void RunFrom(int index, Action next, Action end)
    {
        if (index >= _steps.Count)
        {
            next();
            return;
        }

        _steps[index](() => RunFrom(index + 1, next, end), end);
    }
}
