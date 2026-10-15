namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnRoutineTests
{
    private readonly List<string> _log = [];
    private readonly TimbnRoutine<List<string>> _routine = new();
    private Action? _arrive;
    private double _now;

    private static TimbnRoutineStep<List<string>> Say(string text) =>
        new(text, log =>
        {
            log.Add(text);
            return TimbnStepResult<List<string>>.Next;
        });

    private static TimbnRoutineStep<List<string>> Wait(double minutes) =>
        new("wait", log =>
        {
            log.Add($"wait {minutes}");
            return TimbnStepResult<List<string>>.Wait(minutes);
        });

    private TimbnRoutineStep<List<string>> Walk(string to, bool starts = true) =>
        new("walk", log =>
        {
            log.Add($"walk {to}");
            return TimbnStepResult<List<string>>.WalkThen(arrived =>
            {
                _arrive = arrived;
                return starts;
            });
        });

    private void Tick(int times = 1, bool walking = false, bool held = false)
    {
        for (var i = 0; i < times; i++)
            _routine.Tick(_log, _now, walking, held);
    }

    [Fact]
    public void RunsTheIntroOnceThenRepeatsTheLoop()
    {
        _routine.Start([Say("intro")], [Say("a"), Say("b")]);

        Tick(5);

        Assert.Equal(["intro", "a", "b", "a", "b"], _log);
    }

    [Fact]
    public void WithoutALoopItStopsAfterTheIntro()
    {
        _routine.Start([Say("intro")], []);

        Tick(3);

        Assert.Equal(["intro"], _log);
        Assert.False(_routine.IsActive);
    }

    [Fact]
    public void WaitsFollowTheGameClock()
    {
        _routine.Start([Wait(10), Say("done")], []);

        Tick();
        _now = 9;
        Tick();
        Assert.Equal(1, _routine.WaitLeft(_now), 3);
        Assert.Equal(["wait 10"], _log);

        _now = 10;
        Tick();
        Assert.Equal(["wait 10", "done"], _log);
    }

    [Fact]
    public void AWalkWaitsForArrival()
    {
        _routine.Start([Walk("bench"), Say("there")], []);

        Tick();
        Tick(3, walking: true);
        Assert.True(_routine.IsWaitingForArrival);

        _arrive!();
        Tick();
        Assert.Equal(["walk bench", "there"], _log);
    }

    [Fact]
    public void AWalkThatDoesNotStartIsSkipped()
    {
        _routine.Start([Walk("nowhere", starts: false), Say("next")], []);

        Tick(2);

        Assert.Equal(["walk nowhere", "next"], _log);
    }

    [Fact]
    public void AWalkThatStandsStillTooLongIsGivenUp()
    {
        _routine.Start([Walk("bench"), Say("next")], []);
        Tick();

        _now = TimbnRoutine<List<string>>.GiveUpMinutes;
        Tick();
        Assert.Equal(["walk bench"], _log);

        _now += 1;
        Tick();
        Assert.Equal(["walk bench", "next"], _log);
    }

    [Fact]
    public void AnOldArrivalIsIgnoredAfterARestart()
    {
        _routine.Start([Walk("bench")], []);
        Tick();
        var old = _arrive!;

        _routine.Start([Walk("pond"), Say("there")], []);
        Tick();
        old();
        Tick();

        Assert.Equal(["walk bench", "walk pond"], _log);
    }

    [Fact]
    public void NothingRunsWhileHeld()
    {
        _routine.Start([Say("a")], []);

        Tick(3, held: true);
        Assert.Empty(_log);

        Tick();
        Assert.Equal(["a"], _log);
    }

    [Fact]
    public void HoldingDoesNotGiveUpAWalk()
    {
        _routine.Start([Walk("bench"), Say("next")], []);
        Tick();

        _now = 100;
        Tick(held: true);
        _now = 110;
        Tick();

        Assert.Equal(["walk bench"], _log);
    }

    [Fact]
    public void InsertedStepsRunBeforeTheRest()
    {
        var trip = new TimbnRoutineStep<List<string>>("trip", log =>
        {
            log.Add("roll");
            return TimbnStepResult<List<string>>.Insert([Say("x"), Say("y")]);
        });
        _routine.Start([], [trip, Say("after")]);

        Tick(4);

        Assert.Equal(["roll", "x", "y", "after"], _log);
        Assert.Equal(2, _routine.InsertedTotal);
        Assert.Equal(0, _routine.InsertedLeft);
    }

    [Fact]
    public void InTripLastsUntilTheLastInsertedStepIsDone()
    {
        var trip = new TimbnRoutineStep<List<string>>("trip", _ => TimbnStepResult<List<string>>.Insert([Say("x"), Walk("stall")]));
        _routine.Start([], [trip, Wait(10)]);

        Tick();
        Assert.True(_routine.InTrip);

        Tick(2);
        Assert.True(_routine.InTrip);

        _arrive!();
        Tick();
        Assert.False(_routine.InTrip);
        Assert.Equal(["x", "walk stall", "wait 10"], _log);
    }

    [Fact]
    public void TripEndedRunsOnceWhenTheInsertedStepsAreDone()
    {
        var ended = 0;
        _routine.TripEnded = () => ended++;
        var trip = new TimbnRoutineStep<List<string>>("trip", _ => TimbnStepResult<List<string>>.Insert([Say("x"), Say("y")]));
        _routine.Start([], [trip, Say("after")]);

        Tick(3);
        Assert.Equal(0, ended);

        Tick();
        Assert.Equal(1, ended);
        Assert.Equal(["x", "y", "after"], _log);
    }

    [Fact]
    public void StepStartingRunsForOwnStepsOnlyAndNotForInsertedOnes()
    {
        var starts = 0;
        _routine.StepStarting = () => starts++;
        var trip = new TimbnRoutineStep<List<string>>("trip", _ => TimbnStepResult<List<string>>.Insert([Say("x"), Say("y")]));
        _routine.Start([], [trip, Say("after")]);

        Tick(4);

        Assert.Equal(["x", "y", "after"], _log);
        Assert.Equal(2, starts);
    }

    [Fact]
    public void ChainedInsertsStillEndOneTrip()
    {
        var ended = 0;
        _routine.TripEnded = () => ended++;
        var chain = new TimbnRoutineStep<List<string>>("chain", _ => TimbnStepResult<List<string>>.Insert([Say("z")]));
        var trip = new TimbnRoutineStep<List<string>>("trip", _ => TimbnStepResult<List<string>>.Insert([Say("x"), chain]));
        _routine.Start([], [trip, Say("after")]);

        Tick(4);
        Assert.Equal(0, ended);

        Tick();
        Assert.Equal(1, ended);
    }

    [Fact]
    public void UntilIdleWaitsForTheWalkToEndOrTheLimit()
    {
        var idle = new TimbnRoutineStep<List<string>>("event", log =>
        {
            log.Add("event");
            return TimbnStepResult<List<string>>.UntilIdle(60);
        });
        _routine.Start([idle, Say("home")], []);

        Tick();
        Tick(walking: false);
        Assert.Equal(["event"], _log);

        _now = 1;
        Tick(walking: true);
        Assert.Equal(["event"], _log);

        Tick(walking: false);
        Assert.Equal(["event", "home"], _log);
    }

    [Fact]
    public void UntilIdleWaitsAFewTicksForAWalkThatStartsLate()
    {
        var idle = new TimbnRoutineStep<List<string>>("event", _ => TimbnStepResult<List<string>>.UntilIdle(60));
        _routine.Start([idle, Say("home")], []);

        Tick();
        _now = 5;
        Tick(walking: false);
        Tick(walking: false);
        Assert.Empty(_log);

        Tick(walking: true);
        Tick(walking: false);
        Assert.Equal(["home"], _log);
    }

    [Fact]
    public void WaitRealFollowsTheRealClockNotTheGameClock()
    {
        double real = 0;
        _routine.RealClock = () => real;
        var wait = new TimbnRoutineStep<List<string>>("wait", _ => TimbnStepResult<List<string>>.WaitReal(3));
        _routine.Start([wait, Say("after")], []);

        Tick();
        _now = 500;
        Tick();
        Assert.Empty(_log);

        real = 3;
        Tick(2);
        Assert.Equal(["after"], _log);
    }

    [Fact]
    public void UntilIdleGivesUpAtItsLimit()
    {
        var idle = new TimbnRoutineStep<List<string>>("event", _ => TimbnStepResult<List<string>>.UntilIdle(60));
        _routine.Start([idle, Say("home")], []);

        Tick();
        _now = 61;
        Tick(walking: true);

        Assert.Equal(["home"], _log);
    }

    [Fact]
    public void AWaitKeepsItsLengthWhenTheClockIsSetBack()
    {
        _now = 1000;
        _routine.Start([Wait(30), Say("after")], []);
        Tick();

        _now = 100;
        Tick();
        _now = 129;
        Tick();
        Assert.DoesNotContain("after", _log);

        _now = 131;
        Tick();
        Assert.Contains("after", _log);
    }

    [Fact]
    public void StopEndsTheRoutineAndForgetsItsStep()
    {
        _routine.Start([Say("intro")], [Say("a")]);
        Tick();
        Assert.True(_routine.IsActive);
        Assert.Equal("intro", _routine.Step);

        _routine.Stop();
        Tick(3);

        Assert.False(_routine.IsActive);
        Assert.Equal("", _routine.Step);
        Assert.Equal(["intro"], _log);
    }

    [Fact]
    public void StartingAgainDropsTheOldStepsAndInserts()
    {
        _routine.Start([], [Say("old")]);
        Tick();

        _routine.Start([Say("new")], []);
        Tick(2);

        Assert.Equal(["old", "new"], _log);
        Assert.False(_routine.IsActive);
    }

    [Fact]
    public void AStepThatThrowsIsReportedAndSkipped()
    {
        List<string> failures = [];
        _routine.Failed = failures.Add;
        var broken = new TimbnRoutineStep<List<string>>("broken", _ => throw new InvalidOperationException("no path"));
        _routine.Start([Say("a"), broken, Say("b")], []);

        Tick(3);

        Assert.Equal(["a", "b"], _log);
        var failure = Assert.Single(failures);
        Assert.StartsWith("step 'broken' threw and was skipped: ", failure);
        Assert.Contains("no path", failure);
    }

    [Fact]
    public void StepNamesTheStepBeingRun()
    {
        _routine.Start([Say("intro"), Wait(5)], []);

        Assert.Equal("", _routine.Step);
        Tick();
        Assert.Equal("intro", _routine.Step);
        Tick();
        Assert.Equal("wait", _routine.Step);
    }

    [Fact]
    public void WaitLeftCountsDownOnlyForAGameMinutesWait()
    {
        _routine.Start([Wait(10)], []);

        Assert.Equal(0, _routine.WaitLeft(_now));
        Tick();
        Assert.Equal(10, _routine.WaitLeft(_now));
        _now = 4;
        Assert.Equal(6, _routine.WaitLeft(_now));
        _now = 20;
        Assert.Equal(0, _routine.WaitLeft(_now));
    }

    [Fact]
    public void IsWaitingForIdleOnlyDuringAnUntilIdleStep()
    {
        var idle = new TimbnRoutineStep<List<string>>("idle", _ => TimbnStepResult<List<string>>.UntilIdle(10));
        _routine.Start([idle, Say("after")], []);

        Assert.False(_routine.IsWaitingForIdle);
        Tick();
        Assert.True(_routine.IsWaitingForIdle);
        Assert.False(_routine.IsWaitingForArrival);
        _now = 1;
        Tick(12);
        Assert.False(_routine.IsWaitingForIdle);
        Assert.Contains("after", _log);
    }

    [Fact]
    public void AnEmptyStartIsNotActive()
    {
        _routine.Start([], []);

        Assert.False(_routine.IsActive);
        Tick();
        Assert.Empty(_log);
    }
}
