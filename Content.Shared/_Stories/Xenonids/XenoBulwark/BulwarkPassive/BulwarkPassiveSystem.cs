using Content.Shared._RMC14.Barricade.Components;
using Content.Shared.Damage;

namespace Content.Shared._Stories.Xenonids.WarriorBulwark;

public sealed class BulwarkPassiveSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BulwarkPassiveComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<BulwarkPassiveComponent> xeno, ref DamageModifyEvent args)
    {
        if (args.Tool == null)
            return;

        if (!TryComp<BarbedComponent>(args.Tool, out var barbed) || !barbed.IsBarbed)
            return;

        args.Damage *= xeno.Comp.BarbedDamageMultiplier;
    }
}
