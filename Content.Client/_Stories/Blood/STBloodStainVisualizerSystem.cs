using Content.Client.Clothing;
using Content.Shared._Stories.Blood;
using Content.Shared.Clothing;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._Stories.Blood;

public sealed class STBloodStainVisualizerSystem : EntitySystem
{
    private const string StainsRsi = "_Stories/Effects/blood_stains.rsi";
    private const string ItemMaskState = "itemblood";
    private const string HandsState = "hands_blood";
    private const string FeetState = "feet_blood";
    private const string StainShader = "STBloodStain";

    private const string ItemStainKey = "st-blood-stain";
    private const string ItemMaskKey = "st-blood-stain-mask";
    private const string HandsKey = "st-blood-hands";
    private const string FeetKey = "st-blood-feet";
    private const string GlovesSlotKey = "gloves";

    [Dependency] private readonly STBloodStainSystem _bloodStain = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly List<(SpriteComponent.Layer Layer, int Index)> _sourceLayers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STBloodStainComponent, ComponentStartup>(OnStainStartup);
        SubscribeLocalEvent<STBloodStainComponent, AfterAutoHandleStateEvent>(OnStainState);
        SubscribeLocalEvent<STBloodStainComponent, AppearanceChangeEvent>(OnStainAppearance);
        SubscribeLocalEvent<STBloodStainComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals, after: [typeof(ClientClothingSystem)]);

        SubscribeLocalEvent<STBloodyComponent, ComponentStartup>(OnBloodyStartup);
        SubscribeLocalEvent<STBloodyComponent, AfterAutoHandleStateEvent>(OnBloodyState);
    }

    private void OnStainStartup(Entity<STBloodStainComponent> ent, ref ComponentStartup args)
    {
        RefreshItem(ent);
    }

    private void OnStainState(Entity<STBloodStainComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshItem(ent);
        _item.VisualsChanged(ent);
    }

    private void OnStainAppearance(Entity<STBloodStainComponent> ent, ref AppearanceChangeEvent args)
    {
        RefreshItem(ent);
    }

    private void OnGetEquipmentVisuals(Entity<STBloodStainComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.Color is not { } color || _bloodStain.GetWornState(ent, args.Slot) is not { } state)
            return;

        args.Layers.Add(($"{args.Slot}-{ItemStainKey}", new PrototypeLayerData
        {
            RsiPath = StainsRsi,
            State = state,
            Color = color,
        }));
    }

    private void RefreshItem(Entity<STBloodStainComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (ent.Owner, spriteComp);
        for (var i = 0; ; i++)
        {
            var removedStain = _sprite.RemoveLayer(sprite, $"{ItemStainKey}-{i}", false);
            var removedMask = _sprite.RemoveLayer(sprite, $"{ItemMaskKey}-{i}", false);
            if (!removedStain && !removedMask)
                break;
        }

        if (ent.Comp.Color is not { } color)
            return;

        _sourceLayers.Clear();
        var index = -1;
        foreach (var candidate in spriteComp.AllLayers)
        {
            index++;
            if (candidate is not SpriteComponent.Layer layer ||
                !layer.Visible ||
                layer.Blank ||
                layer.CopyToShaderParameters != null)
            {
                continue;
            }

            _sourceLayers.Add((layer, index));
        }

        for (var i = _sourceLayers.Count - 1; i >= 0; i--)
        {
            var (layer, layerIndex) = _sourceLayers[i];
            AddStainLayers(sprite, layer, layerIndex, i, color);
        }
    }

    private void AddStainLayers(Entity<SpriteComponent?> sprite, SpriteComponent.Layer source, int sourceIndex, int number, Color color)
    {
        var stainKey = $"{ItemStainKey}-{number}";
        var mask = _sprite.AddLayer(sprite, new PrototypeLayerData
        {
            RsiPath = StainsRsi,
            State = ItemMaskState,
            CopyToShaderParameters = new PrototypeCopyToShaderParameters
            {
                LayerKey = stainKey,
                ParameterTexture = "stainMask",
                ParameterUV = "stainMaskUV",
            },
        }, sourceIndex + 1);
        _sprite.LayerMapSet(sprite, $"{ItemMaskKey}-{number}", mask);

        var stain = _sprite.AddLayer(sprite, new PrototypeLayerData
        {
            Shader = StainShader,
            Color = color,
        }, mask + 1);

        if (source.State.IsValid && source.ActualRsi != null)
            _sprite.LayerSetRsi(sprite, stain, source.ActualRsi, source.State);
        else
            _sprite.LayerSetTexture(sprite, stain, source.Texture);

        _sprite.LayerSetOffset(sprite, stain, source.Offset);
        _sprite.LayerSetScale(sprite, stain, source.Scale);
        _sprite.LayerSetRotation(sprite, stain, source.Rotation);
        _sprite.LayerSetDirOffset(sprite, stain, source.DirOffset);
        _sprite.LayerMapSet(sprite, stainKey, stain);
    }

    private void OnBloodyStartup(Entity<STBloodyComponent> ent, ref ComponentStartup args)
    {
        RefreshBody(ent);
    }

    private void OnBloodyState(Entity<STBloodyComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshBody(ent);
    }

    private void RefreshBody(Entity<STBloodyComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var spriteComp))
            return;

        Entity<SpriteComponent?> sprite = (ent.Owner, spriteComp);
        _sprite.RemoveLayer(sprite, HandsKey, false);
        _sprite.RemoveLayer(sprite, FeetKey, false);

        if (ent.Comp.FeetColor is { } feet &&
            _sprite.LayerMapTryGet(sprite, HumanoidVisualLayers.LHand, out var feetIndex, false))
        {
            AddBodyLayer(sprite, FeetKey, FeetState, feet, feetIndex);
        }

        if (ent.Comp.HandsColor is { } hands &&
            _sprite.LayerMapTryGet(sprite, GlovesSlotKey, out var handsIndex, false))
        {
            AddBodyLayer(sprite, HandsKey, HandsState, hands, handsIndex);
        }
    }

    private void AddBodyLayer(Entity<SpriteComponent?> sprite, string key, string state, Color color, int index)
    {
        var layer = _sprite.AddLayer(sprite, new PrototypeLayerData
        {
            RsiPath = StainsRsi,
            State = state,
            Color = color,
        }, index);
        _sprite.LayerMapSet(sprite, key, layer);
    }
}
