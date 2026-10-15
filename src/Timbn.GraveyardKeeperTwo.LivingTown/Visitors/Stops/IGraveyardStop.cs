namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal interface IGraveyardStop
{
    int RollStay(System.Random random);

    TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay);

    IEnumerable<(Vector3 Spot, Vector3 Target, bool Seat)> Spots(GraveyardVisitors visitors);
}
