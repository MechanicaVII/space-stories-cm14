using System.Numerics;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Blood;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class STBloodyComponent : Component
{
    [DataField]
    public Color? BloodColor;

    [DataField]
    public bool CanBeStained = true;

    [DataField]
    public bool LeavesTracks = true;

    [DataField, AutoNetworkedField]
    public Color? HandsColor;

    [DataField, AutoNetworkedField]
    public Color? FeetColor;

    [DataField]
    public int HandTransfers;

    [DataField]
    public int MinHandTransfers = 2;

    [DataField]
    public int MaxHandTransfers = 4;

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> BloodReagents = new()
    {
        "Blood",
        "RMCSynthBlood",
        "CopperBlood",
        "InsectBlood",
        "AmmoniaBlood",
        "ZombieBlood",
        "STHunterBlood",
    };

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> CleaningReagents = new()
    {
        "Water",
        "SpaceCleaner",
        "SoapReagent",
        "Bleach",
    };

    [DataField]
    public float MinPuddleVolume = 1f;

    [DataField]
    public float VolumePerStep = 5f;

    [DataField]
    public int MaxSteps = 6;

    [DataField]
    public float StepDistance = 0.55f;

    [DataField]
    public float StepSideOffset = 0.12f;

    [DataField]
    public float DragDistance = 0.4f;

    [DataField]
    public int MaxDragSteps = 8;

    [DataField]
    public float MinTrackAlpha = 0.35f;

    [DataField]
    public float MaxTrackAlpha = 0.9f;

    [ViewVariables]
    public int Steps;

    [ViewVariables]
    public int DragSteps;

    [ViewVariables]
    public Color TrackColor = Color.White;

    [ViewVariables]
    public bool RightFoot;

    [ViewVariables]
    public EntityUid? TrackGrid;

    [ViewVariables]
    public Vector2i LastTile;

    [ViewVariables]
    public Vector2 LastTrackPosition;
}
