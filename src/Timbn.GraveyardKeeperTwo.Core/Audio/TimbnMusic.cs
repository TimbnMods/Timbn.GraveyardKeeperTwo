using LazyBearTechnology;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnMusic
{
    private const string _prefix = "timbn_music_";
    private const string _gameplay = "gameplay";
    private const float _fade = 2f;

    private static readonly FieldInfo? _instance = AccessTools.Field(typeof(LazyAudio), "instance");

    private static readonly AccessTools.FieldRef<LazyAudio, List<PlaylistController>>? _controllers =
        TimbnReflection.TryField<LazyAudio, List<PlaylistController>>("playlistControllers");

    private static Playing? _current;

    internal static IDisposable Play(string trackId, ManualLogSource logger)
    {
        if (!TimbnGame.IsInGame || Controller(trackId, logger) is not { } music)
            return TimbnUndo.None;

        var paused = _current?.Paused;
        _current?.Stop(restore: false);
        paused ??= LazyAudio.GetPlaylistControllers().Find(each => each != music && each.IsActive && !each.IsPaused);
        paused?.Pause(_fade);
        music.Play(_fade);
        logger.LogDebug($"{nameof(TimbnPluginAudio)}.{nameof(TimbnPluginAudio.PlayMusic)}|Playing {trackId}{(paused != null ? $" over {paused.Id}" : "")}.");
        var playing = new Playing(music, paused);
        _current = playing;
        return new TimbnUndo(() => playing.Stop(restore: true));
    }

    private static PlaylistController? Controller(string trackId, ManualLogSource logger)
    {
        if (_instance is null || _controllers is null || _instance.GetValue(null) is not LazyAudio audio || _controllers(audio) is not { } controllers)
            return null;

        var id = _prefix + trackId;
        if (controllers.Find(each => each != null && each.Id == id) is { } made)
            return made;

        var source = controllers.Where(each => each?.Playlist != null).SelectMany(each => each.Playlist.tracks.Select(track => (Playlist: each.Playlist, Track: track))).FirstOrDefault(pair => pair.Track.id == trackId);
        if (source.Track?.clip == null)
        {
            logger.LogWarning($"{nameof(TimbnPluginAudio)}.{nameof(TimbnPluginAudio.PlayMusic)}|The game has no music track {trackId}.");
            return null;
        }

        var playlist = new Playlist
        {
            id = id,
            loop = true,
            volume = source.Playlist.volume,
            fadeDuration = source.Playlist.fadeDuration,
            group = source.Playlist.group,
        };
        playlist.tracks.Add(new Track { id = trackId, clip = source.Track.clip, volume = source.Track.volume, loop = true, pitch = source.Track.pitch, weight = 1f });
        var controller = new GameObject().AddComponent<PlaylistController>();
        controller.transform.SetParent(audio.transform);
        controller.Initialize(playlist);
        controllers.Add(controller);
        return controller;
    }

    private sealed class Playing
    {
        internal Playing(PlaylistController music, PlaylistController? paused)
        {
            Music = music;
            Paused = paused;
        }

        internal PlaylistController Music { get; }

        internal PlaylistController? Paused { get; }

        internal void Stop(bool restore)
        {
            if (_current != this)
                return;

            _current = null;
            if (Music != null)
                Music.Stop(_fade);

            if (!restore)
                return;

            if (Paused != null && Paused.IsActive && Paused.IsPaused)
                Paused.UnPause(_fade);
            else if (!LazyAudio.GetPlaylistControllers().Exists(each => each != Music && each.IsActive && !each.IsPaused))
                LazyAudio.PlayPlaylist(_gameplay);
        }
    }
}
