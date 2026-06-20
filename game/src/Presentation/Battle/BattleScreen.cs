using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Battle;

public partial class BattleScreen : ComicScreen
{
    private const float PlayerStageGroundY = 980f;
    private const float PlayerSpriteBottomTransparentPadding = 23f;
    private const double ThoughtBubbleDurationSeconds = 2.0;
    private static readonly Vector2 BeatLanePosition = new(440, 626);
    private static readonly Vector2 BeatLaneSize = new(570, 82);
    private static readonly Vector2 BeatSlotSize = new(170, 58);
    private static readonly Vector2 ThoughtBubblePosition = new(346, 382);
    private static readonly Vector2 ThoughtBubbleSize = new(610, 238);

    private readonly CardPlayService cardPlayService = new();

    private CombatState? combat;
    private RunState? run;
    private EncounterDefinition? encounter;
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
    private Control? beatLane;
    private Control? fxLayer;
    private Control? canvasRoot;
    private Control? thoughtFeedbackPanel;
    private Control? inputBlocker;
    private bool showBattleLog;
    private string? currentFailureMessage;
    private int thoughtBubbleVersion;

    public event Action<string, string, int, string?>? CardRequested;
    public event Action? EndTurnRequested;
    public event Action? RestartRequested;

    public void Render(
        CombatState combatState,
        RunState runState,
        EncounterDefinition encounterDefinition,
        string? failureMessage = null)
    {
        combat = combatState;
        run = runState;
        encounter = encounterDefinition;
        currentFailureMessage = failureMessage;
        battleHudView = null;
        battleEnemyView = null;
        battleHandView = null;
        playerNode = null;
        colorEnergyPanel = null;
        blockPanel = null;
        actionPointPanel = null;
        handNode = null;
        beatLane = null;
        fxLayer = null;
        canvasRoot = null;
        thoughtFeedbackPanel = null;
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

        var playerSize = new Vector2(212, 446);
        var playerPosition = new Vector2(230, PlayerStageGroundY - playerSize.Y + PlayerSpriteBottomTransparentPadding);
        playerNode = CreateImage("asset.character.zu.revolver.battle", playerSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        playerNode.Name = "PlayerStand";
        AddAt(root, playerNode, playerPosition, playerSize);

        battleEnemyView = new BattleEnemyView();
        battleEnemyView.Render(root, combat, RequireContent(), targetingOverlay, LoadTexture);

        beatLane = CreateBeatLane(combat);
        if (beatLane is not null)
        {
            AddAt(root, beatLane, BeatLanePosition, BeatLaneSize);
        }

        battleHandView = new BattleHandView();
        battleHandView.CardRequested += (cardInstanceId, cardId, handIndex, targetEnemyId) =>
        {
            SetInteractionsLocked(true);
            CardRequested?.Invoke(cardInstanceId, cardId, handIndex, targetEnemyId);
        };
        battleHandView.FeedbackRequested += ShowDragFeedback;
        battleHandView.Render(combat, run, RequireContent(), cardPlayService, targetingOverlay, LoadTexture, LoadFont, beatPrototype: combat.BeatRound is not null);
        handNode = battleHandView;
        AddAt(root, battleHandView, new Vector2(475, 742), new Vector2(950, 360));

        if (!string.IsNullOrWhiteSpace(failureMessage))
        {
            ShowFailureThoughtBubble(failureMessage);
        }

        if (showBattleLog)
        {
            var restart = CreateRestartButton();
            restart.Pressed += () => RestartRequested?.Invoke();
            AddAt(root, restart, new Vector2(44, 214), new Vector2(122, 48));
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
            Render(combat, run, encounter, currentFailureMessage);
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


    private void ShowDragFeedback(string text)
    {
        if (canvasRoot is null || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        currentFailureMessage = text;
        ShowFailureThoughtBubble(text);
    }

    private void ShowFailureThoughtBubble(string text)
    {
        RemoveThoughtFeedback();
        if (canvasRoot is null)
        {
            return;
        }

        thoughtFeedbackPanel = CreateThoughtBubble(text);
        thoughtFeedbackPanel.ZIndex = 120;
        AddAt(canvasRoot, thoughtFeedbackPanel, ThoughtBubblePosition, ThoughtBubbleSize);
        _ = HideThoughtFeedbackAfterDelayAsync(++thoughtBubbleVersion, thoughtFeedbackPanel);
    }

    private async Task HideThoughtFeedbackAfterDelayAsync(int version, Control panel)
    {
        await ToSignal(GetTree().CreateTimer(ThoughtBubbleDurationSeconds), "timeout");
        if (version != thoughtBubbleVersion || !GodotObject.IsInstanceValid(panel))
        {
            return;
        }

        RemoveThoughtFeedback();
        currentFailureMessage = null;
    }

    private void RemoveThoughtFeedback()
    {
        thoughtBubbleVersion++;
        if (thoughtFeedbackPanel is null)
        {
            return;
        }

        thoughtFeedbackPanel.GetParent()?.RemoveChild(thoughtFeedbackPanel);
        thoughtFeedbackPanel.QueueFree();
        thoughtFeedbackPanel = null;
    }

    private ThoughtBubblePanel CreateThoughtBubble(string text)
    {
        return new ThoughtBubblePanel
        {
            ThoughtText = text,
            ThoughtFont = LoadFont("asset.font.source_han_sans_sc.medium"),
            MouseFilter = MouseFilterEnum.Ignore
        };
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

    private Control? CreateBeatLane(CombatState combatState)
    {
        if (combatState.BeatRound is null)
        {
            return null;
        }

        var panel = CreateFramedPanel(Vector2.Zero, FinisherLine);
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 12);
        panel.AddChild(row);

        foreach (var beat in combatState.BeatRound.PlayerBeats.OrderBy(slot => slot.BeatIndex))
        {
            row.AddChild(CreateBeatSlot(beat));
        }

        return panel;
    }

    private Control CreateBeatSlot(PlayerBeatSlot beat)
    {
        var slot = new PanelContainer
        {
            CustomMinimumSize = BeatSlotSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        slot.AddThemeStyleboxOverride("panel", CreateButtonStyle(FinisherLine, 0.82f));

        var label = CreateSmallLabel(string.IsNullOrWhiteSpace(beat.CardId)
            ? $"第 {beat.BeatIndex + 1} 拍"
            : beat.CardId);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.ClipText = true;
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.86f, 0.50f));
        slot.AddChild(label);
        return slot;
    }

    private Button CreateRestartButton()
    {
        return new SketchParallelogramButton(
            "重开",
            LoadFont("asset.font.source_han_sans_sc.heavy"),
            20)
        {
            TooltipText = "重新开始当前运行",
            CustomMinimumSize = new Vector2(122, 48)
        };
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
