using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Cards;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleHandView : Control
{
    private readonly Dictionary<string, List<Control>> cardNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Control> cardNodesByHandIndex = new();
    private readonly Dictionary<int, HandCardInteraction> handCardsByIndex = new();

    private CombatState? combat;
    private GameContent? content;
    private CardPlayService? cardPlayService;
    private BattleTargetingOverlay? targetingOverlay;
    private Func<string, Texture2D?>? loadTexture;
    private Func<string, Font?>? loadFont;
    private HandCardInteraction? draggingCard;
    private bool interactionsLocked;

    public event Action<string, int, string?>? CardRequested;
    public event Action<string>? FeedbackRequested;

    public void Render(
        CombatState combatState,
        GameContent gameContent,
        CardPlayService playService,
        BattleTargetingOverlay targeting,
        Func<string, Texture2D?> textureLoader,
        Func<string, Font?> fontLoader)
    {
        combat = combatState;
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
            var cardId = combat.DeckZones.Hand[handIndex];
            var cardControl = CreateCardControl(cardId, handIndex);
            var normalized = handCount <= 1 ? 0f : (handIndex / (float)(handCount - 1) - 0.5f) * 2f;
            var arcLift = (1f - Math.Abs(normalized)) * 26f;
            cardControl.Position = new Vector2(startHandX + step * handIndex, 36f - arcLift);
            cardControl.Size = cardSize;
            cardControl.CustomMinimumSize = cardSize;
            cardControl.PivotOffset = cardSize * 0.5f;
            cardControl.RotationDegrees = normalized * 8f;
            cardControl.ZIndex = handIndex;
            InstallCardInteractions(cardControl, cardId, handIndex);

            cardNodesByHandIndex[handIndex] = cardControl;
            if (!cardNodes.TryGetValue(cardId, out var controls))
            {
                controls = new List<Control>();
                cardNodes[cardId] = controls;
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

    private Control CreateCardControl(string cardId, int handIndex)
    {
        var card = RequireContent().CardsById[cardId];
        var canPlay = PreviewCanPlayCard(card, handIndex);
        var panel = CardPanel.Create(card, RequireContent(), RequireTextureLoader(), RequireFontLoader(), width: 220, dimmed: !canPlay.Succeeded);
        panel.MouseFilter = MouseFilterEnum.Ignore;
        return panel;
    }

    private void InstallCardInteractions(Control panel, string cardId, int handIndex)
    {
        var card = RequireContent().CardsById[cardId];
        var canPlay = PreviewCanPlayCard(card, handIndex);
        var typeText = card.Type switch
        {
            CardType.Action => $"行动牌 / {card.Cost} 费 / +1 连锁",
            CardType.Skill => "技能牌 / 0 费",
            CardType.Finisher => $"终结牌 / 需 {card.MinChain} 连锁",
            _ => card.Type.ToString()
        };
        var interaction = new HandCardInteraction(
            panel,
            cardId,
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
    }

    private PlayCardResult PreviewCanPlayCard(CardDefinition card, int handIndex)
    {
        var activeCombat = RequireCombat();
        var previewTarget = card.TargetRule == TargetRule.SingleEnemy
            ? activeCombat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId
            : null;
        return RequireCardPlayService().CanPlayCard(activeCombat, card, previewTarget, handIndex);
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
        var mouseLocalToParent = ToLocal(parent, GetViewport().GetMousePosition());
        card.DragOffset = card.Node.Position - mouseLocalToParent;
        card.Node.ZIndex = 85;
        card.Node.RotationDegrees = 0;
        card.Node.Scale = card.OriginalScale * 1.04f;
        UpdateCardDrag();
    }

    private void UpdateCardDrag()
    {
        var card = draggingCard;
        if (card is null || !GodotObject.IsInstanceValid(card.Node) || card.Node.GetParent() is not Control parent)
        {
            return;
        }

        var viewportMouse = GetViewport().GetMousePosition();
        var mouseLocalToParent = ToLocal(parent, viewportMouse);
        card.Node.Position = mouseLocalToParent + card.DragOffset;

        if (card.Card.TargetRule == TargetRule.SingleEnemy)
        {
            RequireTargetingOverlay().HideReleaseZone();
            var hoveredEnemy = RequireTargetingOverlay().EnemyUnderMouse(RequireCombat().Enemies, viewportMouse);
            card.CurrentTargetEnemyId = hoveredEnemy;
            RequireTargetingOverlay().UpdateEnemyHighlights(hoveredEnemy);
            var canPlay = hoveredEnemy is not null &&
                RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, hoveredEnemy, card.HandIndex).Succeeded;
            RequireTargetingOverlay().ShowArrow(card.Node, viewportMouse, canPlay);
            return;
        }

        RequireTargetingOverlay().HideArrow();
        RequireTargetingOverlay().UpdateEnemyHighlights(null);
        var overReleaseZone = RequireTargetingOverlay().IsPointerOverReleaseZone(viewportMouse);
        var canReleasePlay = RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, null, card.HandIndex).Succeeded;
        RequireTargetingOverlay().ShowReleaseZone(overReleaseZone, canReleasePlay);
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
            canPlay = RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, targetEnemyId, card.HandIndex);
            shouldRequest = targetEnemyId is not null && canPlay.Succeeded;
            if (!shouldRequest)
            {
                var feedback = canPlay.FailureReason == PlayCardFailureReason.TargetMissing
                    ? "需要将箭头指向一个敌人目标"
                    : BattleScreen.FailureText(canPlay);
                FeedbackRequested?.Invoke(feedback);
            }
        }
        else
        {
            var overReleaseZone = RequireTargetingOverlay().IsPointerOverReleaseZone(viewportMouse);
            canPlay = RequireCardPlayService().CanPlayCard(RequireCombat(), card.Card, null, card.HandIndex);
            shouldRequest = overReleaseZone && canPlay.Succeeded;
            if (overReleaseZone && !canPlay.Succeeded)
            {
                FeedbackRequested?.Invoke(BattleScreen.FailureText(canPlay));
            }
        }

        CleanupDragVisuals(restoreCard: !shouldRequest);
        if (!shouldRequest)
        {
            return;
        }

        interactionsLocked = true;
        CardRequested?.Invoke(card.CardId, card.HandIndex, targetEnemyId);
    }

    private void CleanupDragVisuals(bool restoreCard)
    {
        var card = draggingCard;
        draggingCard = null;
        RequireTargetingOverlay().HideDragVisuals();

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
    }

    private CombatState RequireCombat()
    {
        return combat ?? throw new InvalidOperationException("BattleHandView requires an active combat state.");
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

    private static Vector2 ToLocal(Control control, Vector2 viewportPoint)
    {
        return control.GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
    }

    private sealed class HandCardInteraction
    {
        public HandCardInteraction(
            Control node,
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
    }
}
