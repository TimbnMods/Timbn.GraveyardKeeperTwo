namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Marks a box whose controls the gamepad only moves between, such as the list column of a split, so the d-pad and stick
/// never cross into the column beside it. Controls outside every zone share the window's own.
/// </summary>
internal sealed class TimbnNavigationZone : MonoBehaviour
{
    internal static TimbnNavigationZone? Of(Component item) => item.GetComponentInParent<TimbnNavigationZone>();
}
