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
    // 16:9 design-space stage tuning. Adjust this line first when matching feet to the painted ground.
    private const float StageGroundY = 785f;

    private static readonly Dictionary<string, EnemySpriteLayout> EnemySpriteLayouts = new(StringComparer.Ordinal)
    {
        ["enemy.training_dummy"] = new(new Vector2(1254, 1254), 1208f, 0.32f),
        ["enemy.intent_scout"] = new(new Vector2(1254, 1254), 1185f, 0.31f),
        ["enemy.splitling"] = new(new Vector2(1254, 1254), 1173f, 0.25f),
        ["enemy.elite_guardian"] = new(new Vector2(1132, 1390), 1292f, 0.45f),
        ["enemy.relic_tester"] = new(new Vector2(1254, 1254), 1182f, 0.41f),
        ["enemy.chain_warden"] = new(new Vector2(1402, 1122), 1095f, 0.50f)
    };

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
    private bool showBattleLog;
    private string? currentMessage;

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
        currentMessage = message;
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

        var root = CreateCanvas();

        RenderPlayerHud(root);
        RenderChainHud(root);
        RenderEnemyHud(root);

        actionPointPanel = CreateActionPointBadge();
        AddAt(root, actionPointPanel, new Vector2(70, 760), new Vector2(156, 156));
        drawPilePanel = CreatePilePanel("asset.ui.battle.draw_pile_panel", "抽牌", combat.DeckZones.DrawPileCount);
        AddAt(root, drawPilePanel, new Vector2(70, 910), new Vector2(272, 138));
        discardPilePanel = CreatePilePanel("asset.ui.battle.discard_pile_panel", "弃牌", combat.DeckZones.DiscardPileCount);
        AddAt(root, discardPilePanel, new Vector2(1578, 910), new Vector2(272, 138));

        var playerSize = new Vector2(430, 430);
        var playerPosition = new Vector2(250, StageGroundY - playerSize.Y + 50);
        playerNode = CreateImage("asset.character.swordsman.battle", playerSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        playerNode.Name = "PlayerStand";
        AddAt(root, playerNode, playerPosition, playerSize);

        if (run.RelicIds.Count > 0)
        {
            AddAt(root, CreateRelicStrip(), new Vector2(50, 152), new Vector2(350, 48));
        }

        var enemyLayouts = combat.Enemies
            .Select(enemy => (State: enemy, Layout: EnemySpriteLayoutFor(enemy.EnemyId)))
            .ToList();
        var spacing = enemyLayouts.Count <= 1 ? 0 : 28;
        var totalWidth = enemyLayouts.Sum(item => item.Layout.DisplaySize.X) + Math.Max(0, enemyLayouts.Count - 1) * spacing;
        var startX = 1395 - totalWidth / 2f;
        var enemyX = startX;
        foreach (var (enemyState, spriteLayout) in enemyLayouts)
        {
            var enemyPosition = new Vector2(enemyX, StageGroundY - spriteLayout.ContentBottom * spriteLayout.Scale);
            var enemyControl = CreateEnemyButton(enemyState, spriteLayout.DisplaySize);
            enemyNodes[enemyState.InstanceId] = enemyControl;
            AddAt(root, enemyControl, enemyPosition, spriteLayout.DisplaySize);
            enemyX += spriteLayout.DisplaySize.X + spacing;
        }

        var hand = new Control
        {
            ClipContents = false
        };
        handNode = hand;
        var handCount = combat.DeckZones.Hand.Count;
        var cardWidth = handCount <= 5 ? 210f : 188f;
        var cardSize = CardPanel.SizeForWidth(cardWidth);
        var step = handCount <= 1 ? 0f : Math.Min(142f, (900f - cardWidth) / Math.Max(1, handCount - 1));
        var totalHandWidth = cardWidth + step * Math.Max(0, handCount - 1);
        var startHandX = (950f - totalHandWidth) * 0.5f;
        for (var handIndex = 0; handIndex < combat.DeckZones.Hand.Count; handIndex++)
        {
            var cardId = combat.DeckZones.Hand[handIndex];
            var cardControl = CreateCardButton(cardId, handIndex);
            var normalized = handCount <= 1 ? 0f : (handIndex / (float)(handCount - 1) - 0.5f) * 2f;
            var arcLift = (1f - Math.Abs(normalized)) * 26f;
            cardControl.Position = new Vector2(startHandX + step * handIndex, 36f - arcLift);
            cardControl.Size = cardSize;
            cardControl.CustomMinimumSize = cardSize;
            cardControl.PivotOffset = cardSize * 0.5f;
            cardControl.RotationDegrees = normalized * 8f;
            cardNodesByHandIndex[handIndex] = cardControl;
            if (!cardNodes.TryGetValue(cardId, out var controls))
            {
                controls = new List<Control>();
                cardNodes[cardId] = controls;
            }

            controls.Add(cardControl);
            hand.AddChild(cardControl);
        }

        AddAt(root, hand, new Vector2(475, 742), new Vector2(950, 360));

        var endTurn = CreateEndTurnButton();
        endTurn.Pressed += () => EndTurnRequested?.Invoke();
        AddAt(root, endTurn, new Vector2(1530, 790), new Vector2(318, 92));

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

    private void RenderPlayerHud(Control root)
    {
        var healthBar = CreateHudImagePanel("asset.ui.battle.player_health_bar", $"{combat!.PlayerHp}/{combat.PlayerMaxHp}", new Vector2(344, 54), new Rect2(86, 4, 230, 46), 28);
        AddAt(root, healthBar, new Vector2(42, 42), new Vector2(344, 54));

        blockPanel = CreateHudImagePanel("asset.ui.battle.player_block_bar", combat.PlayerBlock.ToString(), new Vector2(288, 50), new Rect2(82, 3, 160, 42), 27);
        AddAt(root, blockPanel, new Vector2(42, 96), new Vector2(288, 50));
    }

    private void RenderEnemyHud(Control root)
    {
        var content = RequireContent();
        var focus = combat!.Enemies.FirstOrDefault(enemy => enemy.InstanceId == selectedEnemyInstanceId && enemy.CurrentHp > 0)
            ?? combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)
            ?? combat.Enemies.FirstOrDefault();
        if (focus is null)
        {
            return;
        }

        var intent = turnService.GetEnemyIntentViews(combat, content.EnemiesById)
            .FirstOrDefault(view => view.EnemyInstanceId == focus.InstanceId);

        var nameBar = CreateHudImagePanel("asset.ui.battle.enemy_name_bar", content.EnemyName(focus.EnemyId), new Vector2(382, 62), new Rect2(48, 5, 286, 46), 27);
        AddAt(root, nameBar, new Vector2(1490, 42), new Vector2(382, 62));

        var hpText = focus.CurrentHp <= 0 ? "击败" : $"{focus.CurrentHp}/{focus.MaxHp}";
        var healthBar = CreateHudImagePanel("asset.ui.battle.enemy_health_bar", hpText, new Vector2(328, 50), new Rect2(80, 3, 194, 42), 26);
        AddAt(root, healthBar, new Vector2(1538, 102), new Vector2(328, 50));

        var blockBar = CreateHudImagePanel("asset.ui.battle.enemy_block_bar", focus.Block.ToString(), new Vector2(294, 48), new Rect2(76, 3, 160, 40), 25);
        AddAt(root, blockBar, new Vector2(1572, 154), new Vector2(294, 48));

        if (intent is null)
        {
            return;
        }

        var intentRoot = new Control();
        var icon = CreateImage(IntentIconAsset(intent.IntentType), new Vector2(38, 38), TextureRect.StretchModeEnum.KeepAspectCentered);
        icon.Position = new Vector2(0, 1);
        intentRoot.AddChild(icon);

        var label = CreateHudLabel(content.EnemyIntentText(focus.EnemyId, intent.IntentId), 22, new Color(0.96f, 0.88f, 0.68f), heavy: false);
        label.Position = new Vector2(44, 0);
        label.Size = new Vector2(250, 40);
        label.CustomMinimumSize = label.Size;
        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.VerticalAlignment = VerticalAlignment.Center;
        intentRoot.AddChild(label);
        AddAt(root, intentRoot, new Vector2(1570, 204), new Vector2(300, 42));
    }

    private void RenderChainHud(Control root)
    {
        var chain = combat!.Chain;
        var chainText = chain > 8 ? $"连锁 {chain}/8+" : $"连锁 {chain}/8";
        var title = CreateHudLabel(chainText, 31, new Color(0.10f, 0.06f, 0.035f), heavy: true, outlineColor: new Color(0.98f, 0.84f, 0.55f, 0.70f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        AddAt(root, title, new Vector2(790, 22), new Vector2(340, 42));

        chainPanel = new Control();
        var meterPosition = new Vector2(0, 18);
        var meterSize = new Vector2(640, 68);
        var meter = CreateImage("asset.ui.battle.chain_meter_8_slots", meterSize, TextureRect.StretchModeEnum.Scale);
        meter.Position = meterPosition;
        meter.Size = meterSize;
        chainPanel.AddChild(meter);

        var pointSize = new Vector2(44, 44);
        var firstCenterX = 31f;
        var slotStep = 82.7f;
        for (var i = 0; i < Math.Min(chain, 8); i++)
        {
            var point = CreateImage("asset.ui.battle.chain_point_red", pointSize, TextureRect.StretchModeEnum.Scale);
            point.Position = meterPosition + new Vector2(firstCenterX + slotStep * i - pointSize.X * 0.5f, 34f - pointSize.Y * 0.5f);
            point.Size = pointSize;
            point.CustomMinimumSize = pointSize;
            chainPanel.AddChild(point);
        }

        foreach (var (threshold, index) in new[] { (3, 2), (5, 4), (8, 7) })
        {
            var hint = CreateHudLabel(threshold.ToString(), 14, chain >= threshold ? new Color(0.82f, 0.18f, 0.12f) : new Color(0.35f, 0.28f, 0.18f), heavy: false, outlineSize: 2);
            hint.HorizontalAlignment = HorizontalAlignment.Center;
            hint.Position = meterPosition + new Vector2(firstCenterX + slotStep * index - 16f, 77f);
            hint.Size = new Vector2(32, 20);
            chainPanel.AddChild(hint);
        }

        AddAt(root, chainPanel, new Vector2(640, 64), new Vector2(640, 104));
    }

    private Control CreateHudImagePanel(string assetId, string text, Vector2 size, Rect2 textRect, int fontSize)
    {
        var root = new Control
        {
            ClipContents = false
        };
        var image = CreateImage(assetId, size, TextureRect.StretchModeEnum.Scale);
        image.Size = size;
        root.AddChild(image);

        var label = CreateHudLabel(text, fontSize, new Color(1.0f, 0.92f, 0.74f), heavy: true);
        label.Position = textRect.Position;
        label.Size = textRect.Size;
        label.CustomMinimumSize = textRect.Size;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        root.AddChild(label);
        return root;
    }

    private Control CreateActionPointBadge()
    {
        var root = new Control();
        var badge = CreateImage("asset.ui.battle.action_point_badge", new Vector2(156, 156), TextureRect.StretchModeEnum.Scale);
        badge.Size = new Vector2(156, 156);
        root.AddChild(badge);

        var value = CreateHudLabel(combat!.ActionPoints.ToString(), 58, new Color(1.0f, 0.92f, 0.75f), heavy: true);
        value.HorizontalAlignment = HorizontalAlignment.Center;
        value.VerticalAlignment = VerticalAlignment.Center;
        value.Position = new Vector2(32, 20);
        value.Size = new Vector2(92, 62);
        root.AddChild(value);

        var ap = CreateHudLabel("AP", 22, new Color(1.0f, 0.85f, 0.54f), heavy: true);
        ap.HorizontalAlignment = HorizontalAlignment.Center;
        ap.VerticalAlignment = VerticalAlignment.Center;
        ap.Position = new Vector2(45, 92);
        ap.Size = new Vector2(68, 30);
        root.AddChild(ap);
        return root;
    }

    private Control CreatePilePanel(string assetId, string labelText, int count)
    {
        var root = new Control();
        var panel = CreateImage(assetId, new Vector2(272, 138), TextureRect.StretchModeEnum.Scale);
        panel.Size = new Vector2(272, 138);
        root.AddChild(panel);

        var label = CreateHudLabel(labelText, 24, new Color(1.0f, 0.89f, 0.66f), heavy: true);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Vector2(116, 28);
        label.Size = new Vector2(118, 32);
        root.AddChild(label);

        var value = CreateHudLabel(count.ToString(), 34, new Color(1.0f, 0.92f, 0.74f), heavy: true);
        value.HorizontalAlignment = HorizontalAlignment.Center;
        value.Position = new Vector2(118, 64);
        value.Size = new Vector2(112, 44);
        root.AddChild(value);
        return root;
    }

    private Button CreateEndTurnButton()
    {
        var button = new Button
        {
            Text = "",
            TooltipText = "结束当前玩家回合"
        };
        MakeTransparentButton(button);

        var image = CreateImage("asset.ui.battle.end_turn_button", new Vector2(318, 92), TextureRect.StretchModeEnum.Scale);
        image.SetAnchorsPreset(LayoutPreset.FullRect);
        button.AddChild(image);

        var label = CreateHudLabel("结束回合", 35, new Color(1.0f, 0.88f, 0.64f), heavy: true);
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        button.AddChild(label);
        return button;
    }

    private Label CreateHudLabel(
        string text,
        int fontSize,
        Color color,
        bool heavy,
        int outlineSize = 4,
        Color? outlineColor = null)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipText = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", outlineColor ?? new Color(0.05f, 0.025f, 0.01f, 0.82f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.02f, 0.012f, 0.006f, 0.55f));
        label.AddThemeConstantOverride("outline_size", outlineSize);
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        var font = LoadFont(heavy ? "asset.font.source_han_sans_sc.heavy" : "asset.font.source_han_sans_sc.medium");
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        return label;
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

    private EnemySpriteLayout EnemySpriteLayoutFor(string enemyId)
    {
        if (EnemySpriteLayouts.TryGetValue(enemyId, out var layout))
        {
            return layout;
        }

        return new EnemySpriteLayout(new Vector2(1254, 1254), 1254f, 0.30f);
    }

    private Control CreateEnemyButton(CombatEnemyState enemyState, Vector2 displaySize)
    {
        var content = RequireContent();
        var panel = new Control
        {
            CustomMinimumSize = displaySize,
            Size = displaySize,
            ClipContents = false,
            MouseFilter = MouseFilterEnum.Pass
        };

        var enemyView = content.EnemyViewsById[enemyState.EnemyId];
        var portrait = CreateImage(enemyView.StandAsset, displaySize, TextureRect.StretchModeEnum.Scale);
        portrait.Position = new Vector2(0, 0);
        portrait.Size = displaySize;
        portrait.Modulate = enemyState.CurrentHp <= 0 ? new Color(0.32f, 0.32f, 0.32f, 0.75f) : Colors.White;
        panel.AddChild(portrait);

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
            targetIcon.Position = new Vector2(displaySize.X * 0.5f - 23, displaySize.Y - 66);
            panel.AddChild(targetIcon);
        }

        return panel;
    }

    private PanelContainer CreateEnemyStatusPill(string text, Color borderColor)
    {
        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.82f, 0.68f, 0.46f, 0.72f),
            BorderColor = borderColor,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 3,
            ContentMarginBottom = 3
        });

        var name = CreateSmallLabel(text);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.VerticalAlignment = VerticalAlignment.Center;
        name.AddThemeFontSizeOverride("font_size", 15);
        name.AddThemeColorOverride("font_color", new Color(0.16f, 0.09f, 0.04f));
        panel.AddChild(name);
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

    public void HidePlayedCard(int handIndex)
    {
        if (cardNodesByHandIndex.TryGetValue(handIndex, out var cardNode))
        {
            cardNode.Visible = false;
            cardNode.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    public async Task PlayLogAnimationsAsync(
        IReadOnlyList<CombatLogEvent> events,
        CardDefinition? playedCard = null,
        int? playedHandIndex = null,
        bool playConcurrently = false)
    {
        if (events.Count == 0)
        {
            return;
        }

        if (playConcurrently)
        {
            await Task.WhenAll(events.Select(item => PlayLogEventAsync(item, playedCard, playedHandIndex)));
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

        if (sourceCard is not null && sourceCard.Visible)
        {
            await PulseNodeAsync(sourceCard, 1.08f, 0.09f);
        }

        if (playedCard?.Type == CardType.Finisher)
        {
            await SpawnVfxAsync(fxLayer, "asset.vfx.finisher_release_shockwave", new Vector2(960, 430), new Vector2(560, 260), new Color(1f, 1f, 1f, 0.95f), 0.28f);
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

            var center = CenterOf(enemyNode);
            await Task.WhenAll(
                SpawnVfxAsync(fxLayer, asset, center, new Vector2(250, 150), new Color(1f, 1f, 1f, 0.94f), 0.20f),
                SpawnVfxAsync(fxLayer, "asset.vfx.enemy_hit_comic_burst", center, new Vector2(180, 130), new Color(1f, 0.85f, 0.72f, 0.92f), 0.18f),
                ShakeNodeAsync(enemyNode, 18f, 0.16f));
        }
    }

    private async Task PlayBlockAsync(CombatLogEvent item)
    {
        var target = item.TargetIds.Contains("player", StringComparer.Ordinal)
            ? playerNode
            : item.TargetIds.Select(id => enemyNodes.TryGetValue(id, out var enemyNode) ? enemyNode : null).FirstOrDefault(node => node is not null);
        var center = target is null ? new Vector2(300, 590) : CenterOf(target);
        await Task.WhenAll(
            SpawnVfxAsync(fxLayer, "asset.vfx.defense_shield_flash", center, new Vector2(260, 180), new Color(0.75f, 0.95f, 1f, 0.9f), 0.22f),
            PulseNodeAsync(target ?? blockPanel, 1.05f, 0.10f),
            PulseNodeAsync(blockPanel, 1.2f, 0.10f));
    }

    private async Task PlayChainChangeAsync(int before, int after)
    {
        if (after > before)
        {
            var animations = new List<Task>
            {
                SpawnVfxAsync(fxLayer, "asset.vfx.chain_gain_spark", new Vector2(960, 118), new Vector2(220, 130), new Color(1f, 1f, 1f, 0.95f), 0.20f),
                PulseNodeAsync(chainPanel, 1.18f, 0.11f)
            };
            foreach (var threshold in new[] { 3, 5, 8 })
            {
                if (before < threshold && after >= threshold)
                {
                    animations.Add(SpawnVfxAsync(fxLayer, $"asset.vfx.chain_threshold_{threshold}_burst", new Vector2(960, 118), new Vector2(300, 180), new Color(1f, 1f, 1f, 0.98f), 0.24f));
                }
            }
            await Task.WhenAll(animations);
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
            await Task.WhenAll(
                LungeNodeAsync(sourceEnemy, new Vector2(-38, 0), 0.12f),
                SpawnVfxAsync(fxLayer, "asset.vfx.enemy_hit_comic_burst", CenterOf(playerNode), new Vector2(210, 150), new Color(1f, 0.74f, 0.62f, 0.9f), 0.18f),
                ShakeNodeAsync(playerNode, 14f, 0.15f));
            return;
        }

        if (effectType == "block" || effectType == "gain_block")
        {
            var center = sourceEnemy is null ? new Vector2(1250, 520) : CenterOf(sourceEnemy);
            await Task.WhenAll(
                SpawnVfxAsync(fxLayer, "asset.vfx.defense_shield_flash", center, new Vector2(220, 160), new Color(0.72f, 0.95f, 1f, 0.88f), 0.22f),
                PulseNodeAsync(sourceEnemy, 1.05f, 0.10f));
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

    private readonly record struct EnemySpriteLayout(Vector2 SourceSize, float ContentBottom, float Scale)
    {
        public Vector2 DisplaySize => SourceSize * Scale;
    }
}
