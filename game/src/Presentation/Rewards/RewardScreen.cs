using Godot;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Presentation.Cards;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Rewards;

public partial class RewardScreen : ComicScreen
{
    public event Action<string>? ColorShardTargetSelected;
    public event Action? ColorShardConfirmed;
    public event Action<string>? WeaponCardSelected;
    public event Action? WeaponCardSkipped;
    public event Action? ConfirmRequested;

    public void RenderColorShardStep(
        EncounterDefinition encounter,
        ColorType shardColor,
        IReadOnlyList<CardInstance> enchantableCards,
        string? selectedInstanceId)
    {
        var content = RequireContent();
        var root = CreateCanvas();
        AddLabelAt(root, "战斗胜利", new Vector2(760, 34), new Vector2(400, 56), 38, new Color(0.04f, 0.035f, 0.03f), HorizontalAlignment.Center);

        var shardReward = CreateShardRewardHeader(shardColor);
        AddAt(root, shardReward, new Vector2(626, 96), new Vector2(668, 92));

        var cards = new GridContainer
        {
            Columns = 5
        };
        cards.AddThemeConstantOverride("h_separation", 54);
        cards.AddThemeConstantOverride("v_separation", 38);

        var sortedCards = enchantableCards
            .Select(instance => new
            {
                Instance = instance,
                Card = content.CardsById[instance.DefinitionId]
            })
            .OrderBy(item => item.Card.Rarity)
            .ThenBy(item => item.Card.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var item in sortedCards)
        {
            cards.AddChild(CreateEnchantTargetCard(item.Instance, item.Card));
        }

        if (sortedCards.Count == 0)
        {
            cards.AddChild(CreateEmptyMessage("当前卡组没有可附魔的未附魔行动牌，色彩碎片会暂存。"));
        }

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            ClipContents = true
        };
        scroll.AddChild(cards);
        AddAt(root, scroll, new Vector2(190, 204), new Vector2(1540, 824));

        if (sortedCards.Count == 0)
        {
            var continueButton = CreateArtButton("暂存碎片，继续", "asset.ui.icon.target_selected", new Vector2(240, 58), GoldLine);
            continueButton.Pressed += () => ColorShardConfirmed?.Invoke();
            AddAt(root, continueButton, new Vector2(840, 958), new Vector2(240, 58));
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectedInstanceId))
        {
            var selected = sortedCards.FirstOrDefault(item => item.Instance.InstanceId == selectedInstanceId);
            if (selected is not null)
            {
                AddEnchantConfirmOverlay(root, selected.Instance, selected.Card, shardColor);
            }
        }
    }

    public void RenderWeaponCardChoiceStep(
        EncounterDefinition encounter,
        ColorType shardColor,
        IReadOnlyList<string> candidateCardIds,
        string? selectedCardId)
    {
        var content = RequireContent();
        var root = CreateCanvas();
        AddLabelAt(root, "战斗胜利", new Vector2(760, 34), new Vector2(400, 56), 38, new Color(0.04f, 0.035f, 0.03f), HorizontalAlignment.Center);
        AddAt(root, CreateRewardDescription("武器卡奖励：请选择一张卡牌加入牌组"), new Vector2(626, 96), new Vector2(668, 92));

        var cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", 30);
        cards.Alignment = BoxContainer.AlignmentMode.Center;

        foreach (var cardId in candidateCardIds)
        {
            cards.AddChild(CreateWeaponCardChoice(content.CardsById[cardId]));
        }

        AddAt(root, cards, new Vector2(573, 268), new Vector2(774, 360));

        if (!string.IsNullOrWhiteSpace(encounter.RewardProfile.RelicId))
        {
            var relic = content.RelicsById[encounter.RewardProfile.RelicId];
            var relicText = $"精英额外遗物：{content.RelicName(relic.Id)}";
            AddAt(root, CreateRewardDescription(relicText), new Vector2(626, 654), new Vector2(668, 54));
        }

        var heavyFont = LoadFont("asset.font.source_han_sans_sc.heavy");
        var skip = new SketchParallelogramButton("跳过", heavyFont, 24);
        skip.Pressed += () => WeaponCardSkipped?.Invoke();
        AddAt(root, skip, new Vector2(840, 760), new Vector2(240, 70));

        if (!string.IsNullOrWhiteSpace(selectedCardId) && candidateCardIds.Contains(selectedCardId, StringComparer.Ordinal))
        {
            AddWeaponCardConfirmOverlay(root, content.CardsById[selectedCardId]);
        }
    }

    private void RenderHeader(Control root, EncounterDefinition encounter, ColorType shardColor, string subtitle)
    {
        AddLabelAt(root, "战斗胜利", new Vector2(760, 70), new Vector2(400, 56), 38, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, subtitle, new Vector2(520, 132), new Vector2(880, 34), 18, new Color(0.30f, 0.20f, 0.12f), HorizontalAlignment.Center);

        var shardPanel = CreateShardPanel(shardColor);
        AddAt(root, shardPanel, new Vector2(760, 184), new Vector2(400, 58));

        if (encounter.NodeType == EncounterNodeType.Elite)
        {
            AddLabelAt(root, "精英战斗会在奖励确认后额外获得普通遗物。", new Vector2(670, 240), new Vector2(580, 28), 16, new Color(0.30f, 0.20f, 0.12f), HorizontalAlignment.Center);
        }
    }

    private Control CreateShardPanel(ColorType shardColor)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CreatePaperStyle(ColorAccent(shardColor), new Color(0.95f, 0.88f, 0.68f, 0.95f)));

        var row = new HBoxContainer();
        row.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddThemeConstantOverride("separation", 10);
        panel.AddChild(row);

        var shardIcon = CreateImage(ColorShardAsset(shardColor), new Vector2(52, 52), TextureRect.StretchModeEnum.KeepAspectCentered);
        row.AddChild(shardIcon);

        var swatch = new ColorRect
        {
            Color = ColorAccent(shardColor),
            CustomMinimumSize = new Vector2(12, 34)
        };
        row.AddChild(swatch);

        var label = new Label
        {
            Text = $"{ColorName(shardColor)}色彩碎片：{ColorShardEffectText(shardColor)}",
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(0.22f, 0.12f, 0.06f));
        row.AddChild(label);
        return panel;
    }

    private TextureRect CreateShardIcon(ColorType shardColor, Vector2 size)
    {
        var icon = CreateImage(ColorShardAsset(shardColor), size, TextureRect.StretchModeEnum.KeepAspectCentered);
        icon.Modulate = Colors.White;
        return icon;
    }

    private Control CreateShardRewardHeader(ColorType shardColor)
    {
        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 18);
        row.AddChild(CreateShardIcon(shardColor, new Vector2(92, 92)));

        var label = new Label
        {
            Text = "色彩碎片：请选择一张卡牌进行附魔",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        label.AddThemeFontSizeOverride("font_size", 26);
        label.AddThemeColorOverride("font_color", new Color(0.04f, 0.035f, 0.03f));
        var mediumFont = LoadFont("asset.font.source_han_sans_sc.medium");
        if (mediumFont is not null)
        {
            label.AddThemeFontOverride("font", mediumFont);
        }

        row.AddChild(label);
        return row;
    }

    private Control CreateRewardDescription(string text)
    {
        var label = new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeFontSizeOverride("font_size", 26);
        label.AddThemeColorOverride("font_color", new Color(0.04f, 0.035f, 0.03f));
        var mediumFont = LoadFont("asset.font.source_han_sans_sc.medium");
        if (mediumFont is not null)
        {
            label.AddThemeFontOverride("font", mediumFont);
        }

        return label;
    }

    private Control CreateEnchantTargetCard(CardInstance instance, CardDefinition card)
    {
        return CreateSelectableRewardCard(
            card,
            "选择这张行动牌",
            () => ColorShardTargetSelected?.Invoke(instance.InstanceId));
    }

    private Control CreateSelectableRewardCard(CardDefinition card, string tooltip, Action pressed)
    {
        var panel = new Control
        {
            CustomMinimumSize = new Vector2(238, 356),
            Size = new Vector2(238, 356)
        };

        var shadow = new Panel
        {
            Position = new Vector2(18, 22),
            Size = new Vector2(198, 304),
            CustomMinimumSize = new Vector2(198, 304),
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore
        };
        shadow.AddThemeStyleboxOverride("panel", CreateCardHoverShadowStyle());
        panel.AddChild(shadow);

        var cardControl = CardPanel.Create(card, RequireContent(), LoadTexture, LoadFont, width: 220);
        var restingPosition = new Vector2(3, 10);
        cardControl.Position = restingPosition;
        panel.AddChild(cardControl);

        var button = new Button
        {
            Text = "",
            TooltipText = tooltip,
            MouseDefaultCursorShape = CursorShape.PointingHand
        };
        MakeTransparentButton(button);
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.MouseEntered += () =>
        {
            shadow.Visible = true;
            cardControl.Position = new Vector2(0, 0);
            cardControl.Scale = new Vector2(1.035f, 1.035f);
        };
        button.MouseExited += () =>
        {
            shadow.Visible = false;
            cardControl.Position = restingPosition;
            cardControl.Scale = Vector2.One;
        };
        button.Pressed += pressed;
        panel.AddChild(button);
        return panel;
    }

    private void AddEnchantConfirmOverlay(Control root, CardInstance instance, CardDefinition card, ColorType shardColor)
    {
        var shade = new ColorRect
        {
            Color = new Color(0.0f, 0.0f, 0.0f, 0.58f),
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 50
        };
        shade.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddChild(shade);

        var panel = new Control
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            ZIndex = 51
        };
        root.AddChild(panel);

        var previewEnchantment = new CardEnchantment
        {
            CardInstanceId = instance.InstanceId,
            Color = shardColor
        };
        var previewCard = CardPanel.Create(card, RequireContent(), LoadTexture, LoadFont, width: 340, enchantment: previewEnchantment);
        previewCard.Position = new Vector2(790, 152);
        panel.AddChild(previewCard);

        var heavyFont = LoadFont("asset.font.source_han_sans_sc.heavy");
        var selectAgain = new SketchParallelogramButton("重新选择", heavyFont, 24);
        selectAgain.Pressed += () => ColorShardTargetSelected?.Invoke(instance.InstanceId);
        AddAt(panel, selectAgain, new Vector2(690, 780), new Vector2(240, 70));

        var confirm = new SketchParallelogramButton("确认", heavyFont, 24);
        confirm.Pressed += () => ColorShardConfirmed?.Invoke();
        AddAt(panel, confirm, new Vector2(990, 780), new Vector2(240, 70));
    }

    private void AddWeaponCardConfirmOverlay(Control root, CardDefinition card)
    {
        var shade = new ColorRect
        {
            Color = new Color(0.0f, 0.0f, 0.0f, 0.58f),
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 50
        };
        shade.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddChild(shade);

        var panel = new Control
        {
            Position = new Vector2(0, 0),
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            ZIndex = 51
        };
        root.AddChild(panel);

        var selectedCard = CardPanel.Create(card, RequireContent(), LoadTexture, LoadFont, width: 340);
        selectedCard.Position = new Vector2(790, 152);
        panel.AddChild(selectedCard);

        var heavyFont = LoadFont("asset.font.source_han_sans_sc.heavy");
        var selectAgain = new SketchParallelogramButton("重新选择", heavyFont, 24);
        selectAgain.Pressed += () => WeaponCardSelected?.Invoke(card.Id);
        AddAt(panel, selectAgain, new Vector2(690, 780), new Vector2(240, 70));

        var confirm = new SketchParallelogramButton("确认", heavyFont, 24);
        confirm.Pressed += () => ConfirmRequested?.Invoke();
        AddAt(panel, confirm, new Vector2(990, 780), new Vector2(240, 70));
    }

    private Control CreateWeaponCardChoice(CardDefinition card)
    {
        return CreateSelectableRewardCard(
            card,
            "选择这张武器卡",
            () => WeaponCardSelected?.Invoke(card.Id));
    }

    private Control CreateEmptyMessage(string text)
    {
        var panel = CreateFramedPanel(new Vector2(680, 120), GoldLine);
        var label = CreateMessage(text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(label);
        return panel;
    }

    private static StyleBoxFlat CreatePaperStyle(Color borderColor, Color backgroundColor)
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
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
    }

    private static StyleBoxFlat CreateCardHoverShadowStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderWidthLeft = 0,
            BorderWidthRight = 0,
            BorderWidthTop = 0,
            BorderWidthBottom = 0,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ShadowColor = new Color(0.02f, 0.018f, 0.016f, 0.30f),
            ShadowSize = 12,
            ShadowOffset = new Vector2(7, 10)
        };
    }

    private static Color ColorAccent(ColorType color)
    {
        return color switch
        {
            ColorType.Red => new Color(0.78f, 0.12f, 0.09f),
            ColorType.Yellow => new Color(0.90f, 0.68f, 0.12f),
            ColorType.Blue => new Color(0.14f, 0.42f, 0.86f),
            ColorType.Green => new Color(0.20f, 0.62f, 0.28f),
            ColorType.Purple => new Color(0.55f, 0.22f, 0.82f),
            _ => GoldLine
        };
    }

    private static string ColorShardAsset(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "asset.reward.color_shard.red",
            ColorType.Yellow => "asset.reward.color_shard.yellow",
            ColorType.Blue => "asset.reward.color_shard.blue",
            ColorType.Green => "asset.reward.color_shard.green",
            ColorType.Purple => "asset.reward.color_shard.purple",
            _ => "asset.reward.color_shard.red"
        };
    }

    private static string ColorName(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "红",
            ColorType.Yellow => "黄",
            ColorType.Blue => "蓝",
            ColorType.Green => "绿",
            ColorType.Purple => "紫",
            _ => "无"
        };
    }

    private static string ColorShardEffectText(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "提高伤害或转化为伤害",
            ColorType.Yellow => "增加卡牌释放次数",
            ColorType.Blue => "转化为防御",
            ColorType.Green => "转化为回复",
            ColorType.Purple => "放大最终效果",
            _ => "无色"
        };
    }

}
