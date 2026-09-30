using LazyBearTechnology;
using System.Collections;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Finds the zombies in the loaded save and the looks the game can give them.</summary>
public static class TimbnZombies
{
    private static readonly Dictionary<string, TimbnZombieSkins?> _skins = [];

    /// <summary>
    /// The zombies spawned in the world of the loaded save, workers and followers alike. Empty at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// var carriers = TimbnZombies.OnScene().Count(zombie => zombie.ZombieType == ZombieType.Caretaker);
    /// </code>
    /// </example>
    /// <returns>Each zombie's world object.</returns>
    public static List<ZombieWgoData> OnScene()
    {
        if (!TimbnGame.IsInGame || MainGame.ZombieSystemData is not { } zombies)
            return [];

        return zombies.zombieOnSceneWgoIds
            .Select(id => zombies.GetZombie(id))
            .Where(zombie => zombie != null)
            .ToList();
    }

    /// <summary>
    /// The bodies, heads, and skin colors the game picks from when it rolls a zombie's looks, read once from the
    /// game's zombie customization config. A mod that restyles zombies offers these so its zombies look like the
    /// game's own.
    /// </summary>
    /// <param name="dataId">Which kind of zombie, the worker zombie by default.</param>
    /// <returns>The looks, or null when the game has no such kind.</returns>
    public static TimbnZombieSkins? Skins(string dataId = ZombieSkinHelper.ZOMBIE_WORKER_DATA_ID)
    {
        if (_skins.TryGetValue(dataId, out var cached))
            return cached;

        var skins = ReadSkins(dataId);
        if (skins != null)
            _skins[dataId] = skins;

        return skins;
    }

    private static TimbnZombieSkins? ReadSkins(string dataId)
    {
        var config = LazySingletonSO<ZombieCustomizationConfig>.Instance;
        var rolledData = config == null ? null : Traverse.Create(config).Field("zombieRolledDatas").GetValue<IList>();
        if (rolledData == null)
            return null;

        foreach (var entry in rolledData)
        {
            var data = Traverse.Create(entry);
            if (data.Field("id").GetValue<string>() != dataId)
                continue;

            return new TimbnZombieSkins(
                data.Field("bodyIds").GetValue<List<int>>(),
                data.Field("headIds").GetValue<List<int>>(),
                data.Field("bodyTextures").GetValue<List<Texture2D>>(),
                data.Field("headTextures").GetValue<List<Texture2D>>());
        }

        return null;
    }
}

/// <summary>The looks the game can roll for one kind of zombie. Get it from TimbnZombies.Skins.</summary>
public sealed class TimbnZombieSkins
{
    internal TimbnZombieSkins(List<int>? bodies, List<int>? heads, List<Texture2D>? bodyColors, List<Texture2D>? headColors)
    {
        Bodies = bodies?.Distinct().ToList() ?? [];
        Heads = heads?.Distinct().ToList() ?? [];
        BodyColors = bodyColors?.Where(texture => texture != null).Select(texture => texture.name).ToList() ?? [];
        HeadColors = headColors?.Where(texture => texture != null).Select(texture => texture.name).ToList() ?? [];
    }

    /// <summary>The body ids, each listed once.</summary>
    public List<int> Bodies { get; }

    /// <summary>The head ids, each listed once.</summary>
    public List<int> Heads { get; }

    /// <summary>The names of the body skin color textures.</summary>
    public List<string> BodyColors { get; }

    /// <summary>The names of the head skin color textures.</summary>
    public List<string> HeadColors { get; }
}
