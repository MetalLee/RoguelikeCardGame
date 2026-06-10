using Godot;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleTargetingOverlay
{
    private readonly Dictionary<string, Control> enemyNodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Control> enemyTargetHighlights = new(StringComparer.Ordinal);
    private PanelContainer? releaseZonePanel;
    private DragArrowOverlay? dragArrow;
    private Control? canvasRoot;

    public void Initialize(Control root)
    {
        canvasRoot = root;
        enemyNodes.Clear();
        enemyTargetHighlights.Clear();

        releaseZonePanel = CreateReleaseZonePanel();
        AddAt(root, releaseZonePanel, new Vector2(646, 392), new Vector2(628, 238));

        dragArrow = new DragArrowOverlay
        {
            Name = "DragArrow",
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 95
        };
        root.AddChild(dragArrow);
    }

    public Control CreateEnemyDragHighlight(string enemyInstanceId)
    {
        var highlight = new Panel
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 30
        };
        highlight.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1.0f, 0.84f, 0.25f, 0.08f),
            BorderColor = new Color(1.0f, 0.83f, 0.24f, 0.95f),
            BorderWidthLeft = 5,
            BorderWidthRight = 5,
            BorderWidthTop = 5,
            BorderWidthBottom = 5,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10
        });
        enemyTargetHighlights[enemyInstanceId] = highlight;
        return highlight;
    }

    public void RegisterEnemy(string enemyInstanceId, Control enemyNode)
    {
        enemyNodes[enemyInstanceId] = enemyNode;
    }

    public string? EnemyUnderMouse(IEnumerable<CombatEnemyState> enemies, Vector2 viewportPoint)
    {
        foreach (var enemy in enemies.Where(enemy => enemy.CurrentHp > 0).Reverse())
        {
            if (enemyNodes.TryGetValue(enemy.InstanceId, out var node) && IsPointOverControl(node, viewportPoint))
            {
                return enemy.InstanceId;
            }
        }

        return null;
    }

    public bool IsPointerOverReleaseZone(Vector2 viewportPoint)
    {
        return releaseZonePanel is not null && IsPointOverControl(releaseZonePanel, viewportPoint);
    }

    public void ShowReleaseZone(bool isHovered, bool canPlay)
    {
        if (releaseZonePanel is null)
        {
            return;
        }

        releaseZonePanel.Visible = true;
        SetReleaseZoneStyle(releaseZonePanel, isHovered, canPlay);
    }

    public void HideReleaseZone()
    {
        if (releaseZonePanel is not null)
        {
            releaseZonePanel.Visible = false;
        }
    }

    public void UpdateEnemyHighlights(string? hoveredEnemy)
    {
        foreach (var (enemyId, highlight) in enemyTargetHighlights)
        {
            highlight.Visible = string.Equals(enemyId, hoveredEnemy, StringComparison.Ordinal);
        }
    }

    public void ShowArrow(Control cardNode, Vector2 viewportMouse, bool isValidTarget)
    {
        if (dragArrow is null || canvasRoot is null)
        {
            return;
        }

        var startInViewport = cardNode.GetGlobalTransformWithCanvas() * new Vector2(cardNode.Size.X * 0.5f, cardNode.Size.Y * 0.18f);
        dragArrow.Start = ToLocal(canvasRoot, startInViewport);
        dragArrow.End = ToLocal(canvasRoot, viewportMouse);
        dragArrow.IsValidTarget = isValidTarget;
        dragArrow.Visible = true;
        dragArrow.QueueRedraw();
    }

    public void HideArrow()
    {
        if (dragArrow is not null)
        {
            dragArrow.Visible = false;
        }
    }

    public void HideDragVisuals()
    {
        HideReleaseZone();
        HideArrow();
        UpdateEnemyHighlights(null);
    }

    private static PanelContainer CreateReleaseZonePanel()
    {
        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = 70
        };
        SetReleaseZoneStyle(panel, isHovered: false, canPlay: true);

        var label = new Label
        {
            Text = "释放到这里",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.AddThemeFontSizeOverride("font_size", 34);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.90f, 0.66f));
        label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.025f, 0.01f, 0.82f));
        label.AddThemeConstantOverride("outline_size", 4);
        panel.AddChild(label);
        return panel;
    }

    private static void SetReleaseZoneStyle(PanelContainer panel, bool isHovered, bool canPlay)
    {
        var border = canPlay
            ? new Color(1.0f, 0.76f, 0.24f, isHovered ? 0.98f : 0.54f)
            : new Color(0.78f, 0.18f, 0.14f, 0.62f);
        var bg = canPlay
            ? new Color(1.0f, 0.78f, 0.24f, isHovered ? 0.20f : 0.08f)
            : new Color(0.65f, 0.08f, 0.06f, 0.10f);
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = isHovered ? 5 : 3,
            BorderWidthRight = isHovered ? 5 : 3,
            BorderWidthTop = isHovered ? 5 : 3,
            BorderWidthBottom = isHovered ? 5 : 3,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8
        });
    }

    private static void AddAt(Control parent, Control child, Vector2 position, Vector2 size)
    {
        child.Position = position;
        child.Size = size;
        child.CustomMinimumSize = size;
        parent.AddChild(child);
    }

    private static bool IsPointOverControl(Control control, Vector2 viewportPoint)
    {
        if (!GodotObject.IsInstanceValid(control) || !control.Visible)
        {
            return false;
        }

        var local = ToLocal(control, viewportPoint);
        return new Rect2(Vector2.Zero, control.Size).HasPoint(local);
    }

    private static Vector2 ToLocal(Control control, Vector2 viewportPoint)
    {
        return control.GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
    }

    private sealed partial class DragArrowOverlay : Control
    {
        private static readonly Color Shadow = new(0.05f, 0.028f, 0.012f, 0.82f);
        private static readonly Color ValidLine = new(1.0f, 0.84f, 0.34f, 0.96f);
        private static readonly Color NeutralLine = new(0.92f, 0.82f, 0.62f, 0.82f);

        public Vector2 Start { get; set; }

        public Vector2 End { get; set; }

        public bool IsValidTarget { get; set; }

        public override void _Draw()
        {
            var vector = End - Start;
            if (vector.LengthSquared() < 16f)
            {
                return;
            }

            var direction = vector.Normalized();
            var normal = new Vector2(-direction.Y, direction.X);
            var lineColor = IsValidTarget ? ValidLine : NeutralLine;
            var end = End - direction * 16f;

            DrawLine(Start + new Vector2(3, 4), end + new Vector2(3, 4), Shadow, 16f, antialiased: true);
            DrawLine(Start, end, lineColor, 9f, antialiased: true);

            var tip = End;
            var left = End - direction * 38f + normal * 22f;
            var right = End - direction * 38f - normal * 22f;
            var shadowPoints = new[] { tip + new Vector2(3, 4), left + new Vector2(3, 4), right + new Vector2(3, 4) };
            var points = new[] { tip, left, right };
            DrawPolygon(shadowPoints, new[] { Shadow, Shadow, Shadow });
            DrawPolygon(points, new[] { lineColor, lineColor, lineColor });
        }
    }
}
