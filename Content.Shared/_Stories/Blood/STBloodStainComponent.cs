using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Blood;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(STBloodStainSystem))]
public sealed partial class STBloodStainComponent : Component
{
    [DataField]
    public bool CanStain = true;

    [DataField]
    [Access(typeof(STBloodStainSystem), Other = AccessPermissions.Read)]
    public string? WornState;

    [DataField, AutoNetworkedField]
    public Color? Color;
}
