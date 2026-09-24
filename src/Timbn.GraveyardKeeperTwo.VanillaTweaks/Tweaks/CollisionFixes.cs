using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class CollisionFixes
{
    private sealed class CollisionFix(string scene, string path, Vector3 shipped, Vector3 offset, string where)
    {
        public string Scene { get; } = scene;

        public string Path { get; } = path;

        public Vector3 Shipped { get; } = shipped;

        public Vector3 Offset { get; } = offset;

        public string Where { get; } = where;
    }

    private static readonly CollisionFix[] _fixes =
    [
        new(
            "RuinedTemple",
            "World/StaticContent/TownZone/PortArea/StonePier/port_area_stone_pier/SmallLadderCollider",
            new Vector3(6.67f, -1.787f, 1.8f),
            new Vector3(0.14f, 0.055f, 0f),
            "the pier stairs at the very right in the Port Area"),
    ];

    private readonly List<(Transform Target, Vector3 Offset)> _applied = [];

    public void Subscribe(TimbnPluginEvents events) =>
        events.StaticEvent<Scene, LoadSceneMode>(
            OnSceneLoaded,
            h => SceneManager.sceneLoaded += AsUnityAction(h),
            h => SceneManager.sceneLoaded -= AsUnityAction(h));

    public void ApplyToLoadedScenes()
    {
        for (var i = 0; i < SceneManager.sceneCount; i++)
            Apply(SceneManager.GetSceneAt(i));
    }

    public void Revert()
    {
        foreach (var (target, offset) in _applied)
        {
            if (target != null)
                target.localPosition -= offset;
        }

        _applied.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    private void Apply(Scene scene)
    {
        if (!scene.isLoaded)
            return;

        _applied.RemoveAll(applied => applied.Target == null);
        foreach (var fix in _fixes)
        {
            if (fix.Scene != scene.name)
                continue;

            var target = Find(scene, fix.Path);
            if (target == null || (target.localPosition - fix.Shipped).sqrMagnitude > 0.000001f)
                continue;

            target.localPosition += fix.Offset;
            _applied.Add((target, fix.Offset));
            Plugin.Logger.LogInfo($"Fixed the ground at {fix.Where} so you can walk there.");
        }
    }

    private static Transform? Find(Scene scene, string path)
    {
        var slash = path.IndexOf('/');
        var rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != rootName)
                continue;

            var found = slash < 0 ? root.transform : root.transform.Find(path[(slash + 1)..]);
            if (found != null)
                return found;
        }

        return null;
    }

    private static UnityAction<Scene, LoadSceneMode> AsUnityAction(Action<Scene, LoadSceneMode> handler) =>
        (UnityAction<Scene, LoadSceneMode>)Delegate.CreateDelegate(typeof(UnityAction<Scene, LoadSceneMode>), handler.Target, handler.Method);
}
