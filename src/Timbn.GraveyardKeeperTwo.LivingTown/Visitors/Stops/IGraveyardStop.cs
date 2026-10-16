namespace Timbn.GraveyardKeeperTwo.LivingTown;

internal interface IGraveyardStop
{
    int RollStay(System.Random random);

    TimbnNpcSteps? Plan(TimbnNpc npc, GraveyardVisitors visitors, int stay);

    int Capacity();
}
