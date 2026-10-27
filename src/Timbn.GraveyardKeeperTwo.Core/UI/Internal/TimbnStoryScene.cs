using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads whether a story scene has the screen or the player, telling the game's own scenes apart from the cinematic
/// bars and the control holds of Core's own talks and mods.
/// </summary>
internal static class TimbnStoryScene
{
    private static readonly AccessTools.FieldRef<UICinematic, bool>? _barsHiding =
        TimbnReflection.TryField<UICinematic, bool>("isAnimatingDisabling");

    private static bool _barsOurs;

    internal static bool BarsUp => Bars() is { } bars && IsUp(bars);

    internal static bool BarsAreOurs => BarsUp && _barsOurs;

    internal static bool IsRunning =>
        (BarsUp && !_barsOurs) || (MainGame.PlayerController is { } player && !player.IsControlEnabledByType(TakenControlType.ByCinematics));

    internal static bool GameHasPlayer(params TakenControlType[] ignored) => TimbnPlayerControl.IsTakenByGame(ignored);

    internal static bool TryShowBars(Action then)
    {
        var shown = false;
        TimbnSafe.Run(
            () =>
            {
                var bars = Bars();
                if (bars == null || IsUp(bars))
                    return;

                bars.EnableCinematic(then);
                _barsOurs = true;
                shown = true;
            },
            $"{nameof(TimbnStoryScene)}|Showing the cinematic bars");
        return shown;
    }

    internal static void HideBars()
    {
        _barsOurs = false;
        TimbnSafe.Run(() => Bars()?.DisableCinematic(), $"{nameof(TimbnStoryScene)}|Hiding the cinematic bars");
    }

    internal static void Reset() => _barsOurs = false;

    private static UICinematic? Bars() => LazyUI.Get<UICinematic>() is { } bars && bars != null ? bars : null;

    private static bool IsUp(UICinematic bars)
    {
        var up = bars.gameObject.activeSelf && (_barsHiding == null || !_barsHiding(bars));
        if (!up)
            _barsOurs = false;

        return up;
    }
}
