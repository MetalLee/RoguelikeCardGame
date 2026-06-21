using Godot;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleEnemyView : Control
{
    private const float StageGroundY = 980f;
    private const float EnemyHealthBarHeight = 12f;

    private static readonly Dictionary<string, EnemySpriteLayout> EnemySpriteLayouts = new(StringComparer.Ordinal)
    {
        ["enemy.training_dummy"] = new(new Vector2(360, 360), 552f, 1.0f),
        ["enemy.splitling"] = new(new Vector2(360, 360), 552f, 1.0f),
        ["enemy.black_tower_warden"] = new(new Vector2(700, 560), 552f, 1.0f)
    };

    private readonly Dictionary<string, Control> enemyNodes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, Control> EnemyNodes => enemyNodes;

    public void Render(
        Control root,
        CombatState combat,
        GameContent content,
        BattleTargetingOverlay targetingOverlay,
        Func<string, Texture2D?> loadTexture)
    {
        enemyNodes.Clear();

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
            var enemyControl = CreateEnemyPanel(enemyState, spriteLayout.DisplaySize, content, targetingOverlay, loadTexture);
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
        Vector2 displaySize,
        GameContent content,
        BattleTargetingOverlay targetingOverlay,
        Func<string, Texture2D?> loadTexture)
    {
        var panel = new Control
        {
            CustomMinimumSize = displaySize,
            Size = displaySize,
            ClipContents = false,
            MouseFilter = MouseFilterEnum.Pass
        };

        var enemyView = content.EnemyViewsById[enemyState.EnemyId];
        var portrait = CreateImage(
            EnemyInitialTexture(enemyView, loadTexture),
            displaySize,
            TextureRect.StretchModeEnum.Scale);
        portrait.Position = new Vector2(0, 0);
        portrait.Size = displaySize;
        portrait.Modulate = enemyState.CurrentHp <= 0 ? new Color(0.32f, 0.32f, 0.32f, 0.75f) : Colors.White;
        panel.AddChild(portrait);
        AddEnemyAnimationTimer(panel, portrait, enemyView.AnimationSheet, enemyState.CurrentHp > 0, loadTexture);

        if (enemyState.CurrentHp > 0)
        {
            panel.AddChild(CreateEnemyHealthBar(enemyState, displaySize));
        }

        var dragHighlight = targetingOverlay.CreateEnemyDragHighlight(enemyState.InstanceId);
        dragHighlight.SetAnchorsPreset(LayoutPreset.FullRect);
        dragHighlight.Visible = false;
        panel.AddChild(dragHighlight);

        return panel;
    }

    private static Control CreateEnemyHealthBar(CombatEnemyState enemyState, Vector2 displaySize)
    {
        var width = Math.Clamp(displaySize.X * 0.44f, 118f, 230f);
        var size = new Vector2(width, EnemyHealthBarHeight);
        return new EnemyHealthStrip
        {
            CurrentHp = enemyState.CurrentHp,
            MaxHp = enemyState.MaxHp,
            Size = size,
            CustomMinimumSize = size,
            Position = new Vector2((displaySize.X - width) * 0.5f, -22f),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 36
        };
    }

    private static Texture2D? EnemyInitialTexture(
        EnemyViewDefinition enemyView,
        Func<string, Texture2D?> loadTexture)
    {
        return enemyView.AnimationSheet is null
            ? loadTexture(enemyView.StandAsset)
            : CreateAnimationFrameTexture(enemyView.AnimationSheet, frameIndex: 0, loadTexture);
    }

    private static void AddEnemyAnimationTimer(
        Control panel,
        TextureRect portrait,
        AnimationSheetDefinition? animationSheet,
        bool isAlive,
        Func<string, Texture2D?> loadTexture)
    {
        if (!isAlive || animationSheet is null || animationSheet.FrameCount <= 1)
        {
            return;
        }

        var frames = Enumerable
            .Range(0, animationSheet.FrameCount)
            .Select(index => CreateAnimationFrameTexture(animationSheet, index, loadTexture))
            .ToArray();
        var frameIndex = 0;
        var timer = new Godot.Timer
        {
            WaitTime = animationSheet.FrameSeconds,
            OneShot = false,
            Autostart = true
        };
        timer.Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(portrait))
            {
                return;
            }

            frameIndex = (frameIndex + 1) % frames.Length;
            portrait.Texture = frames[frameIndex];
        };
        panel.AddChild(timer);
    }

    private static Texture2D? CreateAnimationFrameTexture(
        AnimationSheetDefinition animationSheet,
        int frameIndex,
        Func<string, Texture2D?> loadTexture)
    {
        var atlas = loadTexture(animationSheet.SheetAsset);
        if (atlas is null)
        {
            return null;
        }

        var column = frameIndex % animationSheet.Columns;
        var row = frameIndex / animationSheet.Columns;
        return new AtlasTexture
        {
            Atlas = atlas,
            Region = new Rect2(
                column * animationSheet.FrameWidth,
                row * animationSheet.FrameHeight,
                animationSheet.FrameWidth,
                animationSheet.FrameHeight)
        };
    }

    private static TextureRect CreateImage(
        Texture2D? texture,
        Vector2 minSize,
        TextureRect.StretchModeEnum stretchMode)
    {
        return new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = minSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = MouseFilterEnum.Ignore
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

internal sealed partial class EnemyHealthStrip : Control
{
    private const float BorderWidth = 2.5f;
    private const float FillInset = 2f;

    private static readonly Color Ink = new(0f, 0f, 0f, 1f);
    private static readonly Color Paper = new(1f, 1f, 1f, 1f);

    public int CurrentHp { get; init; }

    public int MaxHp { get; init; }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, Paper, filled: true);

        var fillRect = new Rect2(
            new Vector2(FillInset, FillInset),
            new Vector2(Math.Max(0f, Size.X - FillInset * 2f), Math.Max(0f, Size.Y - FillInset * 2f)));
        var healthRatio = MaxHp <= 0
            ? 0f
            : Math.Clamp((float)CurrentHp / MaxHp, 0f, 1f);
        if (healthRatio > 0f && fillRect.Size.X > 0f && fillRect.Size.Y > 0f)
        {
            DrawRect(
                new Rect2(fillRect.Position, new Vector2(fillRect.Size.X * healthRatio, fillRect.Size.Y)),
                Ink,
                filled: true);
        }

        DrawRect(rect, Ink, filled: false, width: BorderWidth);
    }
}
