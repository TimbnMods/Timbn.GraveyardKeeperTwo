namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Holds a walk speed multiplier for plugins and applies it on top of the game's own speed each move.</summary>
internal static class TimbnMovement
{
    private static readonly TimbnHolds<float> _speedHolds = new(multiplier => _speed = multiplier, () => _speed = 1f);
    private static float _speed = 1f;

    internal static IDisposable SetSpeed(float multiplier) =>
        TimbnGame.IsInGame ? _speedHolds.Take(multiplier) : TimbnUndo.None;

    internal static float? SpeedUp(PlayerPhysicalBody body)
    {
        if (_speed == 1f)
            return null;

        var gameSpeed = body.SpeedMultiplier;
        body.SpeedMultiplier = gameSpeed * _speed;
        return gameSpeed;
    }

    internal static void ReleaseAll() => _speedHolds.ReleaseAll();
}
