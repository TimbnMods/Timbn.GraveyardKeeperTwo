namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A 3D model from a plugin's asset bundle, drawn with the game's own prop shader so it takes the game's lighting,
/// fog and dithering. Get one from <see cref="TimbnModelBundle.Model"/>.
/// </summary>
public sealed class TimbnModel
{
    private readonly GameObject _prefab;

    internal TimbnModel(GameObject prefab)
    {
        _prefab = prefab;
    }

    /// <summary>The prefab's name in the bundle.</summary>
    public string Name => _prefab.name;

    /// <summary>
    /// The model's width, height and depth in world units, for example to hand a station the same footprint as the
    /// drawing. A model made with <see cref="TimbnPluginModels.Build"/> is its pixel size divided by 50, 30 and 40.
    /// </summary>
    public Vector3 Size => LocalBounds.size;

    internal Bounds LocalBounds
    {
        get
        {
            var toRoot = _prefab.transform.worldToLocalMatrix;
            List<(Matrix4x4, Bounds)> meshes = [];
            foreach (var filter in _prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                    meshes.Add((toRoot * filter.transform.localToWorldMatrix, filter.sharedMesh.bounds));
            }

            foreach (var skinned in _prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh != null)
                    meshes.Add((toRoot * skinned.transform.localToWorldMatrix, skinned.sharedMesh.bounds));
            }

            return TimbnModelBounds.Combine(meshes);
        }
    }

    // The point in the model's own pixel frame that it was centered on, for a model built from faces. Two built models
    // share one frame, so the difference between their centers is how far to shift one to line it up with the other.
    internal Vector2? PixelCenter { get; set; }

    // Adds anything a model can only get once the game is running, such as a copy of one of the game's own props.
    internal Action<GameObject>? Decorate { get; set; }

    internal GameObject Create(Transform? parent = null)
    {
        var model = UnityEngine.Object.Instantiate(_prefab, parent, false);
        // A model built in code keeps its prefab inactive so it never draws at the origin, and the copy has to be switched on.
        model.SetActive(true);
        // An imported file can bring its own camera or light, and a stray camera takes over the screen.
        foreach (var stray in model.GetComponentsInChildren<Behaviour>(true).Where(behaviour => behaviour is Camera or Light or AudioListener))
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnModel)}|{Name} came with a {stray.GetType().Name} on {stray.name}, removed it.");
            UnityEngine.Object.DestroyImmediate(stray);
        }

        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is not (MeshRenderer or SkinnedMeshRenderer))
                continue;

            var slots = renderer.sharedMaterials;
            for (var i = 0; i < slots.Length; i++)
                slots[i] = TimbnModels.GameMaterial(slots[i]) ?? slots[i];

            renderer.sharedMaterials = slots;
        }

        if (Decorate is { } decorate)
            TimbnSafe.Run(() => decorate(model), $"Decorating {Name}");

        return model;
    }
}
