namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds 3D models on behalf of a plugin. It loads the plugin's asset bundle and draws the models with the game's own
/// prop shader. Hand a model to the plugin's Stations to make a buildable station or bench extension, or to AddPart
/// for a part hung on a grave. Everything is unloaded when the plugin unloads. Reach it through the plugin's Models
/// property.
/// </summary>
public sealed class TimbnPluginModels
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginModels(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Loads an asset bundle embedded in the plugin's DLL. It is read from memory, which is what works under
    /// ScriptEngine, and unloaded when the plugin unloads, so F6 can load it again. Embed it with an EmbeddedResource
    /// item whose LogicalName is the resource name, and build it with Unity 6000.3.9f1.
    /// </summary>
    /// <param name="resourceName">The embedded resource's name, for example "examples.bundle".</param>
    /// <returns>The bundle. Get models from it with <see cref="TimbnModelBundle.Model"/>.</returns>
    /// <exception cref="InvalidOperationException">The plugin has no such embedded resource, or the bundle could not be loaded.</exception>
    public TimbnModelBundle LoadEmbeddedBundle(string resourceName)
    {
        var bundle = TimbnModels.LoadEmbeddedBundle(_owner.GetType().Assembly, resourceName);
        _owner.Subscriptions.Add(bundle);
        return bundle;
    }

    /// <summary>
    /// Loads an asset bundle file shipped next to the plugin's DLL, in its folder under BepInEx/plugins or
    /// BepInEx/scripts. Unity reads models from the file as they are needed instead of keeping a second copy in
    /// memory, which suits large bundles. It is unloaded when the plugin unloads, so F6 can load it again. Ship it
    /// with a None item set to CopyToOutputDirectory PreserveNewest, and build it with Unity 6000.3.9f1.
    /// </summary>
    /// <param name="fileName">The bundle's path relative to the plugin's folder, for example "Bundles/examples.bundle".</param>
    /// <returns>The bundle. Get models from it with <see cref="TimbnModelBundle.Model"/>.</returns>
    /// <exception cref="FileNotFoundException">There is no file at that path in the plugin's folder.</exception>
    public TimbnModelBundle LoadBundle(string fileName)
    {
        var bundle = TimbnModels.LoadBundle(Path.Combine(_owner.Folder, fileName));
        _owner.Subscriptions.Add(bundle);
        return bundle;
    }

    /// <summary>
    /// Makes a model from PNGs with no 3D modeling and no Unity, drawn with the game's own prop shader so it takes
    /// the game's lighting. Each PNG is a face exactly as the player sees it (a front standing up, a top lying flat,
    /// or a block that is both), its own size is the face's size, and you only say where it stands (see
    /// <see cref="TimbnModelFaces"/>). Hand the result to the plugin's Stations like a model from a bundle. It is
    /// freed when the plugin unloads.
    /// </summary>
    /// <param name="name">The model's name, for example "PcDesk".</param>
    /// <param name="build">Adds the model's faces.</param>
    /// <returns>The model, ready for a station's Model.</returns>
    /// <exception cref="InvalidOperationException">
    /// The model has no faces, a PNG is missing or unreadable, a block's depth leaves no front, or two faces on the
    /// same plane overlap.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">A face's position is below zero or a block's depth is below 1.</exception>
    public TimbnModel Build(string name, Action<TimbnModelFaces> build)
    {
        var faces = new TimbnModelFaces(_owner.GetType().Assembly);
        build(faces);
        var (model, owner) = TimbnBuiltModel.Create(name, faces.Pictures);
        _owner.Subscriptions.Add(owner);
        return model;
    }

    /// <summary>
    /// Makes a model an extra part that the game hangs on a world object with the AddWgoPart expression, the way a
    /// headstone or a fence goes on a grave. The part sits at the world object's origin, so the offset goes into the
    /// prefab, for example a headstone 0.3 units toward the back of the grave with its base 0.13 up. A part is walked
    /// through unless it gets a solid box. A fence needs one, since a grave hides its own mound and collision while a
    /// fence is in it and expects the fence to bring both. A part hung on a world object is kept out of the game's
    /// save while the game writes it and put back on load, so the save still loads once the mod is gone.
    /// </summary>
    /// <param name="partId">The id AddWgoPart and RemoveWgoPart use, for example "timbn_grave_bot_1".</param>
    /// <param name="model">The model to draw.</param>
    /// <param name="solid">
    /// A box the player bumps into, in the world object's space, or null for none. For example
    /// <c>new Bounds(new Vector3(0f, 0.3f, 0.075f), new Vector3(0.6f, 0.6f, 1f))</c> covers a grave.
    /// </param>
    /// <returns>A handle that removes the part's look early when disposed. You don't need to keep it.</returns>
    public IDisposable AddPart(string partId, TimbnModel model, Bounds? solid = null) =>
        _owner.Subscriptions.Add(TimbnModels.AddPart(partId, model, solid));
}
