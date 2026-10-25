using System.Numerics;
using Content.Server.Decals;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._Stories.Blood;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Blood;

public sealed class STBloodTracksSystem : EntitySystem
{
    private const string BareLeftDecal = "STDecalBloodFootprintBareLeft";
    private const string BareRightDecal = "STDecalBloodFootprintBareRight";
    private const string ShoesDecal = "STDecalBloodFootprintShoes";

    private static readonly string[] DragDecals =
    [
        "STDecalBloodDrag1",
        "STDecalBloodDrag2",
        "STDecalBloodDrag3",
        "STDecalBloodDrag4",
        "STDecalBloodDrag5",
    ];

    private static readonly Angle TrackRotationOffset = Angle.FromDegrees(180);
    private const float BleedingDragAlpha = 0.8f;
    private const float MaxTrackJump = 2f;

    private const float WetSplatterVolume = 10f;
    private const int WetSplatterCharges = 2;
    private const float PuddlePickupVolume = 2f;
    private const float SplatterStainRange = 1f;

    private static readonly Vector2 DecalCenterOffset = new(0.5f, 0.5f);
    private static readonly TimeSpan SplatterDryTime = TimeSpan.FromSeconds(60);

    private static readonly Dictionary<EntProtoId, Color> SplatterSpawners = new()
    {
        ["RMCDecalSpawnerBloodSplatters"] = Color.FromHex("#980002"),
        ["RMCDecalSpawnerXenoSplatters"] = Color.FromHex("#BED700"),
        ["RMCDecalSpawnerAcidBloodSplash"] = Color.FromHex("#BED700"),
    };

    [Dependency] private readonly STBloodStainSystem _bloodStain = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<PuddleComponent> _puddleQuery;

    private readonly Dictionary<EntityUid, Dictionary<Vector2i, WetSplatter>> _wetSplatters = new();
    private readonly List<Vector2i> _driedTiles = new();
    private readonly List<EntityUid> _deletedGrids = new();
    private readonly List<(ReagentId Reagent, FixedPoint2 Quantity)> _puddleBlood = new();

    private record struct WetSplatter(Color Color, TimeSpan DriesAt, int Charges);

    public override void Initialize()
    {
        base.Initialize();

        _gridQuery = GetEntityQuery<MapGridComponent>();
        _puddleQuery = GetEntityQuery<PuddleComponent>();

        SubscribeLocalEvent<STBloodyComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<RandomDecalSpawnerComponent, ComponentStartup>(OnDecalSpawnerStartup);
    }

    private void OnDecalSpawnerStartup(Entity<RandomDecalSpawnerComponent> ent, ref ComponentStartup args)
    {
        if (MetaData(ent).EntityPrototype?.ID is not { } protoId ||
            !SplatterSpawners.TryGetValue(protoId, out var color))
        {
            return;
        }

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid ||
            !_map.IsInitialized(xform.MapUid) ||
            !_gridQuery.TryComp(gridUid, out var grid))
        {
            return;
        }

        PruneDeletedGrids();

        var tiles = GetWetTiles(gridUid);
        var center = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var radius = (int) MathF.Ceiling(ent.Comp.Radius);
        var driesAt = _timing.CurTime + SplatterDryTime;
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                tiles[center + new Vector2i(x, y)] = new WetSplatter(color, driesAt, WetSplatterCharges);
            }
        }

        foreach (var bloody in _lookup.GetEntitiesInRange<STBloodyComponent>(_transform.GetMapCoordinates(ent, xform), SplatterStainRange))
        {
            _bloodStain.StainWearer(bloody.AsNullable(), STBloodStainParts.Body, color);
        }
    }

    public void MarkWet(EntityUid gridUid, Vector2i tile, Color color)
    {
        PruneDeletedGrids();
        GetWetTiles(gridUid)[tile] = new WetSplatter(color, _timing.CurTime + SplatterDryTime, WetSplatterCharges);
    }

    private Dictionary<Vector2i, WetSplatter> GetWetTiles(EntityUid gridUid)
    {
        if (!_wetSplatters.TryGetValue(gridUid, out var tiles))
        {
            tiles = new Dictionary<Vector2i, WetSplatter>();
            _wetSplatters[gridUid] = tiles;
        }

        PruneDried(tiles);
        return tiles;
    }

    private void PruneDeletedGrids()
    {
        _deletedGrids.Clear();
        foreach (var grid in _wetSplatters.Keys)
        {
            if (TerminatingOrDeleted(grid))
                _deletedGrids.Add(grid);
        }

        foreach (var grid in _deletedGrids)
        {
            _wetSplatters.Remove(grid);
        }
    }

    private void PruneDried(Dictionary<Vector2i, WetSplatter> tiles)
    {
        var now = _timing.CurTime;
        _driedTiles.Clear();
        foreach (var (tile, splatter) in tiles)
        {
            if (splatter.DriesAt <= now)
                _driedTiles.Add(tile);
        }

        foreach (var tile in _driedTiles)
        {
            tiles.Remove(tile);
        }
    }

    private bool TryGetWetSplatter(EntityUid gridUid, Vector2i tile, out Color color)
    {
        color = default;
        if (!_wetSplatters.TryGetValue(gridUid, out var tiles) ||
            !tiles.TryGetValue(tile, out var splatter))
        {
            return false;
        }

        if (splatter.DriesAt <= _timing.CurTime || splatter.Charges <= 0)
        {
            tiles.Remove(tile);
            return false;
        }

        if (splatter.Charges <= 1)
            tiles.Remove(tile);
        else
            tiles[tile] = splatter with { Charges = splatter.Charges - 1 };

        color = splatter.Color;
        return true;
    }

    private void OnMove(Entity<STBloodyComponent> ent, ref MoveEvent args)
    {
        if (!ent.Comp.LeavesTracks)
            return;

        var xform = args.Component;
        if (xform.GridUid is not { } gridUid ||
            xform.ParentUid != gridUid ||
            !_gridQuery.TryComp(gridUid, out var grid))
        {
            ent.Comp.TrackGrid = null;
            return;
        }

        var position = xform.LocalPosition;
        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        if (ent.Comp.TrackGrid != gridUid)
        {
            ent.Comp.TrackGrid = gridUid;
            ent.Comp.LastTile = tile;
            ent.Comp.LastTrackPosition = position;
            TryPickUpBlood(ent, gridUid, grid, tile, position);
            return;
        }

        if (tile != ent.Comp.LastTile)
        {
            ent.Comp.LastTile = tile;
            TryPickUpBlood(ent, gridUid, grid, tile, position);
        }

        var delta = position - ent.Comp.LastTrackPosition;
        if (delta.Length() > MaxTrackJump)
        {
            ent.Comp.LastTrackPosition = position;
            return;
        }

        if (IsDragged(ent))
            TryLeaveDragTrail(ent, gridUid, position, delta);
        else if (CanLeaveFootprints(ent))
            TryLeaveFootprint(ent, gridUid, position, delta);
        else
            ent.Comp.LastTrackPosition = position;
    }

    private void TryPickUpBlood(Entity<STBloodyComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile, Vector2 position)
    {
        if (!TryGetPuddleBlood(ent, gridUid, grid, tile, out var volume, out var color))
        {
            if (!TryGetWetSplatter(gridUid, tile, out color))
                return;

            volume = WetSplatterVolume;
        }

        var steps = Math.Clamp((int) MathF.Ceiling(volume / ent.Comp.VolumePerStep), 1, ent.Comp.MaxSteps);
        if (_standing.IsDown(ent) || !_mobState.IsAlive(ent))
        {
            ent.Comp.DragSteps = Math.Max(ent.Comp.DragSteps, ent.Comp.MaxDragSteps);
            ent.Comp.TrackColor = color;
            _bloodStain.StainWearer(ent.AsNullable(), STBloodStainParts.Body, color);
            return;
        }

        if (ent.Comp.Steps <= 0)
            ent.Comp.LastTrackPosition = position;

        ent.Comp.Steps = Math.Max(ent.Comp.Steps, steps);
        ent.Comp.TrackColor = color;
        _bloodStain.StainWearer(ent.AsNullable(), STBloodStainParts.Feet, color);
    }

    private bool TryGetPuddleBlood(Entity<STBloodyComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile, out float volume, out Color color)
    {
        volume = 0f;
        color = default;

        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (!_puddleQuery.TryComp(uid, out var puddle) ||
                !_solution.ResolveSolution(uid.Value, puddle.SolutionName, ref puddle.Solution, out var solution))
            {
                continue;
            }

            _puddleBlood.Clear();
            var puddleVolume = 0f;
            var largest = 0f;
            foreach (var reagent in solution.Contents)
            {
                if (!ent.Comp.BloodReagents.Contains(reagent.Reagent.Prototype))
                    continue;

                _puddleBlood.Add((reagent.Reagent, reagent.Quantity));
                var quantity = reagent.Quantity.Float();
                puddleVolume += quantity;
                if (quantity <= largest ||
                    !_reagent.TryIndex(reagent.Reagent, out var proto))
                {
                    continue;
                }

                largest = quantity;
                color = proto.SubstanceColor;
            }

            if (puddleVolume < ent.Comp.MinPuddleVolume)
                continue;

            var drained = Math.Min(PuddlePickupVolume, puddleVolume) / puddleVolume;
            foreach (var (reagent, quantity) in _puddleBlood)
            {
                _solution.RemoveReagent(puddle.Solution.Value, reagent, quantity * drained);
            }

            volume = puddleVolume;
            return true;
        }

        return false;
    }

    private bool IsDragged(EntityUid uid)
    {
        return TryComp<PullableComponent>(uid, out var pullable) &&
               pullable.BeingPulled &&
               (_standing.IsDown(uid) || !_mobState.IsAlive(uid));
    }

    private bool CanLeaveFootprints(Entity<STBloodyComponent> ent)
    {
        return ent.Comp.Steps > 0 && !_standing.IsDown(ent) && _mobState.IsAlive(ent);
    }

    private void TryLeaveDragTrail(Entity<STBloodyComponent> ent, EntityUid gridUid, Vector2 position, Vector2 delta)
    {
        var bleeding = TryComp<BloodstreamComponent>(ent, out var bloodstream) && bloodstream.BleedAmount > 0;
        if (!bleeding && ent.Comp.DragSteps <= 0)
        {
            ent.Comp.LastTrackPosition = position;
            return;
        }

        if (delta.Length() < ent.Comp.DragDistance)
            return;

        Color color;
        if (bleeding && _bloodStain.TryGetBloodColor(ent, out var bloodColor))
        {
            color = bloodColor.WithAlpha(BleedingDragAlpha);
        }
        else
        {
            color = ent.Comp.TrackColor.WithAlpha(GetTrackAlpha(ent.Comp, ent.Comp.DragSteps, ent.Comp.MaxDragSteps));
            ent.Comp.DragSteps--;
        }

        var center = ent.Comp.LastTrackPosition + delta / 2;
        AddTrackDecal(_random.Pick(DragDecals), gridUid, center, color, delta.ToWorldAngle());
        ent.Comp.LastTrackPosition = position;
    }

    private void TryLeaveFootprint(Entity<STBloodyComponent> ent, EntityUid gridUid, Vector2 position, Vector2 delta)
    {
        if (delta.Length() < ent.Comp.StepDistance)
            return;

        var direction = Vector2.Normalize(delta);
        var side = new Vector2(-direction.Y, direction.X) * ent.Comp.StepSideOffset;
        if (ent.Comp.RightFoot)
            side = -side;

        var color = ent.Comp.TrackColor.WithAlpha(GetTrackAlpha(ent.Comp, ent.Comp.Steps, ent.Comp.MaxSteps));
        string decal;
        if (_inventory.TryGetSlotEntity(ent, STBloodStainSystem.ShoesSlot, out _))
            decal = ShoesDecal;
        else
            decal = ent.Comp.RightFoot ? BareRightDecal : BareLeftDecal;

        AddTrackDecal(decal, gridUid, position + side, color, delta.ToWorldAngle());

        ent.Comp.RightFoot = !ent.Comp.RightFoot;
        ent.Comp.Steps--;
        ent.Comp.LastTrackPosition = position;
    }

    private void AddTrackDecal(string decal, EntityUid gridUid, Vector2 center, Color color, Angle rotation)
    {
        var coordinates = new EntityCoordinates(gridUid, center - DecalCenterOffset);
        _decals.TryAddDecal(decal, coordinates, out _, color, rotation + TrackRotationOffset, cleanable: true);
    }

    private static float GetTrackAlpha(STBloodyComponent comp, int remaining, int max)
    {
        var fraction = max <= 0 ? 0f : Math.Clamp((float) remaining / max, 0f, 1f);
        return comp.MinTrackAlpha + (comp.MaxTrackAlpha - comp.MinTrackAlpha) * fraction;
    }
}
