using Godot;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleEnemyView : Control
{
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

    private readonly Dictionary<string, Control> enemyNodes = new(StringComparer.Ordinal);

    public event Action<string>? EnemySelected;

    public IReadOnlyDictionary<string, Control> EnemyNodes => enemyNodes;

    public void Render(
        Control root,
        IReadOnlyList<CombatEnemyState> enemies,
        GameContent content,
        string? selectedEnemyInstanceId,
        BattleTargetingOverlay targetingOverlay,
        Func<string, Texture2D?> loadTexture)
    {
        enemyNodes.Clear();

        var enemyLayouts = enemies
            .Select(enemy => (State: enemy, Layout: EnemySpriteLayoutFor(enemy.EnemyId)))
            .ToList();
        var spacing = enemyLayouts.Count <= 1 ? 0 : 28;
        var totalWidth = enemyLayouts.Sum(item => item.Layout.DisplaySize.X) + Math.Max(0, enemyLayouts.Count - 1) * spacing;
        var startX = 1395 - totalWidth / 2f;
        var enemyX = startX;

        foreach (var (enemyState, spriteLayout) in enemyLayouts)
        {
            var enemyPosition = new Vector2(enemyX, StageGroundY - spriteLayout.ContentBottom * spriteLayout.Scale);
            var enemyControl = CreateEnemyButton(enemyState, spriteLayout.DisplaySize, content, selectedEnemyInstanceId, targetingOverlay, loadTexture);
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

    private Control CreateEnemyButton(
        CombatEnemyState enemyState,
        Vector2 displaySize,
        GameContent content,
        string? selectedEnemyInstanceId,
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
        var portrait = CreateImage(enemyView.StandAsset, displaySize, TextureRect.StretchModeEnum.Scale, loadTexture);
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
            var targetIcon = CreateImage("asset.ui.icon.target_selected", new Vector2(46, 46), TextureRect.StretchModeEnum.KeepAspectCentered, loadTexture);
            targetIcon.Position = new Vector2(displaySize.X * 0.5f - 23, displaySize.Y - 66);
            panel.AddChild(targetIcon);
        }

        var dragHighlight = targetingOverlay.CreateEnemyDragHighlight(enemyState.InstanceId);
        dragHighlight.SetAnchorsPreset(LayoutPreset.FullRect);
        dragHighlight.Visible = false;
        panel.AddChild(dragHighlight);

        return panel;
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

    private readonly record struct EnemySpriteLayout(Vector2 SourceSize, float ContentBottom, float Scale)
    {
        public Vector2 DisplaySize => SourceSize * Scale;
    }
}
