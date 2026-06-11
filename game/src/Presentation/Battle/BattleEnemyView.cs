using Godot;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleEnemyView : Control
{
    private const float StageGroundY = 785f;

    private readonly CombatTurnService turnService = new();
    private static readonly Dictionary<string, EnemySpriteLayout> EnemySpriteLayouts = new(StringComparer.Ordinal)
    {
        ["enemy.training_dummy"] = new(new Vector2(1254, 1254), 1208f, 0.32f),
        ["enemy.intent_scout"] = new(new Vector2(1254, 1254), 1185f, 0.31f),
        ["enemy.splitling"] = new(new Vector2(1254, 1254), 1173f, 0.25f),
        ["enemy.elite_guardian"] = new(new Vector2(1132, 1390), 1292f, 0.45f),
        ["enemy.relic_tester"] = new(new Vector2(1254, 1254), 1182f, 0.41f),
        ["enemy.chain_warden"] = new(new Vector2(1402, 1122), 1095f, 0.50f)
    };

    private readonly Dictionary<string, Control> enemyNodes = new(StringComparer.Ordinal);

    public event Action<string?>? EnemyHoveredChanged;

    public IReadOnlyDictionary<string, Control> EnemyNodes => enemyNodes;

    public void Render(
        Control root,
        CombatState combat,
        GameContent content,
        BattleTargetingOverlay targetingOverlay,
        Func<string, Texture2D?> loadTexture,
        Func<string, Font?> loadFont)
    {
        enemyNodes.Clear();

        var intentsByEnemy = turnService.GetEnemyIntentViews(combat, content.EnemiesById)
            .ToDictionary(intent => intent.EnemyInstanceId, StringComparer.Ordinal);
        var showOverheadStatus = combat.Enemies.Count > 1;
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
            intentsByEnemy.TryGetValue(enemyState.InstanceId, out var intent);
            var enemyControl = CreateEnemyPanel(enemyState, intent, spriteLayout.DisplaySize, showOverheadStatus, content, targetingOverlay, loadTexture, loadFont);
            enemyNodes[enemyState.InstanceId] = enemyControl;
            targetingOverlay.RegisterEnemy(enemyState.InstanceId, enemyControl);
            AddAt(root, enemyControl, enemyPosition, spriteLayout.DisplaySize);
            enemyX += spriteLayout.DisplaySize.X + spacing;
        }
    }

    private static EnemySpriteLayout EnemySpriteLayoutFor(string enemyId)
    {
        if (EnemySpriteLayouts.TryGetValue(enemyId, out var layout))
        {
            return layout;
        }

        return new EnemySpriteLayout(new Vector2(1254, 1254), 1254f, 0.30f);
    }

    private Control CreateEnemyPanel(
        CombatEnemyState enemyState,
        EnemyIntentView? intent,
        Vector2 displaySize,
        bool showOverheadStatus,
        GameContent content,
        BattleTargetingOverlay targetingOverlay,
        Func<string, Texture2D?> loadTexture,
        Func<string, Font?> loadFont)
    {
        var panel = new Control
        {
            CustomMinimumSize = displaySize,
            Size = displaySize,
            ClipContents = false,
            MouseFilter = MouseFilterEnum.Pass
        };
        panel.MouseEntered += () =>
        {
            if (enemyState.CurrentHp > 0)
            {
                EnemyHoveredChanged?.Invoke(enemyState.InstanceId);
            }
        };
        panel.MouseExited += () => EnemyHoveredChanged?.Invoke(null);

        var enemyView = content.EnemyViewsById[enemyState.EnemyId];
        var portrait = CreateImage(enemyView.StandAsset, displaySize, TextureRect.StretchModeEnum.Scale, loadTexture);
        portrait.Position = new Vector2(0, 0);
        portrait.Size = displaySize;
        portrait.Modulate = enemyState.CurrentHp <= 0 ? new Color(0.32f, 0.32f, 0.32f, 0.75f) : Colors.White;
        panel.AddChild(portrait);

        if (showOverheadStatus && enemyState.CurrentHp > 0)
        {
            panel.AddChild(CreateOverheadStatus(enemyState, intent, displaySize, content, loadTexture, loadFont));
        }

        var dragHighlight = targetingOverlay.CreateEnemyDragHighlight(enemyState.InstanceId);
        dragHighlight.SetAnchorsPreset(LayoutPreset.FullRect);
        dragHighlight.Visible = false;
        panel.AddChild(dragHighlight);

        return panel;
    }

    private Control CreateOverheadStatus(
        CombatEnemyState enemyState,
        EnemyIntentView? intent,
        Vector2 displaySize,
        GameContent content,
        Func<string, Texture2D?> loadTexture,
        Func<string, Font?> loadFont)
    {
        var width = Math.Clamp(displaySize.X * 0.62f, 190f, 250f);
        var root = new Control
        {
            Size = new Vector2(width, 92),
            CustomMinimumSize = new Vector2(width, 92),
            Position = new Vector2((displaySize.X - width) * 0.5f, -100f),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 35
        };

        var nameBar = CreateOverheadImagePanel(
            "asset.ui.battle.enemy_name_bar",
            content.EnemyName(enemyState.EnemyId),
            new Vector2(width, 28),
            new Rect2(width * 0.13f, 2f, width * 0.74f, 22f),
            13,
            loadTexture,
            loadFont);
        root.AddChild(nameBar);

        var hpText = enemyState.Block > 0
            ? $"{enemyState.CurrentHp}/{enemyState.MaxHp}  防{enemyState.Block}"
            : $"{enemyState.CurrentHp}/{enemyState.MaxHp}";
        var healthBar = CreateOverheadImagePanel(
            "asset.ui.battle.enemy_health_bar",
            hpText,
            new Vector2(width, 27),
            new Rect2(width * 0.26f, 2f, width * 0.55f, 22f),
            13,
            loadTexture,
            loadFont);
        healthBar.Position = new Vector2(0, 30);
        root.AddChild(healthBar);

        var intentRow = CreateIntentRow(intent, new Vector2(width, 30), loadTexture, loadFont);
        intentRow.Position = new Vector2(0, 60);
        root.AddChild(intentRow);
        return root;
    }

    private static TextureRect CreateImage(
        string assetId,
        Vector2 minSize,
        TextureRect.StretchModeEnum stretchMode,
        Func<string, Texture2D?> loadTexture)
    {
        return new TextureRect
        {
            Texture = loadTexture(assetId),
            CustomMinimumSize = minSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = MouseFilterEnum.Ignore
        };
    }

    private static Label CreateOverheadLabel(
        string text,
        int fontSize,
        Color color,
        Func<string, Font?> loadFont,
        bool heavy)
    {
        var label = new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipText = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0.95f, 0.82f, 0.58f, 0.65f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0.08f, 0.04f, 0.02f, 0.35f));
        label.AddThemeConstantOverride("outline_size", 2);
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        var font = loadFont(heavy ? "asset.font.source_han_sans_sc.heavy" : "asset.font.source_han_sans_sc.medium");
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        return label;
    }

    private static Control CreateOverheadImagePanel(
        string assetId,
        string text,
        Vector2 size,
        Rect2 textRect,
        int fontSize,
        Func<string, Texture2D?> loadTexture,
        Func<string, Font?> loadFont)
    {
        var root = new Control
        {
            Size = size,
            CustomMinimumSize = size,
            MouseFilter = MouseFilterEnum.Ignore
        };

        var image = CreateImage(assetId, size, TextureRect.StretchModeEnum.Scale, loadTexture);
        image.Size = size;
        root.AddChild(image);

        var label = CreateOverheadLabel(text, fontSize, new Color(1f, 0.91f, 0.70f), loadFont, heavy: true);
        label.Position = textRect.Position;
        label.Size = textRect.Size;
        label.CustomMinimumSize = textRect.Size;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(label);
        return root;
    }

    private static Control CreateIntentRow(
        EnemyIntentView? intent,
        Vector2 size,
        Func<string, Texture2D?> loadTexture,
        Func<string, Font?> loadFont)
    {
        var root = new Control
        {
            Size = size,
            CustomMinimumSize = size,
            MouseFilter = MouseFilterEnum.Ignore
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.055f, 0.035f, 0.78f),
            BorderColor = IntentAccent(intent?.IntentType),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        var panel = new PanelContainer
        {
            Size = size,
            CustomMinimumSize = size,
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", style);
        root.AddChild(panel);

        var icon = CreateImage(IntentIconAsset(intent?.IntentType), new Vector2(24, 24), TextureRect.StretchModeEnum.KeepAspectCentered, loadTexture);
        icon.Position = new Vector2(7, 3);
        icon.Size = new Vector2(24, 24);
        root.AddChild(icon);

        var label = CreateOverheadLabel(IntentDisplayText(intent), 13, IntentTextColor(intent?.IntentType), loadFont, heavy: true);
        label.Position = new Vector2(36, 3);
        label.Size = new Vector2(size.X - 44, 24);
        label.HorizontalAlignment = HorizontalAlignment.Left;
        root.AddChild(label);
        return root;
    }

    private static string IntentSummary(EnemyIntentView? intent)
    {
        if (intent is null)
        {
            return "-";
        }

        var damage = intent.EffectPreviews
            .Where(effect => effect.Type == "attack")
            .Sum(effect => effect.Value ?? 0);
        var block = intent.EffectPreviews
            .Where(effect => effect.Type is "gain_block" or "block")
            .Sum(effect => effect.Value ?? 0);

        return intent.IntentType switch
        {
            EnemyIntentType.Attack => damage > 0 ? damage.ToString() : "攻",
            EnemyIntentType.Defend => block > 0 ? block.ToString() : "防",
            EnemyIntentType.Mixed => damage > 0 && block > 0 ? $"{damage}+{block}" : damage > 0 ? damage.ToString() : block > 0 ? block.ToString() : "压",
            _ => "?"
        };
    }

    private static string IntentDisplayText(EnemyIntentView? intent)
    {
        if (intent is null)
        {
            return "意图 -";
        }

        return intent.IntentType switch
        {
            EnemyIntentType.Attack => $"攻击 {IntentSummary(intent)}",
            EnemyIntentType.Defend => $"防守 {IntentSummary(intent)}",
            EnemyIntentType.Mixed => $"压迫 {IntentSummary(intent)}",
            _ => $"意图 {IntentSummary(intent)}"
        };
    }

    private static string IntentIconAsset(EnemyIntentType? intentType)
    {
        return intentType switch
        {
            EnemyIntentType.Attack => "asset.ui.icon.attack_intent",
            EnemyIntentType.Defend => "asset.ui.icon.defend_intent",
            EnemyIntentType.Mixed => "asset.ui.icon.pressure_mixed_intent",
            _ => "asset.ui.icon.pressure_mixed_intent"
        };
    }

    private static Color IntentAccent(EnemyIntentType? intentType)
    {
        return intentType switch
        {
            EnemyIntentType.Attack => new Color(0.78f, 0.13f, 0.08f, 1f),
            EnemyIntentType.Defend => new Color(0.18f, 0.58f, 0.70f, 1f),
            EnemyIntentType.Mixed => new Color(0.55f, 0.22f, 0.82f, 1f),
            _ => new Color(0.62f, 0.42f, 0.16f, 1f)
        };
    }

    private static Color IntentTextColor(EnemyIntentType? intentType)
    {
        return intentType switch
        {
            EnemyIntentType.Attack => new Color(0.72f, 0.06f, 0.04f),
            EnemyIntentType.Defend => new Color(0.08f, 0.40f, 0.50f),
            EnemyIntentType.Mixed => new Color(0.43f, 0.13f, 0.64f),
            _ => new Color(0.28f, 0.16f, 0.06f)
        };
    }

    private static void AddAt(Control parent, Control child, Vector2 position, Vector2 size)
    {
        child.Position = position;
        child.Size = size;
        child.CustomMinimumSize = size;
        parent.AddChild(child);
    }

    private readonly record struct EnemySpriteLayout(Vector2 SourceSize, float ContentBottom, float Scale)
    {
        public Vector2 DisplaySize => SourceSize * Scale;
    }
}
