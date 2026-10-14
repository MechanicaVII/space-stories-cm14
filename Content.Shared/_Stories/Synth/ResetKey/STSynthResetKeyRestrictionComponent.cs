using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Synth.ResetKey;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(STSynthResetKeyRestrictionSystem))]
public sealed partial class STSynthResetKeyRestrictionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public LocId Popup = "st-synth-reset-key-wrong-type";
}
