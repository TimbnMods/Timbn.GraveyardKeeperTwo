namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>What a list of steps acts on, the person.</summary>
internal sealed class TimbnNpcRun
{
    internal TimbnNpcRun(TimbnNpc npc)
    {
        Npc = npc;
    }

    internal TimbnNpc Npc { get; }

    internal bool BlockDue { get; set; }

    internal System.Random Random { get; } = new();
}
