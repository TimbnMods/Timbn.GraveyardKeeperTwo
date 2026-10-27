namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Adds 3D models for a mod. It builds models from the mod's PNGs and draws them with the game's own prop shader.
/// Hand a model to Stations to make a buildable station or bench extension or to AddPart for a part hung on a grave.
/// Everything is freed when the mod unloads. Reach it through the Models property.
/// </summary>
public sealed class TimbnPluginModels
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginModels(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Makes a model from PNGs with no 3D modeling and no Unity, drawn with the game's own prop shader so it takes
    /// the game's lighting. Each PNG is a face exactly as the player sees it (a front standing up, a top lying flat,
    /// or a block that is both), its own size is the face's size, and you only say where it stands (see
    /// <see cref="TimbnModelFaces"/>). Hand the result to Stations or AddPart. It is freed when the mod unloads.
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
    /// Makes a model an extra part that the game hangs on a world object with the AddWgoPart expression. A headstone or
    /// a fence goes on a grave this way. The part sits at the world object's origin, so put the offset in the prefab.
    /// For example a headstone sits 0.3 units toward the back of the grave with its base 0.13 up. A part is walked
    /// through unless it gets a solid box. A fence needs one. A grave hides its own mound and collision while a fence is
    /// in it. It expects the fence to bring both. A part is kept out of the game's save while the game writes it
    /// and put back on load. That way the save still loads once the mod is gone.
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
