namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A 3D model built from a plugin's PNGs, drawn with the game's own prop shader so it takes the game's lighting, fog
/// and dithering. Get one from <see cref="TimbnPluginModels.Build"/>.
/// </summary>
public sealed class TimbnModel
{
    private readonly GameObject _prefab;

    internal TimbnModel(GameObject prefab)
    {
        _prefab = prefab;
    }

    /// <summary>The model's name, as given to <see cref="TimbnPluginModels.Build"/>.</summary>
    public string Name => _prefab.name;

    /// <summary>
    /// The model's width, height and depth in world units, for example to hand a station the same footprint as the
    /// drawing. It is the model's pixel size divided by 50, 30 and 40.
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
        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
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
