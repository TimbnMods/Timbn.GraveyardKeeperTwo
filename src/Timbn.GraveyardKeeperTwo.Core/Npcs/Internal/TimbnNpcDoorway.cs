namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>A point every walk to or from another point passes, set with <see cref="TimbnNpcDay.Doorway"/>.</summary>
internal sealed class TimbnNpcDoorway
{
    internal TimbnNpcDoorway(string point, string via, TimbnWalk walk)
    {
        Point = point;
        Via = via;
        Walk = walk;
    }

    internal string Point { get; }

    internal string Via { get; }

    internal TimbnWalk Walk { get; }
}
