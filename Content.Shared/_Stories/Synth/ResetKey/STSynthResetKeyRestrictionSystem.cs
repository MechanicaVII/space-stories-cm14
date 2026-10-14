using Content.Shared._RMC14.Synth;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared._Stories.Synth.ResetKey;

public sealed class STSynthResetKeyRestrictionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<STSynthResetKeyRestrictionComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
    }

    private void OnBeforeRangedInteract(Entity<STSynthResetKeyRestrictionComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<SynthComponent>(target) || _whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, target))
            return;

        args.Handled = true;
        var msg = Loc.GetString(ent.Comp.Popup, ("user", args.User), ("used", ent.Owner), ("target", target));
        _popup.PopupClient(msg, args.User, args.User, PopupType.SmallCaution);
    }
}
