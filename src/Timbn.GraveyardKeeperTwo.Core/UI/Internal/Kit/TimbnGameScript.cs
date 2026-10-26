using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Puts one of the game's own control scripts (UISwitchButton, UISlider) on pieces Core built, so the game's code runs the
/// control and Core only lays it out. The scripts wire their buttons in Awake from private fields the game's editor fills
/// in, so the host starts off, gets its fields, and is turned on.
/// </summary>
// The private fields set here are checked to exist by TimbnGameScriptTests, so a game update that renames one fails a test.
internal static class TimbnGameScript
{
    /// <summary>Adds the script to a new object under the parent, with its private fields set, and turns it on.</summary>
    internal static T Add<T>(Transform parent, params (string Field, object? Value)[] fields)
        where T : MonoBehaviour
    {
        var host = new GameObject(typeof(T).Name, typeof(RectTransform));
        host.SetActive(false);
        host.transform.SetParent(parent, false);
        // It holds no pixels, so a layout row must not leave a gap for it.
        host.AddComponent<LayoutElement>().ignoreLayout = true;
        var script = host.AddComponent<T>();
        foreach (var (field, value) in fields)
            Set(script, field, value);

        host.SetActive(true);
        return script;
    }

    internal static void Set(object target, string field, object? value) =>
        (AccessTools.Field(target.GetType(), field) ?? throw new MissingFieldException(target.GetType().Name, field)).SetValue(target, value);

    /// <summary>
    /// Turns off the script's own stepping from the gamepad's slider keys, which the page's own gamepad stop already does,
    /// so one press does not step twice. GameKey.None is never pressed.
    /// </summary>
    internal static void SilenceKeys(object script, bool listsOfKeys)
    {
        if (listsOfKeys)
        {
            Set(script, "decreaseKeys", new List<GameKey> { GameKey.None });
            Set(script, "increaseKeys", new List<GameKey> { GameKey.None });
            return;
        }

        Set(script, "decreaseKeys", new[] { GameKey.None });
        Set(script, "increaseKeys", new[] { GameKey.None });
    }
}
