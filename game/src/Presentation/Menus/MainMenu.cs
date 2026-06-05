using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Rewards;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class MainMenu : Control
{
    private readonly RunStateFactory runFactory = new();
    private readonly RunProgressService runProgressService = new();
    private readonly CombatStateFactory combatFactory = new();
    private readonly CombatTurnService turnService = new();
    private readonly CardPlayService cardPlayService = new();
    private readonly RewardService rewardService = new();

    private GameContent? content;
    private RunState? run;
    private CombatState? combat;
    private EncounterDefinition? encounter;
    private string? selectedEnemyInstanceId;
    private int actionCardsPlayedThisTurn;
    private RewardPackDefinition? openedRewardPack;
    private readonly HashSet<string> selectedRewardCards = new(StringComparer.Ordinal);

    public override void _Ready()
    {
        try
        {
            content = GameContent.LoadFromProject();
            ShowMainMenu();
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
        }
    }

    private void ShowMainMenu()
    {
        ClearChildren();
        var root = CreatePage("RoguelikeCardGame MVP");
        root.AddChild(CreateBodyLabel("暗黑哥特 + 手绘漫画方向的最小可玩验证版。当前版本验证 6 场固定战斗、连锁层数、终结牌、卡牌包奖励和精英遗物。"));

        var start = CreatePrimaryButton("开始 MVP Run");
        start.Pressed += StartNewRun;
        root.AddChild(start);

        var hint = CreateSmallLabel("操作：先点敌人选择目标，再点手牌出牌。行动牌消耗行动点并 +1 连锁；终结牌满足连锁后可打出并清空连锁。");
        root.AddChild(hint);
    }

    private void StartNewRun()
    {
        RequireContent();
        var sequence = content!.MvpRun;
        run = runFactory.CreateNewRun(
            runId: $"run_{DateTime.UtcNow:yyyyMMddHHmmss}",
            seed: 12345,
            playerMaxHp: sequence.PlayerMaxHp,
            baseActionPoints: sequence.BaseActionPoints,
            cardsPerTurn: sequence.CardsPerTurn,
            starterDeck: sequence.StarterDeck,
            encounterSequence: sequence.EncounterSequence);
        StartCurrentEncounter();
    }

    private void StartCurrentEncounter()
    {
        RequireContent();
        if (run is null)
        {
            ShowMainMenu();
            return;
        }

        if (run.Status != RunStatus.InProgress)
        {
            ShowRunResult();
            return;
        }

        run = runProgressService.PrepareForCombat(run);
        var encounterId = run.EncounterSequence[run.CurrentEncounterIndex];
        encounter = content!.EncountersById[encounterId];
        combat = combatFactory.CreateCombat(
            combatId: $"combat_{run.CurrentEncounterIndex + 1}",
            runState: run,
            encounter: encounter,
            enemiesById: content.EnemiesById);
        combat = turnService.StartCombat(combat);
        selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
        actionCardsPlayedThisTurn = 0;
        ShowBattle();
    }

    private void ShowBattle(string? message = null)
    {
        RequireContent();
        if (combat is null || encounter is null || run is null)
        {
            ShowMainMenu();
            return;
        }

        ClearChildren();
        var root = CreatePage($"第 {run.CurrentEncounterIndex + 1}/6 战：{content!.T(encounter.TeachingGoalKey)}");

        var status = new HBoxContainer();
        status.AddThemeConstantOverride("separation", 12);
        status.AddChild(CreateInfoPanel($"HP {combat.PlayerHp}/{combat.PlayerMaxHp}", new Color(0.55f, 0.08f, 0.08f)));
        status.AddChild(CreateInfoPanel($"防御 {combat.PlayerBlock}", new Color(0.08f, 0.17f, 0.25f)));
        status.AddChild(CreateInfoPanel($"行动点 {combat.ActionPoints}", new Color(0.22f, 0.18f, 0.06f)));
        status.AddChild(CreateInfoPanel($"连锁 {combat.Chain}   阈值 3 / 5 / 8", new Color(0.18f, 0.08f, 0.08f)));
        status.AddChild(CreateInfoPanel($"抽 {combat.DeckZones.DrawPileCount}  弃 {combat.DeckZones.DiscardPileCount}  牌组 {run.MasterDeck.Count}", new Color(0.08f, 0.11f, 0.12f)));
        root.AddChild(status);

        if (!string.IsNullOrWhiteSpace(message))
        {
            root.AddChild(CreateMessage(message));
        }

        if (run.RelicIds.Count > 0)
        {
            root.AddChild(CreateSmallLabel("遗物：" + string.Join(" / ", run.RelicIds.Select(id => content.T(content.RelicsById[id].TextKey)))));
        }

        var enemies = new HBoxContainer();
        enemies.AddThemeConstantOverride("separation", 12);
        foreach (var enemyState in combat.Enemies)
        {
            enemies.AddChild(CreateEnemyButton(enemyState));
        }

        root.AddChild(enemies);

        var handLabel = CreateSmallLabel("手牌");
        root.AddChild(handLabel);
        var hand = new HBoxContainer();
        hand.AddThemeConstantOverride("separation", 8);
        foreach (var cardId in combat.DeckZones.Hand)
        {
            hand.AddChild(CreateCardButton(cardId));
        }

        root.AddChild(hand);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        var endTurn = CreatePrimaryButton("结束回合");
        endTurn.Pressed += EndTurn;
        actions.AddChild(endTurn);

        var restart = new Button { Text = "重开" };
        restart.Pressed += StartNewRun;
        actions.AddChild(restart);
        root.AddChild(actions);

        root.AddChild(CreateLogPreview());
    }

    private Button CreateEnemyButton(CombatEnemyState enemyState)
    {
        RequireContent();
        var definition = content!.EnemiesById[enemyState.EnemyId];
        var intent = turnService.GetEnemyIntentViews(combat!, content.EnemiesById)
            .FirstOrDefault(view => view.EnemyInstanceId == enemyState.InstanceId);
        var hpText = enemyState.CurrentHp <= 0 ? "击败" : $"HP {enemyState.CurrentHp}/{enemyState.MaxHp}";
        var intentText = intent is null ? "" : content.T(intent.UiTextKey);
        var selected = enemyState.InstanceId == selectedEnemyInstanceId ? " [目标]" : "";

        var button = new Button
        {
            Text = $"{content.T(definition.UiNameKey)}{selected}\n{hpText}  防御 {enemyState.Block}\n{intentText}",
            Disabled = enemyState.CurrentHp <= 0,
            CustomMinimumSize = new Vector2(210, 112)
        };
        button.Pressed += () =>
        {
            selectedEnemyInstanceId = enemyState.InstanceId;
            ShowBattle();
        };
        return button;
    }

    private Button CreateCardButton(string cardId)
    {
        RequireContent();
        var card = content!.CardsById[cardId];
        var canPlay = cardPlayService.CanPlayCard(combat!, card, ResolveTargetFor(card));
        var typeText = card.Type switch
        {
            CardType.Action => $"行动牌 / {card.Cost} 费 / +1 连锁",
            CardType.Skill => "技能牌 / 0 费",
            CardType.Finisher => $"终结牌 / 需 {card.MinChain} 连锁",
            _ => card.Type.ToString()
        };

        var button = new Button
        {
            Text = $"{content.T(card.TextKey)}\n{typeText}",
            Disabled = !canPlay.Succeeded,
            TooltipText = canPlay.Succeeded ? "点击打出" : FailureText(canPlay),
            CustomMinimumSize = new Vector2(190, 150)
        };
        button.Pressed += () => PlayCard(cardId);
        return button;
    }

    private void PlayCard(string cardId)
    {
        RequireContent();
        if (combat is null)
        {
            return;
        }

        var card = content!.CardsById[cardId];
        var targetId = ResolveTargetFor(card);
        var result = cardPlayService.PlayCard(combat, card, targetId);
        combat = result.Combat;

        if (!result.Succeeded)
        {
            ShowBattle(FailureText(result));
            return;
        }

        if (card.Type == CardType.Action)
        {
            ApplyFirstActionRelicIfNeeded(card.Id);
            actionCardsPlayedThisTurn++;
        }

        selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
        if (combat.Status == CombatStatus.Victory)
        {
            ResolveCombatVictory();
            return;
        }

        ShowBattle();
    }

    private void ApplyFirstActionRelicIfNeeded(string cardId)
    {
        if (run is null || combat is null || content is null || actionCardsPlayedThisTurn != 0)
        {
            return;
        }

        if (!run.RelicIds.Contains("relic_mvp_chain_spark"))
        {
            return;
        }

        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_relic_chain_spark_{log.Count + 1}",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = "relic_mvp_chain_spark",
            TargetIds = ["player"],
            NumericChanges = new Dictionary<string, int>
            {
                ["block_gained"] = 2,
                ["player_block_after"] = combat.PlayerBlock + 2
            },
            Metadata = new Dictionary<string, string>
            {
                ["trigger"] = "first_action_card_each_turn",
                ["card_id"] = cardId,
                ["effect_type"] = "gain_block"
            }
        });

        combat = combat with
        {
            PlayerBlock = combat.PlayerBlock + 2,
            Log = log
        };
    }

    private void EndTurn()
    {
        RequireContent();
        if (combat is null || combat.Status != CombatStatus.PlayerTurn)
        {
            return;
        }

        combat = turnService.EndPlayerTurn(combat);
        combat = turnService.ResolveEnemyTurn(combat, content!.EnemiesById);

        if (combat.Status == CombatStatus.Defeat)
        {
            run = runProgressService.ApplyCombatResult(run!, combat, encounter);
            ShowRunResult();
            return;
        }

        combat = turnService.PrepareNextPlayerTurn(combat);
        selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
        actionCardsPlayedThisTurn = 0;
        ShowBattle("敌人行动已结算，新回合开始。");
    }

    private void ResolveCombatVictory()
    {
        run = runProgressService.ApplyCombatResult(run!, combat!, encounter);
        if (run.Status == RunStatus.Cleared || encounter!.NodeType == EncounterNodeType.Boss)
        {
            ShowRunResult();
            return;
        }

        ShowRewardSelection();
    }

    private void ShowRewardSelection()
    {
        RequireContent();
        ClearChildren();
        selectedRewardCards.Clear();
        openedRewardPack = null;

        var root = CreatePage("战斗胜利：选择卡牌包");
        root.AddChild(CreateBodyLabel("选择 1 个卡牌包打开。打开后可以选择 0-3 张加入卡组；少拿或不拿没有补偿。"));

        if (encounter?.RewardProfile.RelicId is not null)
        {
            var relic = content!.RelicsById[encounter.RewardProfile.RelicId];
            root.AddChild(CreateMessage($"精英额外奖励将在确认后获得：{content.T(relic.TextKey)}"));
        }

        var packs = new HBoxContainer();
        packs.AddThemeConstantOverride("separation", 12);
        foreach (var pack in rewardService.GetAvailableCardPacks(encounter!, content!.RewardPacksById))
        {
            var button = CreatePrimaryButton(content.T(pack.TextKey));
            button.CustomMinimumSize = new Vector2(180, 72);
            button.Pressed += () => OpenRewardPack(pack.Id);
            packs.AddChild(button);
        }

        root.AddChild(packs);

        var skip = new Button { Text = "跳过拿牌并进入下一战" };
        skip.Pressed += ConfirmReward;
        root.AddChild(skip);
    }

    private void OpenRewardPack(string packId)
    {
        RequireContent();
        openedRewardPack = rewardService.OpenRewardPack(packId, content!.RewardPacksById);
        selectedRewardCards.Clear();
        RenderOpenedRewardPack();
    }

    private void RenderOpenedRewardPack()
    {
        RequireContent();
        if (openedRewardPack is null)
        {
            ShowRewardSelection();
            return;
        }

        ClearChildren();
        var root = CreatePage($"已打开：{content!.T(openedRewardPack.TextKey)}");
        root.AddChild(CreateBodyLabel("点击卡牌切换选择状态。当前 MVP 奖励池固定可重复，已经拿过的牌仍可再次出现。"));

        var cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", 8);
        foreach (var cardId in openedRewardPack.CandidateIds)
        {
            var card = content.CardsById[cardId];
            var picked = selectedRewardCards.Contains(cardId);
            var button = new Button
            {
                Text = $"{(picked ? "[已选] " : "")}{content.T(card.TextKey)}",
                CustomMinimumSize = new Vector2(220, 150)
            };
            button.Pressed += () =>
            {
                if (!selectedRewardCards.Add(cardId))
                {
                    selectedRewardCards.Remove(cardId);
                }

                RenderOpenedRewardPack();
            };
            cards.AddChild(button);
        }

        root.AddChild(cards);
        var confirm = CreatePrimaryButton($"确认选择 {selectedRewardCards.Count} 张");
        confirm.Pressed += ConfirmReward;
        root.AddChild(confirm);
    }

    private void ConfirmReward()
    {
        if (run is null || encounter is null || content is null)
        {
            ShowMainMenu();
            return;
        }

        if (openedRewardPack is not null)
        {
            run = rewardService.ClaimCards(run, openedRewardPack, selectedRewardCards);
        }

        run = rewardService.GrantEncounterRelic(run, encounter, content.RelicsById);
        run = runProgressService.AdvanceAfterRewards(run, encounter);
        StartCurrentEncounter();
    }

    private void ShowRunResult()
    {
        ClearChildren();
        var cleared = run?.Status == RunStatus.Cleared;
        var failed = run?.Status == RunStatus.Failed;
        var root = CreatePage(cleared ? "MVP 通关" : failed ? "Run 失败" : "Run 结束");
        root.AddChild(CreateBodyLabel(cleared
            ? "Boss 已击败。当前 MVP 闭环完成，可以重开验证另一种卡牌包选择。"
            : "生命归零，本次 MVP Run 结束。"));

        if (run is not null)
        {
            root.AddChild(CreateSmallLabel($"最终卡组数量：{run.MasterDeck.Count}    遗物：{run.RelicIds.Count}"));
        }

        var restart = CreatePrimaryButton("重新开始");
        restart.Pressed += StartNewRun;
        root.AddChild(restart);
    }

    private string? ResolveTargetFor(CardDefinition card)
    {
        if (card.TargetRule != TargetRule.SingleEnemy)
        {
            return null;
        }

        if (combat is null)
        {
            return null;
        }

        var selected = combat.Enemies.FirstOrDefault(enemy =>
            enemy.InstanceId == selectedEnemyInstanceId && enemy.CurrentHp > 0);
        return selected?.InstanceId ?? combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
    }

    private string FailureText(PlayCardResult result)
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

    private Control CreateLogPreview()
    {
        var latest = combat is null
            ? Enumerable.Empty<string>()
            : combat.Log.TakeLast(5).Reverse().Select(LogLine);
        var label = CreateSmallLabel("最近结算：\n" + string.Join("\n", latest));
        label.CustomMinimumSize = new Vector2(0, 96);
        return label;
    }

    private string LogLine(CombatLogEvent item)
    {
        var source = string.IsNullOrWhiteSpace(item.SourceId) ? "" : $" {item.SourceId}";
        var numbers = item.NumericChanges.Count == 0
            ? ""
            : " " + string.Join(", ", item.NumericChanges.Select(pair => $"{pair.Key}:{pair.Value}"));
        return $"{item.EventType}{source}{numbers}";
    }

    private VBoxContainer CreatePage(string title)
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        root.AddChild(titleLabel);
        return root;
    }

    private static Label CreateBodyLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
    }

    private static Label CreateSmallLabel(string text)
    {
        var label = CreateBodyLabel(text);
        label.AddThemeFontSizeOverride("font_size", 13);
        return label;
    }

    private static Button CreatePrimaryButton(string text)
    {
        return new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(180, 44)
        };
    }

    private static Label CreateMessage(string text)
    {
        var label = CreateBodyLabel(text);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.78f, 0.35f));
        return label;
    }

    private static PanelContainer CreateInfoPanel(string text, Color color)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(136, 44) };
        var style = new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = new Color(0.75f, 0.62f, 0.4f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        panel.AddThemeStyleboxOverride("panel", style);
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.AddChild(label);
        return panel;
    }

    private void ShowFatalError(Exception ex)
    {
        ClearChildren();
        var root = CreatePage("启动失败");
        root.AddChild(CreateBodyLabel(ex.ToString()));
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

    private void RequireContent()
    {
        if (content is null)
        {
            throw new InvalidOperationException("Game content is not loaded.");
        }
    }
}
