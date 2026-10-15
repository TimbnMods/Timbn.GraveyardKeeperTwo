using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>Gives a plugin's answer its own button color in the game's answer list, shaded on hover the way the game shades its own.</summary>
internal static class TimbnAnswerColors
{
    private static readonly Dictionary<string, Color> _colors = [];

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, AnswerVisualData> _visualData =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, AnswerVisualData>("visualData");

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, Image> _button =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, Image>("optionButtonImage");

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, Color> _original =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, Color>("originalColor");

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, Color> _highlighted =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, Color>("highlightedColor");

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, RectTransform> _leftArrow =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, RectTransform>("leftArrow");

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, RectTransform> _rightArrow =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, RectTransform>("rightArrow");

    private static readonly AccessTools.FieldRef<UIMultiAnswerOption, List<UIBubbleCorner>> _corners =
        AccessTools.FieldRefAccess<UIMultiAnswerOption, List<UIBubbleCorner>>("corners");

    internal static IDisposable Add(string answerId, Color color)
    {
        _colors[answerId] = color;
        return new TimbnUndo(() => _colors.Remove(answerId));
    }

    internal static void Paint(UIMultiAnswerOption option)
    {
        if (!TryGet(option, out var color))
            return;

        _button(option).color = color;
        foreach (var arrow in new[] { _leftArrow(option), _rightArrow(option) })
        {
            if (arrow != null && arrow.TryGetComponent<Image>(out var image))
                image.color = color;
        }

        foreach (var corner in _corners(option))
        {
            if (corner != null && corner.customImage != null)
                corner.customImage.color = color;
        }
    }

    internal static Color Recolor(UIMultiAnswerOption option, Color color)
    {
        if (!TryGet(option, out var own))
            return color;

        return color == _highlighted(option) ? Hover(own, _original(option), _highlighted(option)) : own;
    }

    private static bool TryGet(UIMultiAnswerOption option, out Color color)
    {
        color = default;
        var id = _visualData(option)?.id;
        return id is not null && _colors.TryGetValue(id, out color);
    }

    private static Color Hover(Color own, Color original, Color highlighted) =>
        new(Scale(own.r, original.r, highlighted.r), Scale(own.g, original.g, highlighted.g), Scale(own.b, original.b, highlighted.b), own.a);

    private static float Scale(float own, float original, float highlighted) =>
        original > 0f ? Mathf.Clamp01(own * highlighted / original) : Mathf.Clamp01(own + highlighted);
}
