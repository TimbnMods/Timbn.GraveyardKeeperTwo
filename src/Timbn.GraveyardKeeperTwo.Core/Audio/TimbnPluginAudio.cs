using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Plays the game's own music and sound effects for the plugin, with music put back the way it was when the plugin
/// is done. Reach it through the plugin's Audio property.
/// </summary>
public sealed class TimbnPluginAudio
{
    private readonly TimbnFrameworkPlugin _owner;

    internal TimbnPluginAudio(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Plays one of the game's music tracks on a loop, such as violin or char_theme_linda, fading out whatever music
    /// was playing. When the handle is disposed the track fades out and the music before it comes back. A newer track
    /// from any plugin takes over, and the music before both comes back when that one ends.
    /// </summary>
    /// <example>
    /// <code>
    /// _song = Audio.PlayMusic(TimbnGameIds.Music.Violin);
    /// // once the performer stops
    /// _song.Dispose();
    /// </code>
    /// </example>
    /// <param name="trackId">The id of a track in one of the game's playlists.</param>
    /// <returns>A handle that stops the track when disposed. It is also stopped when the plugin unloads and on the way to the main menu.</returns>
    public IDisposable PlayMusic(string trackId) => _owner.SessionSubscriptions.Add(TimbnMusic.Play(trackId, _owner.PluginLogger));

    /// <summary>
    /// Plays one of the game's sound effects once, such as item_use_drink when someone takes a drink or coins_sound
    /// when money changes hands. It plays at full volume wherever the player stands, ends on its own and needs no
    /// undo. Nothing plays at the main menu or for an empty id.
    /// </summary>
    /// <example>
    /// <code>
    /// Audio.PlaySound(TimbnGameIds.Sounds.ItemUseDrink);
    /// </code>
    /// </example>
    /// <param name="soundId">The id of one of the game's sounds, the way an item's onUseSound names them.</param>
    public void PlaySound(string soundId)
    {
        if (TimbnGame.IsInGame && !string.IsNullOrEmpty(soundId))
            LazyAudio.PlayAndForget(soundId);
    }
}
