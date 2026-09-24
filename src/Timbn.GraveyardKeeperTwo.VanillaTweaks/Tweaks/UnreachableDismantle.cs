namespace Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

internal sealed class UnreachableDismantle
{
    private DockPoint? _standIn;
    private WgoData? _station;

    public static UnreachableDismantle? Active { get; private set; }

    public void Apply() => Active = this;

    public void Revert()
    {
        Active = null;
        if (_standIn != null)
            UnityEngine.Object.Destroy(_standIn.gameObject);

        _standIn = null;
        _station = null;
    }

    public DockPoint? StandInFor(PlayerWorkComponent work, Wgo station, Transform player, Vector2 facing)
    {
        if (station == null || station.MainWgoPart == null || !IsMarkedForDismantle(station.Data) || !work.CanWorkOn(station.Data))
            return null;

        if (_station != station.Data)
            Plugin.Logger.LogInfo($"Working {station.Data.id} from where you stand, since none of its work spots can be reached.");

        _station = station.Data;
        var standIn = GetStandIn();
        standIn.Init(station.MainWgoPart);
        standIn.direction = facing.ConvertFromVector2();
        standIn.transform.position = player.position;
        return standIn;
    }

    public bool TryGetDropPosition(WgoData data, out Vector3 position)
    {
        position = MainGame.PlayerData.position.Value;
        return data == _station && IsMarkedForDismantle(data);
    }

    private static bool IsMarkedForDismantle(WgoData? data) => data?.CraftComponent?.IsDestroyingCraftActive == true;

    private DockPoint GetStandIn()
    {
        if (_standIn != null)
            return _standIn;

        var host = new GameObject("VanillaTweaks stand in dock point") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.Object.DontDestroyOnLoad(host);
        _standIn = host.AddComponent<DockPoint>();
        return _standIn;
    }
}
