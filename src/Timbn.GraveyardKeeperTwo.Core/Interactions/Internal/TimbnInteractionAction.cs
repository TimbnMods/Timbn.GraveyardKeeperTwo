namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// One action on a key, its label read every time the hint is drawn. A label callback that throws is logged once and the
// action is left off, since it runs every frame.
internal sealed class TimbnInteractionAction<TRun>(TimbnBinding<string> label, TRun run)
    where TRun : Delegate
{
    private bool _broken;

    public TRun Run { get; } = run;

    // The label now, or null when the action is hidden.
    public string? LabelNow(ManualLogSource logger)
    {
        if (_broken)
            return null;

        try
        {
            return label.Current is { Length: > 0 } text ? text : null;
        }
        catch (Exception exception)
        {
            _broken = true;
            logger.LogError($"{nameof(TimbnPluginInteractions)}|A label threw, so its action is off until the mod reloads: {exception}");
            return null;
        }
    }
}
