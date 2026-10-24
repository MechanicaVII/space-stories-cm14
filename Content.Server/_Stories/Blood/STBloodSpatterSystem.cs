using Content.Shared._Stories.Blood;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Item;
using Content.Shared.Weapons.Melee;
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

    [Dependency] private readonly STBloodStainSystem _bloodStain = default!;
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

        var chance = Math.Clamp(BaseChance + ChancePerDamage * brute.Float(), 0f, 1f);
        if (!_random.Prob(chance))
            return;

        var melee = args.Tool is { } tool && HasComp<MeleeWeaponComponent>(tool);
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
}
