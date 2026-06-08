using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Enemies;
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
    private readonly Dictionary<string, Texture2D> textureCache = new(StringComparer.Ordinal);

    private static readonly Color InkPanel = new(0.045f, 0.035f, 0.035f, 0.9f);
    private static readonly Color InkPanelLight = new(0.105f, 0.08f, 0.075f, 0.86f);
    private static readonly Color GoldLine = new(0.82f, 0.62f, 0.34f, 1.0f);
    private static readonly Color BloodLine = new(0.78f, 0.12f, 0.09f, 1.0f);
    private static readonly Color SkillLine = new(0.18f, 0.58f, 0.70f, 1.0f);
    private static readonly Color FinisherLine = new(0.55f, 0.22f, 0.82f, 1.0f);

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
        var root = CreatePage("连锁终章", wide: false);
        root.AddChild(CreateImage("asset.character.swordsman.battle", new Vector2(360, 260), TextureRect.StretchModeEnum.KeepAspectCentered));
        root.AddChild(CreateBodyLabel("第一版 MVP：6 场固定战斗、连锁层数、终结牌、卡牌包奖励和精英遗物。"));

        var start = CreatePrimaryButton("开始 MVP Run");
        start.Pressed += StartNewRun;
        root.AddChild(start);

        var hint = CreateSmallLabel("先点敌人选择目标，再点手牌出牌。行动牌消耗行动点并 +1 连锁；终结牌满足连锁后可打出并清空连锁。");
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
        var root = CreatePage($"第 {run.CurrentEncounterIndex + 1}/6 战：{content!.T(encounter.TeachingGoalKey)}", wide: true);

        var status = new HBoxContainer();
        status.AddThemeConstantOverride("separation", 12);
        status.AddChild(CreateInfoPanel($"HP {combat.PlayerHp}/{combat.PlayerMaxHp}", BloodLine, "asset.ui.icon.life"));
        status.AddChild(CreateInfoPanel($"防御 {combat.PlayerBlock}", SkillLine, "asset.ui.icon.player_block"));
        status.AddChild(CreateInfoPanel($"行动点 {combat.ActionPoints}", GoldLine, "asset.ui.icon.action_point"));
        status.AddChild(CreateInfoPanel($"连锁 {combat.Chain}  阈值 3/5/8", FinisherLine, "asset.ui.icon.chain_count"));
        status.AddChild(CreateInfoPanel($"抽 {combat.DeckZones.DrawPileCount}  弃 {combat.DeckZones.DiscardPileCount}  牌组 {run.MasterDeck.Count}", new Color(0.44f, 0.48f, 0.48f), "asset.ui.icon.deck_library"));
        root.AddChild(status);

        if (!string.IsNullOrWhiteSpace(message))
        {
            root.AddChild(CreateMessage(message));
        }

        if (run.RelicIds.Count > 0)
        {
            root.AddChild(CreateRelicStrip());
        }

        var stage = new HBoxContainer();
        stage.AddThemeConstantOverride("separation", 16);
        stage.AddChild(CreatePlayerStagePanel());

        var enemies = new HBoxContainer();
        enemies.AddThemeConstantOverride("separation", 12);
        enemies.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        foreach (var enemyState in combat.Enemies)
        {
            enemies.AddChild(CreateEnemyButton(enemyState));
        }

        stage.AddChild(enemies);
        root.AddChild(stage);

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

    private Control CreatePlayerStagePanel()
    {
        var panel = CreateFramedPanel(new Vector2(260, 250), SkillLine);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);
        box.AddChild(CreateImage("asset.character.swordsman.battle", new Vector2(240, 180), TextureRect.StretchModeEnum.KeepAspectCentered));
        box.AddChild(CreateSmallLabel($"剑客\nHP {combat!.PlayerHp}/{combat.PlayerMaxHp}  防御 {combat.PlayerBlock}"));
        return panel;
    }

    private Control CreateRelicStrip()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(CreateSmallLabel("遗物"));
        foreach (var relicId in run!.RelicIds)
        {
            var icon = CreateImage(content!.RelicViewsById[relicId].IconAsset, new Vector2(34, 34), TextureRect.StretchModeEnum.KeepAspectCentered);
            icon.TooltipText = $"{content.RelicName(relicId)}：{content.RelicRules(relicId)}";
            row.AddChild(icon);
        }

        return row;
    }

    private Control CreateEnemyButton(CombatEnemyState enemyState)
    {
        RequireContent();
        var intent = turnService.GetEnemyIntentViews(combat!, content!.EnemiesById)
            .FirstOrDefault(view => view.EnemyInstanceId == enemyState.InstanceId);
        var hpText = enemyState.CurrentHp <= 0 ? "击败" : $"HP {enemyState.CurrentHp}/{enemyState.MaxHp}";
        var intentText = intent is null ? "" : content.EnemyIntentText(enemyState.EnemyId, intent.IntentId);
        var selected = enemyState.InstanceId == selectedEnemyInstanceId ? " [目标]" : "";
        var border = enemyState.InstanceId == selectedEnemyInstanceId ? GoldLine : new Color(0.25f, 0.20f, 0.18f);
        var panel = CreateFramedPanel(new Vector2(230, 250), border);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);

        var enemyView = content.EnemyViewsById[enemyState.EnemyId];
        var portrait = CreateImage(enemyView.StandAsset, new Vector2(210, 130), TextureRect.StretchModeEnum.KeepAspectCentered);
        portrait.Modulate = enemyState.CurrentHp <= 0 ? new Color(0.32f, 0.32f, 0.32f, 0.75f) : Colors.White;
        box.AddChild(portrait);

        var intentRow = new HBoxContainer();
        intentRow.AddThemeConstantOverride("separation", 6);
        if (intent is not null)
        {
            intentRow.AddChild(CreateImage(IntentIconAsset(intent.IntentType), new Vector2(28, 28), TextureRect.StretchModeEnum.KeepAspectCentered));
        }

        intentRow.AddChild(CreateSmallLabel(intentText));
        box.AddChild(intentRow);

        var button = new Button
        {
            Text = $"{content.EnemyName(enemyState.EnemyId)}{selected}\n{hpText}  防御 {enemyState.Block}",
            Disabled = enemyState.CurrentHp <= 0,
            CustomMinimumSize = new Vector2(210, 56)
        };
        button.Pressed += () =>
        {
            selectedEnemyInstanceId = enemyState.InstanceId;
            ShowBattle();
        };
        box.AddChild(button);
        return panel;
    }

    private Control CreateCardButton(string cardId)
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
        var border = card.Type switch
        {
            CardType.Action => BloodLine,
            CardType.Skill => SkillLine,
            CardType.Finisher => FinisherLine,
            _ => GoldLine
        };

        var panel = CreateFramedPanel(new Vector2(190, 280), canPlay.Succeeded ? border : new Color(0.20f, 0.18f, 0.18f));
        panel.Modulate = canPlay.Succeeded ? Colors.White : new Color(0.55f, 0.55f, 0.55f);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        panel.AddChild(box);

        var view = content.CardViewsById[card.Id];
        var stack = new Control { CustomMinimumSize = new Vector2(170, 142) };
        var template = CreateImage(view.TemplateAsset, new Vector2(170, 142), TextureRect.StretchModeEnum.Scale);
        template.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        stack.AddChild(template);
        var art = CreateImage(view.ArtAsset, new Vector2(145, 92), TextureRect.StretchModeEnum.KeepAspectCovered);
        art.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        art.OffsetLeft = 12;
        art.OffsetRight = -12;
        art.OffsetTop = 34;
        art.OffsetBottom = -14;
        stack.AddChild(art);
        box.AddChild(stack);

        var name = CreateSmallLabel(content.CardName(card.Id));
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(name);

        var rules = CreateSmallLabel(content.CardRules(card.Id));
        rules.CustomMinimumSize = new Vector2(0, 58);
        box.AddChild(rules);

        var button = new Button
        {
            Text = typeText,
            Disabled = !canPlay.Succeeded,
            TooltipText = canPlay.Succeeded ? "点击打出" : FailureText(canPlay),
            CustomMinimumSize = new Vector2(170, 40)
        };
        button.Pressed += () => PlayCard(cardId);
        box.AddChild(button);
        return panel;
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

        if (!run.RelicIds.Contains("relic.mvp_chain_spark"))
        {
            return;
        }

        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_relic_chain_spark_{log.Count + 1}",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = "relic.mvp_chain_spark",
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

        var root = CreatePage("战斗胜利：选择卡牌包", wide: true);
        root.AddChild(CreateBodyLabel("选择 1 个卡牌包打开。打开后可以选择 0-3 张加入卡组；少拿或不拿没有补偿。"));

        if (encounter?.RewardProfile.RelicId is not null)
        {
            var relic = content!.RelicsById[encounter.RewardProfile.RelicId];
            var relicRow = new HBoxContainer();
            relicRow.AddThemeConstantOverride("separation", 8);
            relicRow.AddChild(CreateImage(content.RelicViewsById[relic.Id].IconAsset, new Vector2(42, 42), TextureRect.StretchModeEnum.KeepAspectCentered));
            relicRow.AddChild(CreateMessage($"精英额外奖励将在确认后获得：{content.RelicName(relic.Id)}"));
            root.AddChild(relicRow);
        }

        var packs = new HBoxContainer();
        packs.AddThemeConstantOverride("separation", 12);
        foreach (var pack in rewardService.GetAvailableCardPacks(encounter!, content!.RewardPacksById))
        {
            packs.AddChild(CreateRewardPackControl(pack));
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
        var root = CreatePage($"已打开：{content!.RewardPackName(openedRewardPack.Id)}", wide: true);
        root.AddChild(CreateBodyLabel("点击卡牌切换选择状态。当前 MVP 奖励池固定可重复，已经拿过的牌仍可再次出现。"));

        var cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", 8);
        foreach (var cardId in openedRewardPack.CandidateIds)
        {
            var card = content.CardsById[cardId];
            var picked = selectedRewardCards.Contains(cardId);
            cards.AddChild(CreateRewardCardControl(card, picked));
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
        root.AddChild(CreateImage(cleared ? "asset.vfx.finisher_release_shockwave" : "asset.vfx.enemy_hit_comic_burst", new Vector2(420, 180), TextureRect.StretchModeEnum.KeepAspectCentered));
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
        var panel = CreateFramedPanel(new Vector2(0, 106), new Color(0.36f, 0.31f, 0.26f));
        var label = CreateSmallLabel("最近结算：\n" + string.Join("\n", latest));
        panel.AddChild(label);
        return panel;
    }

    private string LogLine(CombatLogEvent item)
    {
        var source = string.IsNullOrWhiteSpace(item.SourceId) ? "" : $" {item.SourceId}";
        var numbers = item.NumericChanges.Count == 0
            ? ""
            : " " + string.Join(", ", item.NumericChanges.Select(pair => $"{pair.Key}:{pair.Value}"));
        return $"{item.EventType}{source}{numbers}";
    }

    private Control CreateRewardPackControl(RewardPackDefinition pack)
    {
        var view = content!.RewardPackViewsById[pack.Id];
        var panel = CreateFramedPanel(new Vector2(220, 276), GoldLine);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);
        box.AddChild(CreateImage(view.IconAsset, new Vector2(190, 170), TextureRect.StretchModeEnum.KeepAspectCentered));

        var title = CreateSmallLabel(content.RewardPackName(pack.Id));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 16);
        box.AddChild(title);

        var button = CreatePrimaryButton("打开");
        button.Icon = LoadTexture("asset.ui.icon.deck_library");
        button.CustomMinimumSize = new Vector2(190, 44);
        button.Pressed += () => OpenRewardPack(pack.Id);
        box.AddChild(button);
        return panel;
    }

    private Control CreateRewardCardControl(CardDefinition card, bool picked)
    {
        var border = picked ? GoldLine : card.Type switch
        {
            CardType.Action => BloodLine,
            CardType.Skill => SkillLine,
            CardType.Finisher => FinisherLine,
            _ => GoldLine
        };
        var control = CreateCardDisplay(card, border);
        var button = new Button
        {
            Text = picked ? "取消选择" : "选择",
            CustomMinimumSize = new Vector2(170, 38)
        };
        button.Pressed += () =>
        {
            if (!selectedRewardCards.Add(card.Id))
            {
                selectedRewardCards.Remove(card.Id);
            }

            RenderOpenedRewardPack();
        };

        if (control is PanelContainer panel && panel.GetChildCount() > 0 && panel.GetChild(0) is VBoxContainer box)
        {
            box.AddChild(button);
        }

        return control;
    }

    private Control CreateCardDisplay(CardDefinition card, Color border)
    {
        var panel = CreateFramedPanel(new Vector2(190, 244), border);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        panel.AddChild(box);

        var view = content!.CardViewsById[card.Id];
        var stack = new Control { CustomMinimumSize = new Vector2(170, 126) };
        var template = CreateImage(view.TemplateAsset, new Vector2(170, 126), TextureRect.StretchModeEnum.Scale);
        template.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        stack.AddChild(template);
        var art = CreateImage(view.ArtAsset, new Vector2(145, 78), TextureRect.StretchModeEnum.KeepAspectCovered);
        art.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        art.OffsetLeft = 12;
        art.OffsetRight = -12;
        art.OffsetTop = 34;
        art.OffsetBottom = -14;
        stack.AddChild(art);
        box.AddChild(stack);

        var name = CreateSmallLabel(content.CardName(card.Id));
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(name);

        var rules = CreateSmallLabel(content.CardRules(card.Id));
        rules.CustomMinimumSize = new Vector2(0, 58);
        box.AddChild(rules);
        return panel;
    }

    private VBoxContainer CreatePage(string title, bool wide = false)
    {
        AddBackground();

        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(scroll);

        var margin = new MarginContainer();
        margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        margin.AddThemeConstantOverride("margin_left", wide ? 20 : 180);
        margin.AddThemeConstantOverride("margin_right", wide ? 20 : 180);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        scroll.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        margin.AddChild(root);

        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 30);
        titleLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.52f));
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
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(180, 44)
        };
        button.AddThemeFontSizeOverride("font_size", 15);
        return button;
    }

    private static Label CreateMessage(string text)
    {
        var label = CreateBodyLabel(text);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.78f, 0.35f));
        return label;
    }

    private PanelContainer CreateInfoPanel(string text, Color color, string? iconAsset = null)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(136, 44) };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(color.R * 0.18f, color.G * 0.18f, color.B * 0.18f, 0.92f),
            BorderColor = color,
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
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        row.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(row);
        if (iconAsset is not null)
        {
            row.AddChild(CreateImage(iconAsset, new Vector2(26, 26), TextureRect.StretchModeEnum.KeepAspectCentered));
        }

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.AddChild(label);
        return panel;
    }

    private PanelContainer CreateFramedPanel(Vector2 minSize, Color borderColor)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = minSize,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        var style = new StyleBoxFlat
        {
            BgColor = InkPanel,
            BorderColor = borderColor,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private TextureRect CreateImage(string assetId, Vector2 minSize, TextureRect.StretchModeEnum stretchMode)
    {
        return new TextureRect
        {
            Texture = LoadTexture(assetId),
            CustomMinimumSize = minSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
            MouseFilter = MouseFilterEnum.Ignore
        };
    }

    private Texture2D? LoadTexture(string assetId)
    {
        if (textureCache.TryGetValue(assetId, out var cached))
        {
            return cached;
        }

        var assets = content?.AssetsById;
        if (assets is null || !assets.TryGetValue(assetId, out var asset))
        {
            return null;
        }

        if (asset.Path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            Texture2D? pngTexture = null;
            var image = new Image();
            var bytes = System.IO.File.ReadAllBytes(ProjectSettings.GlobalizePath(asset.Path));
            var error = image.LoadPngFromBuffer(bytes);
            if (error == Error.Ok)
            {
                pngTexture = ImageTexture.CreateFromImage(image);
            }

            if (pngTexture is not null)
            {
                textureCache[assetId] = pngTexture;
            }

            return pngTexture;
        }

        var texture = GD.Load<Texture2D>(asset.Path);

        if (texture is not null)
        {
            textureCache[assetId] = texture;
        }

        return texture;
    }

    private void AddBackground()
    {
        var background = CreateImage("asset.background.mvp_battle.hd", Vector2.Zero, TextureRect.StretchModeEnum.KeepAspectCovered);
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        background.Modulate = new Color(0.42f, 0.38f, 0.36f, 1.0f);
        AddChild(background);

        var veil = new ColorRect
        {
            Color = new Color(0.015f, 0.012f, 0.012f, 0.48f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        veil.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(veil);
    }

    private static string IntentIconAsset(EnemyIntentType intentType)
    {
        return intentType switch
        {
            EnemyIntentType.Attack => "asset.ui.icon.attack_intent",
            EnemyIntentType.Defend => "asset.ui.icon.defend_intent",
            EnemyIntentType.Mixed => "asset.ui.icon.pressure_mixed_intent",
            _ => "asset.ui.icon.pressure_mixed_intent"
        };
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
