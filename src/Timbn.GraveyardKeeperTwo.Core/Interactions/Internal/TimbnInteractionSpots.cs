using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The spots plugins have made in the world. Each frame the one in the player's reach shows the game's hint over it and
/// takes the two interaction keys, the way the game does for its own objects. Reach is the game's own interaction box,
/// the one held in front of the keeper, so the player faces a spot to use it the same as a chest.
/// </summary>
internal static class TimbnInteractionSpots
{
    // The game keeps its box this size, 0.4 by 0.5 in front of the keeper, should its private field ever be renamed.
    private static readonly Vector3 _fallbackExtents = new(0.2f, 0.5f, 0.25f);

    // A spot more than this far above or below the keeper's reach is on another floor.
    private const float _floorGap = 2f;

    private static readonly List<TimbnInteractionSpot> _spots = [];
    private static readonly AccessTools.FieldRef<PlayerInteractionComponent, BoxCollider>? _box =
        TimbnReflection.TryField<PlayerInteractionComponent, BoxCollider>("interactionCollider");

    private static TimbnInteractionHint? _hint;

    // The last frame no spot could be used, such as while a window was open.
    private static int _unavailableFrame = -1;

    internal static IDisposable Add(TimbnInteractionSpot spot)
    {
        _spots.Add(spot);
        return spot;
    }

    internal static void Remove(TimbnInteractionSpot spot) => _spots.Remove(spot);

    internal static void Tick()
    {
        var (spot, primary, secondary) = _spots.Count == 0 ? default : Reachable();
        if (spot == null)
        {
            _unavailableFrame = Time.frameCount;
            _hint?.Hide();
            return;
        }

        _hint ??= new TimbnInteractionHint();
        var over = spot.Position + Vector3.up * spot.HintHeight;
        if (primary != null && secondary != null)
            _hint.Show(over, primary, GameKey.Interaction, secondary, GameKey.Action);
        else if (primary != null)
            _hint.Show(over, primary, GameKey.Interaction);
        else
            _hint.Show(over, secondary!, GameKey.Action);

        // A key that closed a window this frame, such as a gamepad's A picking an item, is still down and is not a press here.
        if (Time.frameCount - _unavailableFrame <= 1)
            return;

        if (primary != null && GameKey.Interaction.IsDownInGame())
            TimbnSafe.Run(spot.Primary!.Run, spot.Logger, $"{nameof(TimbnPluginInteractions)}|{TimbnSafe.Describe(spot.Primary.Run)}");
        else if (secondary != null && GameKey.Action.IsDownInGame())
            TimbnSafe.Run(spot.Secondary!.Run, spot.Logger, $"{nameof(TimbnPluginInteractions)}|{TimbnSafe.Describe(spot.Secondary.Run)}");
    }

    // The closest spot with an action showing whose edge the player's interaction box reaches, while the player can act and
    // no object of the game's own is in reach, with the labels it read so they are not read again this frame.
    private static (TimbnInteractionSpot? Spot, string? Primary, string? Secondary) Reachable()
    {
        if (!TimbnGame.IsInGame || MainGame.PlayerController is not { } player || !TimbnInput.PlayerHasControl || TimbnInput.GameHasInteraction || TimbnUI.IsScreenFading)
            return default;

        var component = player.PlayerInteractionComponent;
        if (component == null)
            return default;

        var box = _box?.Invoke(component);
        var center = box != null ? box.bounds.center : component.transform.position;
        var extents = box != null ? box.bounds.extents : _fallbackExtents;
        (TimbnInteractionSpot? Spot, string? Primary, string? Secondary) best = default;
        var bestDistance = float.MaxValue;
        foreach (var spot in _spots)
        {
            var offset = spot.Position - center;
            if (Mathf.Abs(offset.y) > extents.y + _floorGap)
                continue;

            var x = Mathf.Max(Mathf.Abs(offset.x) - extents.x, 0f);
            var z = Mathf.Max(Mathf.Abs(offset.z) - extents.z, 0f);
            var distance = (x * x) + (z * z);
            if (distance > spot.Radius * spot.Radius || distance >= bestDistance)
                continue;

            // The labels are read only for a spot that is closer than the best so far, so most spots cost nothing.
            var primary = spot.Primary?.LabelNow(spot.Logger);
            var secondary = spot.Secondary?.LabelNow(spot.Logger);
            if (primary == null && secondary == null)
                continue;

            best = (spot, primary, secondary);
            bestDistance = distance;
        }

        return best;
    }
}
