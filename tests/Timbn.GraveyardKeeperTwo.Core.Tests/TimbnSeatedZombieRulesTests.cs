namespace Timbn.GraveyardKeeperTwo.Core.Tests;

public sealed class TimbnSeatedZombieRulesTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ADetachedZombieIsRememberedAsLockedOnlyWhenItWasNotInteractable(bool interactable, bool locked)
    {
        Assert.Equal(locked, TimbnSeatedZombieRules.WasLocked(interactable));
    }

    [Fact]
    public void AFreeLockedZombieWithNoStationIsStranded()
    {
        Assert.True(TimbnSeatedZombieRules.IsStranded(ZombieType.Free, hasStation: false, isInteractable: false));
    }

    [Fact]
    public void AnInteractableZombieIsNeverStranded()
    {
        Assert.False(TimbnSeatedZombieRules.IsStranded(ZombieType.Free, hasStation: false, isInteractable: true));
    }

    [Theory]
    [InlineData(ZombieType.Caretaker)]
    [InlineData(ZombieType.Porter)]
    [InlineData(ZombieType.Crafter)]
    public void AZombieThatWorksIsLockedOnPurpose(ZombieType type)
    {
        Assert.False(TimbnSeatedZombieRules.IsStranded(type, hasStation: true, isInteractable: false));
    }

    [Fact]
    public void AFreeZombieStillHeldByAStationIsNotStranded()
    {
        Assert.False(TimbnSeatedZombieRules.IsStranded(ZombieType.Free, hasStation: true, isInteractable: false));
    }
}
