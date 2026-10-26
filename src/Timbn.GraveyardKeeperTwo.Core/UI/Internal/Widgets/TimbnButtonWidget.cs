using LazyBearTechnology;
using TMPro;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>The game's red button with its text, as wide as the text needs.</summary>
internal sealed class TimbnButtonWidget : TimbnWidget
{
    public TimbnButtonWidget(GameObject root, string label, Action onClick)
        : base((RectTransform)root.transform, root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault())
    {
        var button = root.GetComponent<LazyButton>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick());
        button.SetCallbacksIntoGamepadNavigationItem();
        MarkSelect(Root);
        if (Label != null)
            Label.text = label;

        Trim(Root);
    }

    // The button sizes itself with its own fitters, which would hold its root at no width inside a layout. Its content is
    // measured once and handed up as the button's size, then the fitters go and the art fills whatever width the
    // button is given.
    internal static void Trim(RectTransform root)
    {
        if (root.Find("Content") is not RectTransform content)
            return;

        foreach (var fitted in Enumerable.Reverse(content.GetComponentsInChildren<RectTransform>(true)))
            LayoutRebuilder.ForceRebuildLayoutImmediate(fitted);

        TimbnUi.Fixed(root, content.rect.width, TimbnWidgets.ButtonArt);
        TimbnUi.TrimArt(root, TimbnTheme.RedButtonEdge);
        foreach (var fitter in root.GetComponentsInChildren<ContentSizeFitter>(true))
            UnityEngine.Object.DestroyImmediate(fitter);

        UnityEngine.Object.DestroyImmediate(root.GetComponent<HorizontalLayoutGroup>());
        UnityEngine.Object.DestroyImmediate(content.GetComponent<HorizontalLayoutGroup>());
        // The art is drawn for a box as tall as a setting row, so it reaches above and below the box by as much.
        var bleed = (TimbnWidgets.RowHeight - TimbnWidgets.ButtonArt) / 2f;
        TimbnUi.Stretch(content, TimbnTheme.RedButtonEdge, bleed, TimbnTheme.RedButtonEdge, bleed);
        if (content.Find("Back") is RectTransform back)
            TimbnUi.Stretch(back);
    }
}
