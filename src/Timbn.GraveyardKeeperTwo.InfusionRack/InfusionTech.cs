namespace Timbn.GraveyardKeeperTwo.InfusionRack;

internal static class InfusionTech
{
    public const string Id = "timbn_infusion_rack";

    public static void Register(TimbnFrameworkPlugin plugin) =>
        plugin.Techs.Add(Id, "", tech => tech
            .Tab(TechTreeTab.Anatomy)
            .At(8, 2)
            .After(TimbnGameIds.Techs.BodyPlaces2)
            .Cost(red: 50, green: 50, blue: 20)
            .Shows(SpiritSolution.ItemId(TechColor.Red), InfusionMachine.StationId, InfusionStand.StationId));
}
