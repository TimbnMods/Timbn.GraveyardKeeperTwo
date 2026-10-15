using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnLookTests
{
    private static readonly IReadOnlyCollection<TimbnFacing> _downAndLeft = [TimbnFacing.Down, TimbnFacing.Left];

    private static readonly IReadOnlyDictionary<TimbnFacing, TimbnFacing> _none = new Dictionary<TimbnFacing, TimbnFacing>();

    [Fact]
    public void HoldingNeedsAnIdAndAtLeastOne()
    {
        Assert.Throws<ArgumentException>(() => TimbnLook.HoldsItem(""));
        Assert.Throws<ArgumentException>(() => TimbnLook.HoldsGroup(" "));
        Assert.Throws<ArgumentException>(() => TimbnLook.Resource("", TimbnComparison.Equal, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimbnLook.HoldsItem("body_zombie", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TimbnLook.HoldsGroup("body", -1));
    }

    [Fact]
    public void ANotConditionIsTheOppositeAndNotNotIsTheSame()
    {
        var holds = TimbnLook.HoldsGroup("body");

        Assert.False(holds.IsInverted);
        Assert.True(holds.Not().IsInverted);
        Assert.False(holds.Not().Not().IsInverted);
        Assert.Equal("body", holds.Not().Id);
    }

    [Theory]
    [InlineData((int)TimbnLookKind.HoldsItem, ConditionalEventType.ItemsChanged)]
    [InlineData((int)TimbnLookKind.HoldsGroup, ConditionalEventType.ItemsChanged)]
    [InlineData((int)TimbnLookKind.Resource, ConditionalEventType.GameResChanged)]
    [InlineData((int)TimbnLookKind.Working, ConditionalEventType.WorkStateChanged)]
    [InlineData((int)TimbnLookKind.HasWorker, ConditionalEventType.WorkStateChanged)]
    [InlineData((int)TimbnLookKind.Crafting, ConditionalEventType.CraftStatusChanged)]
    public void EachConditionWatchesWhatItDependsOn(int kind, ConditionalEventType expected)
    {
        Assert.Equal(expected, TimbnLookRules.EventFor((TimbnLookKind)kind));
    }

    [Theory]
    [InlineData(TimbnComparison.Less, ComparisonOperator.Less)]
    [InlineData(TimbnComparison.LessOrEqual, ComparisonOperator.LessOrEqual)]
    [InlineData(TimbnComparison.Equal, ComparisonOperator.Equal)]
    [InlineData(TimbnComparison.GreaterOrEqual, ComparisonOperator.GreaterOrEqual)]
    [InlineData(TimbnComparison.Greater, ComparisonOperator.Greater)]
    public void EveryComparisonMapsToTheGamesOperator(TimbnComparison comparison, ComparisonOperator expected)
    {
        Assert.Equal(expected, TimbnLookRules.OperatorFor(comparison));
    }

    [Fact]
    public void AnExtraModelShowsWhenTheConditionIsTrueAndTheBaseTheOtherWay()
    {
        var holds = TimbnLook.HoldsGroup("body");

        Assert.False(TimbnLookRules.ShowsWhenFalse(holds, forBase: false));
        Assert.True(TimbnLookRules.ShowsWhenFalse(holds, forBase: true));
        Assert.True(TimbnLookRules.ShowsWhenFalse(holds.Not(), forBase: false));
        Assert.False(TimbnLookRules.ShowsWhenFalse(holds.Not(), forBase: true));
    }

    [Fact]
    public void HoldingAGroupBecomesTheGamesGroupCountRuleFromOneUp()
    {
        var rule = (ItemGroupCountCondition)TimbnConditionalLooks.ConditionFor(TimbnLook.HoldsGroup("body", 2));

        Assert.Equal("body", rule.itemGroupId);
        Assert.Equal(ComparisonOperator.GreaterOrEqual, rule.comparison);
        Assert.Equal(2, rule.targetCount);
    }

    [Fact]
    public void HoldingAnItemBecomesTheGamesItemCountRule()
    {
        var rule = (ItemCountCondition)TimbnConditionalLooks.ConditionFor(TimbnLook.HoldsItem("body_zombie"));

        Assert.Equal("body_zombie", rule.itemId);
        Assert.Equal(1, rule.targetCount);
    }

    [Fact]
    public void AResourceBecomesTheGamesGameResRule()
    {
        var rule = (GameResCondition)TimbnConditionalLooks.ConditionFor(TimbnLook.Resource("charge", TimbnComparison.Greater, 3));

        Assert.Equal("charge", rule.gameResId);
        Assert.Equal(ComparisonOperator.Greater, rule.comparison);
        Assert.Equal(3f, rule.targetValue);
    }

    [Fact]
    public void WorkingAndHavingAWorkerBecomeTheGamesWorkRules()
    {
        Assert.Equal(WorkConditionType.IsInWork, ((WorkCondition)TimbnConditionalLooks.ConditionFor(TimbnLook.IsWorking())).conditionType);
        Assert.Equal(WorkConditionType.HasWorker, ((WorkCondition)TimbnConditionalLooks.ConditionFor(TimbnLook.HasWorker())).conditionType);
    }

    [Fact]
    public void ARuleStartsAnExtraModelOffAndTheBaseModelOn()
    {
        var overlay = TimbnConditionalLooks.Rule(TimbnLook.HasWorker(), null!, forBase: false);
        var basis = TimbnConditionalLooks.Rule(TimbnLook.HasWorker(), null!, forBase: true);

        var overlayAction = (SetActiveAction)overlay.actions.Single();
        var baseAction = (SetActiveAction)basis.actions.Single();
        Assert.False(overlayAction.defaultActiveState);
        Assert.False(overlayAction.invertLogic);
        Assert.True(baseAction.defaultActiveState);
        Assert.True(baseAction.invertLogic);
    }

    [Fact]
    public void ModelsBuiltFromFacesLineUpByTheDifferenceOfTheirCenters()
    {
        var offset = TimbnLookRules.AlignmentOffset(new Vector2(20f, 12f), new Vector2(30f, 20f));

        Assert.Equal(-10f / 50f, offset.x, 5);
        Assert.Equal(0f, offset.y);
        Assert.Equal(-8f / 40f, offset.z, 5);
    }

    [Fact]
    public void AModelWithNoPixelCenterNeedsNoShift()
    {
        Assert.Equal(Vector3.zero, TimbnLookRules.AlignmentOffset(null, new Vector2(1f, 1f)));
        Assert.Equal(Vector3.zero, TimbnLookRules.AlignmentOffset(new Vector2(1f, 1f), null));
    }

    [Fact]
    public void ExtraModelsOnDrawnFacingsFit()
    {
        Assert.Null(TimbnLookRules.FindProblem([(TimbnFacing.Down, true), (TimbnFacing.Left, false)], _downAndLeft, _none));
        Assert.Null(TimbnLookRules.FindProblem([], _downAndLeft, _none));
    }

    [Fact]
    public void AnExtraModelForAFacingTheStationLacksIsRefused()
    {
        var problem = TimbnLookRules.FindProblem([(TimbnFacing.Up, false)], _downAndLeft, _none);

        Assert.Contains("Up", problem);
    }

    [Fact]
    public void AnExtraModelForAMirroredFacingIsSentToItsSource()
    {
        Dictionary<TimbnFacing, TimbnFacing> mirrored = new() { [TimbnFacing.Right] = TimbnFacing.Left };

        var problem = TimbnLookRules.FindProblem([(TimbnFacing.Right, false)], [TimbnFacing.Down, TimbnFacing.Left], mirrored);

        Assert.Contains("Left", problem);
        Assert.Contains("Right", problem);
    }

    [Fact]
    public void TwoModelsReplacingTheSameBaseAreRefused()
    {
        var problem = TimbnLookRules.FindProblem([(TimbnFacing.Down, true), (TimbnFacing.Down, true)], _downAndLeft, _none);

        Assert.Contains("Down", problem);
        Assert.Null(TimbnLookRules.FindProblem([(TimbnFacing.Down, true), (TimbnFacing.Left, true)], _downAndLeft, _none));
    }

    [Fact]
    public void AnExtraModelNeedsAConditionAndAModel()
    {
        TimbnStationBuilder station = new("timbn_slab", "", "");

        Assert.Throws<ArgumentNullException>(() => station.ShowWhen(TimbnLook.HasWorker(), null!));
        Assert.Throws<ArgumentNullException>(() => station.ShowWhen(null!, null!));
        Assert.Empty(station.Overlays);
    }
}
