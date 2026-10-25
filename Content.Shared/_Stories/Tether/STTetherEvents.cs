using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Stories.Tether;

public sealed partial class STTetherResistAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed partial class STTetherResistDoAfterEvent : SimpleDoAfterEvent;

[ByRefEvent]
public readonly record struct STTetherReleasedEvent(EntityUid Tethered, EntityUid? Origin);
