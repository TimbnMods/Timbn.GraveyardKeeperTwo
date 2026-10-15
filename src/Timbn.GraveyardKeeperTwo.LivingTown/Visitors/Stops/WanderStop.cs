namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal sealed class WanderStop : IGraveyardStop
{
    private const float _shortestHop = 4f;
    private const double _longStayChance = 0.25;

    public int RollStay(System.Random random) =>
        random.NextDouble() < _longStayChance ? random.Next(40, 81) : random.Next(10, 31);

    // Wandering has no spot of its own, so it adds nothing to how many the graveyard holds.
    public int Capacity() => 0;

    public TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay)
    {
        if (!visitors.Paths.TryRandomSpot(npc.Position, _shortestHop, visitors.Random, out var spot))
        {
            GraveyardVisitors.Log($"{npc.Id} finds no spot on the paths to wander to.");
            return null;
        }

        GraveyardVisitors.Log($"{npc.Id} wanders {TimbnWorld.FlatDistance(npc.Position, spot):0.0} from {GraveyardVisitors.Flat(npc.Position)} to {GraveyardVisitors.Flat(spot)}.");
        return visitors.Paths.Walk(npc, new TimbnNpcSteps(), spot)
            .WaitGameMinutes(stay);
    }
}
