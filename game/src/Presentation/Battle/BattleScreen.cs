using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Battle;

public partial class BattleScreen : ComicScreen
{
    private const float PlayerStageGroundY = 785f;

    private readonly CardPlayService cardPlayService = new();

    private CombatState? combat;
    private RunState? run;
    private EncounterDefinition? encounter;
    private string? hoveredEnemyInstanceId;
    private string? dragPointedEnemyInstanceId;
    private bool isSingleEnemyDragActive;
    private readonly BattleTargetingOverlay targetingOverlay = new();
    private BattleLogAnimator? logAnimator;
    private BattleHudView? battleHudView;
    private BattleEnemyView? battleEnemyView;
    private BattleHandView? battleHandView;
    private Control? playerNode;
    private Control? colorEnergyPanel;
    private Control? blockPanel;
    private Control? actionPointPanel;
    private Control? handNode;
    private Control? drawPilePanel;
    private Control? discardPilePanel;
    private Control? fxLayer;
    private Control? canvasRoot;
    private Control? dragFeedbackPanel;
    private Control? inputBlocker;
    private bool showBattleLog;
    private string? currentMessage;

    public event Action<string, int, string?>? CardRequested;
    public event Action? EndTurnRequested;
    public event Action? RestartRequested;

    public void Render(
        CombatState combatState,
        RunState runState,
        EncounterDefinition encounterDefinition,
        string? message = null)
    {
        combat = combatState;
        run = runState;
        encounter = encounterDefinition;
        hoveredEnemyInstanceId = null;
        dragPointedEnemyInstanceId = null;
        isSingleEnemyDragActive = false;
        currentMessage = message;
        battleHudView = null;
        battleEnemyView = null;
        battleHandView = null;
        playerNode = null;
        colorEnergyPanel = null;
        blockPanel = null;
        actionPointPanel = null;
        handNode = null;
        drawPilePanel = null;
        discardPilePanel = null;
        fxLayer = null;
        canvasRoot = null;
        dragFeedbackPanel = null;
        inputBlocker = null;

        var root = CreateCanvas();
        canvasRoot = root;
        logAnimator ??= new BattleLogAnimator(this);
        targetingOverlay.Initialize(root);

        battleHudView = new BattleHudView();
        battleHudView.Render(root, combat, run, RequireContent(), LoadTexture, LoadFont, () =>
        {
            SetInteractionsLocked(true);
            EndTurnRequested?.Invoke();
        });
        colorEnergyPanel = battleHudView.ColorEnergyPanel;
        blockPanel = battleHudView.BlockPanel;
        actionPointPanel = battleHudView.ActionPointPanel;
        drawPilePanel = battleHudView.DrawPilePanel;
        discardPilePanel = battleHudView.DiscardPilePanel;

        var playerSize = new Vector2(430, 430);
        var playerPosition = new Vector2(250, PlayerStageGroundY - playerSize.Y + 50);
        playerNode = CreateImage("asset.character.swordsman.battle", playerSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        playerNode.Name = "PlayerStand";
        AddAt(root, playerNode, playerPosition, playerSize);

        battleEnemyView = new BattleEnemyView();
        battleEnemyView.EnemyHoveredChanged += OnEnemyHoveredChanged;
        battleEnemyView.Render(root, combat, RequireContent(), targetingOverlay, LoadTexture, LoadFont);

        battleHandView = new BattleHandView();
        battleHandView.CardRequested += (cardId, handIndex, targetEnemyId) =>
        {
            SetInteractionsLocked(true);
            CardRequested?.Invoke(cardId, handIndex, targetEnemyId);
        };
        battleHandView.FeedbackRequested += ShowDragFeedback;
        battleHandView.SingleEnemyDragTargetChanged += OnSingleEnemyDragTargetChanged;
        battleHandView.Render(combat, run, RequireContent(), cardPlayService, targetingOverlay, LoadTexture, LoadFont);
        handNode = battleHandView;
        AddAt(root, battleHandView, new Vector2(475, 742), new Vector2(950, 360));

        var restart = CreateArtButton("重开", "asset.ui.icon.deck_library", new Vector2(122, 48), new Color(0.36f, 0.31f, 0.26f));
        restart.Pressed += () => RestartRequested?.Invoke();
        AddAt(root, restart, new Vector2(44, 214), new Vector2(122, 48));

        if (!string.IsNullOrWhiteSpace(message))
        {
            AddAt(root, CreateMessagePanel(message), new Vector2(650, 150), new Vector2(620, 44));
        }

        if (showBattleLog)
        {
            AddAt(root, CreateLogPreview(), new Vector2(1365, 230), new Vector2(390, 166));
        }

        fxLayer = CreateFxLayer("FxLayer");
        root.AddChild(fxLayer);

        inputBlocker = CreateInputBlocker();
        root.AddChild(inputBlocker);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent ||
            keyEvent.Keycode != Key.F12)
        {
            return;
        }

        showBattleLog = !showBattleLog;
        if (combat is not null && run is not null && encounter is not null)
        {
            Render(combat, run, encounter, currentMessage);
        }

        GetViewport().SetInputAsHandled();
    }

    public static string FailureText(PlayCardResult result)
    {
        return result.FailureReason switch
        {
            PlayCardFailureReason.InsufficientActionPoints => $"行动点不足：需要 {result.RequiredActionPoints}，当前 {result.CurrentActionPoints}",
            PlayCardFailureReason.InsufficientColorEnergy => $"彩能不足：需要 {result.RequiredColorEnergy}，当前 {result.CurrentColorEnergy}",
            PlayCardFailureReason.TargetMissing => "需要将箭头指向一个魔物目标",
            PlayCardFailureReason.NotPlayerTurn => "当前不是玩家回合",
            PlayCardFailureReason.CardNotInHand => "这张牌不在手牌中",
            _ => "无法打出"
        };
    }


    private void OnEnemyHoveredChanged(string? enemyInstanceId)
    {
        hoveredEnemyInstanceId = enemyInstanceId;
        RefreshEnemyHudFocus();
    }

    private void OnSingleEnemyDragTargetChanged(string? enemyInstanceId, bool isDragging)
    {
        isSingleEnemyDragActive = isDragging;
        dragPointedEnemyInstanceId = enemyInstanceId;
        RefreshEnemyHudFocus();
    }

    private void RefreshEnemyHudFocus()
    {
        var focusEnemyId = isSingleEnemyDragActive ? dragPointedEnemyInstanceId : hoveredEnemyInstanceId;
        battleHudView?.SetFocusedEnemy(focusEnemyId);
    }

    private void ShowDragFeedback(string text)
    {
        RemoveDragFeedback();
        if (canvasRoot is null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        dragFeedbackPanel = CreateMessagePanel(text);
        dragFeedbackPanel.ZIndex = 120;
        AddAt(canvasRoot, dragFeedbackPanel, new Vector2(650, 150), new Vector2(620, 44));
    }

    private void RemoveDragFeedback()
    {
        if (dragFeedbackPanel is null)
        {
            return;
        }

        dragFeedbackPanel.GetParent()?.RemoveChild(dragFeedbackPanel);
        dragFeedbackPanel.QueueFree();
        dragFeedbackPanel = null;
    }

    private Control CreateLogPreview()
    {
        var latest = combat is null
            ? Enumerable.Empty<string>()
            : combat.Log.TakeLast(5).Reverse().Select(LogLine);
        var panel = CreateFramedPanel(new Vector2(0, 106), new Color(0.36f, 0.31f, 0.26f));
        var label = CreateSmallLabel("最近结算：\n" + string.Join("\n", latest));
        panel.AddChild(label);
        return panel;
    }

    private static string LogLine(CombatLogEvent item)
    {
        if (item.Metadata.TryGetValue("effect_type", out var effectType))
        {
            if (effectType == "gain_color_energy")
            {
                var color = item.Metadata.TryGetValue("color", out var colorName) ? colorName : "Colorless";
                var gained = item.NumericChanges.GetValueOrDefault("color_energy_generated");
                return $"获得彩能 {ColorText(color)} x{gained}";
            }

            if (effectType == "yellow_extra_casts")
            {
                return $"黄色追加释放 +{item.NumericChanges.GetValueOrDefault("extra_casts")}";
            }

            if (effectType == "blue_gain_block")
            {
                return $"蓝色追加防御 +{item.NumericChanges.GetValueOrDefault("block_gained")}";
            }

            if (effectType == "green_heal")
            {
                return $"绿色追加回复 +{item.NumericChanges.GetValueOrDefault("healed")}";
            }
        }

        if (item.EventType == CombatLogEventType.CardPlayed && item.NumericChanges.GetValueOrDefault("color_energy_spent") > 0)
        {
            return $"消耗彩能 {item.NumericChanges.GetValueOrDefault("color_energy_spent")}：{item.Metadata.GetValueOrDefault("spent_colors")}";
        }

        if (item.NumericChanges.GetValueOrDefault("purple_multiplier") > 1)
        {
            return $"紫色放大 x{item.NumericChanges.GetValueOrDefault("purple_multiplier")}";
        }

        var source = string.IsNullOrWhiteSpace(item.SourceId) ? "" : $" {item.SourceId}";
        var numbers = item.NumericChanges.Count == 0
            ? ""
            : " " + string.Join(", ", item.NumericChanges.Select(pair => $"{pair.Key}:{pair.Value}"));
        return $"{item.EventType}{source}{numbers}";
    }

    private static string ColorText(string color)
    {
        return color switch
        {
            "Red" => "红色",
            "Yellow" => "黄色",
            "Blue" => "蓝色",
            "Green" => "绿色",
            "Purple" => "紫色",
            _ => "无色"
        };
    }

    public void HidePlayedCard(int handIndex)
    {
        battleHandView?.HidePlayedCard(handIndex);
    }

    public void SetInteractionsLocked(bool locked)
    {
        battleHandView?.SetInteractionsLocked(locked);
        if (inputBlocker is not null)
        {
            inputBlocker.Visible = locked;
        }
    }

    public async Task PlayLogAnimationsAsync(
        IReadOnlyList<CombatLogEvent> events,
        CardDefinition? playedCard = null,
        int? playedHandIndex = null,
        bool playConcurrently = false)
    {
        if (logAnimator is null)
        {
            return;
        }

        await logAnimator.PlayAsync(events, CreateAnimationTargets(), playedCard, playedHandIndex, playConcurrently);
    }

    private BattleAnimationTargets CreateAnimationTargets()
    {
        return new BattleAnimationTargets(
            playerNode,
            colorEnergyPanel,
            blockPanel,
            actionPointPanel,
            handNode,
            drawPilePanel,
            discardPilePanel,
            fxLayer,
            battleEnemyView?.EnemyNodes ?? new Dictionary<string, Control>(),
            handIndex => battleHandView?.GetCardNodeByHandIndex(handIndex),
            cardId => battleHandView?.GetFirstCardNode(cardId));
    }

    private static Control CreateInputBlocker()
    {
        return new Control
        {
            Name = "BattleInputBlocker",
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
            ZIndex = 300,
            ZAsRelative = false
        };
    }
}
