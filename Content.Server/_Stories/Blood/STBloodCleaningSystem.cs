using Content.Server.Forensics;
using Content.Shared._Stories.Blood;
using Content.Shared.Chemistry;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._Stories.Blood;

public sealed class STBloodCleaningSystem : EntitySystem
{
    private static readonly TimeSpan ItemCleanDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan WearerCleanDelay = TimeSpan.FromSeconds(6);
    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png"));

    [Dependency] private readonly STBloodStainSystem _bloodStain = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STBloodStainComponent, GetVerbsEvent<UtilityVerb>>(OnStainUtilityVerb);
        SubscribeLocalEvent<STBloodyComponent, GetVerbsEvent<UtilityVerb>>(OnBloodyUtilityVerb);
        SubscribeLocalEvent<STBloodStainComponent, STBloodCleanDoAfterEvent>(OnStainCleanDoAfter);
        SubscribeLocalEvent<STBloodyComponent, STBloodCleanDoAfterEvent>(OnBloodyCleanDoAfter);
        SubscribeLocalEvent<STBloodyComponent, ReactionEntityEvent>(OnBloodyReaction);
    }

    private void OnStainUtilityVerb(Entity<STBloodStainComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (ent.Comp.Color == null)
            return;

        AddCleanVerb(ref args, ItemCleanDelay);
    }

    private void OnBloodyUtilityVerb(Entity<STBloodyComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!_bloodStain.IsWearerBloody(ent.AsNullable()))
            return;

        AddCleanVerb(ref args, WearerCleanDelay);
    }

    private void AddCleanVerb(ref GetVerbsEvent<UtilityVerb> args, TimeSpan delay)
    {
        if (!args.CanInteract || !args.CanAccess || !HasComp<CleansForensicsComponent>(args.Using))
            return;

        var user = args.User;
        var target = args.Target;
        var used = args.Using.Value;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartCleaning(user, target, used, delay),
            Icon = VerbIcon,
            Text = Loc.GetString("st-blood-stain-clean-verb"),
            DoContactInteraction = false,
        });
    }

    private void StartCleaning(EntityUid user, EntityUid target, EntityUid used, TimeSpan delay)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, delay, new STBloodCleanDoAfterEvent(), target, target, used)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("st-blood-stain-clean-start", ("target", target)), user, user);
    }

    private void OnStainCleanDoAfter(Entity<STBloodStainComponent> ent, ref STBloodCleanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (_bloodStain.TryClean(ent))
            _popup.PopupEntity(Loc.GetString("st-blood-stain-clean-finish", ("target", ent.Owner)), args.User, args.User);
    }

    private void OnBloodyCleanDoAfter(Entity<STBloodyComponent> ent, ref STBloodCleanDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (_bloodStain.CleanWearer(ent.AsNullable()))
            _popup.PopupEntity(Loc.GetString("st-blood-stain-clean-finish", ("target", ent.Owner)), args.User, args.User);
    }

    private void OnBloodyReaction(Entity<STBloodyComponent> ent, ref ReactionEntityEvent args)
    {
        if (args.Method != ReactionMethod.Touch || !ent.Comp.CleaningReagents.Contains(args.Reagent.ID))
            return;

        _bloodStain.CleanWearer(ent.AsNullable());
    }
}
