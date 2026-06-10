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
    private string? selectedEnemyInstanceId;
    private readonly BattleTargetingOverlay targetingOverlay = new();
    private BattleLogAnimator? logAnimator;
    private BattleHudView? battleHudView;
    private BattleEnemyView? battleEnemyView;
    private BattleHandView? battleHandView;
    private Control? playerNode;
    private Control? chainPanel;
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

    public event Action<string>? EnemySelected;
    public event Action<string, int, string?>? CardRequested;
    public event Action? EndTurnRequested;
    public event Action? RestartRequested;

    public void Render(
        CombatState combatState,
        RunState runState,
        EncounterDefinition encounterDefinition,
        string? selectedEnemy,
        string? message = null)
    {
        combat = combatState;
        run = runState;
        encounter = encounterDefinition;
        selectedEnemyInstanceId = selectedEnemy;
        currentMessage = message;
        battleHudView = null;
        battleEnemyView = null;
        battleHandView = null;
        playerNode = null;
        chainPanel = null;
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
        battleHudView.Render(root, combat, run, RequireContent(), selectedEnemyInstanceId, LoadTexture, LoadFont, () =>
        {
            SetInteractionsLocked(true);
            EndTurnRequested?.Invoke();
        });
        chainPanel = battleHudView.ChainPanel;
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
        battleEnemyView.EnemySelected += enemyId => EnemySelected?.Invoke(enemyId);
        battleEnemyView.Render(root, combat.Enemies, RequireContent(), selectedEnemyInstanceId, targetingOverlay, LoadTexture);

        battleHandView = new BattleHandView();
        battleHandView.CardRequested += (cardId, handIndex, targetEnemyId) =>
        {
            SetInteractionsLocked(true);
            CardRequested?.Invoke(cardId, handIndex, targetEnemyId);
        };
        battleHandView.FeedbackRequested += ShowDragFeedback;
        battleHandView.Render(combat, RequireContent(), cardPlayService, targetingOverlay, LoadTexture, LoadFont);
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
            Render(combat, run, encounter, selectedEnemyInstanceId, currentMessage);
        }

        GetViewport().SetInputAsHandled();
    }

    public static string FailureText(PlayCardResult result)
    {
        return result.FailureReason switch
        {
            PlayCardFailureReason.InsufficientActionPoints => $"行动点不足：需要 {result.RequiredActionPoints}，当前 {result.CurrentActionPoints}",
            PlayCardFailureReason.InsufficientChain => $"连锁不足：需要 {result.RequiredChain}，当前 {result.CurrentChain}",
            PlayCardFailureReason.TargetMissing => "需要先选择一个敌人目标",
            PlayCardFailureReason.NotPlayerTurn => "当前不是玩家回合",
            PlayCardFailureReason.CardNotInHand => "这张牌不在手牌中",
            _ => "无法打出"
        };
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
        var source = string.IsNullOrWhiteSpace(item.SourceId) ? "" : $" {item.SourceId}";
        var numbers = item.NumericChanges.Count == 0
            ? ""
            : " " + string.Join(", ", item.NumericChanges.Select(pair => $"{pair.Key}:{pair.Value}"));
        return $"{item.EventType}{source}{numbers}";
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
            chainPanel,
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
