using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Hunter.Weapons;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HunterArrowSystem))]
public sealed partial class HunterArrowComponent : Component
{
    [DataField]
    public EntProtoId? ToggleTo;

    [DataField]
    public LocId? TogglePopup;

    [DataField]
    public EntProtoId<SkillDefinitionComponent> Skill = "STSkillIllegalTechnology";

    [DataField]
    public TimeSpan? StunTime;

    [DataField]
    public TimeSpan? DazeTime;

    [DataField]
    public float? PushMaxRange;

    [DataField]
    public bool DetachFromNonHumans;
}
