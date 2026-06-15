using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Shared;

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

        RenderPlayerHud(rootControl, combatState);
        RenderColorEnergyHud(rootControl, combatState);

        ActionPointPanel = CreateActionPointBadge(combatState);
        AddAt(rootControl, ActionPointPanel, new Vector2(70, 760), new Vector2(156, 156));

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
        var vitals = CreatePlayerVitalsHud(combat);
        AddAt(root, vitals, new Vector2(42, 38), new Vector2(1260, 199));
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
        ColorEnergyPanel = new Control
        {
            TooltipText = ColorEnergyTooltip(combat.ColorEnergy)
        };
        var slotSize = new Vector2(88, 110);
        var slotGap = 12f;
        var totalWidth = slotSize.X * ColorEnergyPool.DefaultCapacity + slotGap * (ColorEnergyPool.DefaultCapacity - 1);
        var startX = (640f - totalWidth) * 0.5f;
        for (var index = 0; index < ColorEnergyPool.DefaultCapacity; index++)
        {
            var color = index < combat.ColorEnergy.Slots.Count
                ? combat.ColorEnergy.Slots[index].Color
                : ColorType.Colorless;
            var slot = CreateColorEnergySlot(color, filled: index < combat.ColorEnergy.Slots.Count);
            slot.Position = new Vector2(startX + (slotSize.X + slotGap) * index, 8);
            slot.Size = slotSize;
            slot.CustomMinimumSize = slotSize;
            ColorEnergyPanel.AddChild(slot);
        }

        AddAt(root, ColorEnergyPanel, new Vector2(640, 246), new Vector2(640, 128));
    }

    private Control CreateColorEnergySlot(ColorType color, bool filled)
    {
        var slotSize = new Vector2(88, 110);
        var frameSize = new Vector2(80, 80);
        var flameSize = new Vector2(106, 106);
        var root = new Control
        {
            CustomMinimumSize = slotSize,
            Size = slotSize,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        var frame = CreateImage("asset.ui.battle.color_energy.slot.empty", frameSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        frame.Position = new Vector2((slotSize.X - frameSize.X) * 0.5f, 28);
        frame.Size = frameSize;
        root.AddChild(frame);

        if (filled)
        {
            var frameAssets = ColorEnergyFlameAssets(color);
            var fill = CreateImage(frameAssets[0], flameSize, TextureRect.StretchModeEnum.KeepAspectCentered);
            fill.Position = new Vector2((slotSize.X - flameSize.X) * 0.5f, 5);
            fill.Size = flameSize;
            fill.ZIndex = 1;
            root.AddChild(fill);
            AddColorEnergyFlameTimer(root, fill, frameAssets);
        }

        return root;
    }

    private void AddColorEnergyFlameTimer(Control root, TextureRect fill, IReadOnlyList<string> frameAssets)
    {
        if (frameAssets.Count <= 1)
        {
            return;
        }

        var frameIndex = 0;
        var timer = new Godot.Timer
        {
            WaitTime = 0.085,
            OneShot = false,
            Autostart = true
        };
        timer.Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(fill))
            {
                return;
            }

            frameIndex = (frameIndex + 1) % frameAssets.Count;
            fill.Texture = RequireTextureLoader()(frameAssets[frameIndex]);
        };
        root.AddChild(timer);
    }

    private static string ColorEnergyTooltip(ColorEnergyPool pool)
    {
        if (pool.Count == 0)
        {
            return "当前没有彩能。行动牌会生成彩能，回合结束会清空。";
        }

        return "当前彩能：" + string.Join(" / ", pool.Slots.Select(slot => FullColorName(slot.Color)));
    }

    private static IReadOnlyList<string> ColorEnergyFlameAssets(ColorType color)
    {
        var key = ColorEnergyAssetKey(color);
        return Enumerable.Range(0, 8)
            .Select(index => $"asset.ui.battle.color_energy.flame.{key}.{index:00}")
            .ToArray();
    }

    private static string ColorEnergyAssetKey(ColorType color)
    {
        return color switch
        {
            ColorType.Red => "red",
            ColorType.Yellow => "yellow",
            ColorType.Blue => "blue",
            ColorType.Green => "green",
            ColorType.Purple => "purple",
            _ => "colorless"
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

    private Control CreatePlayerVitalsHud(CombatState combat)
    {
        var root = new Control
        {
            ClipContents = false
        };

        var frameSize = new Vector2(685, 199);
        var fillPosition = new Vector2(139, 51);
        var fillSize = new Vector2(527, 45);
        var healthRatio = combat.PlayerMaxHp <= 0
            ? 0.0f
            : Math.Clamp((float)combat.PlayerHp / combat.PlayerMaxHp, 0.0f, 1.0f);

        var armorArrowOverlap = 56.0f;
        var blockWidth = Math.Clamp(160 + combat.PlayerBlock * 10, 220, (int)fillSize.X);
        var blockClipWidth = blockWidth + armorArrowOverlap;
        var blockRoot = new Control
        {
            ClipContents = true,
            Position = new Vector2(fillPosition.X + fillSize.X - armorArrowOverlap, fillPosition.Y),
            Size = new Vector2(blockClipWidth, fillSize.Y),
            CustomMinimumSize = new Vector2(blockClipWidth, fillSize.Y),
            Visible = combat.PlayerBlock > 0
        };
        var blockFill = CreateImage("asset.ui.battle.player_block_bar", fillSize, TextureRect.StretchModeEnum.Scale);
        blockFill.Position = new Vector2(blockClipWidth - fillSize.X, 0);
        blockFill.Size = fillSize;
        blockRoot.AddChild(blockFill);
        root.AddChild(blockRoot);
        BlockPanel = blockRoot;

        var healthClip = new Control
        {
            ClipContents = true,
            Position = fillPosition,
            Size = new Vector2(fillSize.X * healthRatio, fillSize.Y),
            CustomMinimumSize = new Vector2(fillSize.X * healthRatio, fillSize.Y),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var healthFill = CreateImage("asset.ui.battle.player_health.fill", fillSize, TextureRect.StretchModeEnum.Scale);
        healthFill.Size = fillSize;
        healthClip.AddChild(healthFill);
        root.AddChild(healthClip);

        var frame = CreateImage("asset.ui.battle.player_health_bar", frameSize, TextureRect.StretchModeEnum.Scale);
        frame.Size = frameSize;
        root.AddChild(frame);

        var healthLabel = CreateHudLabel(
            $"{combat.PlayerHp}/{combat.PlayerMaxHp}",
            34,
            Colors.White,
            heavy: true,
            outlineSize: 4,
            outlineColor: new Color(0.0f, 0.0f, 0.0f, 0.88f));
        healthLabel.Position = new Vector2(fillPosition.X + 18, fillPosition.Y - 3);
        healthLabel.Size = new Vector2(fillSize.X - 36, fillSize.Y + 8);
        healthLabel.CustomMinimumSize = healthLabel.Size;
        healthLabel.HorizontalAlignment = HorizontalAlignment.Center;
        healthLabel.VerticalAlignment = VerticalAlignment.Center;
        root.AddChild(healthLabel);

        if (combat.PlayerBlock > 0)
        {
            var blockLabel = CreateHudLabel(
                combat.PlayerBlock.ToString(),
                30,
                Colors.White,
                heavy: true,
                outlineSize: 4,
                outlineColor: new Color(0.0f, 0.0f, 0.0f, 0.86f));
            blockLabel.Position = blockRoot.Position + new Vector2(armorArrowOverlap + 14, -3);
            blockLabel.Size = new Vector2(blockWidth - 28, fillSize.Y + 8);
            blockLabel.CustomMinimumSize = blockLabel.Size;
            blockLabel.HorizontalAlignment = HorizontalAlignment.Center;
            blockLabel.VerticalAlignment = VerticalAlignment.Center;
            root.AddChild(blockLabel);
        }

        return root;
    }

    private Control CreateActionPointBadge(CombatState combat)
    {
        var root = new Control();
        var badge = new ActionPointDiamondBadge
        {
            CustomMinimumSize = new Vector2(156, 156),
            Size = new Vector2(156, 156),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        badge.Size = new Vector2(156, 156);
        root.AddChild(badge);

        var value = CreateHudLabel(
            combat.ActionPoints.ToString(),
            58,
            Colors.White,
            heavy: true,
            outlineSize: 2,
            outlineColor: new Color(0.0f, 0.0f, 0.0f, 0.72f));
        value.HorizontalAlignment = HorizontalAlignment.Center;
        value.VerticalAlignment = VerticalAlignment.Center;
        value.Position = new Vector2(32, 20);
        value.Size = new Vector2(92, 62);
        root.AddChild(value);

        var ap = CreateHudLabel(
            "AP",
            22,
            Colors.White,
            heavy: true,
            outlineSize: 1,
            outlineColor: new Color(0.0f, 0.0f, 0.0f, 0.68f));
        ap.HorizontalAlignment = HorizontalAlignment.Center;
        ap.VerticalAlignment = VerticalAlignment.Center;
        ap.Position = new Vector2(45, 92);
        ap.Size = new Vector2(68, 30);
        root.AddChild(ap);
        return root;
    }

    private Button CreateEndTurnButton()
    {
        return new SketchParallelogramButton(
            "结束回合",
            RequireFontLoader()("asset.font.source_han_sans_sc.heavy"),
            32)
        {
            TooltipText = "结束当前玩家回合",
            CustomMinimumSize = new Vector2(318, 92)
        };
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

}

internal sealed partial class ActionPointDiamondBadge : Control
{
    private static readonly Color Shadow = new(0.0f, 0.0f, 0.0f, 0.30f);
    private static readonly Color Ink = new(0.015f, 0.014f, 0.012f, 0.96f);
    private static readonly Color PaperLine = new(1.0f, 1.0f, 0.96f, 0.96f);

    public override void _Draw()
    {
        var center = Size * 0.5f;
        var radius = MathF.Min(Size.X, Size.Y) * 0.47f;
        var points = new[]
        {
            center + new Vector2(0, -radius),
            center + new Vector2(radius, 0),
            center + new Vector2(0, radius),
            center + new Vector2(-radius, 0)
        };

        DrawOffsetPolygon(points, new Vector2(9, 11), Shadow);
        DrawPolygon(points, [Ink, Ink, Ink, Ink]);
        DrawSketchLine(points[0], points[1], 0.0f, 4.6f, PaperLine);
        DrawSketchLine(points[1], points[2], 1.4f, 3.4f, PaperLine);
        DrawSketchLine(points[2], points[3], -0.8f, 4.2f, PaperLine);
        DrawSketchLine(points[3], points[0], 1.0f, 3.1f, PaperLine);

        var inner = points
            .Select(point => center + (point - center) * 0.78f)
            .ToArray();
        DrawSketchLine(inner[0], inner[1], -0.5f, 1.4f, new Color(PaperLine.R, PaperLine.G, PaperLine.B, 0.34f));
        DrawSketchLine(inner[2], inner[3], 0.7f, 1.2f, new Color(PaperLine.R, PaperLine.G, PaperLine.B, 0.24f));
    }

    private void DrawOffsetPolygon(Vector2[] points, Vector2 offset, Color color)
    {
        var shifted = new Vector2[points.Length];
        for (var index = 0; index < points.Length; index++)
        {
            shifted[index] = points[index] + offset;
        }

        DrawPolygon(shifted, [color, color, color, color]);
    }

    private void DrawSketchLine(Vector2 from, Vector2 to, float normalOffset, float width, Color color)
    {
        var normal = (to - from).Orthogonal().Normalized();
        DrawLine(from + normal * normalOffset, to + normal * normalOffset, color, width, true);
        DrawLine(
            from - normal * (normalOffset + 1.8f),
            to - normal * (normalOffset + 0.3f),
            new Color(color.R, color.G, color.B, color.A * 0.48f),
            MathF.Max(1.0f, width * 0.36f),
            true);
    }
}
