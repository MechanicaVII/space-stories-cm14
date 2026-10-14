using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Synth.WorkingJoe;

[RegisterComponent, NetworkedComponent]
public sealed partial class STUnstrippableWhileAbleComponent : Component
{
    [DataField]
    public LocId UnequipReason = "st-unstrippable-while-able-unequip";

    [DataField]
    public LocId EquipReason = "st-unstrippable-while-able-equip";
}
