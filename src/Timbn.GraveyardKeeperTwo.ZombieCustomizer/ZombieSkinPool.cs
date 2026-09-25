using LazyBearTechnology;
using System.Collections;

namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

internal sealed class ZombieSkinPool(List<int>? bodies, List<int>? heads, List<Texture2D>? bodyColors, List<Texture2D>? headColors)
{
    private static ZombieSkinPool? _worker;

    public List<int> Bodies { get; } = bodies?.Distinct().ToList() ?? [];

    public List<int> Heads { get; } = heads?.Distinct().ToList() ?? [];

    public List<string> BodyColors { get; } = bodyColors?.Select(t => t.name).ToList() ?? [];

    public List<string> HeadColors { get; } = headColors?.Select(t => t.name).ToList() ?? [];

    public static ZombieSkinPool? Worker => _worker ??= Read(ZombieSkinHelper.ZOMBIE_WORKER_DATA_ID);

    private static ZombieSkinPool? Read(string dataId)
    {
        var config = LazySingletonSO<ZombieCustomizationConfig>.Instance;
        var rolledData = Traverse.Create(config).Field("zombieRolledDatas").GetValue<IList>();
        if (rolledData == null)
            return null;

        foreach (var entry in rolledData)
        {
            var data = Traverse.Create(entry);
            if (data.Field("id").GetValue<string>() != dataId)
                continue;

            return new ZombieSkinPool(
                data.Field("bodyIds").GetValue<List<int>>(),
                data.Field("headIds").GetValue<List<int>>(),
                data.Field("bodyTextures").GetValue<List<Texture2D>>(),
                data.Field("headTextures").GetValue<List<Texture2D>>());
        }

        return null;
    }
}
