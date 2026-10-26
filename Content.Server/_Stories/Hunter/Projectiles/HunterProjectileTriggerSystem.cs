using Content.Server.Explosion.EntitySystems;
using Content.Shared._Stories.Hunter.Projectiles;

namespace Content.Server._Stories.Hunter.Projectiles;

public sealed class HunterProjectileTriggerSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HunterProjectileMaxRangeComponent, HunterProjectileMaxRangeEvent>(OnMaxRange);
    }

    private void OnMaxRange(Entity<HunterProjectileMaxRangeComponent> ent, ref HunterProjectileMaxRangeEvent args)
    {
        _trigger.Trigger(ent);
    }
}
