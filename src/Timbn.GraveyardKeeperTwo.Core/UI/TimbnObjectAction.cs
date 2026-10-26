using System.ComponentModel;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

[Obsolete("Use Interactions.Register with a position, then Player.WalkTo and TimbnPlayer.Face. This does nothing and goes away in the next major version.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TimbnObjectAction(string objectName, Vector3 stand, string hintKey)
{
    public string ObjectName { get; } = objectName;
    public Vector3 Stand { get; } = stand;
    public string HintKey { get; } = hintKey;
    public Direction Facing { get; set; } = Direction.Up;
    public AnimationState Animation { get; set; } = AnimationState.WorkHands;
    public double GameMinutes { get; set; } = 10;
    public Func<bool>? CanPerform { get; set; }
    public Action? OnStarted { get; set; }
    public Action? OnFinished { get; set; }
}
