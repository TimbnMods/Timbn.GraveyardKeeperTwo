namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Turns a station's look conditions into the rules of the game's conditional drawer, the component the game's own
/// autopsy table uses to show its cloth when it holds a body. The rules are built in code and the prefab is cloned with
/// them, the way the game's prefabs carry theirs.
/// </summary>
internal static class TimbnConditionalLooks
{
    internal static IConditionalDrawerCondition ConditionFor(TimbnLookCondition condition) => condition.Kind switch
    {
        TimbnLookKind.HoldsItem => new ItemCountCondition
        {
            itemId = condition.Id,
            comparison = TimbnLookRules.OperatorFor(condition.Comparison),
            targetCount = condition.Value,
        },
        TimbnLookKind.HoldsGroup => new ItemGroupCountCondition
        {
            itemGroupId = condition.Id,
            comparison = TimbnLookRules.OperatorFor(condition.Comparison),
            targetCount = condition.Value,
        },
        TimbnLookKind.Resource => new GameResCondition
        {
            gameResId = condition.Id,
            comparison = TimbnLookRules.OperatorFor(condition.Comparison),
            targetValue = condition.Value,
        },
        TimbnLookKind.Working => new WorkCondition { conditionType = WorkConditionType.IsInWork },
        TimbnLookKind.Crafting => new CraftStatusCondition { expectedStatus = ExpectedCraftStatus.Started },
        _ => new WorkCondition { conditionType = WorkConditionType.HasWorker },
    };

    // An extra model starts switched off and the base model starts on, which is also what the drawer puts back when it resets.
    internal static ConditionalDrawerRule Rule(TimbnLookCondition condition, GameObject target, bool forBase)
    {
        var rule = new ConditionalDrawerRule { condition = ConditionFor(condition) };
        rule.actions.Add(new SetActiveAction
        {
            target = target,
            invertLogic = TimbnLookRules.ShowsWhenFalse(condition, forBase),
            defaultActiveState = forBase,
        });
        return rule;
    }
}
