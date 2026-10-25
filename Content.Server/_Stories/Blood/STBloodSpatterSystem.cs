using System.Numerics;
using Content.Server._RMC14.Decals;
using Content.Server.Decals;
using Content.Shared._Stories.Blood;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Decals;
using Content.Shared.Item;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Stories.Blood;

public sealed class STBloodSpatterSystem : EntitySystem
{
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";

    private const float BaseChance = 0.25f;
    private const float ChancePerDamage = 0.02f;
    private const float SplashChance = 0.33f;
    private const float HeadChance = 0.25f;
    private const float MeleeContactRange = 1.5f;
    private const float MeleeFloorChance = 0.33f;
    private const float RangedFloorChance = 0.15f;
    private const float FloorSplatterShift = 0.3f;
    private const int MaxFloorSplattersPerTile = 3;

    private static readonly ProtoId<DecalPrototype>[] FloorSplatterDecals =
    [
        "STDecalBloodSplatter1",
        "STDecalBloodSplatter2",
        "STDecalBloodSplatter3",
        "STDecalBloodSplatter4",
        "STDecalBloodSplatter5",
        "STDecalBloodSplatter6",
        "STDecalBloodSplatter7",
    ];

    private static readonly EntProtoId SplatterEffect = "STBloodSplatterEffect";
    private static readonly Vector2 DecalCenterOffset = new(0.5f, 0.5f);

    [Dependency] private readonly STBloodStainSystem _bloodStain = default!;
    [Dependency] private readonly STBloodTracksSystem _bloodTracks = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RMCDecalSystem _rmcDecal = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STBloodyComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<STBloodyComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.DamageDelta is not { } delta ||
            !_prototype.TryIndex(BruteGroup, out var bruteGroup) ||
            !delta.TryGetDamageInGroup(bruteGroup, out var brute) ||
            brute <= 0 ||
            !_bloodStain.TryGetBloodColor(ent, out var color))
        {
            return;
        }

        var melee = args.Tool is { } tool && HasComp<MeleeWeaponComponent>(tool);
        var direction = GetHitDirection(ent, args.Origin);
        SpawnSplatterEffect(ent, color, direction);

        if (_random.Prob(melee ? MeleeFloorChance : RangedFloorChance))
            AddFloorSplatter(ent, color, direction);

        var chance = Math.Clamp(BaseChance + ChancePerDamage * brute.Float(), 0f, 1f);
        if (!_random.Prob(chance))
            return;

        if (melee && args.Tool != args.Origin && HasComp<ItemComponent>(args.Tool))
            _bloodStain.TryStain(args.Tool!.Value, color);

        if (!_random.Prob(SplashChance))
            return;

        var parts = _random.Prob(HeadChance) ? STBloodStainParts.Head : STBloodStainParts.Body;
        _bloodStain.StainWearer(ent.AsNullable(), parts, color);

        if (melee &&
            args.Origin is { } origin &&
            origin != ent.Owner &&
            _transform.InRange(Transform(origin).Coordinates, Transform(ent).Coordinates, MeleeContactRange))
        {
            _bloodStain.StainWearer(origin, STBloodStainParts.Body | STBloodStainParts.Hands, color);
        }
    }

    private Angle GetHitDirection(EntityUid target, EntityUid? origin)
    {
        if (origin is { } source && source != target && !TerminatingOrDeleted(source))
        {
            var delta = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(source);
            if (delta.LengthSquared() > 0.0001f)
                return delta.ToWorldAngle();
        }

        return _random.NextAngle();
    }

    private void SpawnSplatterEffect(EntityUid target, Color color, Angle direction)
    {
        var effect = Spawn(SplatterEffect, _transform.GetMapCoordinates(target));
        var comp = EnsureComp<STBloodSplatterEffectComponent>(effect);
        comp.Color = color;
        comp.Direction = direction;
        comp.Variant = _random.Next(comp.States.Count);
        Dirty(effect, comp);
    }

    private void AddFloorSplatter(EntityUid target, Color color, Angle direction)
    {
        var xform = Transform(target);
        if (xform.GridUid is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var grid) ||
            _rmcDecal.GetDecalsInTile(target, FloorSplatterDecals) >= MaxFloorSplattersPerTile)
        {
            return;
        }

        var worldPosition = _transform.GetWorldPosition(xform) + direction.RotateVec(new Vector2(0, -FloorSplatterShift));
        var local = Vector2.Transform(worldPosition, _transform.GetInvWorldMatrix(gridUid));
        var coordinates = new EntityCoordinates(gridUid, local);
        if (!_decals.TryAddDecal(_random.Pick(FloorSplatterDecals), coordinates.Offset(-DecalCenterOffset), out _, color, _random.NextAngle(), cleanable: true))
            return;

        _bloodTracks.MarkWet(gridUid, _map.TileIndicesFor(gridUid, grid, coordinates), color);
    }
}
