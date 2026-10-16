namespace Timbn.GraveyardKeeperTwo.InfusionRack;

internal static class PluginConfig
{
    public static ConfigEntry<int> PointsPerSolution { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        PointsPerSolution = config.BindRange("Infusion Rack", "PointsPerSolution", 50,
            "Tech points each zombie lying in a stand gets from one spirit solution. The distillation cube makes one solution for each white skull on the organ.", 1, 500);
    }
}
