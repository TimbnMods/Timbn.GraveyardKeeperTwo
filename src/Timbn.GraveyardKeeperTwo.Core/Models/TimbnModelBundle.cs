namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// An asset bundle loaded for a plugin with <see cref="TimbnPluginModels.LoadEmbeddedBundle"/> or
/// <see cref="TimbnPluginModels.LoadBundle"/>. It is unloaded, with every model and material made from it, when
/// the plugin unloads.
/// </summary>
public sealed class TimbnModelBundle : IDisposable
{
    private readonly AssetBundle _bundle;
    private readonly Dictionary<string, TimbnModel> _models = [];

    internal TimbnModelBundle(AssetBundle bundle)
    {
        _bundle = bundle;
    }

    /// <summary>The bundle's name.</summary>
    public string Name => _bundle.name;

    /// <summary>
    /// Gets a model by its prefab name. The bundle has to be built with the game's Unity version (6000.3.9f1), and
    /// its prefabs can't carry game scripts.
    /// </summary>
    /// <param name="prefabName">The prefab's name in the bundle, for example "RedWorkbench".</param>
    /// <returns>The model, the same one each time for a name.</returns>
    /// <exception cref="InvalidOperationException">The bundle has no prefab with that name.</exception>
    public TimbnModel Model(string prefabName)
    {
        if (_models.TryGetValue(prefabName, out var model))
            return model;

        var prefab = _bundle.LoadAsset<GameObject>(prefabName)
            ?? throw new InvalidOperationException($"Bundle {Name} has no prefab named {prefabName}.");
        model = new TimbnModel(prefab);
        _models[prefabName] = model;
        return model;
    }

    /// <summary>Unloads the bundle and frees its models and their game materials.</summary>
    public void Dispose()
    {
        _models.Clear();
        if (_bundle != null)
            _bundle.Unload(true);

        TimbnModels.ReleaseMaterialsOfUnloadedTextures();
    }
}
