using Timbn.GraveyardKeeperTwo.Core.Framework;

namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSliderStepsTests
{
    [Fact]
    public void WholeNumbersStepByOne() => Assert.Equal(0, TimbnSliderSteps.Decimals(true, 1f, 18f));

    [Fact]
    public void OneDecimalInTheRangeStepsByTenths() => Assert.Equal(1, TimbnSliderSteps.Decimals(false, 3.2f, 5f));

    [Fact]
    public void ADecimalDefaultSetsTheStep() => Assert.Equal(2, TimbnSliderSteps.Decimals(false, 0f, 1f, 0.25f));

    [Fact]
    public void AWholeFloatRangeWithAWholeDefaultStepsByOne() => Assert.Equal(0, TimbnSliderSteps.Decimals(false, 1f, 50f, 7f));

    [Fact]
    public void ATightRangeGetsFinerSteps() => Assert.Equal(1, TimbnSliderSteps.Decimals(false, 0f, 1f));

    [Fact]
    public void StepIsOneInTheLastPlace() => Assert.Equal(0.01f, TimbnSliderSteps.Step(2));
}
