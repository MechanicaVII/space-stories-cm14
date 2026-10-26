using Content.Shared._RMC14.Xenonids;
using Content.Shared._Stories.Hunter.Marking.Components;
using Content.Shared._Stories.Xenonids.Predalien;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Stories.Hunter.Projectiles;

public sealed class HunterPlasmaCasterProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<EntityUid> _reachedMaxRange = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HunterStunBoltComponent, ProjectileHitEvent>(OnStunBoltHit);

        SubscribeLocalEvent<HunterAreaStunOnHitComponent, ProjectileHitEvent>(OnAreaStunHit);
        SubscribeLocalEvent<HunterAreaStunOnHitComponent, StartCollideEvent>(OnAreaStunCollide);
        SubscribeLocalEvent<HunterAreaStunOnHitComponent, HunterProjectileMaxRangeEvent>(OnAreaStunMaxRange);
    }

    private void OnStunBoltHit(Entity<HunterStunBoltComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyStunBolt(ent, args.Target);
    }

    private void ApplyStunBolt(Entity<HunterStunBoltComponent> ent, EntityUid target)
    {
        if (!HasComp<MobStateComponent>(target))
            return;

        if (HasComp<HunterComponent>(target) || HasComp<STPredalienComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("st-hunter-projectile-immune"), target, target);
            return;
        }

        var duration = IsHuman(target) ? ent.Comp.HumanStunTime : ent.Comp.StunTime;

        if (duration > TimeSpan.Zero)
        {
            _stun.TryParalyze(target, duration, true);

            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("st-hunter-projectile-stun-feedback"),
                    target,
                    target,
                    PopupType.MediumCaution);
        }
    }

    private void OnAreaStunHit(Entity<HunterAreaStunOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        ExplodeAreaStun(ent);
    }

    private void OnAreaStunCollide(Entity<HunterAreaStunOnHitComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Detonated)
            return;

        ExplodeAreaStun(ent);
    }

    private void OnAreaStunMaxRange(Entity<HunterAreaStunOnHitComponent> ent, ref HunterProjectileMaxRangeEvent args)
    {
        ExplodeAreaStun(ent);
    }

    private void ExplodeAreaStun(Entity<HunterAreaStunOnHitComponent> ent)
    {
        if (ent.Comp.Detonated)
            return;

        ent.Comp.Detonated = true;
        var coords = _transform.GetMapCoordinates(ent);
        var targets = _lookup.GetEntitiesInRange(coords, ent.Comp.Radius * MathF.Sqrt(2));

        foreach (var target in targets)
        {
            if (!HasComp<MobStateComponent>(target) || HasComp<STPredalienComponent>(target))
                continue;

            if (!HasComp<XenoComponent>(target) && !HasComp<HumanoidAppearanceComponent>(target))
                continue;

            var delta = _transform.GetMapCoordinates(target).Position - coords.Position;
            if (MathF.Abs(delta.X) > ent.Comp.Radius + 0.5f || MathF.Abs(delta.Y) > ent.Comp.Radius + 0.5f)
                continue;

            var duration = ent.Comp.StunTime;

            if (HasComp<HunterComponent>(target))
            {
                duration -= ent.Comp.HunterReductionTime;
                if (duration < TimeSpan.Zero)
                    duration = TimeSpan.Zero;
            }

            if (duration > TimeSpan.Zero)
            {
                _stun.TryParalyze(target, duration, true);

                if (_net.IsServer)
                    _popup.PopupEntity(Loc.GetString("st-hunter-projectile-aoe-stun-feedback"),
                        target,
                        target,
                        PopupType.LargeCaution);
            }
        }

        if (_net.IsServer)
            QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        _reachedMaxRange.Clear();

        var query = EntityQueryEnumerator<HunterProjectileMaxRangeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Origin is not { } origin)
            {
                if (TryComp(uid, out ProjectileComponent? projectile) &&
                    projectile.Shooter is { } shooter &&
                    !_container.IsEntityInContainer(uid))
                {
                    comp.Origin = _transform.GetMapCoordinates(shooter);
                }

                continue;
            }

            var position = _transform.GetMapCoordinates(uid);
            if (position.MapId != origin.MapId || (position.Position - origin.Position).Length() >= comp.Range)
                _reachedMaxRange.Add(uid);
        }

        foreach (var uid in _reachedMaxRange)
        {
            var ev = new HunterProjectileMaxRangeEvent();
            RaiseLocalEvent(uid, ref ev);

            if (!TerminatingOrDeleted(uid))
                RemComp<HunterProjectileMaxRangeComponent>(uid);
        }
    }

    private bool IsHuman(EntityUid target)
    {
        return HasComp<HumanoidAppearanceComponent>(target) && !HasComp<XenoComponent>(target) && !HasComp<HunterComponent>(target);
    }
}
