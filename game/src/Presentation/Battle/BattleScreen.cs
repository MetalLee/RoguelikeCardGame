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
    private static readonly Vector2 EnemyBeatLanePosition = new(1065, 470);
    private static readonly Vector2 EnemyBeatLaneSize = new(710, 180);
    private static readonly Vector2 EnemyBeatSlotSize = new(150, 56);
    private static readonly Vector2 ThoughtBubblePosition = new(346, 382);
    private static readonly Vector2 ThoughtBubbleSize = new(610, 238);

    private readonly CardPlayService cardPlayService = new();
    private readonly Dictionary<int, Control> playerBeatSlotNodes = new();
    private readonly Dictionary<(string EnemyInstanceId, int BeatIndex), Control> enemyBeatSlotNodes = new();

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
    private int? activeTargetingPlayerBeatIndex;
    private bool showBattleLog;
    private string? currentFailureMessage;
    private int thoughtBubbleVersion;

    public event Action<string, string, int, string?>? CardRequested;
    public event Action<string, string, int, int>? BeatCardDroppedOnSlot;
    public event Action<int, BeatTarget>? BeatTargetSelected;
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
        activeTargetingPlayerBeatIndex = null;
        playerBeatSlotNodes.Clear();
        enemyBeatSlotNodes.Clear();

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

        var enemyBeatLane = CreateEnemyBeatLane(combat);
        if (enemyBeatLane is not null)
        {
            AddAt(root, enemyBeatLane, EnemyBeatLanePosition, EnemyBeatLaneSize);
        }

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
        battleHandView.BeatCardDropRequested += HandleBeatCardDropRequested;
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
        if (HandleBeatTargetingInput(@event))
        {
            return;
        }

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

    private void HandleBeatCardDropRequested(string cardInstanceId, string cardId, int handIndex, Vector2 viewportMouse)
    {
        var beatIndex = PlayerBeatIndexUnderMouse(viewportMouse);
        if (beatIndex is null)
        {
            ShowDragFeedback("需要把行动牌拖到一个空的玩家拍位");
            return;
        }

        BeatCardDroppedOnSlot?.Invoke(cardInstanceId, cardId, handIndex, beatIndex.Value);
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

    private Control? CreateEnemyBeatLane(CombatState combatState)
    {
        if (combatState.BeatRound is null || combatState.BeatRound.EnemyBeats.Count == 0)
        {
            return null;
        }

        var panel = CreateFramedPanel(Vector2.Zero, BloodLine);
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var column = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);

        foreach (var group in combatState.BeatRound.EnemyBeats
            .GroupBy(beat => beat.EnemyInstanceId)
            .OrderBy(group => EnemyOrder(group.Key)))
        {
            column.AddChild(CreateEnemyBeatGroup(combatState, group.Key, group.OrderBy(beat => beat.BeatIndex)));
        }

        return panel;
    }

    private Control CreateEnemyBeatGroup(
        CombatState combatState,
        string enemyInstanceId,
        IEnumerable<EnemyBeatSlot> enemyBeats)
    {
        var groupPanel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        groupPanel.AddThemeStyleboxOverride("panel", CreateButtonStyle(BloodLine, 0.64f));

        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 8);
        groupPanel.AddChild(row);

        var nameLabel = CreateSmallLabel(EnemyDisplayName(combatState, enemyInstanceId));
        nameLabel.CustomMinimumSize = new Vector2(116, EnemyBeatSlotSize.Y);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.VerticalAlignment = VerticalAlignment.Center;
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.76f, 0.54f));
        row.AddChild(nameLabel);

        foreach (var enemyBeat in enemyBeats)
        {
            row.AddChild(CreateEnemyBeatSlot(combatState, enemyBeat));
        }

        if (CanTargetEnemyBody(combatState, enemyInstanceId))
        {
            var bodyLabel = CreateSmallLabel("本体可锁定");
            bodyLabel.CustomMinimumSize = new Vector2(108, EnemyBeatSlotSize.Y);
            bodyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            bodyLabel.VerticalAlignment = VerticalAlignment.Center;
            bodyLabel.AddThemeFontSizeOverride("font_size", 15);
            bodyLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.36f));
            row.AddChild(bodyLabel);
        }

        return groupPanel;
    }

    private Control CreateEnemyBeatSlot(CombatState combatState, EnemyBeatSlot enemyBeat)
    {
        var lockedBy = PlayerBeatLockingEnemyBeat(combatState, enemyBeat.EnemyInstanceId, enemyBeat.BeatIndex);
        var slot = new PanelContainer
        {
            CustomMinimumSize = EnemyBeatSlotSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        enemyBeatSlotNodes[(enemyBeat.EnemyInstanceId, enemyBeat.BeatIndex)] = slot;
        slot.AddThemeStyleboxOverride("panel", CreateButtonStyle(lockedBy is null ? BloodLine : GoldLine, lockedBy is null ? 0.78f : 0.96f));

        var label = CreateSmallLabel(
            lockedBy is null
                ? $"敌 {enemyBeat.BeatIndex + 1}\n{BeatActionSummary(enemyBeat)}"
                : $"已锁定 P{lockedBy.Value + 1}\n{BeatActionSummary(enemyBeat)}");
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.ClipText = true;
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", lockedBy is null
            ? new Color(1.0f, 0.78f, 0.62f)
            : new Color(1.0f, 0.92f, 0.42f));
        slot.AddChild(label);
        return slot;
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
        var hasCard = !string.IsNullOrWhiteSpace(beat.CardId);
        var hasTarget = beat.Target is not null;
        var slot = new PanelContainer
        {
            CustomMinimumSize = BeatSlotSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        playerBeatSlotNodes[beat.BeatIndex] = slot;
        slot.AddThemeStyleboxOverride("panel", CreateButtonStyle(hasTarget ? GoldLine : FinisherLine, hasCard ? 0.94f : 0.82f));

        var label = CreateSmallLabel(!hasCard
            ? $"第 {beat.BeatIndex + 1} 拍"
            : $"{beat.CardId}\n{BeatTargetText(beat.Target)}");
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.ClipText = true;
        label.AddThemeFontSizeOverride("font_size", hasCard ? 14 : 18);
        label.AddThemeColorOverride("font_color", hasTarget
            ? new Color(1.0f, 0.92f, 0.42f)
            : new Color(1.0f, 0.86f, 0.50f));
        slot.AddChild(label);
        return slot;
    }

    private bool HandleBeatTargetingInput(InputEvent @event)
    {
        if (combat?.BeatRound is null || inputBlocker?.Visible == true)
        {
            return false;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton)
        {
            if (mouseButton.Pressed)
            {
                var beatIndex = TargetablePlayerBeatIndexUnderMouse(mouseButton.Position);
                if (beatIndex is null)
                {
                    return false;
                }

                activeTargetingPlayerBeatIndex = beatIndex.Value;
                UpdateBeatTargetingArrow(mouseButton.Position);
                GetViewport().SetInputAsHandled();
                return true;
            }

            if (activeTargetingPlayerBeatIndex is null)
            {
                return false;
            }

            var selectedTarget = TargetUnderMouse(mouseButton.Position);
            targetingOverlay.HideArrow();
            var playerBeatIndex = activeTargetingPlayerBeatIndex.Value;
            activeTargetingPlayerBeatIndex = null;
            if (selectedTarget is null)
            {
                ShowDragFeedback("需要把箭头指向魔物拍位；全部拍位锁定后可指向魔物本体");
                GetViewport().SetInputAsHandled();
                return true;
            }

            BeatTargetSelected?.Invoke(playerBeatIndex, selectedTarget);
            GetViewport().SetInputAsHandled();
            return true;
        }

        if (@event is InputEventMouseMotion mouseMotion && activeTargetingPlayerBeatIndex is not null)
        {
            UpdateBeatTargetingArrow(mouseMotion.Position);
            GetViewport().SetInputAsHandled();
            return true;
        }

        return false;
    }

    private void UpdateBeatTargetingArrow(Vector2 viewportMouse)
    {
        if (activeTargetingPlayerBeatIndex is null ||
            !playerBeatSlotNodes.TryGetValue(activeTargetingPlayerBeatIndex.Value, out var slot))
        {
            targetingOverlay.HideArrow();
            return;
        }

        targetingOverlay.ShowArrowFromViewport(CenterOf(slot), viewportMouse, TargetUnderMouse(viewportMouse) is not null);
    }

    private int? TargetablePlayerBeatIndexUnderMouse(Vector2 viewportPoint)
    {
        if (combat?.BeatRound is null)
        {
            return null;
        }

        foreach (var pair in playerBeatSlotNodes.OrderBy(item => item.Key))
        {
            if (!IsPointInsideControl(pair.Value, viewportPoint))
            {
                continue;
            }

            var beat = combat.BeatRound.PlayerBeats.FirstOrDefault(slot => slot.BeatIndex == pair.Key);
            if (beat is null ||
                string.IsNullOrWhiteSpace(beat.CardInstanceId) ||
                string.IsNullOrWhiteSpace(beat.CardId) ||
                beat.Target is not null)
            {
                return null;
            }

            return pair.Key;
        }

        return null;
    }

    private BeatTarget? TargetUnderMouse(Vector2 viewportPoint)
    {
        if (combat?.BeatRound is null)
        {
            return null;
        }

        foreach (var pair in enemyBeatSlotNodes.OrderByDescending(item => EnemyOrder(item.Key.EnemyInstanceId)).ThenByDescending(item => item.Key.BeatIndex))
        {
            if (!IsPointInsideControl(pair.Value, viewportPoint) ||
                PlayerBeatLockingEnemyBeat(combat, pair.Key.EnemyInstanceId, pair.Key.BeatIndex) is not null)
            {
                continue;
            }

            return new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBeat,
                EnemyInstanceId = pair.Key.EnemyInstanceId,
                EnemyBeatIndex = pair.Key.BeatIndex
            };
        }

        if (battleEnemyView is null)
        {
            return null;
        }

        foreach (var enemy in combat.Enemies.Where(enemy => enemy.CurrentHp > 0).Reverse())
        {
            if (!CanTargetEnemyBody(combat, enemy.InstanceId) ||
                !battleEnemyView.EnemyNodes.TryGetValue(enemy.InstanceId, out var enemyNode) ||
                !IsPointInsideControl(enemyNode, viewportPoint))
            {
                continue;
            }

            return new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBody,
                EnemyInstanceId = enemy.InstanceId
            };
        }

        return null;
    }

    private int? PlayerBeatIndexUnderMouse(Vector2 viewportPoint)
    {
        if (combat?.BeatRound is null)
        {
            return null;
        }

        foreach (var pair in playerBeatSlotNodes.OrderBy(item => item.Key))
        {
            if (!IsPointInsideControl(pair.Value, viewportPoint))
            {
                continue;
            }

            var beat = combat.BeatRound.PlayerBeats.FirstOrDefault(slot => slot.BeatIndex == pair.Key);
            if (beat is null ||
                !string.IsNullOrWhiteSpace(beat.CardInstanceId) ||
                !string.IsNullOrWhiteSpace(beat.CardId) ||
                beat.Target is not null)
            {
                return null;
            }

            return pair.Key;
        }

        return null;
    }

    private int EnemyOrder(string enemyInstanceId)
    {
        return combat?.Enemies.FindIndex(enemy => string.Equals(enemy.InstanceId, enemyInstanceId, StringComparison.Ordinal)) ?? 0;
    }

    private static int? PlayerBeatLockingEnemyBeat(CombatState combatState, string enemyInstanceId, int enemyBeatIndex)
    {
        return combatState.BeatRound?.PlayerBeats
            .Where(beat =>
                beat.Target?.Kind == BeatTargetKind.EnemyBeat &&
                string.Equals(beat.Target.EnemyInstanceId, enemyInstanceId, StringComparison.Ordinal) &&
                beat.Target.EnemyBeatIndex == enemyBeatIndex)
            .Select(beat => (int?)beat.BeatIndex)
            .FirstOrDefault();
    }

    private static bool CanTargetEnemyBody(CombatState combatState, string enemyInstanceId)
    {
        if (combatState.BeatRound is null)
        {
            return false;
        }

        var enemyBeatKeys = combatState.BeatRound.EnemyBeats
            .Where(beat => string.Equals(beat.EnemyInstanceId, enemyInstanceId, StringComparison.Ordinal))
            .Select(beat => beat.BeatIndex)
            .ToHashSet();
        if (enemyBeatKeys.Count == 0)
        {
            return true;
        }

        var lockedBeatKeys = combatState.BeatRound.PlayerBeats
            .Where(beat =>
                beat.Target?.Kind == BeatTargetKind.EnemyBeat &&
                string.Equals(beat.Target.EnemyInstanceId, enemyInstanceId, StringComparison.Ordinal) &&
                beat.Target.EnemyBeatIndex is not null)
            .Select(beat => beat.Target!.EnemyBeatIndex!.Value)
            .ToHashSet();
        return enemyBeatKeys.All(lockedBeatKeys.Contains);
    }

    private static string EnemyDisplayName(CombatState combatState, string enemyInstanceId)
    {
        var index = combatState.Enemies.FindIndex(enemy => string.Equals(enemy.InstanceId, enemyInstanceId, StringComparison.Ordinal));
        return index < 0 ? "魔物" : $"魔物 {index + 1}";
    }

    private static string BeatActionSummary(EnemyBeatSlot enemyBeat)
    {
        if (enemyBeat.Hidden)
        {
            return "未知";
        }

        if (enemyBeat.Actions.Count == 0)
        {
            return enemyBeat.ActionCardId;
        }

        return string.Join(" / ", enemyBeat.Actions.Select(BeatActionText));
    }

    private static string BeatActionText(BeatActionDefinition action)
    {
        var repeat = action.Repeat > 1 ? $"x{action.Repeat}" : "";
        return action.Kind switch
        {
            BeatActionKind.Attack => $"{AttackTypeText(action.AttackType)}{action.Value}{repeat}",
            BeatActionKind.Block => $"防{action.Value}{repeat}",
            BeatActionKind.Dodge => $"闪{action.DodgeChancePercent}%",
            _ => action.Kind.ToString()
        };
    }

    private static string AttackTypeText(BeatAttackType? attackType)
    {
        return attackType switch
        {
            BeatAttackType.Slash => "斩",
            BeatAttackType.Strike => "击",
            BeatAttackType.Projectile => "弹",
            _ => "攻"
        };
    }

    private static string BeatTargetText(BeatTarget? target)
    {
        return target?.Kind switch
        {
            BeatTargetKind.EnemyBeat => $"目标：敌拍 {target.EnemyBeatIndex.GetValueOrDefault() + 1}",
            BeatTargetKind.EnemyBody => "目标：本体",
            _ => "拖出箭头选目标"
        };
    }

    private static bool IsPointInsideControl(Control control, Vector2 viewportPoint)
    {
        if (!GodotObject.IsInstanceValid(control) || !control.Visible)
        {
            return false;
        }

        var localPoint = control.GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
        var size = control.Size;
        if (size.X <= 0 || size.Y <= 0)
        {
            size = control.CustomMinimumSize;
        }

        return localPoint.X >= 0 &&
            localPoint.Y >= 0 &&
            localPoint.X <= size.X &&
            localPoint.Y <= size.Y;
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
