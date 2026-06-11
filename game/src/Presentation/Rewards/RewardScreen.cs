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
        packs.AddThemeConstantOverride("separation", 52);
        packs.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (var pack in rewardService.GetAvailableCardPacks(encounter, content.RewardPacksById))
        {
            var packControl = CreateRewardPackControl(pack);
            rewardPackNodes[pack.Id] = packControl;
            packs.AddChild(packControl);
        }

        AddAt(root, packs, new Vector2(505, 258), new Vector2(910, 386));

        var skip = CreatePaperActionButton("不拿牌，进入下一战", "asset.ui.icon.end_turn", new Vector2(300, 62));
        skip.Pressed += () => ConfirmRequested?.Invoke();
        AddAt(root, skip, new Vector2(810, 760), new Vector2(300, 62));

        AddFxLayer(root);
        _ = PlayPackSelectionEntranceAsync();
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
        var accent = PackAccent(pack.Id);
        var root = new Control
        {
            CustomMinimumSize = new Vector2(250, 344),
            Size = new Vector2(250, 344),
            MouseFilter = MouseFilterEnum.Ignore
        };

        var imageSize = new Vector2(230, 286);
        var imagePosition = new Vector2(10, 0);
        var hoverGlow = CreateImage(view.IconAsset, imageSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        hoverGlow.Position = imagePosition;
        hoverGlow.Size = imageSize;
        hoverGlow.PivotOffset = imageSize * 0.5f;
        hoverGlow.Scale = new Vector2(1.05f, 1.05f);
        hoverGlow.Modulate = new Color(accent.R, accent.G, accent.B, 0f);
        root.AddChild(hoverGlow);

        var image = CreateImage(view.IconAsset, new Vector2(230, 286), TextureRect.StretchModeEnum.KeepAspectCentered);
        image.Position = imagePosition;
        image.Size = imageSize;
        root.AddChild(image);

        var title = CreateRewardPackLabel(content.RewardPackName(pack.Id), accent);
        title.Position = new Vector2(20, 286);
        title.Size = new Vector2(210, 46);
        root.AddChild(title);

        var hitArea = new RewardPackHitArea
        {
            Position = imagePosition,
            ZIndex = 10
        };
        hitArea.Configure(pack.Id, LoadImage(view.IconAsset), imageSize, id => RewardPackRequested?.Invoke(id));
        hitArea.MouseEntered += () => SetRewardPackHover(root, image, hoverGlow, title, hovered: true);
        hitArea.MouseExited += () => SetRewardPackHover(root, image, hoverGlow, title, hovered: false);
        root.AddChild(hitArea);
        return root;
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
        button.MouseEntered += () => SetRewardCardHover(control, hovered: true, picked);
        button.MouseExited += () => SetRewardCardHover(control, hovered: false, picked);
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
            await PulseNodeAsync(packNode, 1.12f, 0.12f);
            await SpawnVfxAsync(fxLayer, "asset.vfx.chain_gain_spark", CenterOf(packNode), new Vector2(270, 180), new Color(1f, 0.88f, 0.48f, 0.95f), 0.24f);
        }
    }

    private async Task PlayPackSelectionEntranceAsync()
    {
        foreach (var packNode in rewardPackNodes.Values)
        {
            if (!GodotObject.IsInstanceValid(packNode))
            {
                continue;
            }

            packNode.PivotOffset = packNode.Size * 0.5f;
            packNode.Scale = new Vector2(0.9f, 0.9f);
            packNode.Modulate = new Color(1f, 1f, 1f, 0f);

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetTrans(Tween.TransitionType.Back);
            tween.SetEase(Tween.EaseType.Out);
            tween.TweenProperty(packNode, "scale", Vector2.One, 0.18);
            tween.TweenProperty(packNode, "modulate", Colors.White, 0.18);
            await ToSignal(tween, "finished");
            await WaitAsync(0.045);
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
            await SpawnVfxAsync(fxLayer, "asset.ui.icon.target_selected", CenterOf(cardNode), new Vector2(80, 80), new Color(1f, 0.92f, 0.44f, 0.95f), 0.18f);
        }
    }

    private void AddFxLayer(Control root)
    {
        fxLayer = CreateFxLayer("RewardFxLayer");
        root.AddChild(fxLayer);
    }

    private void SetRewardPackHover(Control root, CanvasItem image, CanvasItem hoverGlow, Control title, bool hovered)
    {
        if (!GodotObject.IsInstanceValid(root))
        {
            return;
        }

        root.PivotOffset = root.Size * 0.5f;
        root.ZIndex = hovered ? 30 : 0;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(root, "scale", hovered ? new Vector2(1.055f, 1.055f) : Vector2.One, 0.10);
        tween.TweenProperty(image, "modulate", hovered ? new Color(1f, 0.98f, 0.88f, 1f) : Colors.White, 0.10);
        tween.TweenProperty(hoverGlow, "modulate", hovered ? new Color(1f, 0.73f, 0.22f, 0.35f) : new Color(1f, 0.73f, 0.22f, 0f), 0.10);
        tween.TweenProperty(title, "scale", hovered ? new Vector2(1.035f, 1.035f) : Vector2.One, 0.10);
    }

    private void SetRewardCardHover(Control card, bool hovered, bool picked)
    {
        if (!GodotObject.IsInstanceValid(card))
        {
            return;
        }

        card.PivotOffset = card.Size * 0.5f;
        card.ZIndex = hovered ? 35 : picked ? 12 : 0;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "scale", hovered ? new Vector2(1.045f, 1.045f) : Vector2.One, 0.10);
        tween.TweenProperty(card, "modulate", hovered ? new Color(1f, 0.98f, 0.88f, 1f) : Colors.White, 0.10);
    }

    private Image LoadImage(string assetId)
    {
        var content = RequireContent();
        if (!content.AssetsById.TryGetValue(assetId, out var asset))
        {
            throw new InvalidOperationException($"Asset '{assetId}' is not defined.");
        }

        var image = new Image();
        var bytes = System.IO.File.ReadAllBytes(ProjectSettings.GlobalizePath(asset.Path));
        var error = image.LoadPngFromBuffer(bytes);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"Failed to load PNG image '{asset.Path}': {error}.");
        }

        return image;
    }

    private PanelContainer CreateRewardPackLabel(string text, Color accent)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(210, 46),
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", CreatePaperLabelStyle(accent));

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(0.22f, 0.12f, 0.06f));
        panel.AddChild(label);
        return panel;
    }

    private Button CreatePaperActionButton(string text, string iconAsset, Vector2 minSize)
    {
        var button = new Button
        {
            Text = text,
            Icon = LoadTexture(iconAsset),
            CustomMinimumSize = minSize,
            ExpandIcon = true,
            Alignment = HorizontalAlignment.Center
        };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.AddThemeColorOverride("font_color", new Color(0.23f, 0.12f, 0.05f));
        button.AddThemeColorOverride("font_hover_color", new Color(0.15f, 0.08f, 0.04f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.40f, 0.16f, 0.06f));
        button.AddThemeColorOverride("icon_normal_color", new Color(0.50f, 0.20f, 0.08f));
        button.AddThemeColorOverride("icon_hover_color", new Color(0.70f, 0.24f, 0.08f));
        button.AddThemeColorOverride("icon_pressed_color", new Color(0.36f, 0.12f, 0.04f));
        button.AddThemeStyleboxOverride("normal", CreatePaperButtonStyle(new Color(0.70f, 0.38f, 0.12f), new Color(0.86f, 0.70f, 0.44f, 0.92f)));
        button.AddThemeStyleboxOverride("hover", CreatePaperButtonStyle(new Color(0.92f, 0.48f, 0.12f), new Color(0.95f, 0.78f, 0.48f, 0.98f)));
        button.AddThemeStyleboxOverride("pressed", CreatePaperButtonStyle(new Color(0.62f, 0.26f, 0.08f), new Color(0.78f, 0.56f, 0.32f, 0.98f)));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        return button;
    }

    private static StyleBoxFlat CreatePaperLabelStyle(Color accent)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.86f, 0.72f, 0.49f, 0.90f),
            BorderColor = accent,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
    }

    private static StyleBoxFlat CreatePaperButtonStyle(Color borderColor, Color backgroundColor)
    {
        return new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = borderColor,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
    }

    private static Color PackAccent(string packId)
    {
        if (packId.Contains(".action", StringComparison.Ordinal))
        {
            return BloodLine;
        }

        if (packId.Contains(".skill", StringComparison.Ordinal))
        {
            return SkillLine;
        }

        return FinisherLine;
    }
}
