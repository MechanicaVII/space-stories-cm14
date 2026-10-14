using Robust.Shared.GameStates;

namespace Content.Shared._Stories.Synth;

[RegisterComponent, NetworkedComponent]
[Access(typeof(STSynthGripSystem))]
public sealed partial class STSynthGrippedComponent : Component;
