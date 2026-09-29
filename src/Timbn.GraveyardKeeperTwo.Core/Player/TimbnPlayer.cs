namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads where the player is in the loaded save and moves them. See the plugin's Player property for changes
/// that have to be undone, such as holding the player still.
/// </summary>
public static class TimbnPlayer
{
    /// <summary>The player's position in their current scene, or zero at the main menu.</summary>
    public static Vector3 Position => TimbnGame.IsInGame ? MainGame.PlayerData.position.Value : Vector3.zero;

    /// <summary>The id of the scene the player is in, such as the graveyard or an interior, or null at the main menu.</summary>
    public static string? SceneId => TimbnGame.IsInGame ? MainGame.PlayerData.currentGameSceneId : null;

    /// <summary>
    /// The scene the player is in, holding its world objects and the items dropped on its ground, or null at the
    /// main menu.
    /// </summary>
    public static GameSceneData? Scene =>
        SceneId is { Length: > 0 } id ? MainGame.WorldData.GetGameSceneDataById(id) : null;

    /// <summary>
    /// Moves the player to a spot in their current scene, the way the game moves them. Pair it with
    /// <see cref="TimbnWorld.TryFindWalkable"/> so they land on open ground.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.TryFindWalkable(TimbnPlayer.Position, 5f, out var ground))
    ///     TimbnPlayer.MoveTo(ground);
    /// </code>
    /// </example>
    /// <param name="position">Where to put the player.</param>
    /// <returns>False at the main menu, when there is no player to move.</returns>
    public static bool MoveTo(Vector3 position)
    {
        if (!TimbnGame.IsInGame || MainGame.PlayerController == null)
            return false;

        MainGame.PlayerController.SetPosition(position);
        return true;
    }
}
