using UnityEngine.AddressableAssets;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The game's own yellow warning sign, the "common_tablet_02" object every vanilla construction site stands in its
/// "prebuild" group. A site copies it from one vanilla site's prefab once the game is up, and goes without if the prefab
/// has moved, so a game update can only cost the sign.
/// </summary>
internal static class TimbnVanillaSign
{
    private const string _sitePrefab = "Assets/AddressableAssets/WGOs/circular_saw_place.prefab";
    private const string _signName = "common_tablet_02";

    private static GameObject? _source;
    private static bool _failed;

    internal static GameObject? Source()
    {
        if (_source != null || _failed)
            return _source;

        try
        {
            var site = Addressables.LoadAssetAsync<GameObject>(_sitePrefab).WaitForCompletion();
            _source = site == null ? null : site.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == _signName)?.gameObject;
        }
        catch (Exception exception)
        {
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnVanillaSign)}|Could not load the game's warning sign from {_sitePrefab}, so construction sites go without it. {exception.Message}");
        }

        if (_source == null)
        {
            _failed = true;
            TimbnCorePlugin.Logger.LogWarning($"{nameof(TimbnVanillaSign)}|The game's warning sign ({_signName}) was not found in {_sitePrefab}, so construction sites go without it.");
        }

        return _source;
    }

    // The sign stands just inside the front edge of the outline, a little off center, where a keeper would see it.
    internal static void AddTo(GameObject site, Vector3 size)
    {
        if (Source() is not { } source)
            return;

        var sign = UnityEngine.Object.Instantiate(source, site.transform, false);
        sign.name = _signName;
        sign.transform.localPosition = new Vector3(size.x * 0.25f, 0f, -size.z / 2f + 0.06f);
        sign.transform.localRotation = Quaternion.identity;
        sign.SetActive(true);
    }
}
