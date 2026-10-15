namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Something the player can do beside an object in the world that the game gives no interaction for, such as petting
/// a cat on a box. Standing by the object shows a hint, and pressing Interaction walks the player straight to a spot beside it,
/// where they face one way and play an animation for a while. Add it with the plugin's UI.AddObjectAction.
/// </summary>
/// <example>
/// <code>
/// UI.AddObjectAction(new TimbnObjectAction("common_box_08", new Vector3(-104.36f, 0f, -338.20f), "my_mod_pet")
/// {
///     Facing = Direction.Up,
///     Animation = AnimationState.WorkHands,
///     GameMinutes = 10,
/// });
/// </code>
/// </example>
public sealed class TimbnObjectAction
{
    /// <summary>Describes the action.</summary>
    /// <param name="objectName">The name the object's GameObject starts with, such as common_box_08. Its parents count too.</param>
    /// <param name="stand">The spot the player stands at to do it. Its y is ignored, since the player keeps the height they have.</param>
    /// <param name="hintKey">A text key added with Text.Add, shown as the hint, such as "Pet".</param>
    public TimbnObjectAction(string objectName, Vector3 stand, string hintKey)
    {
        ObjectName = objectName;
        Stand = stand;
        HintKey = hintKey;
    }

    /// <summary>The name the object's GameObject starts with.</summary>
    public string ObjectName { get; }

    /// <summary>The spot the player stands at. Its y is ignored.</summary>
    public Vector3 Stand { get; }

    /// <summary>The text key of the hint.</summary>
    public string HintKey { get; }

    /// <summary>The way the player faces while they do it. Up by default.</summary>
    public Direction Facing { get; set; } = Direction.Up;

    /// <summary>The animation the player plays while they do it. Working with their hands by default.</summary>
    public AnimationState Animation { get; set; } = AnimationState.WorkHands;

    /// <summary>How many game minutes it lasts. 10 by default.</summary>
    public double GameMinutes { get; set; } = 10;

    /// <summary>Asked before the hint shows and the walk starts. Return false to hide the action, such as until a quest is done.</summary>
    public Func<bool>? CanPerform { get; set; }

    /// <summary>Called when the player reaches the spot and starts the animation.</summary>
    public Action? OnStarted { get; set; }

    /// <summary>Called when the animation runs its full minutes. It is not called when the game takes the controls back first.</summary>
    public Action? OnFinished { get; set; }
}
