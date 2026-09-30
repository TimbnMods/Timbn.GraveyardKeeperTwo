using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnVoice
{
    private static readonly List<Func<string, bool>> _filters = [];

    internal static IDisposable Allow(Func<string, bool> filter)
    {
        _filters.Add(filter);
        return new TimbnUndo(() => _filters.Remove(filter));
    }

    internal static bool AllowLine(VoiceOverPlayer player, string id)
    {
        foreach (var filter in _filters.ToList())
        {
            bool allowed;
            try
            {
                allowed = filter(id);
            }
            catch (Exception ex)
            {
                TimbnCorePlugin.Logger.LogError($"{nameof(TimbnVoice)}|{TimbnSafe.Describe(filter)} threw: {ex}");
                continue;
            }

            if (allowed)
                continue;

            player.Stop();
            return false;
        }

        return true;
    }
}
