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
    public event Action? ConfirmRequested;

    public void RenderColorShardStep(
        EncounterDefinition encounter,
        ColorType shardColor,
        IReadOnlyList<CardInstance> enchantableCards,
        string? selectedInstanceId)
    {
        var content = RequireContent();
        var root = CreateCanvas();
        RenderHeader(root, encounter, shardColor, "选择 1 张未附魔行动牌，将本次色彩碎片镶入这张牌。");

        var cards = new GridContainer
        {
            Columns = 2
        };
        cards.AddThemeConstantOverride("h_separation", 20);
        cards.AddThemeConstantOverride("v_separation", 16);

        foreach (var instance in enchantableCards)
        {
            var card = content.CardsById[instance.DefinitionId];
            cards.AddChild(CreateEnchantTargetButton(instance, card, selectedInstanceId == instance.InstanceId));
        }

        if (enchantableCards.Count == 0)
        {
            cards.AddChild(CreateEmptyMessage("当前卡组没有可附魔的未附魔行动牌，色彩碎片会暂存。"));
        }

        AddAt(root, cards, new Vector2(420, 270), new Vector2(1080, 430));

        var confirm = CreateArtButton(
            enchantableCards.Count == 0 ? "暂存碎片，继续" : "确认附魔",
            "asset.ui.icon.target_selected",
            new Vector2(240, 58),
            ColorAccent(shardColor));
        confirm.Disabled = enchantableCards.Count > 0 && selectedInstanceId is null;
        confirm.Pressed += () => ColorShardConfirmed?.Invoke();
        AddAt(root, confirm, new Vector2(840, 760), new Vector2(240, 58));
    }

    public void RenderWeaponCardChoiceStep(
        EncounterDefinition encounter,
        ColorType shardColor,
        IReadOnlyList<string> candidateCardIds,
        string? selectedCardId)
    {
        var content = RequireContent();
        var root = CreateCanvas();
        RenderHeader(root, encounter, shardColor, "从主手 / 副手武器奖励池中选择 1 张卡牌加入牌组。");

        var cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", 30);
        cards.Alignment = BoxContainer.AlignmentMode.Center;

        foreach (var cardId in candidateCardIds)
        {
            cards.AddChild(CreateWeaponCardChoice(content.CardsById[cardId], selectedCardId == cardId));
        }

        AddAt(root, cards, new Vector2(600, 268), new Vector2(720, 360));

        if (!string.IsNullOrWhiteSpace(encounter.RewardProfile.RelicId))
        {
            var relic = content.RelicsById[encounter.RewardProfile.RelicId];
            var relicText = $"精英额外遗物：{content.RelicName(relic.Id)}";
            AddAt(root, CreateMessagePanel(relicText), new Vector2(700, 672), new Vector2(520, 48));
        }

        var confirm = CreateArtButton("加入牌组，进入下一战", "asset.ui.icon.deck_library", new Vector2(300, 58), GoldLine);
        confirm.Disabled = selectedCardId is null;
        confirm.Pressed += () => ConfirmRequested?.Invoke();
        AddAt(root, confirm, new Vector2(810, 760), new Vector2(300, 58));
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

        var swatch = new ColorRect
        {
            Color = ColorAccent(shardColor),
            CustomMinimumSize = new Vector2(34, 34)
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

    private Control CreateEnchantTargetButton(CardInstance instance, CardDefinition card, bool selected)
    {
        var content = RequireContent();
        var button = new Button
        {
            Text = $"{content.CardName(card.Id)}   {content.WeaponName(card.WeaponId)} / 行动牌 / {card.Cost} 费\n{content.CardRules(card.Id)}",
            CustomMinimumSize = new Vector2(520, 92),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TooltipText = instance.InstanceId
        };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.AddThemeColorOverride("font_color", new Color(0.22f, 0.12f, 0.06f));
        button.AddThemeStyleboxOverride("normal", CreatePaperStyle(selected ? GoldLine : new Color(0.40f, 0.24f, 0.10f), new Color(0.91f, 0.81f, 0.62f, 0.94f)));
        button.AddThemeStyleboxOverride("hover", CreatePaperStyle(GoldLine, new Color(0.96f, 0.86f, 0.62f, 0.98f)));
        button.AddThemeStyleboxOverride("pressed", CreatePaperStyle(GoldLine, new Color(0.86f, 0.68f, 0.42f, 0.98f)));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.Pressed += () => ColorShardTargetSelected?.Invoke(instance.InstanceId);
        return button;
    }

    private Control CreateWeaponCardChoice(CardDefinition card, bool selected)
    {
        var panel = new Control
        {
            CustomMinimumSize = new Vector2(220, 346)
        };

        var cardControl = CardPanel.Create(card, RequireContent(), LoadTexture, LoadFont, width: 220);
        panel.AddChild(cardControl);

        var button = new Button { Text = "", TooltipText = selected ? "已选择" : "选择这张武器卡" };
        MakeTransparentButton(button);
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.Pressed += () => WeaponCardSelected?.Invoke(card.Id);
        panel.AddChild(button);

        if (selected)
        {
            var icon = CreateImage("asset.ui.icon.target_selected", new Vector2(42, 42), TextureRect.StretchModeEnum.KeepAspectCentered);
            icon.Position = new Vector2(10, 10);
            icon.ZIndex = 6;
            panel.AddChild(icon);
        }

        return panel;
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
