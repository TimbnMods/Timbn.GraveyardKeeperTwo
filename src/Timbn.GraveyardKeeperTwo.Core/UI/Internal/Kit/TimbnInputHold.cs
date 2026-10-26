using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

// Several widgets can want the game's input off at once (a key capture while a text box has the caret). One counted
// hold remembers the state from before the first and puts it back after the last, so the order they end in does not matter.
internal static class TimbnInputHold
{
    private static readonly AccessTools.FieldRef<LazyInput?> _instance =
        AccessTools.StaticFieldRefAccess<LazyInput?>(AccessTools.Field(typeof(LazyInput), "instance"));

    private static readonly AccessTools.FieldRef<LazyInput, List<GameKey>> _pressedKeys = AccessTools.FieldRefAccess<LazyInput, List<GameKey>>("pressedKeys");

    private static readonly AccessTools.FieldRef<LazyInput, List<GameKey>> _heldKeys = AccessTools.FieldRefAccess<LazyInput, List<GameKey>>("holdedKeys");

    private static int _holders;
    private static bool _wasActive;

    internal static void Acquire()
    {
        if (_holders++ == 0)
        {
            _wasActive = LazyInput.IsInputActive();
            LazyInput.SetInputActivity(false);

            // The game stops updating its keys while its input is off, so the key pressed this frame would read as pressed
            // for as long as the hold lasts. The key that opened a page would press that page's button of the same key.
            if (_instance() is { } input)
            {
                _pressedKeys(input).Clear();
                _heldKeys(input).Clear();
            }
        }
    }

    internal static void Release()
    {
        if (_holders == 0)
            return;

        if (--_holders == 0)
            LazyInput.SetInputActivity(_wasActive);
    }
}
