using Content.Server._RMC14.Medical.Surgery;
using Content.Server._RMC14.Xenonids.Acid;
using Content.Server._RMC14.Xenonids.Heal;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Construction;
using Content.Shared._RMC14.Marines.Dogtags;
using Content.Shared._RMC14.Medical.Surgery;
using Content.Shared._RMC14.Tackle;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.JoinXeno;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Projectiles;
using Content.Shared.Roles;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.FunFacts;

public sealed partial class STFunFactsSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly GunIFFSystem _iff = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMSurgerySystem _surgery = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan KillCreditWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan KnockdownCreditWindow = TimeSpan.FromSeconds(2);

    private static readonly ProtoId<JobPrototype> QueenRole = "CMXenoQueen";

    private static readonly HashSet<string> ExcludedCastes =
    [
        "CMXenoLarva",
        "STXenoPredalienLarva",
        "CMXenoLesserDrone",
        "RMCXenoLesserCarrier",
    ];

    private readonly List<FunFactsEntry> _departed = new();
    private readonly Dictionary<NetUserId, FunFactsPlayer> _players = new();
    private readonly HashSet<EntityUid> _countedTags = new();
    private readonly Dictionary<EntityUid, EntityUid> _nestAttackers = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NewXenoEvolvedEvent>(OnXenoEvolved);
        SubscribeLocalEvent<XenoDevolvedEvent>(OnXenoDevolved);
        SubscribeLocalEvent<XenoBurstPriorityEvent>(OnXenoBurst);
        SubscribeLocalEvent<ActiveTimerTriggerEvent>(OnActiveTimerTrigger);

        SubscribeLocalEvent<STFunFactsTrackerComponent, ComponentShutdown>(OnTrackerShutdown);
        SubscribeLocalEvent<STFunFactsTrackerComponent, DamageChangedEvent>(OnDamageChanged, before: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<STFunFactsTrackerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<STFunFactsTrackerComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<STFunFactsTrackerComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<STFunFactsTrackerComponent, ExtinguishedEvent>(OnExtinguished);
        SubscribeLocalEvent<STFunFactsTrackerComponent, RMCExtinguishedEvent>(OnExtinguished);
        SubscribeLocalEvent<STFunFactsTrackerComponent, CMSurgeryDoAfterEvent>(OnSurgeryDoAfter, after: [typeof(CMSurgerySystem)]);
        SubscribeLocalEvent<STFunFactsTrackerComponent, XenoSecreteStructureDoAfterEvent>(OnSecreteStructure, after: [typeof(SharedXenoConstructionSystem)]);
        SubscribeLocalEvent<STFunFactsTrackerComponent, XenoPlantWeedsActionEvent>(OnPlantWeeds, after: [typeof(SharedXenoConstructionSystem)]);
        SubscribeLocalEvent<STFunFactsTrackerComponent, XenoCorrosiveAcidDoAfterEvent>(OnCorrosiveAcid, after: [typeof(XenoAcidSystem)]);
        SubscribeLocalEvent<STFunFactsTrackerComponent, XenoHealActionEvent>(OnXenoHeal, after: [typeof(XenoHealSystem)]);
        SubscribeLocalEvent<STFunFactsTrackerComponent, XenoApplySalveActionEvent>(OnXenoHeal, after: [typeof(XenoHealSystem)]);

        SubscribeLocalEvent<ItemComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<ItemComponent, RMCConstructionBuildDoAfterEvent>(OnConstructionBuild, after: [typeof(RMCConstructionSystem)]);
        SubscribeLocalEvent<FoodComponent, AfterFullyEatenEvent>(OnFullyEaten);
        SubscribeLocalEvent<InformationTagsComponent, GotEquippedHandEvent>(OnInfoTagsPickedUp);
        SubscribeLocalEvent<DamageableComponent, XenoNestDoAfterEvent>(OnNestDoAfter, after: [typeof(XenoNestSystem)]);
        SubscribeLocalEvent<ParasiteSpentComponent, ComponentStartup>(OnParasiteSpent);
        SubscribeLocalEvent<XenoNestComponent, DamageChangedEvent>(OnNestDamaged, before: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<XenoNestComponent, DestructionEventArgs>(OnNestDestroyed);

        InitializeReport();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _departed.Clear();
        _players.Clear();
        _countedTags.Clear();
        _nestAttackers.Clear();
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!HasComp<MobStateComponent>(ev.Entity) || HasComp<GhostComponent>(ev.Entity))
            return;

        var tracker = EnsureComp<STFunFactsTrackerComponent>(ev.Entity);
        tracker.Player = ev.Player.UserId;
        RefreshIdentity((ev.Entity, tracker));

        var player = GetPlayer(ev.Player.UserId);
        player.Name = tracker.Name;
        player.Gender = tracker.Gender;
        player.Xeno = tracker.Xeno;
    }

    private void OnXenoEvolved(ref NewXenoEvolvedEvent args)
    {
        TransferStats(args.OldXeno, args.NewXeno);

        if (args.SubtractPoints && TryComp(args.NewXeno, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.Evolutions++;
    }

    private void OnXenoDevolved(ref XenoDevolvedEvent args)
    {
        TransferStats(args.OldXeno, args.NewXeno);
    }

    private void OnXenoBurst(ref XenoBurstPriorityEvent args)
    {
        if (args.InfectorUserId is { } infector)
            GetPlayer(infector).LarvaeBurst++;
    }

    private void OnActiveTimerTrigger(ref ActiveTimerTriggerEvent args)
    {
        if (HasComp<ItemComponent>(args.Triggered) && TryComp(args.User, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.Grenades++;
    }

    private void OnTrackerShutdown(Entity<STFunFactsTrackerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Transferred || ent.Comp.Player is not { } player || _gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        _departed.Add(new FunFactsEntry(player, ent.Comp.Name, ent.Comp.Gender, ent.Comp.Xeno, ent.Comp.ExcludedCaste, ent.Comp.Dead, ent.Comp.DeathCause, ent.Comp.Stats));
    }

    private void OnDamageChanged(Entity<STFunFactsTrackerComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta)
            return;

        var total = delta.GetTotal().Float();
        var actor = ResolveActor(args.Origin);
        if (actor == ent.Owner)
            actor = null;

        if (total < 0)
        {
            if (TryComp(actor, out STFunFactsTrackerComponent? healer))
                healer.Stats.Healed -= total;

            return;
        }

        if (total == 0 || ent.Comp.Dead)
            return;

        ent.Comp.Stats.DamageTaken += total;

        if (actor is not { } attacker || !TryComp(attacker, out STFunFactsTrackerComponent? attackerTracker))
            return;

        ent.Comp.LastAttacker = attacker;
        ent.Comp.LastAttackedAt = _timing.CurTime;

        if (IsFriendly(attacker, ent))
            attackerTracker.Stats.FriendlyDamage += total;
        else
            attackerTracker.Stats.DamageDealt += total;

        if (args.Tool is not { } tool || !HasComp<ProjectileComponent>(tool))
            return;

        var tick = _timing.CurTick;
        if (attackerTracker.LastHitTick == tick && attackerTracker.LastHitTarget == ent.Owner)
            return;

        attackerTracker.LastHitTick = tick;
        attackerTracker.LastHitTarget = ent.Owner;
        attackerTracker.Stats.ShotsHit++;
    }

    private void OnMobStateChanged(Entity<STFunFactsTrackerComponent> ent, ref MobStateChangedEvent args)
    {
        var now = _timing.CurTime;
        if (args.OldMobState == MobState.Critical && ent.Comp.CritStartedAt is { } critStarted)
        {
            ent.Comp.Stats.CritTime += now - critStarted;
            ent.Comp.CritStartedAt = null;
        }

        if (args.NewMobState == MobState.Critical)
            ent.Comp.CritStartedAt = now;

        if (args.NewMobState == MobState.Dead)
        {
            OnDeath(ent, args.Origin);
            return;
        }

        if (args.OldMobState != MobState.Dead)
            return;

        ent.Comp.Dead = false;
        ent.Comp.DeathCause = null;
        ent.Comp.Stats.TimesRevived++;

        var reviver = ResolveActor(args.Origin);
        if (reviver != ent.Owner && TryComp(reviver, out STFunFactsTrackerComponent? reviverTracker))
            reviverTracker.Stats.Revives++;
    }

    private void OnDeath(Entity<STFunFactsTrackerComponent> ent, EntityUid? origin)
    {
        ent.Comp.Dead = true;
        ent.Comp.Name = Name(ent);

        var killer = ResolveActor(origin);
        if (killer == null &&
            ent.Comp.LastAttacker is { } lastAttacker &&
            !TerminatingOrDeleted(lastAttacker) &&
            _timing.CurTime - ent.Comp.LastAttackedAt <= KillCreditWindow)
        {
            killer = lastAttacker;
        }

        if (killer == ent.Owner)
            killer = null;

        ent.Comp.DeathCause = killer is { } cause ? Name(cause) : null;

        if (killer is { } k && !IsFriendly(k, ent) && TryComp(k, out STFunFactsTrackerComponent? killerTracker))
            killerTracker.Stats.Kills++;

        if (ent.Comp.Player is { } player)
            GetPlayer(player).Deaths++;
    }

    private void OnDowned(Entity<STFunFactsTrackerComponent> ent, ref DownedEvent args)
    {
        if (!_mobState.IsAlive(ent))
            return;

        var by = ResolveActor(args.DownedBy);
        if (by == null && TryComp(ent, out TackledRecentlyByComponent? tackled) && tackled.Tacklers.Count == 1)
        {
            foreach (var tackler in tackled.Tacklers)
            {
                by = ResolveActor(tackler);
            }
        }

        if (by == null &&
            ent.Comp.LastAttacker is { } lastAttacker &&
            _timing.CurTime - ent.Comp.LastAttackedAt <= KnockdownCreditWindow)
        {
            by = lastAttacker;
        }

        if (by is not { } knocker ||
            knocker == ent.Owner ||
            TerminatingOrDeleted(knocker) ||
            IsFriendly(knocker, ent) ||
            !TryComp(knocker, out STFunFactsTrackerComponent? knockerTracker))
        {
            return;
        }

        knockerTracker.Stats.Knockdowns++;
    }

    private void OnIgnited(Entity<STFunFactsTrackerComponent> ent, ref IgnitedEvent args)
    {
        if (ent.Comp.Burning)
            return;

        ent.Comp.Burning = true;
        ent.Comp.Stats.Ignited++;
    }

    private void OnExtinguished<T>(Entity<STFunFactsTrackerComponent> ent, ref T args)
    {
        ent.Comp.Burning = false;
    }

    private void OnSurgeryDoAfter(Entity<STFunFactsTrackerComponent> ent, ref CMSurgeryDoAfterEvent args)
    {
        if (args.Cancelled || args.User == ent.Owner)
            return;

        if (_surgery.GetSingleton(args.Surgery) is not { } surgery ||
            !TryComp(surgery, out CMSurgeryComponent? surgeryComp) ||
            surgeryComp.Steps.Count == 0 ||
            surgeryComp.Steps[^1] != args.Step)
        {
            return;
        }

        if (TryComp(args.User, out STFunFactsTrackerComponent? surgeon))
            surgeon.Stats.Surgeries++;
    }

    private void OnSecreteStructure(Entity<STFunFactsTrackerComponent> ent, ref XenoSecreteStructureDoAfterEvent args)
    {
        if (!args.Cancelled && args.Handled)
            ent.Comp.Stats.ResinBuilt++;
    }

    private void OnPlantWeeds(Entity<STFunFactsTrackerComponent> ent, ref XenoPlantWeedsActionEvent args)
    {
        if (args.Handled)
            ent.Comp.Stats.WeedsPlanted++;
    }

    private void OnCorrosiveAcid(Entity<STFunFactsTrackerComponent> ent, ref XenoCorrosiveAcidDoAfterEvent args)
    {
        if (!args.Cancelled && args.Handled)
            ent.Comp.Stats.AcidApplied++;
    }

    private void OnXenoHeal<T>(Entity<STFunFactsTrackerComponent> ent, ref T args) where T : BaseActionEvent
    {
        if (args.Handled)
            ent.Comp.Stats.XenoHeals++;
    }

    private void OnGunShot(Entity<ItemComponent> ent, ref GunShotEvent args)
    {
        if (TryComp(args.User, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.ShotsFired += args.Ammo.Count;
    }

    private void OnConstructionBuild(Entity<ItemComponent> ent, ref RMCConstructionBuildDoAfterEvent args)
    {
        if (!args.Cancelled && args.Handled && TryComp(args.User, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.Built++;
    }

    private void OnFullyEaten(Entity<FoodComponent> ent, ref AfterFullyEatenEvent args)
    {
        if (TryComp(args.User, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.Eaten++;
    }

    private void OnInfoTagsPickedUp(Entity<InformationTagsComponent> ent, ref GotEquippedHandEvent args)
    {
        if (_countedTags.Add(ent.Owner) && TryComp(args.User, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.Dogtags++;
    }

    private void OnNestDoAfter(Entity<DamageableComponent> ent, ref XenoNestDoAfterEvent args)
    {
        if (args.Cancelled || !args.Handled || args.Target is not { } victim || !HasComp<XenoNestedComponent>(victim))
            return;

        if (TryComp(args.User, out STFunFactsTrackerComponent? tracker))
            tracker.Stats.Nested++;
    }

    private void OnParasiteSpent(Entity<ParasiteSpentComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out XenoParasiteComponent? parasite) ||
            parasite.PendingInfectorUserId is not { } infector ||
            !_player.TryGetSessionById(infector, out var session) ||
            !TryComp(session.AttachedEntity, out STFunFactsTrackerComponent? tracker))
        {
            return;
        }

        tracker.Stats.Infected++;
    }

    private void OnNestDamaged(Entity<XenoNestComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased && ResolveActor(args.Origin) is { } attacker)
            _nestAttackers[ent.Owner] = attacker;
    }

    private void OnNestDestroyed(Entity<XenoNestComponent> ent, ref DestructionEventArgs args)
    {
        if (!_nestAttackers.Remove(ent.Owner, out var rescuer) ||
            ent.Comp.Nested is not { } victim ||
            rescuer == victim ||
            TerminatingOrDeleted(rescuer) ||
            HasComp<XenoComponent>(rescuer) ||
            _mobState.IsDead(victim) ||
            !TryComp(rescuer, out STFunFactsTrackerComponent? tracker))
        {
            return;
        }

        tracker.Stats.Rescues++;
    }

    private void TransferStats(EntityUid oldUid, EntityUid newUid)
    {
        if (!TryComp(oldUid, out STFunFactsTrackerComponent? oldTracker) || oldTracker.Transferred)
            return;

        var newTracker = EnsureComp<STFunFactsTrackerComponent>(newUid);
        newTracker.Player = oldTracker.Player;
        newTracker.Stats = oldTracker.Stats;
        RefreshIdentity((newUid, newTracker));
        oldTracker.Transferred = true;
    }

    private void RefreshIdentity(Entity<STFunFactsTrackerComponent> ent)
    {
        ent.Comp.Name = Name(ent);
        ent.Comp.Xeno = TryComp(ent, out XenoComponent? xeno);

        if (xeno?.Role == QueenRole)
            ent.Comp.Gender = "female";
        else if (TryComp(ent, out GrammarComponent? grammar) && grammar.Gender is { } gender)
            ent.Comp.Gender = gender.ToString().ToLowerInvariant();
        else
            ent.Comp.Gender = "male";

        ent.Comp.ExcludedCaste = HasComp<XenoParasiteComponent>(ent) ||
                                 MetaData(ent).EntityPrototype?.ID is { } protoId && ExcludedCastes.Contains(protoId);
    }

    private EntityUid? ResolveActor(EntityUid? uid)
    {
        if (uid is not { } actor || TerminatingOrDeleted(actor))
            return null;

        if (HasComp<STFunFactsTrackerComponent>(actor))
            return actor;

        var parent = Transform(actor).ParentUid;
        return HasComp<STFunFactsTrackerComponent>(parent) ? parent : null;
    }

    private bool IsFriendly(EntityUid attacker, EntityUid victim)
    {
        var attackerXeno = HasComp<XenoComponent>(attacker);
        if (attackerXeno != HasComp<XenoComponent>(victim))
            return false;

        if (attackerXeno)
            return _hive.FromSameHive(attacker, victim);

        return _iff.TryGetFaction(attacker, out var faction) && _iff.IsInFaction(victim, faction);
    }

    private FunFactsPlayer GetPlayer(NetUserId userId)
    {
        if (!_players.TryGetValue(userId, out var player))
        {
            player = new FunFactsPlayer();
            _players[userId] = player;
        }

        return player;
    }

    private bool IsConnected(NetUserId userId)
    {
        return _player.TryGetSessionById(userId, out var session) &&
               session.Status is SessionStatus.Connected or SessionStatus.InGame;
    }

    private sealed record FunFactsEntry(
        NetUserId Player,
        string Name,
        string Gender,
        bool Xeno,
        bool ExcludedCaste,
        bool Dead,
        string? DeathCause,
        STFunFactsStats Stats);

    private sealed class FunFactsPlayer
    {
        public string Name = string.Empty;
        public string Gender = "male";
        public bool Xeno;
        public int Deaths;
        public int LarvaeBurst;
    }
}
