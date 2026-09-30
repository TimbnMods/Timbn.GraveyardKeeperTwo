using LazyBearTechnology;
using UnityEngine.UI;

namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

internal sealed class RestyleButton(ZombieCustomization customization)
{
    public const string TooltipKey = "timbn_zombie_restyle";

    private const string _name = "TimbnRestyleButton";

    private static readonly AccessTools.FieldRef<UIZombieWorkerWindow, UIWorkerIcon> _workerIcon =
        AccessTools.FieldRefAccess<UIZombieWorkerWindow, UIWorkerIcon>("workerIcon");

    private readonly ZombieCustomization _customization = customization;
    private UIZombieWorkerWindowData? _data;

    public void Attach(UIZombieWorkerWindow window, UIZombieWorkerWindowData data)
    {
        _data = data;
        var icon = _workerIcon(window);
        if (icon == null || icon.transform.Find(_name) != null)
            return;

        var button = new GameObject(_name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)button.transform;
        rect.SetParent(icon.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        var image = button.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        button.GetComponent<Button>().onClick.AddListener(OnClick);
        UIMouseTooltip.Attach(button, TooltipKey);
    }

    public void Remove()
    {
        _data = null;
        var window = LazyUI.GetWindow<UIZombieWorkerWindow>();
        var icon = window == null ? null : _workerIcon(window);
        var button = icon == null ? null : icon.transform.Find(_name);
        if (button != null)
            UnityEngine.Object.Destroy(button.gameObject);
    }

    private void OnClick()
    {
        var data = _data;
        if (data?.ZombieWgoData == null)
            return;

        LazyUI.GetWindow<UIZombieWorkerWindow>().Close();
        _customization.Open(data.ZombieWgoData, data);
    }
}
