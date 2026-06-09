using Godot;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Rewards;
using RoguelikeCardGame.Presentation.Cards;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Rewards;

public partial class RewardScreen : ComicScreen
{
    private readonly RewardService rewardService = new();
    private readonly Dictionary<string, Control> rewardPackNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Control>> rewardCardNodes = new(StringComparer.Ordinal);
    private Control? fxLayer;

    public event Action<string>? RewardPackRequested;
    public event Action<string>? RewardCardToggled;
    public event Action? ConfirmRequested;

    public void RenderPackSelection(EncounterDefinition encounter)
    {
        rewardPackNodes.Clear();
        rewardCardNodes.Clear();
        fxLayer = null;
        var content = RequireContent();
        var root = CreateCanvas();

        AddLabelAt(root, "战斗胜利", new Vector2(760, 78), new Vector2(400, 56), 38, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, "选择 1 个卡牌包打开。打开后可以选择 0-3 张加入卡组。", new Vector2(565, 145), new Vector2(790, 34), 18, new Color(0.30f, 0.20f, 0.12f), HorizontalAlignment.Center);

        if (encounter.RewardProfile.RelicId is not null)
        {
            var relic = content.RelicsById[encounter.RewardProfile.RelicId];
            var relicRow = new HBoxContainer();
            relicRow.AddThemeConstantOverride("separation", 8);
            relicRow.AddChild(CreateImage(content.RelicViewsById[relic.Id].IconAsset, new Vector2(42, 42), TextureRect.StretchModeEnum.KeepAspectCentered));
            relicRow.AddChild(CreateMessage($"精英额外奖励将在确认后获得：{content.RelicName(relic.Id)}"));
            AddAt(root, relicRow, new Vector2(690, 195), new Vector2(540, 48));
        }

        var packs = new HBoxContainer();
        packs.AddThemeConstantOverride("separation", 36);
        packs.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (var pack in rewardService.GetAvailableCardPacks(encounter, content.RewardPacksById))
        {
            var packControl = CreateRewardPackControl(pack);
            rewardPackNodes[pack.Id] = packControl;
            packs.AddChild(packControl);
        }

        AddAt(root, packs, new Vector2(540, 300), new Vector2(840, 330));

        var skip = CreateArtButton("跳过并进入下一战", "asset.ui.icon.end_turn", new Vector2(240, 58), new Color(0.36f, 0.31f, 0.26f));
        skip.Pressed += () => ConfirmRequested?.Invoke();
        AddAt(root, skip, new Vector2(840, 760), new Vector2(240, 58));

        AddFxLayer(root);
    }

    public void RenderOpenedPack(RewardPackDefinition openedRewardPack, IReadOnlySet<string> selectedRewardCards)
    {
        rewardPackNodes.Clear();
        rewardCardNodes.Clear();
        fxLayer = null;
        var content = RequireContent();
        var root = CreateCanvas();

        AddLabelAt(root, content.RewardPackName(openedRewardPack.Id), new Vector2(700, 72), new Vector2(520, 56), 36, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, "点击卡牌切换选择状态。奖励池固定可重复，已经拿过的牌仍可再次出现。", new Vector2(500, 140), new Vector2(920, 34), 18, new Color(0.30f, 0.20f, 0.12f), HorizontalAlignment.Center);

        var cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", 30);
        cards.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (var cardId in openedRewardPack.CandidateIds)
        {
            var card = content.CardsById[cardId];
            var cardControl = CreateRewardCardControl(card, selectedRewardCards.Contains(cardId));
            if (!rewardCardNodes.TryGetValue(cardId, out var nodes))
            {
                nodes = new List<Control>();
                rewardCardNodes[cardId] = nodes;
            }

            nodes.Add(cardControl);
            cards.AddChild(cardControl);
        }

        AddAt(root, cards, new Vector2(600, 260), new Vector2(720, 360));
        var confirm = CreateArtButton($"确认选择 {selectedRewardCards.Count} 张", "asset.ui.icon.deck_library", new Vector2(240, 58), GoldLine);
        confirm.Pressed += () => ConfirmRequested?.Invoke();
        AddAt(root, confirm, new Vector2(840, 760), new Vector2(240, 58));

        AddFxLayer(root);
    }

    private Control CreateRewardPackControl(RewardPackDefinition pack)
    {
        var content = RequireContent();
        var view = content.RewardPackViewsById[pack.Id];
        var panel = CreateFramedPanel(new Vector2(230, 286), GoldLine);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);
        box.AddChild(CreateImage(view.IconAsset, new Vector2(205, 200), TextureRect.StretchModeEnum.KeepAspectCentered));

        var title = CreateSmallLabel(content.RewardPackName(pack.Id));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 17);
        box.AddChild(title);

        var button = new Button
        {
            Text = "",
            TooltipText = "打开卡牌包"
        };
        MakeTransparentButton(button);
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.Pressed += () => RewardPackRequested?.Invoke(pack.Id);
        panel.AddChild(button);
        return panel;
    }

    private Control CreateRewardCardControl(CardDefinition card, bool picked)
    {
        var control = CardPanel.Create(card, RequireContent(), LoadTexture, LoadFont, width: 220);
        var button = new Button { Text = "", TooltipText = picked ? "取消选择" : "选择" };
        MakeTransparentButton(button);
        button.Position = Vector2.Zero;
        button.Size = control.CustomMinimumSize;
        button.CustomMinimumSize = control.CustomMinimumSize;
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.Pressed += () => RewardCardToggled?.Invoke(card.Id);
        control.AddChild(button);

        if (picked)
        {
            var icon = CreateImage("asset.ui.icon.target_selected", new Vector2(42, 42), TextureRect.StretchModeEnum.KeepAspectCentered);
            icon.Position = new Vector2(10, 10);
            control.AddChild(icon);
        }

        return control;
    }

    public async Task PlayPackOpenAsync(string packId)
    {
        if (rewardPackNodes.TryGetValue(packId, out var packNode))
        {
            await PulseNodeAsync(packNode, 1.08f, 0.12f);
            await SpawnVfxAsync("asset.vfx.chain_gain_spark", CenterOf(packNode), new Vector2(230, 150), new Color(1f, 0.88f, 0.48f, 0.95f), 0.22f);
        }
    }

    public async Task PlayOpenedCardsEntranceAsync()
    {
        foreach (var cardNode in rewardCardNodes.Values.SelectMany(nodes => nodes))
        {
            cardNode.PivotOffset = cardNode.Size * 0.5f;
            cardNode.Scale = new Vector2(0.86f, 0.86f);
            cardNode.Modulate = new Color(1f, 1f, 1f, 0f);
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenProperty(cardNode, "scale", Vector2.One, 0.16);
            tween.TweenProperty(cardNode, "modulate", Colors.White, 0.16);
            await ToSignal(tween, "finished");
            await WaitAsync(0.03);
        }
    }

    public async Task PlayRewardCardToggleAsync(string cardId, bool picked)
    {
        if (!rewardCardNodes.TryGetValue(cardId, out var nodes))
        {
            return;
        }

        var cardNode = nodes.FirstOrDefault();
        if (cardNode is null)
        {
            return;
        }

        await PulseNodeAsync(cardNode, picked ? 1.07f : 0.96f, 0.10f);
        if (picked)
        {
            await SpawnVfxAsync("asset.ui.icon.target_selected", CenterOf(cardNode), new Vector2(80, 80), new Color(1f, 0.92f, 0.44f, 0.95f), 0.18f);
        }
    }

    private void AddFxLayer(Control root)
    {
        fxLayer = new Control
        {
            Name = "RewardFxLayer",
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 100
        };
        root.AddChild(fxLayer);
    }

    private async Task PulseNodeAsync(Control? node, float peakScale, double duration)
    {
        if (node is null)
        {
            return;
        }

        var originalScale = node.Scale;
        node.PivotOffset = node.Size * 0.5f;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "scale", originalScale * peakScale, duration);
        tween.TweenProperty(node, "scale", originalScale, duration);
        await ToSignal(tween, "finished");
    }

    private async Task SpawnVfxAsync(string assetId, Vector2 center, Vector2 size, Color tint, double duration)
    {
        if (fxLayer is null)
        {
            return;
        }

        var vfx = CreateImage(assetId, size, TextureRect.StretchModeEnum.KeepAspectCentered);
        vfx.Position = center - size * 0.5f;
        vfx.Size = size;
        vfx.CustomMinimumSize = size;
        vfx.Modulate = new Color(tint.R, tint.G, tint.B, 0f);
        vfx.PivotOffset = size * 0.5f;
        vfx.Scale = new Vector2(0.78f, 0.78f);
        vfx.ZIndex = 110;
        fxLayer.AddChild(vfx);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(vfx, "modulate", tint, duration * 0.35);
        tween.TweenProperty(vfx, "scale", new Vector2(1.12f, 1.12f), duration);
        tween.Chain().TweenProperty(vfx, "modulate", new Color(tint.R, tint.G, tint.B, 0f), duration * 0.45);
        await ToSignal(tween, "finished");
        vfx.QueueFree();
    }

    private async Task WaitAsync(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), "timeout");
    }

    private static Vector2 CenterOf(Control node)
    {
        return node.Position + node.Size * 0.5f;
    }
}
