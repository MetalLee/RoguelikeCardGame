using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed class BattleHudView
{
    private readonly CombatTurnService turnService = new();

    private readonly List<Control> enemyHudNodes = new();

    private Control? root;
    private CombatState? combat;
    private GameContent? content;
    private Func<string, Texture2D?>? loadTexture;
    private Func<string, Font?>? loadFont;
    private bool showFocusedEnemyHud;

    public Control? ColorEnergyPanel { get; private set; }

    public Control? BlockPanel { get; private set; }

    public Control? ActionPointPanel { get; private set; }

    public Control? DrawPilePanel { get; private set; }

    public Control? DiscardPilePanel { get; private set; }

    public void Render(
        Control rootControl,
        CombatState combatState,
        RunState run,
        GameContent gameContent,
        Func<string, Texture2D?> textureLoader,
        Func<string, Font?> fontLoader,
        Action endTurnRequested)
    {
        root = rootControl;
        combat = combatState;
        content = gameContent;
        loadTexture = textureLoader;
        loadFont = fontLoader;
        showFocusedEnemyHud = combatState.Enemies.Count <= 1;
        ClearEnemyHud();
        ColorEnergyPanel = null;
        BlockPanel = null;
        ActionPointPanel = null;
        DrawPilePanel = null;
        DiscardPilePanel = null;

        RenderPlayerHud(rootControl, combatState);
        RenderColorEnergyHud(rootControl, combatState);

        ActionPointPanel = CreateActionPointBadge(combatState);
        AddAt(rootControl, ActionPointPanel, new Vector2(70, 760), new Vector2(156, 156));

        DrawPilePanel = CreatePilePanel("asset.ui.battle.draw_pile_panel", "抽牌", combatState.DeckZones.DrawPileCount);
        AddAt(rootControl, DrawPilePanel, new Vector2(70, 910), new Vector2(272, 138));

        DiscardPilePanel = CreatePilePanel("asset.ui.battle.discard_pile_panel", "弃牌", combatState.DeckZones.DiscardPileCount);
        AddAt(rootControl, DiscardPilePanel, new Vector2(1578, 910), new Vector2(272, 138));

        if (run.RelicIds.Count > 0)
        {
            AddAt(rootControl, CreateRelicStrip(run, gameContent), new Vector2(50, 152), new Vector2(350, 48));
        }

        if (showFocusedEnemyHud)
        {
            RenderEnemyHud(enemyInstanceId: null);
        }

        var endTurn = CreateEndTurnButton();
        endTurn.Pressed += () =>
        {
            PlayEndTurnClickAnimation(endTurn);
            endTurnRequested();
        };
        AddAt(rootControl, endTurn, new Vector2(1530, 790), new Vector2(318, 92));
    }

    private void RenderPlayerHud(Control root, CombatState combat)
    {
        var healthBar = CreateHudImagePanel("asset.ui.battle.player_health_bar", $"{combat.PlayerHp}/{combat.PlayerMaxHp}", new Vector2(344, 54), new Rect2(86, 4, 230, 46), 28);
        AddAt(root, healthBar, new Vector2(42, 42), new Vector2(344, 54));

        BlockPanel = CreateHudImagePanel("asset.ui.battle.player_block_bar", combat.PlayerBlock.ToString(), new Vector2(288, 50), new Rect2(82, 3, 160, 42), 27);
        AddAt(root, BlockPanel, new Vector2(42, 96), new Vector2(288, 50));
    }

    public void SetFocusedEnemy(string? enemyInstanceId)
    {
        ClearEnemyHud();
        if (!showFocusedEnemyHud)
        {
            return;
        }

        RenderEnemyHud(enemyInstanceId);
    }

    private void RenderEnemyHud(string? enemyInstanceId)
    {
        if (root is null || combat is null || content is null)
        {
            return;
        }

        var focus = combat.Enemies.FirstOrDefault(enemy => enemy.InstanceId == enemyInstanceId && enemy.CurrentHp > 0) ??
                    combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0);
        if (focus is null)
        {
            return;
        }

        var intent = turnService.GetEnemyIntentViews(combat, content.EnemiesById)
            .FirstOrDefault(view => view.EnemyInstanceId == focus.InstanceId);

        var nameBar = CreateHudImagePanel("asset.ui.battle.enemy_name_bar", content.EnemyName(focus.EnemyId), new Vector2(382, 62), new Rect2(48, 5, 286, 46), 27);
        AddEnemyHudAt(root, nameBar, new Vector2(1490, 42), new Vector2(382, 62));

        var hpText = focus.CurrentHp <= 0 ? "击败" : $"{focus.CurrentHp}/{focus.MaxHp}";
        var healthBar = CreateHudImagePanel("asset.ui.battle.enemy_health_bar", hpText, new Vector2(328, 50), new Rect2(80, 3, 194, 42), 26);
        AddEnemyHudAt(root, healthBar, new Vector2(1538, 102), new Vector2(328, 50));

        var blockBar = CreateHudImagePanel("asset.ui.battle.enemy_block_bar", focus.Block.ToString(), new Vector2(294, 48), new Rect2(76, 3, 160, 40), 25);
        AddEnemyHudAt(root, blockBar, new Vector2(1572, 154), new Vector2(294, 48));

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
        AddEnemyHudAt(root, intentRoot, new Vector2(1570, 204), new Vector2(300, 42));
    }

    private void AddEnemyHudAt(Control parent, Control child, Vector2 position, Vector2 size)
    {
        AddAt(parent, child, position, size);
        enemyHudNodes.Add(child);
    }

    private void ClearEnemyHud()
    {
        foreach (var node in enemyHudNodes)
        {
            if (!GodotObject.IsInstanceValid(node))
            {
                continue;
            }

            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }

        enemyHudNodes.Clear();
    }

    private void RenderColorEnergyHud(Control root, CombatState combat)
    {
        var title = CreateHudLabel($"彩能 {combat.ColorEnergy.Count}/{ColorEnergyPool.DefaultCapacity}", 31, new Color(0.10f, 0.06f, 0.035f), heavy: true, outlineColor: new Color(0.98f, 0.84f, 0.55f, 0.70f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        AddAt(root, title, new Vector2(790, 22), new Vector2(340, 42));

        ColorEnergyPanel = new Control
        {
            TooltipText = ColorEnergyTooltip(combat.ColorEnergy)
        };
        var slotSize = new Vector2(78, 58);
        var slotGap = 16f;
        var totalWidth = slotSize.X * ColorEnergyPool.DefaultCapacity + slotGap * (ColorEnergyPool.DefaultCapacity - 1);
        var startX = (640f - totalWidth) * 0.5f;
        for (var index = 0; index < ColorEnergyPool.DefaultCapacity; index++)
        {
            var color = index < combat.ColorEnergy.Slots.Count
                ? combat.ColorEnergy.Slots[index].Color
                : ColorType.Colorless;
            var slot = CreateColorEnergySlot(color, filled: index < combat.ColorEnergy.Slots.Count);
            slot.Position = new Vector2(startX + (slotSize.X + slotGap) * index, 18);
            slot.Size = slotSize;
            slot.CustomMinimumSize = slotSize;
            ColorEnergyPanel.AddChild(slot);
        }

        AddAt(root, ColorEnergyPanel, new Vector2(640, 64), new Vector2(640, 104));
    }

    private Control CreateColorEnergySlot(ColorType color, bool filled)
    {
        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = filled ? ColorForEnergy(color) : new Color(0.78f, 0.72f, 0.62f, 0.52f),
            BorderColor = filled ? new Color(0.12f, 0.08f, 0.04f, 0.92f) : new Color(0.28f, 0.22f, 0.14f, 0.72f),
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6
        });

        var label = CreateHudLabel(filled ? ShortColorName(color) : "", 20, TextColorForEnergy(color), heavy: true, outlineSize: 2);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(label);
        return panel;
    }

    private static string ColorEnergyTooltip(ColorEnergyPool pool)
    {
        if (pool.Count == 0)
        {
            return "当前没有彩能。行动牌会生成彩能，回合结束会清空。";
        }

        return "当前彩能：" + string.Join(" / ", pool.Slots.Select(slot => FullColorName(slot.Color)));
    }

    private static Color ColorForEnergy(ColorType color)
    {
        return color switch
        {
            ColorType.Red => new Color(0.78f, 0.13f, 0.10f, 0.94f),
            ColorType.Yellow => new Color(0.95f, 0.72f, 0.13f, 0.94f),
            ColorType.Blue => new Color(0.14f, 0.40f, 0.82f, 0.94f),
            ColorType.Green => new Color(0.18f, 0.62f, 0.25f, 0.94f),
            ColorType.Purple => new Color(0.52f, 0.22f, 0.78f, 0.94f),
            _ => new Color(0.84f, 0.80f, 0.70f, 0.94f)
        };
    }

    private static Color TextColorForEnergy(ColorType color)
    {
        return color == ColorType.Yellow || color == ColorType.Colorless
            ? new Color(0.14f, 0.08f, 0.04f)
            : new Color(1.0f, 0.94f, 0.78f);
    }

    private static string ShortColorName(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "红",
            ColorType.Yellow => "黄",
            ColorType.Blue => "蓝",
            ColorType.Green => "绿",
            ColorType.Purple => "紫",
            _ => "无"
        };
    }

    private static string FullColorName(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "红色",
            ColorType.Yellow => "黄色",
            ColorType.Blue => "蓝色",
            ColorType.Green => "绿色",
            ColorType.Purple => "紫色",
            _ => "无色"
        };
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

    private Control CreateActionPointBadge(CombatState combat)
    {
        var root = new Control();
        var badge = CreateImage("asset.ui.battle.action_point_badge", new Vector2(156, 156), TextureRect.StretchModeEnum.Scale);
        badge.Size = new Vector2(156, 156);
        root.AddChild(badge);

        var value = CreateHudLabel(combat.ActionPoints.ToString(), 58, new Color(1.0f, 0.92f, 0.75f), heavy: true);
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
        image.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        button.AddChild(image);

        var label = CreateHudLabel("结束回合", 35, new Color(1.0f, 0.88f, 0.64f), heavy: true);
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        button.AddChild(label);
        return button;
    }

    private static void PlayEndTurnClickAnimation(Control button)
    {
        if (!GodotObject.IsInstanceValid(button))
        {
            return;
        }

        button.PivotOffset = button.Size * 0.5f;
        button.Scale = Vector2.One;
        button.Modulate = Colors.White;

        var scaleTween = button.CreateTween();
        scaleTween.SetTrans(Tween.TransitionType.Cubic);
        scaleTween.SetEase(Tween.EaseType.Out);
        scaleTween.TweenProperty(button, "scale", new Vector2(0.94f, 0.94f), 0.045);
        scaleTween.TweenProperty(button, "scale", new Vector2(1.055f, 1.055f), 0.075);
        scaleTween.TweenProperty(button, "scale", Vector2.One, 0.09);

        var flashTween = button.CreateTween();
        flashTween.SetTrans(Tween.TransitionType.Sine);
        flashTween.SetEase(Tween.EaseType.Out);
        flashTween.TweenProperty(button, "modulate", new Color(1.0f, 0.92f, 0.72f, 1.0f), 0.05);
        flashTween.TweenProperty(button, "modulate", Colors.White, 0.14);
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
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", outlineColor ?? new Color(0.05f, 0.025f, 0.01f, 0.82f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.02f, 0.012f, 0.006f, 0.55f));
        label.AddThemeConstantOverride("outline_size", outlineSize);
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        var font = RequireFontLoader()(heavy ? "asset.font.source_han_sans_sc.heavy" : "asset.font.source_han_sans_sc.medium");
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        return label;
    }

    private Control CreateRelicStrip(RunState run, GameContent content)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        foreach (var relicId in run.RelicIds)
        {
            var icon = CreateImage(content.RelicViewsById[relicId].IconAsset, new Vector2(34, 34), TextureRect.StretchModeEnum.KeepAspectCentered);
            icon.TooltipText = $"{content.RelicName(relicId)}：{content.RelicRules(relicId)}";
            row.AddChild(icon);
        }

        return row;
    }

    private TextureRect CreateImage(string assetId, Vector2 minSize, TextureRect.StretchModeEnum stretchMode)
    {
        return new TextureRect
        {
            Texture = RequireTextureLoader()(assetId),
            CustomMinimumSize = minSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    private Func<string, Texture2D?> RequireTextureLoader()
    {
        return loadTexture ?? throw new InvalidOperationException("BattleHudView requires a texture loader.");
    }

    private Func<string, Font?> RequireFontLoader()
    {
        return loadFont ?? throw new InvalidOperationException("BattleHudView requires a font loader.");
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

    private static void AddAt(Control parent, Control child, Vector2 position, Vector2 size)
    {
        child.Position = position;
        child.Size = size;
        child.CustomMinimumSize = size;
        parent.AddChild(child);
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
}
