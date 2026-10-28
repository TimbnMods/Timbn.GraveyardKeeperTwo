namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnTextMeasure
{
    /// <summary>The width of one line of text in a style, measured on a label that is made under the box and removed again.</summary>
    internal static float Width(Transform parent, TimbnTextStyle style, string text)
    {
        if (text.Length == 0)
            return 0f;

        var scratch = TimbnUi.Box(parent, "Measure");
        var width = TimbnUi.Text(scratch, style, text).GetPreferredValues(text).x;
        UnityEngine.Object.DestroyImmediate(scratch.gameObject);
        return width;
    }
}
