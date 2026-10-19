using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Burrow;
using System.Numerics; // Stories-MesonFix
using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers; // Stories-MesonFix
using Robust.Shared.Map; // Stories-MesonFix
using Robust.Shared.Player;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!; // Stories-MesonFix
    [Dependency] private readonly SharedTransformSystem _transform = default!; // Stories-MesonFix

    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<NightVisionComponent> _nvQuery;

    // Stories-MesonPerf-Start
    private const float MesonSpriteUpdateInterval = 0.25f;
    private float _mesonSpriteUpdateAccumulator;
    // Stories-MesonPerf-End

    // Stories-MesonFix-Start
    private readonly HashSet<EntityUid> _mesonHidden = new();

    private static readonly Vector2[] MesonSampleOffsets =
    {
        Vector2.Zero,
        new(-0.35f, -0.35f),
        new(0.35f, -0.35f),
        new(-0.35f, 0.35f),
        new(0.35f, 0.35f),
    };
    // Stories-MesonFix-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnNightVisionAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnNightVisionDetached);

        _xenoQuery = _entity.GetEntityQuery<XenoComponent>();
        _nvQuery = _entity.GetEntityQuery<NightVisionComponent>();
    }

    private void OnNightVisionAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    protected override void NightVisionChanged(Entity<NightVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        switch (ent.Comp.State)
        {
            case NightVisionState.Off:
                Off();
                break;
            case NightVisionState.Half:
                Half(ent);
                break;
            case NightVisionState.Full:
                Full(ent);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void NightVisionRemoved(Entity<NightVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off();
    }

    private void SetMesons(bool on)
    {
        if (_player.LocalEntity == null)
            return;

        _eye.SetDrawFov(_player.LocalEntity.Value, !on);
    }

    private void Off()
    {
        RemoveOverlays(); // Stories-MesonFix
        _light.DrawLighting = true;
        SetMesons(false);
        SetMesonSprites(false);
    }

    private void Half(Entity<NightVisionComponent> ent)
    {
        RemoveOverlays(); // Stories-MesonFix
        SetMesonSprites(ent.Comp.Mesons); // Stories-MesonFix

        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new NightVisionOverlay());

        if (ent.Comp.Green)
            _overlay.AddOverlay(new NightVisionFilterOverlay());

        _overlay.AddOverlay(new HalfNightVisionBrightnessOverlay());

        _light.DrawLighting = true;
        SetMesons(ent.Comp.Mesons);
    }

    private void Full(Entity<NightVisionComponent> ent)
    {
        RemoveOverlays(); // Stories-MesonFix
        SetMesonSprites(ent.Comp.Mesons); // Stories-MesonFix

        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new NightVisionOverlay());

        if (ent.Comp.Green)
            _overlay.AddOverlay(new NightVisionFilterOverlay());

        _light.DrawLighting = false;
        SetMesons(ent.Comp.Mesons);
    }

    // Stories-MesonFix-Start
    private void RemoveOverlays()
    {
        _overlay.RemoveOverlay<NightVisionOverlay>();
        _overlay.RemoveOverlay<NightVisionFilterOverlay>();
        _overlay.RemoveOverlay<HalfNightVisionBrightnessOverlay>();
    }

    private void SetMesonSprites(bool mesons)
    {
        _mesonSpriteUpdateAccumulator = 0f;

        var local = _player.LocalEntity;
        if (!mesons || local == null)
        {
            foreach (var hidden in _mesonHidden)
            {
                if (TryComp(hidden, out SpriteComponent? hiddenSprite))
                    hiddenSprite.Visible = true;
            }

            _mesonHidden.Clear();
            return;
        }

        var isXeno = _xenoQuery.HasComp(local.Value);
        var origin = _transform.GetMapCoordinates(local.Value);

        var query = EntityQueryEnumerator<RMCMesonsNonviewableComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var viewable, out var sprite))
        {
            if (isXeno && viewable.XenoVisible)
            {
                ShowMesonSprite(uid, sprite);
                continue;
            }

            if (TryComp<XenoBurrowComponent>(uid, out var burrow) && burrow.Active)
                continue;

            if (uid == local.Value || _container.IsEntityOrParentInContainer(uid) || IsMesonVisible(origin, uid))
            {
                ShowMesonSprite(uid, sprite);
                continue;
            }

            if (!sprite.Visible)
                continue;

            sprite.Visible = false;
            _mesonHidden.Add(uid);
        }
    }

    private void ShowMesonSprite(EntityUid uid, SpriteComponent sprite)
    {
        if (_mesonHidden.Remove(uid))
            sprite.Visible = true;
    }

    private bool IsMesonVisible(MapCoordinates origin, EntityUid uid)
    {
        var target = _transform.GetMapCoordinates(uid);
        if (target.MapId != origin.MapId)
            return false;

        foreach (var offset in MesonSampleOffsets)
        {
            var point = new MapCoordinates(target.Position + offset, target.MapId);
            if (_examine.InRangeUnOccluded(origin, point, ExamineSystemShared.ExamineRange, null))
                return true;
        }

        return false;
    }
    // Stories-MesonFix-End

    public override void Update(float frameTime)
    {
        if (_player.LocalEntity == null)
            return;

        if (!_nvQuery.TryComp(_player.LocalEntity.Value, out var nightVision))
            return;

        if (nightVision.State == NightVisionState.Off)
            return;

        // Stories-MesonPerf-Start
        _mesonSpriteUpdateAccumulator += frameTime;
        if (_mesonSpriteUpdateAccumulator < MesonSpriteUpdateInterval)
            return;

        _mesonSpriteUpdateAccumulator = 0f;
        // Stories-MesonPerf-End
        SetMesonSprites(nightVision.Mesons);
    }
}
