namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// One stage of a quest added with <see cref="TimbnQuestBuilder.AddStage"/>, with its own goal, talk and rewards. Core
/// makes it a game quest of its own that continues the stage before.
/// </summary>
public sealed class TimbnQuestStage
{
    internal TimbnQuestStage(TimbnQuestBuilder step)
    {
        Step = step;
    }

    internal TimbnQuestBuilder Step { get; }

    /// <summary>The stage's name in the quest tree and log. Left out, it uses the quest's name.</summary>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Name(string name) => With(() => Step.SetName(name));

    /// <inheritdoc cref="TimbnQuestBuilder.Icon(string)"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Icon(string iconId) => With(() => Step.Icon(iconId));

    /// <inheritdoc cref="TimbnQuestBuilder.Icon(Func{TimbnQuestRun, string})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Icon(Func<TimbnQuestRun, string> iconId) => With(() => Step.Icon(iconId));

    /// <inheritdoc cref="TimbnQuestBuilder.Phase"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Phase(int phase) => With(() => Step.Phase(phase));

    /// <summary>The quest description once this stage is done. Left out, it keeps the stage's description.</summary>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage DoneDescription(string text) => With(() => Step.DoneDescription(text));

    /// <inheritdoc cref="TimbnQuestBuilder.TalksWith(string)"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage TalksWith(string npcId) => With(() => Step.TalksWith(npcId));

    /// <inheritdoc cref="TimbnQuestBuilder.TalksWith(Func{TimbnQuestRun, string})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage TalksWith(Func<TimbnQuestRun, string> npcId) => With(() => Step.TalksWith(npcId));

    /// <inheritdoc cref="TimbnQuestBuilder.Needs(Action{TimbnNeeds})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Needs(Action<TimbnNeeds> needs) => With(() => Step.Needs(needs));

    /// <inheritdoc cref="TimbnQuestBuilder.Needs(Action{TimbnNeeds, TimbnQuestRun})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Needs(Action<TimbnNeeds, TimbnQuestRun> needs) => With(() => Step.Needs(needs));

    /// <inheritdoc cref="TimbnQuestBuilder.Cost(Action{TimbnCost})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Cost(Action<TimbnCost> cost) => With(() => Step.Cost(cost));

    /// <inheritdoc cref="TimbnQuestBuilder.Cost(Action{TimbnCost, TimbnQuestRun})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Cost(Action<TimbnCost, TimbnQuestRun> cost) => With(() => Step.Cost(cost));

    /// <inheritdoc cref="TimbnQuestBuilder.Talk"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Talk(Action<TimbnQuestTalk> talk) => With(() => Step.Talk(talk));

    /// <inheritdoc cref="TimbnQuestBuilder.Reward(Action{TimbnRewards})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Reward(Action<TimbnRewards> rewards) => With(() => Step.Reward(rewards));

    /// <inheritdoc cref="TimbnQuestBuilder.Reward(Action{TimbnRewards, TimbnQuestRun})"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage Reward(Action<TimbnRewards, TimbnQuestRun> rewards) => With(() => Step.Reward(rewards));

    /// <inheritdoc cref="TimbnQuestBuilder.ReadyByCode"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage ReadyByCode() => With(() => Step.ReadyByCode());

    /// <inheritdoc cref="TimbnQuestBuilder.ReadyWhen"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage ReadyWhen(Func<bool> condition) => With(() => Step.ReadyWhen(condition));

    /// <inheritdoc cref="TimbnQuestBuilder.ReadyOn"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage ReadyOn(TimbnQuestEvent gameEvent) => With(() => Step.ReadyOn(gameEvent));

    /// <inheritdoc cref="TimbnQuestBuilder.DoneLines"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage DoneLines(Action<TimbnLines> lines) => With(() => Step.DoneLines(lines));

    /// <inheritdoc cref="TimbnQuestBuilder.OnDone"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage OnDone(Action action) => With(() => Step.OnDone(action));

    /// <inheritdoc cref="TimbnQuestBuilder.OnStart"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage OnStart(Action action) => With(() => Step.OnStart(action));

    /// <inheritdoc cref="TimbnQuestBuilder.OnEnd"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage OnEnd(Action action) => With(() => Step.OnEnd(action));

    /// <inheritdoc cref="TimbnQuestBuilder.OnStartExpression"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage OnStartExpression(string expression) => With(() => Step.OnStartExpression(expression));

    /// <inheritdoc cref="TimbnQuestBuilder.OnDoneExpression"/>
    /// <returns>This stage, for chaining.</returns>
    public TimbnQuestStage OnDoneExpression(string expression) => With(() => Step.OnDoneExpression(expression));

    private TimbnQuestStage With(Action change)
    {
        change();
        return this;
    }
}
