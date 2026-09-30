namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

/// <summary>Finds the campfire the player stands by from the colliders around them.</summary>
internal sealed class Campfires
{
    private const string _fireName = "light_source_bonfire_w_logs_m";
    private const string _obstacleLayer = "Obstacle";
    private static readonly HashSet<string> _homeZones = ["garden", "inquisitions_base"];

    private readonly Collider[] _hits = new Collider[32];
    private int _mask;

    public static bool IsAtHome => TimbnPlayer.ZoneId is { } zone && _homeZones.Contains(zone);

    public bool TryFindNear(Vector3 player, out Vector3 fire)
    {
        fire = default;
        if (PluginConfig.HomeOnly.Value && !IsAtHome)
            return false;

        if (_mask == 0)
        {
            var layer = LayerMask.NameToLayer(_obstacleLayer);
            _mask = layer >= 0 ? 1 << layer : Physics.AllLayers;
        }

        var count = Physics.OverlapSphereNonAlloc(player, PluginConfig.Radius.Value, _hits, _mask, QueryTriggerInteraction.Collide);
        var best = float.MaxValue;
        var found = false;
        for (var i = 0; i < count; i++)
        {
            var root = FireOf(_hits[i].transform);
            if (root == null)
                continue;

            var distance = (_hits[i].ClosestPoint(player) - player).sqrMagnitude;
            if (distance >= best)
                continue;

            best = distance;
            fire = root.position;
            found = true;
        }

        return found;
    }

    private static Transform? FireOf(Transform hit)
    {
        Transform? fire = null;
        for (var current = hit; current != null; current = current.parent)
        {
            if (current.name.StartsWith(_fireName))
                fire = current;
        }

        return fire;
    }
}
