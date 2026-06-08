using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Presentation.Cards;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Battle;

public partial class BattleScreen : ComicScreen
{
    private readonly CombatTurnService turnService = new();
    private readonly CardPlayService cardPlayService = new();

    private CombatState? combat;
    private RunState? run;
    private EncounterDefinition? encounter;
    private string? selectedEnemyInstanceId;

    public event Action<string>? EnemySelected;
    public event Action<string>? CardRequested;
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

        var content = RequireContent();
        var root = CreateCanvas();

        AddLabelAt(root, $"第 {run.CurrentEncounterIndex + 1}/6 战", new Vector2(790, 24), new Vector2(340, 32), 22, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, content.T(encounter.TeachingGoalKey), new Vector2(610, 58), new Vector2(700, 30), 16, new Color(0.35f, 0.23f, 0.12f), HorizontalAlignment.Center);

        AddAt(root, CreateInfoPanel($"{combat.PlayerHp}/{combat.PlayerMaxHp}", BloodLine, "asset.ui.icon.life"), new Vector2(52, 42), new Vector2(150, 48));
        AddAt(root, CreateInfoPanel($"{combat.PlayerBlock}", SkillLine, "asset.ui.icon.player_block"), new Vector2(52, 98), new Vector2(112, 44));
        AddAt(root, CreateInfoPanel($"{combat.ActionPoints}", GoldLine, "asset.ui.icon.action_point"), new Vector2(174, 98), new Vector2(92, 44));
        AddAt(root, CreateInfoPanel($"{combat.Chain}  /  3  5  8", FinisherLine, "asset.ui.icon.chain_count"), new Vector2(52, 152), new Vector2(214, 44));
        AddAt(root, CreateInfoPanel($"{combat.DeckZones.DrawPileCount}", new Color(0.32f, 0.36f, 0.36f), "asset.ui.icon.draw_pile"), new Vector2(1488, 888), new Vector2(92, 44));
        AddAt(root, CreateInfoPanel($"{combat.DeckZones.DiscardPileCount}", new Color(0.32f, 0.36f, 0.36f), "asset.ui.icon.discard_pile"), new Vector2(1590, 888), new Vector2(92, 44));
        AddAt(root, CreateInfoPanel($"{run.MasterDeck.Count}", new Color(0.32f, 0.36f, 0.36f), "asset.ui.icon.deck_library"), new Vector2(1692, 888), new Vector2(104, 44));

        AddImageAt(root, "asset.character.swordsman.battle", new Vector2(120, 330), new Vector2(455, 585), TextureRect.StretchModeEnum.KeepAspectCentered);
        AddLabelAt(root, $"剑客  防御 {combat.PlayerBlock}", new Vector2(175, 814), new Vector2(330, 28), 16, new Color(0.18f, 0.11f, 0.07f), HorizontalAlignment.Center);

        if (run.RelicIds.Count > 0)
        {
            AddAt(root, CreateRelicStrip(), new Vector2(300, 42), new Vector2(350, 48));
        }

        var enemyCount = combat.Enemies.Count;
        var enemyWidth = enemyCount <= 1 ? 300 : 245;
        var spacing = enemyCount <= 1 ? 0 : 34;
        var totalWidth = enemyCount * enemyWidth + Math.Max(0, enemyCount - 1) * spacing;
        var startX = 1190 - totalWidth / 2f;
        for (var i = 0; i < combat.Enemies.Count; i++)
        {
            AddAt(root, CreateEnemyButton(combat.Enemies[i]), new Vector2(startX + i * (enemyWidth + spacing), 295), new Vector2(enemyWidth, 390));
        }

        var hand = new HBoxContainer();
        hand.AddThemeConstantOverride("separation", 10);
        hand.Alignment = BoxContainer.AlignmentMode.Center;
        foreach (var cardId in combat.DeckZones.Hand)
        {
            hand.AddChild(CreateCardButton(cardId));
        }

        AddAt(root, hand, new Vector2(505, 742), new Vector2(890, 300));

        var endTurn = CreateArtButton("结束回合", "asset.ui.icon.end_turn", new Vector2(164, 62), GoldLine);
        endTurn.Pressed += () => EndTurnRequested?.Invoke();
        AddAt(root, endTurn, new Vector2(1580, 782), new Vector2(164, 62));

        var restart = CreateArtButton("重开", "asset.ui.icon.deck_library", new Vector2(122, 48), new Color(0.36f, 0.31f, 0.26f));
        restart.Pressed += () => RestartRequested?.Invoke();
        AddAt(root, restart, new Vector2(52, 888), new Vector2(122, 48));

        if (!string.IsNullOrWhiteSpace(message))
        {
            AddAt(root, CreateMessagePanel(message), new Vector2(650, 104), new Vector2(620, 44));
        }

        AddAt(root, CreateLogPreview(), new Vector2(1365, 96), new Vector2(390, 166));
    }

    private Control CreateRelicStrip()
    {
        var content = RequireContent();
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        foreach (var relicId in run!.RelicIds)
        {
            var icon = CreateImage(content.RelicViewsById[relicId].IconAsset, new Vector2(34, 34), TextureRect.StretchModeEnum.KeepAspectCentered);
            icon.TooltipText = $"{content.RelicName(relicId)}：{content.RelicRules(relicId)}";
            row.AddChild(icon);
        }

        return row;
    }

    private Control CreateEnemyButton(CombatEnemyState enemyState)
    {
        var content = RequireContent();
        var intent = turnService.GetEnemyIntentViews(combat!, content.EnemiesById)
            .FirstOrDefault(view => view.EnemyInstanceId == enemyState.InstanceId);
        var hpText = enemyState.CurrentHp <= 0 ? "击败" : $"HP {enemyState.CurrentHp}/{enemyState.MaxHp}";
        var intentText = intent is null ? "" : content.EnemyIntentText(enemyState.EnemyId, intent.IntentId);
        var border = enemyState.InstanceId == selectedEnemyInstanceId ? GoldLine : new Color(0.25f, 0.20f, 0.18f);
        var panel = CreateFramedPanel(new Vector2(230, 340), border);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 5);
        panel.AddChild(box);

        var enemyView = content.EnemyViewsById[enemyState.EnemyId];
        var portrait = CreateImage(enemyView.StandAsset, new Vector2(210, 222), TextureRect.StretchModeEnum.KeepAspectCentered);
        portrait.Modulate = enemyState.CurrentHp <= 0 ? new Color(0.32f, 0.32f, 0.32f, 0.75f) : Colors.White;
        box.AddChild(portrait);

        var name = CreateSmallLabel($"{content.EnemyName(enemyState.EnemyId)}  {hpText}");
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(name);

        var intentRow = new HBoxContainer();
        intentRow.AddThemeConstantOverride("separation", 6);
        intentRow.Alignment = BoxContainer.AlignmentMode.Center;
        if (intent is not null)
        {
            intentRow.AddChild(CreateImage(IntentIconAsset(intent.IntentType), new Vector2(28, 28), TextureRect.StretchModeEnum.KeepAspectCentered));
        }

        var intentLabel = CreateSmallLabel($"{intentText}  防御 {enemyState.Block}");
        intentLabel.HorizontalAlignment = HorizontalAlignment.Center;
        intentRow.AddChild(intentLabel);
        box.AddChild(intentRow);

        var button = new Button
        {
            Text = "",
            Disabled = enemyState.CurrentHp <= 0,
            TooltipText = "点击选择目标"
        };
        MakeTransparentButton(button);
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.Pressed += () => EnemySelected?.Invoke(enemyState.InstanceId);
        panel.AddChild(button);

        if (enemyState.InstanceId == selectedEnemyInstanceId && enemyState.CurrentHp > 0)
        {
            var targetIcon = CreateImage("asset.ui.icon.target_selected", new Vector2(46, 46), TextureRect.StretchModeEnum.KeepAspectCentered);
            targetIcon.Position = new Vector2(12, 12);
            panel.AddChild(targetIcon);
        }

        return panel;
    }

    private Control CreateCardButton(string cardId)
    {
        var content = RequireContent();
        var card = content.CardsById[cardId];
        var canPlay = cardPlayService.CanPlayCard(combat!, card, ResolveTargetFor(card));
        var typeText = card.Type switch
        {
            CardType.Action => $"行动牌 / {card.Cost} 费 / +1 连锁",
            CardType.Skill => "技能牌 / 0 费",
            CardType.Finisher => $"终结牌 / 需 {card.MinChain} 连锁",
            _ => card.Type.ToString()
        };
        var panel = CardPanel.Create(card, content, LoadTexture, LoadFont, width: 220, dimmed: !canPlay.Succeeded);

        var button = new Button
        {
            Text = "",
            Disabled = !canPlay.Succeeded,
            TooltipText = canPlay.Succeeded ? typeText : FailureText(canPlay)
        };
        MakeTransparentButton(button);
        button.Position = Vector2.Zero;
        button.Size = panel.CustomMinimumSize;
        button.CustomMinimumSize = panel.CustomMinimumSize;
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        button.Pressed += () => CardRequested?.Invoke(cardId);
        panel.AddChild(button);
        return panel;
    }

    private string? ResolveTargetFor(CardDefinition card)
    {
        if (card.TargetRule != TargetRule.SingleEnemy || combat is null)
        {
            return null;
        }

        var selected = combat.Enemies.FirstOrDefault(enemy =>
            enemy.InstanceId == selectedEnemyInstanceId && enemy.CurrentHp > 0);
        return selected?.InstanceId ?? combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
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
}
