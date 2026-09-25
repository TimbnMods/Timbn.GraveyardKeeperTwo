namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

internal static class ZombieTint
{
    private const string _key = "timbn_zombie_tint";

    private static readonly string[] _bodyParts = ["bdy", "arm", "leg"];

    public static Color Read(WgoData zombie)
    {
        var saved = zombie.GameResStr.Get(_key);
        if (string.IsNullOrEmpty(saved))
            return Color.white;

        return ColorUtility.TryParseHtmlString("#" + saved, out var color) ? Opaque(color) : Color.white;
    }

    public static void Save(WgoData zombie, Color color) =>
        zombie.GameResStr.Set(_key, IsPlain(color) ? string.Empty : ColorUtility.ToHtmlStringRGB(color));

    public static List<Color> Palette(Color current)
    {
        List<Color> palette = [Color.white];
        palette.AddRange(PluginConfig.ClothesColors.Select(entry => Opaque(entry.Value)));
        if (!palette.Any(color => Same(color, current)))
            palette.Insert(1, Opaque(current));

        return palette;
    }

    public static bool Same(Color first, Color second) =>
        ColorUtility.ToHtmlStringRGB(first) == ColorUtility.ToHtmlStringRGB(second);

    public static string Describe(Color color) => IsPlain(color) ? "none" : "#" + ColorUtility.ToHtmlStringRGB(color);

    public static void Paint(Component root, Color color)
    {
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (IsBodyPart(renderer.name))
                renderer.color = color;
        }
    }

    public static void PaintAll(bool reset)
    {
        if (!TimbnGame.IsInGame)
            return;

        var zombies = MainGame.ZombieSystemData;
        foreach (var id in zombies.zombieOnSceneWgoIds)
        {
            var zombie = zombies.GetZombie(id);
            var view = zombie == null ? null : GameScene.GetWgoViewGlobal(zombie.UniqueId);
            if (view != null)
                Paint(view, reset ? Color.white : Read(zombie!));
        }
    }

    public static bool IsBodyPart(string name) => _bodyParts.Any(name.StartsWith);

    private static bool IsPlain(Color color) => Same(color, Color.white);

    private static Color Opaque(Color color) => new(color.r, color.g, color.b, 1f);
}
