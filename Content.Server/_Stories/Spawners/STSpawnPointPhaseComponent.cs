namespace Content.Server._Stories.Spawners;

[RegisterComponent]
public sealed partial class STSpawnPointPhaseComponent : Component
{
    [DataField]
    public bool RoundStart = true;

    [DataField]
    public bool LateJoin = true;
}
