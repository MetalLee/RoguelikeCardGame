using Godot;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BattleTargetingOverlay
{
    private static readonly Vector2 NonTargetReleaseZonePosition = new(0, 0);
    private static readonly Vector2 NonTargetReleaseZoneSize = new(1920, 720);

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
        AddAt(root, releaseZonePanel, NonTargetReleaseZonePosition, NonTargetReleaseZoneSize);

        dragArrow = new DragArrowOverlay
        {
            Name = "DragArrow",
            Size = new Vector2(1920, 1080),
            CustomMinimumSize = new Vector2(1920, 1080),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 220,
            ZAsRelative = false
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
        return releaseZonePanel is not null && IsPointInsideControl(releaseZonePanel, viewportPoint, requireVisible: false);
    }

    public void ShowReleaseZone(bool isHovered, bool canPlay)
    {
        HideReleaseZone();
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
        foreach (var (_, highlight) in enemyTargetHighlights)
        {
            highlight.Visible = false;
        }
    }

    public void ShowArrow(Control cardNode, Vector2 viewportMouse, bool isValidTarget)
    {
        if (dragArrow is null || canvasRoot is null)
        {
            return;
        }

        var startInViewport = cardNode.GetGlobalTransformWithCanvas() * new Vector2(cardNode.Size.X * 0.5f, cardNode.Size.Y * 0.18f);
        ShowArrowFromViewport(startInViewport, viewportMouse, isValidTarget);
    }

    public void ShowArrowFromViewport(Vector2 viewportStart, Vector2 viewportMouse, bool isValidTarget)
    {
        if (dragArrow is null || canvasRoot is null)
        {
            return;
        }

        dragArrow.Start = ToLocal(canvasRoot, viewportStart);
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
        return IsPointInsideControl(control, viewportPoint, requireVisible: true);
    }

    private static bool IsPointInsideControl(Control control, Vector2 viewportPoint, bool requireVisible)
    {
        if (!GodotObject.IsInstanceValid(control) || (requireVisible && !control.Visible))
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
            if (vector.LengthSquared() < 576f)
            {
                return;
            }

            var distance = vector.Length();
            var direction = vector.Normalized();
            var lineColor = IsValidTarget ? ValidLine : NeutralLine;
            var curveEnd = End - direction * 18f;
            var curvePoints = BuildCurvePoints(Start, curveEnd, direction, distance);
            var shadowOffset = new Vector2(3, 4);

            DrawCurve(curvePoints, shadowOffset, Shadow, 16f);
            DrawCurve(curvePoints, Vector2.Zero, lineColor, 9f);

            var tangent = (curvePoints[^1] - curvePoints[^3]).Normalized();
            var normal = new Vector2(-tangent.Y, tangent.X);
            var tip = End;
            var left = End - tangent * 38f + normal * 22f;
            var right = End - tangent * 38f - normal * 22f;
            var shadowPoints = new[] { tip + shadowOffset, left + shadowOffset, right + shadowOffset };
            var points = new[] { tip, left, right };
            DrawPolygon(shadowPoints, new[] { Shadow, Shadow, Shadow });
            DrawPolygon(points, new[] { lineColor, lineColor, lineColor });
        }

        private static Vector2[] BuildCurvePoints(Vector2 start, Vector2 end, Vector2 direction, float distance)
        {
            const int Segments = 24;
            var normal = new Vector2(-direction.Y, direction.X);
            var arc = Math.Clamp(distance * 0.18f, 36f, 118f);
            var lift = Math.Clamp(distance * 0.06f, 14f, 48f);
            var side = direction.X >= 0f ? -1f : 1f;
            var control = (start + end) * 0.5f + normal * arc * side + new Vector2(0, -lift);
            var points = new Vector2[Segments + 1];
            for (var i = 0; i <= Segments; i++)
            {
                var t = i / (float)Segments;
                points[i] = QuadraticBezier(start, control, end, t);
            }

            return points;
        }

        private void DrawCurve(IReadOnlyList<Vector2> points, Vector2 offset, Color color, float width)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                DrawLine(points[i] + offset, points[i + 1] + offset, color, width, antialiased: true);
            }
        }

        private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var inverse = 1f - t;
            return start * (inverse * inverse) + control * (2f * inverse * t) + end * (t * t);
        }
    }
}
