namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>An empty box that takes the room left in a row, pushing what comes after it to the far end.</summary>
public sealed class TimbnSpring : TimbnComponent
{
    protected internal override RectTransform Build(Transform parent) => TimbnUi.Spring(parent);
}
