namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads where the player is in the loaded save and moves them. See the plugin's Player property for changes
/// that have to be undone, such as holding the player still.
/// </summary>
public static class TimbnPlayer
{
    /// <summary>
    /// Whether the game itself has taken the player's control, such as for a cutscene, sleep, or a teleport.
    /// Control taken by mods with the plugin's Player.TakeControl does not count, so a mod holding the player can
    /// use it to notice the game stepping in.
    /// </summary>
    /// <example>
    /// Stop meditating when the game takes control for anything but an open window.
    /// <code>
    /// if (TimbnPlayer.IsControlTakenByGame(TakenControlType.ByUI))
    ///     meditation.Stop();
    /// </code>
    /// </example>
    /// <param name="ignored">The game's reasons that should not count, such as TakenControlType.ByUI for open windows.</param>
    /// <returns>True when the game has taken control for any other reason. False at the main menu.</returns>
    public static bool IsControlTakenByGame(params TakenControlType[] ignored) => TimbnControl.IsTakenByGame(ignored);

    /// <summary>Whether the player has the game's tired debuff. False at the main menu.</summary>
    /// <example>
    /// <code>
    /// if (TimbnPlayer.IsTired)
    ///     return;
    /// </code>
    /// </example>
    public static bool IsTired => HasPerk(LazyConsts.Perks.LACK_OF_SLEEP_DEBUFF);

    /// <summary>Whether the player has a perk.</summary>
    /// <example>
    /// <code>
    /// var greenThumb = TimbnPlayer.HasPerk("perk_green_thumb");
    /// </code>
    /// </example>
    /// <param name="id">The perk's id, such as perk_green_thumb.</param>
    /// <returns>True when the player has it. False at the main menu.</returns>
    public static bool HasPerk(string id) =>
        TimbnGame.IsInGame && MainGame.Instance.GameSave?.perkSystemData?.HasPerk(id) == true;

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
