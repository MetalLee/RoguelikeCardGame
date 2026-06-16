using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Cards;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleHandView : Control
{
    private readonly Dictionary<string, List<Control>> cardNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Control> cardNodesByHandIndex = new();
    private readonly Dictionary<int, HandCardInteraction> handCardsByIndex = new();

    private CombatState? combat;
    private RunState? run;
    private GameContent? content;
    private CardPlayService? cardPlayService;
    private BattleTargetingOverlay? targetingOverlay;
    private Func<string, Texture2D?>? loadTexture;
    private Func<string, Font?>? loadFont;
    private HandCardInteraction? draggingCard;
    private bool interactionsLocked;

    public event Action<string, string, int, string?>? CardRequested;
    public event Action<string>? FeedbackRequested;
    public event Action<string?, bool>? SingleEnemyDragTargetChanged;

    public void Render(
        CombatState combatState,
        RunState runState,
        GameContent gameContent,
        CardPlayService playService,
        BattleTargetingOverlay targeting,
        Func<string, Texture2D?> textureLoader,
        Func<string, Font?> fontLoader)
    {
        combat = combatState;
        run = runState;
        content = gameContent;
        cardPlayService = playService;
        targetingOverlay = targeting;
        loadTexture = textureLoader;
        loadFont = fontLoader;
        draggingCard = null;
        interactionsLocked = false;
        cardNodes.Clear();
        cardNodesByHandIndex.Clear();
        handCardsByIndex.Clear();
        ClearChildren();

        ClipContents = false;
        ZIndex = 80;

        var handCount = combat.DeckZones.Hand.Count;
        var cardWidth = handCount <= 5 ? 210f : 188f;
        var cardSize = CardPanel.SizeForWidth(cardWidth);
        var step = handCount <= 1 ? 0f : Math.Min(142f, (900f - cardWidth) / Math.Max(1, handCount - 1));
        var totalHandWidth = cardWidth + step * Math.Max(0, handCount - 1);
        var startHandX = (950f - totalHandWidth) * 0.5f;

        for (var handIndex = 0; handIndex < combat.DeckZones.Hand.Count; handIndex++)
        {
            var cardInstanceId = combat.DeckZones.Hand[handIndex];
            var card = ResolveCardDefinitionForInstance(cardInstanceId);
            var cardControl = CreateCardControl(cardInstanceId, card, handIndex);
            var normalized = handCount <= 1 ? 0f : (handIndex / (float)(handCount - 1) - 0.5f) * 2f;
            var arcLift = (1f - Math.Abs(normalized)) * 26f;
            cardControl.Position = new Vector2(startHandX + step * handIndex, 60f - arcLift);
            cardControl.Size = cardSize;
            cardControl.CustomMinimumSize = cardSize;
            cardControl.PivotOffset = cardSize * 0.5f;
            cardControl.RotationDegrees = normalized * 8f;
            cardControl.ZIndex = handIndex;
            InstallCardInteractions(cardControl, cardInstanceId, card, handIndex);

            cardNodesByHandIndex[handIndex] = cardControl;
            if (!cardNodes.TryGetValue(card.Id, out var controls))
            {
                controls = new List<Control>();
                cardNodes[card.Id] = controls;
            }

            controls.Add(cardControl);
            AddChild(cardControl);
        }
    }

    public override void _Process(double delta)
    {
        if (draggingCard is null)
        {
            return;
        }

        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            CompleteCardDrag();
            return;
        }

        UpdateCardDrag();
    }

    public void SetInteractionsLocked(bool locked)
    {
        interactionsLocked = locked;
        if (locked && draggingCard is not null)
        {
            CleanupDragVisuals(restoreCard: true);
        }
    }

    public void HidePlayedCard(int handIndex)
    {
        if (cardNodesByHandIndex.TryGetValue(handIndex, out var cardNode))
        {
            cardNode.Visible = false;
            cardNode.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    public Control? GetCardNodeByHandIndex(int handIndex)
    {
        return cardNodesByHandIndex.TryGetValue(handIndex, out var node) ? node : null;
    }

    public Control? GetFirstCardNode(string cardId)
    {
        return cardNodes.TryGetValue(cardId, out var nodes) ? nodes.FirstOrDefault() : null;
    }

    private Control CreateCardControl(string cardInstanceId, CardDefinition card, int handIndex)
    {
        var canPlay = PreviewCanPlayCard(cardInstanceId, card, handIndex);
        var enchantment = ResolveEnchantmentForInstance(cardInstanceId);
        var preview = RequireCardPlayService().PreviewCard(RequireCombat(), card, null, enchantment);
        var panel = CardPanel.Create(card, RequireContent(), RequireTextureLoader(), RequireFontLoader(), width: 220, dimmed: !canPlay.Succeeded, enchantment: enchantment, preview: preview);
        panel.MouseFilter = MouseFilterEnum.Ignore;
        return panel;
    }

    private void InstallCardInteractions(Control panel, string cardInstanceId, CardDefinition card, int handIndex)
    {
        var canPlay = PreviewCanPlayCard(cardInstanceId, card, handIndex);
        var enchantment = ResolveEnchantmentForInstance(cardInstanceId);
        var preview = RequireCardPlayService().PreviewCard(RequireCombat(), card, card.TargetRule == TargetRule.SingleEnemy ? RequireCombat().Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId : null, enchantment);
        var typeText = BuildCardTooltip(card, preview, canPlay);
        var interaction = new HandCardInteraction(
            panel,
            cardInstanceId,
            card.Id,
            handIndex,
            card,
            panel.Position,
            panel.RotationDegrees,
            panel.ZIndex,
            panel.Scale,
            panel.Modulate);
        handCardsByIndex[handIndex] = interaction;

        var button = new Button
        {
            Text = "",
            TooltipText = canPlay.Succeeded ? typeText : BattleScreen.FailureText(canPlay)
        };
        MakeTransparentButton(button);
        button.Position = Vector2.Zero;
        button.Size = panel.CustomMinimumSize;
        button.CustomMinimumSize = panel.CustomMinimumSize;
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.MouseEntered += () => SetCardHover(interaction, hovered: true);
        button.MouseExited += () => SetCardHover(interaction, hovered: false);
        button.GuiInput += input => OnCardGuiInput(interaction, input);
        panel.AddChild(button);

        interaction.ValidTargetMask = CreateValidTargetMask();
        panel.AddChild(interaction.ValidTargetMask);
    }

    private PlayCardResult PreviewCanPlayCard(string cardInstanceId, CardDefinition card, int handIndex)
    {
        var activeCombat = RequireCombat();
        var previewTarget = card.TargetRule == TargetRule.SingleEnemy
            ? activeCombat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId
            : null;
        return RequireCardPlayService().CanPlayCard(activeCombat, card, previewTarget, handIndex, ResolveEnchantmentForInstance(cardInstanceId), cardInstanceId);
    }

    private static string ColorEnergyCostText(CardDefinition card)
    {
        if (card.ColorEnergyCost is null)
        {
            return "?";
        }

        return card.ColorEnergyCost.Mode switch
        {
            ColorEnergySpendMode.Fixed => card.ColorEnergyCost.Amount.ToString(),
            ColorEnergySpendMode.X => $"X，至少 {card.ColorEnergyCost.MinAmount}",
            ColorEnergySpendMode.All => $"全部，至少 {card.ColorEnergyCost.MinAmount}",
            _ => "?"
        };
    }

    private string BuildCardTooltip(CardDefinition card, CardPlayPreview preview, PlayCardResult canPlay)
    {
        var builder = new List<string>();
        if (card.Type == CardType.Action)
        {
            builder.Add($"行动牌 / {card.Cost} 费");
            builder.Add($"附魔：{ColorName(preview.EnchantmentColor ?? ColorType.Colorless)}");
            builder.Add($"生成：{preview.GeneratedColorEnergyAmount} 点 {ColorName(preview.GeneratedColorEnergyColor)}彩能");
            var colorLines = preview.ColorEffects.Count == 0
                ? "颜色追加：无"
                : "颜色追加：" + string.Join("，", preview.ColorEffects.Select(ColorEffectText));
            builder.Add(colorLines);
        }
        else
        {
            builder.Add($"终结牌 / 需 {ColorEnergyCostText(card)} 彩能");
            builder.Add($"将消耗：{preview.ColorEnergyCost} 点");
            builder.Add(preview.ConsumedColors.Count == 0
                ? "颜色构成：无"
                : "颜色构成：" + string.Join(" / ", preview.ConsumedColors.Select(ColorName)));
            builder.Add("基础效果：" + string.Join("，", preview.BaseEffects.Select(effect => $"{effect.EffectType} {effect.Value}")));
            builder.Add(preview.ColorEffects.Count == 0
                ? "逐色追加：无"
                : "逐色追加：" + string.Join("；", preview.ColorEffects.Select(ColorEffectText)));
            builder.Add($"预估：伤害 {preview.EstimatedDamage} / 防御 {preview.EstimatedBlock} / 回复 {preview.EstimatedHealing} / 额外释放 {preview.EstimatedExtraCasts}");
        }

        if (!canPlay.Succeeded)
        {
            builder.Add(BattleScreen.FailureText(canPlay));
        }

        return string.Join("\n", builder);
    }

    private CardInstance ResolveCardInstance(string cardInstanceId)
    {
        return RequireRun().MasterDeckInstances.FirstOrDefault(instance =>
                string.Equals(instance.InstanceId, cardInstanceId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Unknown card instance id '{cardInstanceId}'.");
    }

    private CardDefinition ResolveCardDefinitionForInstance(string cardInstanceId)
    {
        var instance = ResolveCardInstance(cardInstanceId);
        return RequireContent().CardsById[instance.DefinitionId];
    }

    private CardEnchantment? ResolveEnchantmentForInstance(string cardInstanceId)
    {
        return ResolveCardInstance(cardInstanceId).Enchantment;
    }

    private static string ColorEffectText(ColorEffectPreview effect)
    {
        return effect.EffectType switch
        {
            "red_damage_bonus" => $"{ColorName(effect.Color)} +{effect.Value} 伤害",
            "extra_casts" => $"{ColorName(effect.Color)} +{effect.Value} 次释放",
            "gain_block" => $"{ColorName(effect.Color)} +{effect.Value} 防御",
            "heal" => $"{ColorName(effect.Color)} 回复 {effect.Value}",
            "double_final_value" => $"{ColorName(effect.Color)} 放大 {effect.Value} 次",
            _ => $"{ColorName(effect.Color)} {effect.EffectType} {effect.Value}"
        };
    }

    private static string ColorName(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "红色",
            ColorType.Yellow => "黄色",
            ColorType.Blue => "蓝色",
            ColorType.Green => "绿色",
            ColorType.Purple => "紫色",
            _ => "无色"
        };
    }

    private void OnCardGuiInput(HandCardInteraction card, InputEvent input)
    {
        if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            return;
        }

        BeginCardDrag(card);
        GetViewport().SetInputAsHandled();
    }

    private void SetCardHover(HandCardInteraction card, bool hovered)
    {
        if (draggingCard is not null || interactionsLocked || !GodotObject.IsInstanceValid(card.Node))
        {
            return;
        }

        card.IsHovered = hovered;
        if (hovered)
        {
            card.Node.Position = card.OriginalPosition + new Vector2(0, -54);
            card.Node.RotationDegrees = card.OriginalRotationDegrees;
            card.Node.ZIndex = 70;
            card.Node.Scale = card.OriginalScale * 1.02f;
            return;
        }

        RestoreCardTransform(card);
    }

    private void BeginCardDrag(HandCardInteraction card)
    {
        if (interactionsLocked || draggingCard is not null || !GodotObject.IsInstanceValid(card.Node) || card.Node.GetParent() is not Control parent)
        {
            return;
        }

        draggingCard = card;
        card.IsHovered = false;
        card.Node.ZIndex = 85;
        if (card.Card.TargetRule == TargetRule.SingleEnemy)
        {
            card.Node.Position = card.OriginalPosition + new Vector2(0, -62);
            card.Node.RotationDegrees = 0;
            card.Node.Scale = card.OriginalScale * 1.02f;
            SingleEnemyDragTargetChanged?.Invoke(null, true);
        }
        else
        {
            var mouseLocalToParent = ToLocal(parent, GetViewport().GetMousePosition());
            card.DragOffset = card.Node.Position - mouseLocalToParent;
            card.Node.RotationDegrees = 0;
            card.Node.Scale = card.OriginalScale * 1.04f;
        }

        UpdateCardDrag();
    }

    private void UpdateCardDrag()
    {
        var card = draggingCard;
        if (card is null || !GodotObject.IsInstanceValid(card.Node))
        {
            return;
        }

        var viewportMouse = GetViewport().GetMousePosition();
        if (card.Card.TargetRule == TargetRule.SingleEnemy)
        {
            RequireTargetingOverlay().HideReleaseZone();
            var hoveredEnemy = RequireTargetingOverlay().EnemyUnderMouse(RequireCombat().Enemies, viewportMouse);
            if (card.CurrentTargetEnemyId != hoveredEnemy)
            {
                card.CurrentTargetEnemyId = hoveredEnemy;
                SingleEnemyDragTargetChanged?.Invoke(hoveredEnemy, true);
            }

            RequireTargetingOverlay().UpdateEnemyHighlights(hoveredEnemy);
            var canPlay = hoveredEnemy is not null &&
                RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, hoveredEnemy, card.HandIndex, ResolveEnchantmentForInstance(card.CardInstanceId), card.CardInstanceId).Succeeded;
            SetValidTargetMask(card, canPlay);
            RequireTargetingOverlay().ShowArrowFromViewport(CurrentCardAnchorInViewport(card), viewportMouse, canPlay);
            return;
        }

        if (card.Node.GetParent() is not Control parent)
        {
            return;
        }

        var mouseLocalToParent = ToLocal(parent, viewportMouse);
        card.Node.Position = mouseLocalToParent + card.DragOffset;
        RequireTargetingOverlay().HideArrow();
        RequireTargetingOverlay().UpdateEnemyHighlights(null);
        var overReleaseZone = RequireTargetingOverlay().IsPointerOverReleaseZone(viewportMouse);
        var canReleasePlay = RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, null, card.HandIndex, ResolveEnchantmentForInstance(card.CardInstanceId), card.CardInstanceId).Succeeded;
        RequireTargetingOverlay().HideReleaseZone();
        SetValidTargetMask(card, overReleaseZone && canReleasePlay);
    }

    private void CompleteCardDrag()
    {
        var card = draggingCard;
        if (card is null)
        {
            return;
        }

        var viewportMouse = GetViewport().GetMousePosition();
        string? targetEnemyId = null;
        PlayCardResult canPlay;
        var shouldRequest = false;

        if (card.Card.TargetRule == TargetRule.SingleEnemy)
        {
            targetEnemyId = RequireTargetingOverlay().EnemyUnderMouse(RequireCombat().Enemies, viewportMouse);
            canPlay = RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, targetEnemyId, card.HandIndex, ResolveEnchantmentForInstance(card.CardInstanceId), card.CardInstanceId);
            shouldRequest = targetEnemyId is not null && canPlay.Succeeded;
            if (!shouldRequest)
            {
                var feedback = canPlay.FailureReason == PlayCardFailureReason.TargetMissing
                    ? "需要将箭头指向一个魔物目标"
                    : BattleScreen.FailureText(canPlay);
                FeedbackRequested?.Invoke(feedback);
            }
        }
        else
        {
            var overReleaseZone = RequireTargetingOverlay().IsPointerOverReleaseZone(viewportMouse);
            canPlay = RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, null, card.HandIndex, ResolveEnchantmentForInstance(card.CardInstanceId), card.CardInstanceId);
            shouldRequest = overReleaseZone && canPlay.Succeeded;
            if (overReleaseZone && !canPlay.Succeeded)
            {
                FeedbackRequested?.Invoke(BattleScreen.FailureText(canPlay));
            }
        }

        CleanupDragVisuals(restoreCard: !shouldRequest || card.Card.TargetRule == TargetRule.SingleEnemy);
        if (!shouldRequest)
        {
            return;
        }

        interactionsLocked = true;
        CardRequested?.Invoke(card.CardInstanceId, card.CardId, card.HandIndex, targetEnemyId);
    }

    private void CleanupDragVisuals(bool restoreCard)
    {
        var card = draggingCard;
        draggingCard = null;
        if (card?.Card.TargetRule == TargetRule.SingleEnemy)
        {
            SingleEnemyDragTargetChanged?.Invoke(null, false);
        }

        RequireTargetingOverlay().HideDragVisuals();
        if (card is not null)
        {
            SetValidTargetMask(card, visible: false);
        }

        if (restoreCard && card is not null && GodotObject.IsInstanceValid(card.Node))
        {
            RestoreCardTransform(card);
        }
    }

    private static void RestoreCardTransform(HandCardInteraction card)
    {
        card.Node.Position = card.OriginalPosition;
        card.Node.RotationDegrees = card.OriginalRotationDegrees;
        card.Node.ZIndex = card.OriginalZIndex;
        card.Node.Scale = card.OriginalScale;
        card.Node.Modulate = card.OriginalModulate;
        SetValidTargetMask(card, visible: false);
    }

    private static Vector2 CurrentCardAnchorInViewport(HandCardInteraction card)
    {
        return card.Node.GetGlobalTransformWithCanvas() * new Vector2(card.Node.Size.X * 0.5f, card.Node.Size.Y * 0.18f);
    }

    private CombatState RequireCombat()
    {
        return combat ?? throw new InvalidOperationException("BattleHandView requires an active combat state.");
    }

    private RunState RequireRun()
    {
        return run ?? throw new InvalidOperationException("BattleHandView requires an active run state.");
    }

    private GameContent RequireContent()
    {
        return content ?? throw new InvalidOperationException("BattleHandView requires loaded game content.");
    }

    private CardPlayService RequireCardPlayService()
    {
        return cardPlayService ?? throw new InvalidOperationException("BattleHandView requires a card play service.");
    }

    private BattleTargetingOverlay RequireTargetingOverlay()
    {
        return targetingOverlay ?? throw new InvalidOperationException("BattleHandView requires a targeting overlay.");
    }

    private Func<string, Texture2D?> RequireTextureLoader()
    {
        return loadTexture ?? throw new InvalidOperationException("BattleHandView requires a texture loader.");
    }

    private Func<string, Font?> RequireFontLoader()
    {
        return loadFont ?? throw new InvalidOperationException("BattleHandView requires a font loader.");
    }

    private void ClearChildren()
    {
        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                RemoveChild(node);
                node.QueueFree();
            }
        }
    }

    private static void MakeTransparentButton(Button button)
    {
        var empty = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", empty);
        button.AddThemeStyleboxOverride("hover", empty);
        button.AddThemeStyleboxOverride("pressed", empty);
        button.AddThemeStyleboxOverride("disabled", empty);
        button.AddThemeStyleboxOverride("focus", empty);
    }

    private static Control CreateValidTargetMask()
    {
        var mask = new Panel
        {
            Name = "ValidTargetMask",
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 95
        };
        mask.SetAnchorsPreset(LayoutPreset.FullRect);
        mask.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1.0f, 0.75f, 0.18f, 0.24f),
            BorderColor = new Color(1.0f, 0.88f, 0.38f, 0.84f),
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10
        });
        return mask;
    }

    private static void SetValidTargetMask(HandCardInteraction card, bool visible)
    {
        if (card.ValidTargetMask is not null && GodotObject.IsInstanceValid(card.ValidTargetMask))
        {
            card.ValidTargetMask.Visible = visible;
        }
    }

    private static Vector2 ToLocal(Control control, Vector2 viewportPoint)
    {
        return control.GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
    }

    private sealed class HandCardInteraction
    {
        public HandCardInteraction(
            Control node,
            string cardInstanceId,
            string cardId,
            int handIndex,
            CardDefinition card,
            Vector2 originalPosition,
            float originalRotationDegrees,
            int originalZIndex,
            Vector2 originalScale,
            Color originalModulate)
        {
            Node = node;
            CardInstanceId = cardInstanceId;
            CardId = cardId;
            HandIndex = handIndex;
            Card = card;
            OriginalPosition = originalPosition;
            OriginalRotationDegrees = originalRotationDegrees;
            OriginalZIndex = originalZIndex;
            OriginalScale = originalScale;
            OriginalModulate = originalModulate;
        }

        public Control Node { get; }

        public string CardInstanceId { get; }

        public string CardId { get; }

        public int HandIndex { get; }

        public CardDefinition Card { get; }

        public Vector2 OriginalPosition { get; }

        public float OriginalRotationDegrees { get; }

        public int OriginalZIndex { get; }

        public Vector2 OriginalScale { get; }

        public Color OriginalModulate { get; }

        public bool IsHovered { get; set; }

        public Vector2 DragOffset { get; set; }

        public string? CurrentTargetEnemyId { get; set; }

        public Control? ValidTargetMask { get; set; }
    }
}
