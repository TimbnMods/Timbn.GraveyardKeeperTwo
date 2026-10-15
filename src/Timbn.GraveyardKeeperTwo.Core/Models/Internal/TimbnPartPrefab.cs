using LazyBearTechnology;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// The template the pool copies for one mod model, standing in for the addressable prefab a world object would
/// load. It is an inactive GameObject built in code (a bundle can't carry game scripts) with a WgoPart, the colliders
/// of its <see cref="TimbnPartShape"/>, and the model under it, plus the baked data the game reads for its footprint,
/// dock point and chunk bounds.
/// </summary>
internal sealed class TimbnPartPrefab : IDisposable
{
    private const string _keyPrefix = "Assets/AddressableAssets/WGOs/";
    private const string _keySuffix = ".prefab";

    private static readonly AccessTools.FieldRef<WgoPart, List<Collider>> _interactableColliders =
        AccessTools.FieldRefAccess<WgoPart, List<Collider>>("interactableColliders");
    private static readonly AccessTools.FieldRef<WgoPart, List<DockPoint>> _dockPoints =
        AccessTools.FieldRefAccess<WgoPart, List<DockPoint>>("dockPoints");
    private static readonly AccessTools.FieldRef<WgoPart, List<Object3D>> _cachedObjects3D =
        AccessTools.FieldRefAccess<WgoPart, List<Object3D>>("cachedObjects3D");
    private static readonly AccessTools.FieldRef<WgoPartBakedData, Dictionary<int, Rect>> _boundsRectCache =
        AccessTools.FieldRefAccess<WgoPartBakedData, Dictionary<int, Rect>>("variationCollisionBoundsRectDict");
    private static readonly AccessTools.FieldRef<WgoPartBakedData, Dictionary<int, List<DockPointData.Baked>>> _dockPointCache =
        AccessTools.FieldRefAccess<WgoPartBakedData, Dictionary<int, List<DockPointData.Baked>>>("variationDockPointsDict");
    private static readonly AccessTools.FieldRef<WgoPartBakedDataCollection, Dictionary<string, WgoPartBakedData>> _bakedDataCache =
        AccessTools.FieldRefAccess<WgoPartBakedDataCollection, Dictionary<string, WgoPartBakedData>>("cachedData");
    private static readonly AccessTools.FieldRef<WgoPartPool, Dictionary<string, Stack<WgoPart>>> _pooledParts =
        AccessTools.FieldRefAccess<WgoPartPool, Dictionary<string, Stack<WgoPart>>>("pools");
    private static readonly AccessTools.FieldRef<WgoPartPool, Dictionary<string, AsyncOperationHandle<GameObject>>> _loadedHandles =
        AccessTools.FieldRefAccess<WgoPartPool, Dictionary<string, AsyncOperationHandle<GameObject>>>("loadedHandles");
    private static readonly AccessTools.FieldRef<BuildArea, string> _buildAreaId = AccessTools.FieldRefAccess<BuildArea, string>("id");
    private static readonly AccessTools.FieldRef<BuildArea, Collider> _buildAreaCollider = AccessTools.FieldRefAccess<BuildArea, Collider>("collider");
    private static readonly AccessTools.FieldRef<Wgo, bool> _partsLoaded = AccessTools.FieldRefAccess<Wgo, bool>("wgoPartsLoaded");
    private static readonly Action<Wgo> _deInitVisualBindings = WgoMethod<Action<Wgo>>("DeInitVisualBindings");
    private static readonly Action<Wgo> _releasePartsToPool = WgoMethod<Action<Wgo>>("ReleaseWgoPartsToPool");
    private static readonly Action<Wgo, bool> _refreshVisuals = WgoMethod<Action<Wgo, bool>>("RefreshVisuals");
    private static readonly MaterialPropertyBlock _tintBlock = new();

    private readonly string _assetId;
    private readonly IReadOnlyList<TimbnPartPrefabSide> _sides;
    private readonly GameObject _template;
    private readonly Dictionary<TimbnFacing, GameObject> _sideRoots = [];
    private readonly DockPoint? _dockPoint;
    private readonly bool _zombieSpot;
    private readonly ConditionalDrawer? _drawer;
    private bool _hasVisual;

    internal TimbnPartPrefab(string assetId, IReadOnlyList<TimbnPartPrefabSide> sides, bool zombieSpot = false)
    {
        _assetId = assetId;
        _sides = sides;
        _zombieSpot = zombieSpot;
        Key = KeyFor(assetId);

        _template = new GameObject(assetId);
        _template.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(_template);
        var part = _template.AddComponent<WgoPart>();
        _cachedObjects3D(part) = [];
        _interactableColliders(part) = [];
        _dockPoints(part) = [];

        if (IsRotatable)
            AddSides(part);
        else
        {
            AddBoxes(sides[0].Shape, _template.transform, part);
            AddSlots(sides[0], _template.transform);
        }

        // The game finds the drawer on the part's own object and runs its rules whenever what they watch changes.
        if (sides.Any(side => side.Overlays.Count > 0))
            _drawer = _template.AddComponent<ConditionalDrawer>();

        if (sides[0].Shape.DockPoint is not { } spot)
            return;

        var dockPoint = new GameObject("DockPoint") { layer = TimbnPartShape.DockPointLayer };
        dockPoint.transform.SetParent(_template.transform, false);
        dockPoint.transform.localPosition = spot;
        var dockCollider = dockPoint.AddComponent<SphereCollider>();
        dockCollider.isTrigger = true;
        dockCollider.radius = sides[0].Shape.DockRadius;
        _dockPoint = dockPoint.AddComponent<DockPoint>();
        _dockPoint.direction = Direction.Up;
        _dockPoints(part).Add(_dockPoint);
    }

    internal string Key { get; }

    internal string AssetId => _assetId;

    // A part with more than one facing has a state per facing that the game's rotate key cycles through. The shared
    // dock point is moved to the active facing's edge when a state is applied.
    private bool IsRotatable => _sides.Count > 1;

    internal static string KeyFor(string assetId) => _keyPrefix + assetId + _keySuffix;

    internal AsyncOperationHandle<GameObject> CreateHandle()
    {
        if (!_hasVisual)
        {
            foreach (var side in _sides.Where(side => side.MirrorOf == null))
            {
                var parent = IsRotatable ? _sideRoots[side.Facing].transform : _template.transform;
                var visual = side.Model.Create(parent);
                foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);

                AddOverlays(side, visual, parent);
            }

            _hasVisual = true;
        }

        return Addressables.ResourceManager.CreateCompletedOperation(_template, null);
    }

    // Each extra model is a switched off sibling of the base model that its rule turns on while its condition holds. One
    // that replaces the base gets a second rule that turns the base off, the opposite way.
    private void AddOverlays(TimbnPartPrefabSide side, GameObject baseVisual, Transform parent)
    {
        foreach (var overlay in side.Overlays)
        {
            var visual = overlay.Model.Create(parent);
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);

            visual.transform.localPosition += TimbnLookRules.AlignmentOffset(overlay.Model.PixelCenter, side.Model.PixelCenter);
            visual.SetActive(false);
            _drawer!.Rules.Add(TimbnConditionalLooks.Rule(overlay.Condition, visual, forBase: false));
            if (overlay.ReplacesBase)
                _drawer.Rules.Add(TimbnConditionalLooks.Rule(overlay.Condition, baseVisual, forBase: true));
        }
    }

    internal void RegisterBakedData()
    {
        if (!TryGetBakedData(out var collection))
            return;

        var data = new WgoPartBakedData(_assetId);
        if (IsRotatable)
            RegisterSideData(data);
        else if (_sides[0].Shape.FootprintRect is { } rect)
        {
            var hash = WgoPartData.GetStateHash(null, -1);
            data.SetCollisionBoundsRect(rect, hash);
            // The constructor leaves these lookups as empty dictionaries that are never rebuilt, so the rect and
            // dock points read as nothing until they are nulled and lazily rebuilt from what was just set.
            _boundsRectCache(data) = null!;
            if (_dockPoint != null)
            {
                data.SetDockPointsData([new DockPointData.Baked(_dockPoint)], hash);
                _dockPointCache(data) = null!;
            }
        }

        if (_zombieSpot)
            BakeZombieSpots(data);

        var bounds = ChunkBounds();
        data.SetChunkBounds(new ChunkBoundsPair(bounds, bounds));
        collection.AddOrUpdate(data);
    }

    // Moves the shared dock point to the edge the part's active facing is on, since the game finds a dock point by its
    // index in the part and a second one per facing would be read as the wrong one.
    internal static void PlaceDock(WgoPart part)
    {
        if (part.WgoPartData == null || !TimbnModels.TryGetPart(part.PooledAddressableKey, out var prefab) || !prefab.IsRotatable)
            return;

        var side = prefab._sides.FirstOrDefault(candidate => candidate.RotationIndex == part.WgoPartData.rotationIndex);
        if (side?.Shape.DockPoint is { } spot && part.DockPoints.Count > 0)
            part.DockPoints[0].transform.localPosition = spot;
    }

    // Each drawn facing gets a child with its own colliders and model, and a state the game switches on. A mirrored
    // facing points its state at the drawn facing's child and lets the game flip it.
    private void AddSides(WgoPart part)
    {
        foreach (var side in _sides.Where(side => side.MirrorOf == null))
        {
            var root = new GameObject($"Side{side.RotationIndex}");
            root.transform.SetParent(_template.transform, false);
            root.SetActive(side.Facing == TimbnFacing.Down);
            AddBoxes(side.Shape, root.transform, part);
            AddSlots(side, root.transform);
            _sideRoots[side.Facing] = root;
        }

        foreach (var side in _sides)
        {
            part.Variations.Add(new WgoPartState
            {
                variationId = "",
                rotationIndex = side.RotationIndex,
                gameObject = _sideRoots[side.MirrorOf ?? side.Facing],
                mirror = side.MirrorOf != null,
                isDefault = side.Facing == TimbnFacing.Down,
                customEvents = [],
            });
        }
    }

    private void RegisterSideData(WgoPartBakedData data)
    {
        foreach (var side in _sides)
        {
            var hash = WgoPartData.GetStateHash("", side.RotationIndex);
            if (side.Shape.FootprintRect is { } rect)
                data.SetCollisionBoundsRect(rect, hash);

            // The game flips a mirrored state's baked dock position itself, so it is baked as the flipped facing's own.
            var dockShape = side.MirrorOf is { } source ? _sides.First(candidate => candidate.Facing == source).Shape : side.Shape;
            if (_dockPoint != null && dockShape.DockPoint is { } spot)
            {
                // A baked dock point is read off the live one, so it is moved to that edge for the read.
                _dockPoint.transform.localPosition = spot;
                data.SetDockPointsData([new DockPointData.Baked(_dockPoint)], hash);
            }
        }

        if (_dockPoint != null && _sides[0].Shape.DockPoint is { } first)
            _dockPoint.transform.localPosition = first;

        // The constructor leaves these lookups as empty dictionaries that are never rebuilt, so they are nulled to be
        // rebuilt from everything just set.
        _boundsRectCache(data) = null!;
        _dockPointCache(data) = null!;
    }

    // The game's station handlers seat a zombie at the GD point of this id, so each facing gets one at its dock spot. A
    // new world object registers its points before its facing is set, under the unset state, so the facing every station
    // has also answers to that.
    private void BakeZombieSpots(WgoPartBakedData data)
    {
        foreach (var side in _sides)
        {
            if (side.Shape.DockPoint is not { } own)
                continue;

            var source = side.MirrorOf is { } flipped ? _sides.First(candidate => candidate.Facing == flipped).Shape.DockPoint : null;
            var spot = new WgoPartBakedData.GDPointBakedData
            {
                id = TimbnZombieSpotRules.PointId,
                customTag = "",
                direction = TimbnZombieSpotRules.DirectionFor(side.Facing),
                localPosition = TimbnZombieSpotRules.LocalPosition(own, source, TimbnZombieSpotRules.GameFlipsMirroredPoints),
                transitToGdPointId = "",
                worldIdToTransit = "",
                enabled = true,
            };
            var hash = IsRotatable ? WgoPartData.GetStateHash("", side.RotationIndex) : WgoPartData.GetStateHash(null, -1);
            data.SetGDPointsBakedData([spot], hash);
            if (IsRotatable && side.Facing == TimbnFacing.Down)
                data.SetGDPointsBakedData([spot], WgoPartData.GetStateHash(null, -1));
        }
    }

    private Bounds ChunkBounds()
    {
        var bounds = _sides[0].Shape.ChunkBounds(_sides[0].Model.LocalBounds);
        foreach (var side in _sides.Skip(1))
            bounds.Encapsulate(side.Shape.ChunkBounds(side.Model.LocalBounds));

        return bounds;
    }

    private void AddBoxes(TimbnPartShape shape, Transform parent, WgoPart part)
    {
        foreach (var box in shape.Boxes)
        {
            var collider = AddBox(box, parent);
            if (box.Layer == TimbnPartShape.InteractLayer)
                _interactableColliders(part).Add(collider);
        }
    }

    // Each slot is one of the game's own BuildAreas, a flat trigger on the build area layer, grouped the way the conveyor
    // furnace groups its own. Under a facing's root it only exists while that facing shows, and a mirrored facing flips it.
    // Shown as a buff area, it stays out of the station's footprint while the station is placed and draws as a blue square.
    private static void AddSlots(TimbnPartPrefabSide side, Transform parent)
    {
        if (side.Slots.Count == 0)
            return;

        var group = new GameObject("BuildAreas");
        group.transform.SetParent(parent, false);
        foreach (var slot in side.Slots)
        {
            var child = new GameObject(slot.Id) { layer = TimbnPartShape.BuildAreaLayer };
            child.transform.SetParent(group.transform, false);
            child.transform.localPosition = new Vector3(slot.Position.x, 0f, slot.Position.z);
            var collider = child.AddComponent<BoxCollider>();
            collider.size = new Vector3(slot.Size.x, TimbnSlotRules.AreaHeight, slot.Size.z);
            collider.isTrigger = true;
            var area = child.AddComponent<BuildArea>();
            _buildAreaId(area) = slot.Id;
            _buildAreaCollider(area) = collider;
            area.foprceShowAsBuffAreaForPointerPlacement = true;
        }
    }

    internal static void Redraw(Wgo wgo)
    {
        if (wgo != null)
            _refreshVisuals(wgo, false);
    }

    internal static void Tint(WgoPart part, Color color, float amount)
    {
        foreach (var renderer in part.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is not (MeshRenderer or SkinnedMeshRenderer))
                continue;

            renderer.GetPropertyBlock(_tintBlock);
            _tintBlock.SetColor(Object3DMesh.matIdSelectionTintColor, color);
            _tintBlock.SetFloat(Object3DMesh.matIdSelectionTintAmount, amount);
            renderer.SetPropertyBlock(_tintBlock);
        }
    }

    internal List<Wgo> ReleaseFromWorld()
    {
        List<Wgo> released = [];
        if (!TimbnGame.IsInGame)
            return released;

        foreach (var wgo in UnityEngine.Object.FindObjectsByType<Wgo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!_partsLoaded(wgo) || !Draws(wgo))
                continue;

            _deInitVisualBindings(wgo);
            _releasePartsToPool(wgo);
            _partsLoaded(wgo) = false;
            released.Add(wgo);
        }

        return released;
    }

    public void Dispose()
    {
        if (TryGetBakedData(out var collection))
            _bakedDataCache(collection).Remove(_assetId);

        if (TimbnGame.IsInGame)
        {
            var pool = LazySingleton<WgoPartPool>.Instance;
            if (_pooledParts(pool).Remove(Key, out var parts))
            {
                foreach (var part in parts)
                {
                    if (part != null)
                        UnityEngine.Object.Destroy(part.gameObject);
                }
            }

            if (_loadedHandles(pool).Remove(Key, out var handle) && handle.IsValid())
                Addressables.Release(handle);
        }

        UnityEngine.Object.Destroy(_template);
    }

    private bool Draws(Wgo wgo) =>
        wgo.MainWgoPart?.PooledAddressableKey == Key || wgo.AdditionalWgoParts.Any(part => part != null && part.PooledAddressableKey == Key);

    private static TDelegate WgoMethod<TDelegate>(string name) where TDelegate : Delegate =>
        AccessTools.MethodDelegate<TDelegate>(AccessTools.Method(typeof(Wgo), name));

    private static Collider AddBox(TimbnPartBox box, Transform parent)
    {
        var child = new GameObject(box.Name) { layer = box.Layer };
        child.transform.SetParent(parent, false);
        var collider = child.AddComponent<BoxCollider>();
        collider.center = box.Center;
        collider.size = box.Size;
        collider.isTrigger = box.IsTrigger;
        return collider;
    }

    private static bool TryGetBakedData(out WgoPartBakedDataCollection collection)
    {
        // Addressables may not be up yet when a plugin registers at startup, and the balance load registers again.
        try
        {
            collection = LazySingletonSerializedSO<WgoPartBakedDataCollection>.Instance;
        }
        catch (Exception)
        {
            collection = null!;
        }

        return collection != null;
    }
}
