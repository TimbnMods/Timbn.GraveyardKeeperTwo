namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

internal static class PluginConfig
{
    private const string _section = "Clothes Colors";

    private static readonly Color[] _defaults =
    [
        new(1f, 0.55f, 0.55f),
        new(1f, 0.75f, 0.45f),
        new(1f, 1f, 0.55f),
        new(0.6f, 1f, 0.6f),
        new(0.55f, 1f, 1f),
        new(0.6f, 0.7f, 1f),
        new(0.8f, 0.6f, 1f),
        new(1f, 0.65f, 0.85f),
        new(0.7f, 0.7f, 0.7f),
        new(0.45f, 0.45f, 0.5f),
    ];

    public static IReadOnlyList<ConfigEntry<Color>> ClothesColors { get; private set; } = [];

    public static void Bind(ConfigFile config) =>
        ClothesColors = [.. _defaults
            .Select((color, i) => config.Bind(
                _section,
                $"ClothesColor{i + 1}",
                color,
                "One of the colors offered in the Clothes Color row when you customize a zombie. It tints the zombie's clothes, "
                + "so white leaves them as they are and darker colors show more. Changing it does not recolor zombies you already styled."))];
}
