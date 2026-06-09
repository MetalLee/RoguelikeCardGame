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
    private readonly Dictionary<string, Control> enemyNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Control>> cardNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Control> cardNodesByHandIndex = new();
    private Control? playerNode;
    private Control? chainPanel;
    private Control? blockPanel;
    private Control? actionPointPanel;
    private Control? handNode;
    private Control? drawPilePanel;
    private Control? discardPilePanel;
    private Control? fxLayer;

    public event Action<string>? EnemySelected;
    public event Action<string, int>? CardRequested;
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
        enemyNodes.Clear();
        cardNodes.Clear();
        cardNodesByHandIndex.Clear();
        playerNode = null;
        chainPanel = null;
        blockPanel = null;
        actionPointPanel = null;
        handNode = null;
        drawPilePanel = null;
        discardPilePanel = null;
        fxLayer = null;

        var content = RequireContent();
        var root = CreateCanvas();

        AddLabelAt(root, $"第 {run.CurrentEncounterIndex + 1}/6 战", new Vector2(790, 24), new Vector2(340, 32), 22, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, content.T(encounter.TeachingGoalKey), new Vector2(610, 58), new Vector2(700, 30), 16, new Color(0.35f, 0.23f, 0.12f), HorizontalAlignment.Center);

        AddAt(root, CreateInfoPanel($"{combat.PlayerHp}/{combat.PlayerMaxHp}", BloodLine, "asset.ui.icon.life"), new Vector2(52, 42), new Vector2(150, 48));
        blockPanel = CreateInfoPanel($"{combat.PlayerBlock}", SkillLine, "asset.ui.icon.player_block");
        AddAt(root, blockPanel, new Vector2(52, 98), new Vector2(112, 44));
        actionPointPanel = CreateInfoPanel($"{combat.ActionPoints}", GoldLine, "asset.ui.icon.action_point");
        AddAt(root, actionPointPanel, new Vector2(174, 98), new Vector2(92, 44));
        chainPanel = CreateInfoPanel($"{combat.Chain}  /  3  5  8", FinisherLine, "asset.ui.icon.chain_count");
        AddAt(root, chainPanel, new Vector2(52, 152), new Vector2(214, 44));
        drawPilePanel = CreateInfoPanel($"{combat.DeckZones.DrawPileCount}", new Color(0.32f, 0.36f, 0.36f), "asset.ui.icon.draw_pile");
        AddAt(root, drawPilePanel, new Vector2(1488, 888), new Vector2(92, 44));
        discardPilePanel = CreateInfoPanel($"{combat.DeckZones.DiscardPileCount}", new Color(0.32f, 0.36f, 0.36f), "asset.ui.icon.discard_pile");
        AddAt(root, discardPilePanel, new Vector2(1590, 888), new Vector2(92, 44));
        AddAt(root, CreateInfoPanel($"{run.MasterDeck.Count}", new Color(0.32f, 0.36f, 0.36f), "asset.ui.icon.deck_library"), new Vector2(1692, 888), new Vector2(104, 44));

        playerNode = CreateImage("asset.character.swordsman.battle", new Vector2(455, 585), TextureRect.StretchModeEnum.KeepAspectCentered);
        playerNode.Name = "PlayerStand";
        AddAt(root, playerNode, new Vector2(120, 330), new Vector2(455, 585));
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
            var enemyControl = CreateEnemyButton(combat.Enemies[i]);
            enemyNodes[combat.Enemies[i].InstanceId] = enemyControl;
            AddAt(root, enemyControl, new Vector2(startX + i * (enemyWidth + spacing), 405), new Vector2(enemyWidth, 330));
        }

        var hand = new HBoxContainer();
        handNode = hand;
        hand.AddThemeConstantOverride("separation", 10);
        hand.Alignment = BoxContainer.AlignmentMode.Center;
        for (var handIndex = 0; handIndex < combat.DeckZones.Hand.Count; handIndex++)
        {
            var cardId = combat.DeckZones.Hand[handIndex];
            var cardControl = CreateCardButton(cardId, handIndex);
            cardNodesByHandIndex[handIndex] = cardControl;
            if (!cardNodes.TryGetValue(cardId, out var controls))
            {
                controls = new List<Control>();
                cardNodes[cardId] = controls;
            }

            controls.Add(cardControl);
            hand.AddChild(cardControl);
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

        fxLayer = new Control
        {
            Name = "FxLayer",
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 100
        };
        root.AddChild(fxLayer);
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
        var border = enemyState.InstanceId == selectedEnemyInstanceId ? GoldLine : new Color(0.25f, 0.20f, 0.18f);
        var panel = CreateFramedPanel(new Vector2(230, 300), border);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 5);
        panel.AddChild(box);

        var enemyView = content.EnemyViewsById[enemyState.EnemyId];
        var portrait = CreateImage(enemyView.StandAsset, new Vector2(210, 235), TextureRect.StretchModeEnum.KeepAspectCentered);
        portrait.Modulate = enemyState.CurrentHp <= 0 ? new Color(0.32f, 0.32f, 0.32f, 0.75f) : Colors.White;
        box.AddChild(portrait);

        var name = CreateSmallLabel($"{content.EnemyName(enemyState.EnemyId)}  {hpText}  防御 {enemyState.Block}");
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AddThemeFontSizeOverride("font_size", 15);
        box.AddChild(name);

        if (intent is not null)
        {
            var intentIcon = CreateImage(IntentIconAsset(intent.IntentType), new Vector2(34, 34), TextureRect.StretchModeEnum.KeepAspectCentered);
            intentIcon.Position = new Vector2(188, 14);
            intentIcon.TooltipText = content.EnemyIntentText(enemyState.EnemyId, intent.IntentId);
            panel.AddChild(intentIcon);
        }

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
            var targetIcon = CreateImage("asset.ui.icon.target_selected", new Vector2(24, 24), TextureRect.StretchModeEnum.KeepAspectCentered);
            targetIcon.SetAnchorsPreset(LayoutPreset.Center);
            targetIcon.OffsetLeft = -12;
            targetIcon.OffsetTop = -12;
            targetIcon.OffsetRight = 12;
            targetIcon.OffsetBottom = 12;
            panel.AddChild(targetIcon);
        }

        return panel;
    }

    private Control CreateCardButton(string cardId, int handIndex)
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
        button.Pressed += () => CardRequested?.Invoke(cardId, handIndex);
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

    public async Task PlayLogAnimationsAsync(IReadOnlyList<CombatLogEvent> events, CardDefinition? playedCard = null, int? playedHandIndex = null)
    {
        if (events.Count == 0)
        {
            return;
        }

        foreach (var item in events)
        {
            await PlayLogEventAsync(item, playedCard, playedHandIndex);
        }
    }

    private async Task PlayLogEventAsync(CombatLogEvent item, CardDefinition? playedCard, int? playedHandIndex)
    {
        switch (item.EventType)
        {
            case CombatLogEventType.CardPlayed:
                await PlayCardPlayedAsync(item, playedCard, playedHandIndex);
                break;
            case CombatLogEventType.EffectResolved:
                await PlayEffectResolvedAsync(item, playedCard);
                break;
            case CombatLogEventType.EnemyIntentResolved:
                await PlayEnemyIntentAsync(item);
                break;
            case CombatLogEventType.EnemyDied:
                await PlayEnemyDiedAsync(item);
                break;
            case CombatLogEventType.PlayerTurnEnded:
                await PulseNodeAsync(handNode, 0.94f, 0.10f);
                break;
            case CombatLogEventType.CardsDrawn:
                await PulseNodeAsync(drawPilePanel, 1.18f, 0.12f);
                await PulseNodeAsync(handNode, 1.03f, 0.10f);
                break;
            case CombatLogEventType.CardsDiscarded:
                await PulseNodeAsync(discardPilePanel, 1.18f, 0.12f);
                break;
            case CombatLogEventType.DeckReshuffled:
                await PulseNodeAsync(drawPilePanel, 1.22f, 0.12f);
                await PulseNodeAsync(discardPilePanel, 1.22f, 0.12f);
                break;
        }
    }

    private async Task PlayCardPlayedAsync(CombatLogEvent item, CardDefinition? playedCard, int? playedHandIndex)
    {
        var sourceCard = playedHandIndex is not null && cardNodesByHandIndex.TryGetValue(playedHandIndex.Value, out var indexedNode)
            ? indexedNode
            : item.SourceId is not null && cardNodes.TryGetValue(item.SourceId, out var nodes)
            ? nodes.FirstOrDefault()
            : null;

        if (sourceCard is not null)
        {
            await PulseNodeAsync(sourceCard, 1.08f, 0.09f);
        }

        if (playedCard?.Type == CardType.Finisher)
        {
            await SpawnVfxAsync("asset.vfx.finisher_release_shockwave", new Vector2(960, 430), new Vector2(560, 260), new Color(1f, 1f, 1f, 0.95f), 0.28f);
            await WaitAsync(0.05);
        }
    }

    private async Task PlayEffectResolvedAsync(CombatLogEvent item, CardDefinition? playedCard)
    {
        var effectType = item.Metadata.TryGetValue("effect_type", out var value) ? value : "";
        if (effectType == "damage" && item.NumericChanges.TryGetValue("hp_damage", out var hpDamage) && hpDamage > 0)
        {
            await PlayDamageAsync(item, playedCard);
            return;
        }

        if ((effectType == "block" || effectType == "gain_block") && item.NumericChanges.TryGetValue("block_gained", out var block) && block > 0)
        {
            await PlayBlockAsync(item);
            return;
        }

        if (effectType == "default_chain_change" && item.NumericChanges.TryGetValue("chain_before", out var before) && item.NumericChanges.TryGetValue("chain_after", out var after))
        {
            await PlayChainChangeAsync(before, after);
            return;
        }

        if (effectType == "draw_cards")
        {
            await PulseNodeAsync(handNode, 1.04f, 0.10f);
            return;
        }

        if (effectType == "gain_action_points")
        {
            await PulseNodeAsync(actionPointPanel, 1.2f, 0.12f);
            return;
        }

        if (effectType == "temporary_discount_placeholder")
        {
            await PulseNodeAsync(handNode, 1.04f, 0.10f);
        }
    }

    private async Task PlayDamageAsync(CombatLogEvent item, CardDefinition? playedCard)
    {
        var asset = playedCard?.Type switch
        {
            CardType.Finisher => "asset.vfx.finisher_release_shockwave",
            CardType.Action when playedCard.Cost >= 2 => "asset.vfx.heavy_strike_impact_frame",
            _ => "asset.vfx.slash_speed_lines"
        };

        foreach (var targetId in item.TargetIds)
        {
            if (!enemyNodes.TryGetValue(targetId, out var enemyNode))
            {
                continue;
            }

            await SpawnVfxAsync(asset, CenterOf(enemyNode), new Vector2(250, 150), new Color(1f, 1f, 1f, 0.94f), 0.20f);
            await SpawnVfxAsync("asset.vfx.enemy_hit_comic_burst", CenterOf(enemyNode), new Vector2(180, 130), new Color(1f, 0.85f, 0.72f, 0.92f), 0.18f);
            await ShakeNodeAsync(enemyNode, 18f, 0.16f);
        }
    }

    private async Task PlayBlockAsync(CombatLogEvent item)
    {
        var target = item.TargetIds.Contains("player", StringComparer.Ordinal)
            ? playerNode
            : item.TargetIds.Select(id => enemyNodes.TryGetValue(id, out var enemyNode) ? enemyNode : null).FirstOrDefault(node => node is not null);
        var center = target is null ? new Vector2(300, 590) : CenterOf(target);
        await SpawnVfxAsync("asset.vfx.defense_shield_flash", center, new Vector2(260, 180), new Color(0.75f, 0.95f, 1f, 0.9f), 0.22f);
        await PulseNodeAsync(target ?? blockPanel, 1.05f, 0.10f);
        await PulseNodeAsync(blockPanel, 1.2f, 0.10f);
    }

    private async Task PlayChainChangeAsync(int before, int after)
    {
        if (after > before)
        {
            await SpawnVfxAsync("asset.vfx.chain_gain_spark", new Vector2(160, 174), new Vector2(180, 120), new Color(1f, 1f, 1f, 0.95f), 0.20f);
            await PulseNodeAsync(chainPanel, 1.18f, 0.11f);
            foreach (var threshold in new[] { 3, 5, 8 })
            {
                if (before < threshold && after >= threshold)
                {
                    await SpawnVfxAsync($"asset.vfx.chain_threshold_{threshold}_burst", new Vector2(190, 178), new Vector2(250, 160), new Color(1f, 1f, 1f, 0.98f), 0.24f);
                }
            }
            return;
        }

        if (after < before)
        {
            await PulseNodeAsync(chainPanel, 0.9f, 0.12f);
        }
    }

    private async Task PlayEnemyIntentAsync(CombatLogEvent item)
    {
        var sourceEnemy = item.SourceId is not null && enemyNodes.TryGetValue(item.SourceId, out var enemyNode)
            ? enemyNode
            : null;
        var effectType = item.Metadata.TryGetValue("effect_type", out var value) ? value : "";

        if (effectType == "damage")
        {
            await LungeNodeAsync(sourceEnemy, new Vector2(-38, 0), 0.12f);
            await SpawnVfxAsync("asset.vfx.enemy_hit_comic_burst", CenterOf(playerNode), new Vector2(210, 150), new Color(1f, 0.74f, 0.62f, 0.9f), 0.18f);
            await ShakeNodeAsync(playerNode, 14f, 0.15f);
            return;
        }

        if (effectType == "block" || effectType == "gain_block")
        {
            var center = sourceEnemy is null ? new Vector2(1250, 520) : CenterOf(sourceEnemy);
            await SpawnVfxAsync("asset.vfx.defense_shield_flash", center, new Vector2(220, 160), new Color(0.72f, 0.95f, 1f, 0.88f), 0.22f);
            await PulseNodeAsync(sourceEnemy, 1.05f, 0.10f);
        }
    }

    private async Task PlayEnemyDiedAsync(CombatLogEvent item)
    {
        foreach (var targetId in item.TargetIds)
        {
            if (!enemyNodes.TryGetValue(targetId, out var enemyNode))
            {
                continue;
            }

            enemyNode.PivotOffset = enemyNode.Size * 0.5f;
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(enemyNode, "scale", enemyNode.Scale * 0.9f, 0.16);
            tween.TweenProperty(enemyNode, "modulate", new Color(0.25f, 0.25f, 0.25f, 0.55f), 0.16);
            await ToSignal(tween, "finished");
        }
    }

    private async Task PulseNodeAsync(Control? node, float peakScale, double duration)
    {
        if (node is null)
        {
            return;
        }

        var originalScale = node.Scale;
        node.PivotOffset = node.Size * 0.5f;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "scale", originalScale * peakScale, duration);
        tween.TweenProperty(node, "scale", originalScale, duration);
        await ToSignal(tween, "finished");
    }

    private async Task ShakeNodeAsync(Control? node, float distance, double duration)
    {
        if (node is null)
        {
            return;
        }

        var originalPosition = node.Position;
        var step = duration / 4.0;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(distance, 0), step);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(-distance * 0.7f, 0), step);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(distance * 0.35f, 0), step);
        tween.TweenProperty(node, "position", originalPosition, step);
        await ToSignal(tween, "finished");
    }

    private async Task LungeNodeAsync(Control? node, Vector2 offset, double duration)
    {
        if (node is null)
        {
            return;
        }

        var originalPosition = node.Position;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "position", originalPosition + offset, duration);
        tween.TweenProperty(node, "position", originalPosition, duration);
        await ToSignal(tween, "finished");
    }

    private async Task SpawnVfxAsync(string assetId, Vector2 center, Vector2 size, Color tint, double duration)
    {
        if (fxLayer is null)
        {
            return;
        }

        var vfx = CreateImage(assetId, size, TextureRect.StretchModeEnum.KeepAspectCentered);
        vfx.Position = center - size * 0.5f;
        vfx.Size = size;
        vfx.CustomMinimumSize = size;
        vfx.Modulate = new Color(tint.R, tint.G, tint.B, 0f);
        vfx.PivotOffset = size * 0.5f;
        vfx.Scale = new Vector2(0.78f, 0.78f);
        vfx.ZIndex = 110;
        fxLayer.AddChild(vfx);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(vfx, "modulate", tint, duration * 0.35);
        tween.TweenProperty(vfx, "scale", new Vector2(1.12f, 1.12f), duration);
        tween.Chain().TweenProperty(vfx, "modulate", new Color(tint.R, tint.G, tint.B, 0f), duration * 0.45);
        await ToSignal(tween, "finished");
        vfx.QueueFree();
    }

    private async Task WaitAsync(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), "timeout");
    }

    private static Vector2 CenterOf(Control? node)
    {
        return node is null
            ? new Vector2(960, 540)
            : node.Position + node.Size * 0.5f;
    }
}
