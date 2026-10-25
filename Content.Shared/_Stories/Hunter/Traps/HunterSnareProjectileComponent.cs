using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Hunter.Traps;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HunterTrapSystem))]
public sealed partial class HunterSnareProjectileComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Deployed;

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(3);

    [DataField]
    public float Knockback = 2f;
}
