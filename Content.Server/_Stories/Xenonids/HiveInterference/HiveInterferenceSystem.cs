using Content.Server._RMC14.Xenonids.Watch;
using Content.Shared._RMC14.Xenonids.Eye;
using Content.Shared._RMC14.Xenonids.Watch;
using Content.Shared._Stories.Xenonids.HiveInterference;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Stories.Xenonids.HiveInterference;

public sealed class HiveInterferenceSystem : SharedHiveInterferenceSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly XenoWatchSystem _watch = default!;

    private readonly List<EntityUid> _toUnwatch = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiveInterferenceComponent, XenoWatchActionEvent>(OnWatchAction, before: [typeof(XenoWatchSystem)]);
    }

    private void OnWatchAction(Entity<HiveInterferenceComponent> ent, ref XenoWatchActionEvent args)
    {
        if (args.Handled || !IsInterfered((ent, ent.Comp)))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("st-hive-interference-watch-blocked"), ent, ent, PopupType.MediumCaution);
    }

    protected override void OnInterferenceStarted(EntityUid xeno)
    {
        CutWatching(xeno);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HiveInterferenceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Immune)
                CutWatching(uid);
        }
    }

    private void CutWatching(EntityUid xeno)
    {
        _toUnwatch.Clear();

        if (HasComp<XenoWatchingComponent>(xeno))
            _toUnwatch.Add(xeno);

        if (TryComp(xeno, out XenoWatchedComponent? watched))
            _toUnwatch.AddRange(watched.Watching);

        foreach (var watcher in _toUnwatch)
        {
            ICommonSession? session = null;
            if (TryComp(watcher, out ActorComponent? actor))
                session = actor.PlayerSession;
            else if (TryComp(watcher, out QueenEyeComponent? eye) && TryComp(eye.Queen, out ActorComponent? eyeActor))
                session = eyeActor.PlayerSession;

            if (session == null)
                continue;

            _watch.Unwatch(watcher, session);

            var msg = watcher == xeno ? "st-hive-interference-watch-blocked" : "st-hive-interference-watch-target-blocked";
            _popup.PopupEntity(Loc.GetString(msg), watcher, watcher, PopupType.MediumCaution);
        }
    }
}
