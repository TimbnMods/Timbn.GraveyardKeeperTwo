using LazyBearTechnology;
using System.Reflection;

namespace Timbn.GraveyardKeeperTwo.ZombieCustomizer;

internal static class ZombieCustomization
{
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _hairSwitcher = Switcher("hairSwitcher");
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _beardSwitcher = Switcher("beardSwitcher");
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _bodySwitcher = Switcher("bodySwitcher");
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _headColorSwitcher = Switcher("hedColorSwitch");
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _bodyColorSwitcher = Switcher("bdy1ColorSwitch");
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _secondBodyColorSwitcher = Switcher("bdy2ColorSwitch");
    private static readonly AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> _thirdBodyColorSwitcher = Switcher("bdy3ColorSwitch");

    private static readonly AccessTools.FieldRef<UICustomizationWindow, Sprite> _unavailableSprite =
        AccessTools.FieldRefAccess<UICustomizationWindow, Sprite>("nonInteractableSwitcherImage");

    private static readonly MethodInfo _setupZombieSkin = AccessTools.Method(typeof(WgoPart), "SetupZombieSkin");
    private static readonly MethodInfo _updateDropView = AccessTools.Method(typeof(DropView), "UpdateView");

    private const string _headRenderer = "hed";

    private static readonly List<(Material Material, Shader Shader)> _swappedShaders = [];
    private static readonly List<(SpriteRenderer Renderer, Color Color)> _previewColors = [];
    private static readonly Dictionary<int, Texture2D> _swatches = [];

    private static Session? _session;
    private static bool _loggedShaders;

    public static void Open(ZombieWgoData zombie, UIZombieWorkerWindowData returnTo)
    {
        var pool = ZombieSkinPool.Worker;
        if (pool == null || pool.Bodies.Count == 0 || pool.Heads.Count == 0)
        {
            Plugin.Logger.LogWarning("No zombie skin pool found, nothing to choose from.");
            return;
        }

        _session = new Session(zombie, pool) { ReturnTo = returnTo };
        var data = new UICustomizationWindowData { CurrentData = PlayerCustomizationData.Copy(MainGame.PlayerData.customization) };
        LazyUI.GetWindow<UICustomizationWindow>().Open(data);
    }

    public static void OnWindowOpened(UICustomizationWindow window)
    {
        var session = _session;
        if (session == null)
            return;

        var pool = session.Pool;
        Bind(window, _hairSwitcher(window), pool.Heads, session.Head, value => session.Head = value);
        Bind(window, _bodySwitcher(window), pool.Bodies, session.Body, value => session.Body = value);
        Bind(window, _headColorSwitcher(window), pool.HeadColors, session.HeadColor, value => session.HeadColor = value);
        BindTint(_bodyColorSwitcher(window), session);
        Disable(window, _beardSwitcher(window));
        Disable(window, _secondBodyColorSwitcher(window));
        Disable(window, _thirdBodyColorSwitcher(window));
        UseColorSwapShader(session.Zombie);
        ShowPreview(session);
    }

    public static bool TryApply(UICustomizationWindow window)
    {
        var session = _session;
        if (session == null)
            return false;

        var zombie = session.Zombie;
        ZombieSkinHelper.ApplySkinToZombieWgoData(zombie, session.Body, session.Head, session.BodyColor, session.HeadColor);
        ZombieTint.Save(zombie, session.Tint);
        Redraw(zombie);
        var view = GameScene.GetWgoViewGlobal(zombie.UniqueId);
        if (view != null)
            ZombieTint.Paint(view, session.Tint);

        Plugin.Logger.LogMessage($"Restyled {LLBase.L(zombie.Name)} to body {session.Body}, head {session.Head}, "
            + $"colors {Describe(session.BodyColor)} and {Describe(session.HeadColor)}, clothes tint {ZombieTint.Describe(session.Tint)}.");
        window.Close();
        return true;
    }

    public static void OnWindowClosed()
    {
        var returnTo = _session?.ReturnTo;
        RestorePreview();
        _session = null;
        if (returnTo != null)
            LazyUI.GetWindow<UIZombieWorkerWindow>().Open(returnTo);
    }

    public static void Stop()
    {
        if (_session == null)
            return;

        _session.ReturnTo = null;
        var window = LazyUI.GetWindow<UICustomizationWindow>();
        if (window != null && window.IsShown)
            window.Close();

        _session = null;
    }

    private static void Bind<TValue>(UICustomizationWindow window, UICharacterOptionSwitcher switcher, List<TValue> values, TValue current, Action<TValue> choose)
    {
        if (values.Count <= 1)
        {
            Disable(window, switcher);
            return;
        }

        var index = Math.Max(0, values.IndexOf(current));
        choose(values[index]);
        var labels = Enumerable.Range(1, values.Count).Select(i => $"{i}/{values.Count}").ToArray();
        switcher.IsInteractable = true;
        switcher.Initialize(i =>
        {
            choose(values[i]);
            if (_session != null)
                ShowPreview(_session);
        }, labels, index, loopNavigation: false);
        switcher.SetColorImage(null);
    }

    private static void BindTint(UICharacterOptionSwitcher switcher, Session session)
    {
        var palette = session.Palette;
        var labels = Enumerable.Range(1, palette.Count).Select(i => $"{i}/{palette.Count}").ToArray();
        var index = Math.Max(0, palette.FindIndex(color => ZombieTint.Same(color, session.Tint)));
        switcher.IsInteractable = true;
        switcher.Initialize(i =>
        {
            session.Tint = palette[i];
            switcher.SetColorImage(Swatch(i, palette[i]));
            ShowPreview(session);
        }, labels, index, loopNavigation: false);
        switcher.SetColorImage(Swatch(index, palette[index]));
    }

    private static Texture2D Swatch(int index, Color color)
    {
        if (_swatches.TryGetValue(index, out var swatch))
            return swatch;

        swatch = new Texture2D(1, 1);
        swatch.SetPixel(0, 0, color);
        swatch.Apply();
        _swatches[index] = swatch;
        return swatch;
    }

    private static void Disable(UICustomizationWindow window, UICharacterOptionSwitcher switcher)
    {
        switcher.IsInteractable = false;
        switcher.SetSprite(_unavailableSprite(window));
    }

    private static void ShowPreview(Session session)
    {
        var preview = ZombieSkinHelper.GetPresetForCustomizationData(
            ZombieSkinHelper.ZOMBIE_WORKER_DATA_ID, session.Body, session.Head, session.BodyColor, session.HeadColor);
        var character = MainGame.PlayerController.View.CustomizationCharacter;
        character.ChangeSkinPreset(preview);
        if (_previewColors.Count == 0)
        {
            foreach (var renderer in character.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (ZombieTint.IsBodyPart(renderer.name))
                    _previewColors.Add((renderer, renderer.color));
            }
        }

        ZombieTint.Paint(character, session.Tint);
    }

    private static void UseColorSwapShader(ZombieWgoData zombie)
    {
        LogColorSwapShaders();
        var shader = FindColorSwapShader(zombie);
        if (shader == null)
        {
            Plugin.Logger.LogWarning("Found no zombie head shader to borrow, so the preview shows hair in its raw magenta.");
            return;
        }

        foreach (var renderer in MainGame.PlayerController.View.CustomizationCharacter.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var material = renderer.material;
            if (material.shader == shader)
                continue;

            _swappedShaders.Add((material, material.shader));
            material.shader = shader;
        }

        Plugin.Logger.LogMessage($"Preview borrowed the {shader.name} shader for {_swappedShaders.Count} sprites.");
    }

    private static void RestorePreview()
    {
        foreach (var (material, shader) in _swappedShaders)
        {
            if (material != null)
                material.shader = shader;
        }

        foreach (var (renderer, color) in _previewColors)
        {
            if (renderer != null)
                renderer.color = color;
        }

        foreach (var swatch in _swatches.Values)
            UnityEngine.Object.Destroy(swatch);

        _swappedShaders.Clear();
        _previewColors.Clear();
        _swatches.Clear();
    }

    private static Shader? FindColorSwapShader(ZombieWgoData zombie)
    {
        var wgo = GameScene.GetWgoViewGlobal(zombie.UniqueId);
        var own = wgo == null ? null : HeadShader(wgo.GetComponentsInChildren<SpriteRenderer>(true));
        return own ?? HeadShader(Resources.FindObjectsOfTypeAll<SpriteRenderer>());
    }

    private static Shader? HeadShader(IEnumerable<SpriteRenderer> renderers) =>
        renderers
            .Where(r => r.name == _headRenderer && r.sharedMaterial != null && r.sharedMaterial.HasProperty(SkinChangerGK2.shaderPaletteLutId))
            .Select(r => r.sharedMaterial.shader)
            .FirstOrDefault();

    private static void LogColorSwapShaders()
    {
        if (_loggedShaders)
            return;

        _loggedShaders = true;
        var names = Resources.FindObjectsOfTypeAll<Shader>()
            .Where(s => s.FindPropertyIndex("_ReplaceLUT") >= 0)
            .Select(s => s.name)
            .Distinct();
        Plugin.Logger.LogMessage($"Loaded shaders with a color swap LUT: {string.Join(", ", names)}.");
    }

    private static void Redraw(ZombieWgoData zombie)
    {
        var wgo = GameScene.GetWgoViewGlobal(zombie.UniqueId);
        if (wgo == null)
        {
            if (MainGame.PlayerController.TryGetCurrentGameScene(out var scene) && scene.TryGetDropView(zombie.ZombieItem, out var dropView))
                _updateDropView.Invoke(dropView, []);

            return;
        }

        if (zombie.ZombieType == ZombieType.Fighter)
        {
            wgo.OnZombieItemsChanged(new Inventory(zombie.ZombieItem));
            return;
        }

        if (wgo.MainWgoPart == null)
            return;

        var definition = zombie.Definition;
        var visualId = definition == null ? string.Empty : definition.hasCustomVisualId ? definition.customVisualId : definition.id;
        _setupZombieSkin.Invoke(wgo.MainWgoPart, [zombie, visualId]);
    }

    private static string Describe(string color) => string.IsNullOrEmpty(color) ? "none" : color;

    private static AccessTools.FieldRef<UICustomizationWindow, UICharacterOptionSwitcher> Switcher(string field) =>
        AccessTools.FieldRefAccess<UICustomizationWindow, UICharacterOptionSwitcher>(field);

    private sealed class Session(ZombieWgoData zombie, ZombieSkinPool pool)
    {
        public ZombieWgoData Zombie { get; } = zombie;

        public ZombieSkinPool Pool { get; } = pool;

        public int Body { get; set; } = zombie.GetGameResInt("zombie_body_id");

        public int Head { get; set; } = zombie.GetGameResInt("zombie_head_id");

        public string BodyColor { get; set; } = zombie.GameResStr.Get("zombie_body_lut") ?? string.Empty;

        public string HeadColor { get; set; } = zombie.GameResStr.Get("zombie_head_lut") ?? string.Empty;

        public Color Tint { get; set; } = ZombieTint.Read(zombie);

        public List<Color> Palette { get; } = ZombieTint.Palette(ZombieTint.Read(zombie));

        public UIZombieWorkerWindowData? ReturnTo { get; set; }
    }
}
