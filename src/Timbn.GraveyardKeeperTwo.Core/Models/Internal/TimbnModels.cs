using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Loads plugin asset bundles, hands out copies of the game's prop material per texture, and keeps the parts that
/// serve a model as the look of a world object, keyed by the addressable the game would have loaded. A part that
/// goes away while its objects are in the world has them released to the pool and redrawn when the same key comes
/// back, which is how a hot reload keeps built stations visible.
/// </summary>
internal static class TimbnModels
{
    private const string _gameShader = "Custom/3D Object";
    private const string _gameMaterial = "3DObject";

    private static readonly Dictionary<Texture, Material> _materials = [];
    private static readonly Dictionary<string, TimbnPartPrefab> _parts = [];
    private static readonly Dictionary<string, List<Wgo>> _waitingForRedraw = [];
    private static bool _warnedNoTemplate;

    internal static TimbnModelBundle LoadEmbeddedBundle(Assembly assembly, string resourceName)
    {
        var bundle = AssetBundle.LoadFromMemory(TimbnEmbedded.Read(assembly, resourceName))
            ?? throw new InvalidOperationException($"{resourceName} could not be loaded. Another plugin may have loaded a bundle with the same name.");

        return new TimbnModelBundle(bundle);
    }

    internal static TimbnModelBundle LoadBundle(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"There is no asset bundle at {path}.", path);

        var bundle = AssetBundle.LoadFromFile(path)
            ?? throw new InvalidOperationException($"{path} could not be loaded. Another plugin may have loaded a bundle with the same name.");

        return new TimbnModelBundle(bundle);
    }

    internal static IDisposable AddStation(string assetId, IReadOnlyList<TimbnSide<TimbnModel>> sides, float? dockDistance, bool zombieSpot = false,
        IReadOnlyList<TimbnStationOverlay>? overlays = null, IReadOnlyList<TimbnStationSlot>? slots = null) =>
        Register(new TimbnPartPrefab(assetId, [.. sides.Select(side =>
            new TimbnPartPrefabSide(side.Model!, TimbnPartShape.Station(side.Size, dockDistance, side.Facing), side.Facing, side.MirrorOf,
                side.MirrorOf == null ? overlays?.Where(overlay => overlay.Facing == side.Facing).ToList() : null,
                side.MirrorOf == null ? slots?.Where(slot => slot.Facing == side.Facing).ToList() : null))], zombieSpot));

    internal static IDisposable AddExtension(string assetId, TimbnModel model, Vector3 size, float reach) =>
        Register(new TimbnPartPrefab(assetId, [new TimbnPartPrefabSide(model, TimbnPartShape.Extension(size, reach))]));

    internal static IDisposable AddPart(string partId, TimbnModel model, Bounds? solid) =>
        Register(new TimbnPartPrefab(partId, [new TimbnPartPrefabSide(model, TimbnPartShape.Part(solid))]));

    internal static bool TryGetPart(string? key, out TimbnPartPrefab part)
    {
        if (key is { Length: > 0 })
            return _parts.TryGetValue(key, out part!);

        part = null!;
        return false;
    }

    internal static HashSet<string> AddedPartIds() => [.. _parts.Values.Select(part => part.AssetId)];

    internal static void OnBalanceLoaded()
    {
        foreach (var part in _parts.Values)
            part.RegisterBakedData(allowLoad: true);
    }

    internal static void OnLeftGame() => _waitingForRedraw.Clear();

    // A plugin part has no Object3D, so the game's build ghost and remove tool tint skips it without this.
    internal static void TintParts(Wgo wgo, Color color, float amount)
    {
        if (wgo.MainWgoPart is { } main && TryGetPart(main.PooledAddressableKey, out _))
            TimbnPartPrefab.Tint(main, color, amount);

        foreach (var part in wgo.AdditionalWgoParts)
        {
            if (part != null && TryGetPart(part.PooledAddressableKey, out _))
                TimbnPartPrefab.Tint(part, color, amount);
        }
    }

    private static IDisposable Register(TimbnPartPrefab part)
    {
        if (_parts.TryGetValue(part.Key, out var previous))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModels)}|The part {part.AssetId} was added again while an earlier one with that id was still in, so the new one replaces it.");
            Unload(previous);
        }

        _parts[part.Key] = part;
        part.RegisterBakedData();
        if (_waitingForRedraw.Remove(part.Key, out var objects))
        {
            foreach (var wgo in objects)
                TimbnPartPrefab.Redraw(wgo);
        }

        return new TimbnUndo(() =>
        {
            // A part another registration replaced was unloaded then, and unloading it again would release the
            // world objects and baked data of the one that took its key.
            if (!_parts.TryGetValue(part.Key, out var current) || current != part)
                return;

            _parts.Remove(part.Key);
            Unload(part);
        });
    }

    private static void Unload(TimbnPartPrefab part)
    {
        var objects = part.ReleaseFromWorld();
        if (objects.Count > 0)
            _waitingForRedraw[part.Key] = objects;

        part.Dispose();
    }

    internal static Material? GameMaterial(Material original)
    {
        var texture = original != null ? original.mainTexture : null;
        if (texture == null)
            return null;

        if (_materials.TryGetValue(texture, out var material) && material != null)
            return material;

        var template = Resources.FindObjectsOfTypeAll<Material>()
            .Where(candidate => candidate.shader != null && candidate.shader.name == _gameShader && candidate.GetFloat("_IsTreeCrown") == 0f && !_materials.ContainsValue(candidate))
            .OrderByDescending(candidate => candidate.name == _gameMaterial)
            .FirstOrDefault();

        if (template == null)
        {
            if (!_warnedNoTemplate)
                TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModels)}|No {_gameShader} material is loaded yet, so models keep their own materials.");

            _warnedNoTemplate = true;
            return null;
        }

        material = new Material(template) { name = $"Timbn {texture.name}" };
        material.SetFloat("_UseNormalMap", 0f);
        material.SetFloat("_UseLUTColorReplace", 0f);
        material.SetColor("_Color", Color.white);
        material.mainTexture = texture;
        _materials[texture] = material;
        return material;
    }

    internal static void ReleaseMaterialsOfUnloadedTextures()
    {
        // Swept by destroyed key, since a bundle's LoadAllAssets can miss the texture a prefab's material used.
        foreach (var texture in _materials.Keys.Where(texture => texture == null).ToList())
        {
            _materials.Remove(texture, out var material);
            if (material != null)
                UnityEngine.Object.Destroy(material);
        }
    }
}
