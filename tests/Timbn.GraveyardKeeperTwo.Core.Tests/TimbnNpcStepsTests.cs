using System.Globalization;
using UnityEngine;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnNpcStepsTests
{
    [Fact]
    public void StepsAreCountedAndLabeledInOrder()
    {
        var steps = new TimbnNpcSteps()
            .Walk("npc_sim_village_right_side_03")
            .WalkNavmesh("RT_herm_spawn")
            .WaitGameMinutes(45)
            .Face(Direction.Down)
            .Animate("adjusting_jabbot")
            .Teleport("RT_herm_spawn")
            .FireEvent("go_home")
            .RunCode(_ => { })
            .Decide(_ => null)
            .ChainTrips((_, _) => null)
            .GoToFreePoint(["a", "b", "c"])
            .GoToFreePoint("tavern_seats", "seat_");

        Assert.Equal(12, steps.Count);
        Assert.Equal(
            [
                "walk to npc_sim_village_right_side_03",
                "navmesh walk to RT_herm_spawn",
                "wait 45 minutes",
                "face Down",
                "animate adjusting_jabbot",
                "teleport to RT_herm_spawn",
                "event go_home",
                "run code",
                "decide",
                "pick a trip",
                "go to a free point of 3",
                "go to a free point in tavern_seats",
            ],
            steps.Steps.Select(step => step.Text));
    }

    [Fact]
    public void ARangedWaitSaysBothEnds()
    {
        var steps = new TimbnNpcSteps().WaitGameMinutes(10, 30).WaitGameMinutes(20, 20);

        Assert.Equal(["wait 10 to 30 minutes", "wait 20 minutes"], steps.Steps.Select(step => step.Text));
    }

    [Fact]
    public void ANavmeshSpotIsLabeledWithItsCoordinates()
    {
        var steps = new TimbnNpcSteps().WalkNavmesh(1.5f, -2f);

        Assert.Equal($"navmesh walk to ({1.5f.ToString("0.00", CultureInfo.CurrentCulture)}, {(-2f).ToString("0.00", CultureInfo.CurrentCulture)})", steps.Steps[0].Text);
    }

    [Fact]
    public void AFacedPointIsLabeledByXAndZ()
    {
        var steps = new TimbnNpcSteps().Face(new Vector3(1f, 99f, 2f));

        Assert.Equal($"face ({1f.ToString("0.00", CultureInfo.CurrentCulture)}, {2f.ToString("0.00", CultureInfo.CurrentCulture)})", steps.Steps[0].Text);
    }

    [Fact]
    public void ARealSecondsWaitIsAWaitOnTheRealClock()
    {
        var steps = new TimbnNpcSteps().WaitRealSeconds(2.5f);

        Assert.Equal("wait 2.5 real seconds", steps.Steps[0].Text.Replace(',', '.'));
        var result = steps.Steps[0].Run(null!);
        Assert.Equal(TimbnStepKind.WaitReal, result.Kind);
        Assert.Equal(2.5, result.Minutes, 0.0001);
    }

    [Fact]
    public void ANewStepListIsEmpty() =>
        Assert.Equal(0, new TimbnNpcSteps().Count);

    [Fact]
    public void ADayCollectsItsBlocksInOrder()
    {
        var day = new TimbnNpcDay()
            .Block("working", 0.25f, block => block.Origin("RT_herm_spawn").Intro(steps => steps.FireEvent("go_to_spawn")).Loop(steps => steps.WaitGameMinutes(10, 30)))
            .Block("night", 0.85f, block => block.LateIntro(steps => steps.Teleport("RT_herm_home")));

        Assert.Equal(["working", "night"], day.Blocks.Select(block => block.Name));
        Assert.Equal([0.25f, 0.85f], day.Blocks.Select(block => block.Start));
        Assert.Equal("RT_herm_spawn", day.Blocks[0].OriginPoint);
        Assert.Null(day.Blocks[1].OriginPoint);
        Assert.Equal(1, day.Blocks[0].IntroSteps.Count);
        Assert.Equal(0, day.Blocks[0].LateSteps.Count);
        Assert.Equal(1, day.Blocks[0].LoopSteps.Count);
        Assert.Equal(0, day.Blocks[1].IntroSteps.Count);
        Assert.Equal(1, day.Blocks[1].LateSteps.Count);
    }

    [Fact]
    public void ADayStartsWithNoRulesSet()
    {
        var day = new TimbnNpcDay();

        Assert.Empty(day.Blocks);
        Assert.Null(day.Condition);
        Assert.Null(day.StartPoint);
        Assert.Null(day.ResumePoint);
        Assert.Empty(day.Doorways);
        Assert.False(day.Logged);
    }

    [Fact]
    public void ADayKeepsItsRules()
    {
        Func<bool> condition = () => true;
        var day = new TimbnNpcDay()
            .OnlyWhile(condition)
            .StartWhenAt("RT_larry_spawn")
            .ResumeWhenAt("RT_larry_home")
            .Doorway("RT_tavern_owner_spawn", "village_letter_owner_spawn", TimbnWalk.Straight)
            .Doorway("RT_larry_home", "larry_door")
            .LogSteps();

        Assert.Same(condition, day.Condition);
        Assert.Equal("RT_larry_spawn", day.StartPoint);
        Assert.Equal("RT_larry_home", day.ResumePoint);
        Assert.True(day.Logged);
        Assert.Collection(
            day.Doorways,
            doorway =>
            {
                Assert.Equal("RT_tavern_owner_spawn", doorway.Point);
                Assert.Equal("village_letter_owner_spawn", doorway.Via);
                Assert.Equal(TimbnWalk.Straight, doorway.Walk);
            },
            doorway =>
            {
                Assert.Equal("RT_larry_home", doorway.Point);
                Assert.Equal("larry_door", doorway.Via);
                Assert.Equal(TimbnWalk.Navmesh, doorway.Walk);
            });
    }

    [Fact]
    public void ATripHasThreePartsThatStartEmpty()
    {
        var trip = new TimbnNpcTrip();

        Assert.Equal(0, trip.LeaveSteps.Count);
        Assert.Equal(0, trip.BodySteps.Count);
        Assert.Equal(0, trip.ReturnSteps.Count);
    }

    [Fact]
    public void ATripKeepsEachPartSeparately()
    {
        var trip = new TimbnNpcTrip()
            .Leave(steps => steps.WalkNavmesh("road"))
            .Body(steps => steps.Walk("bench").WaitGameMinutes(45))
            .Return(steps => steps.Walk("road").WalkNavmesh("RT_herm_spawn"));

        Assert.Equal(["navmesh walk to road"], trip.LeaveSteps.Steps.Select(step => step.Text));
        Assert.Equal(["walk to bench", "wait 45 minutes"], trip.BodySteps.Steps.Select(step => step.Text));
        Assert.Equal(["walk to road", "navmesh walk to RT_herm_spawn"], trip.ReturnSteps.Steps.Select(step => step.Text));
    }
}
