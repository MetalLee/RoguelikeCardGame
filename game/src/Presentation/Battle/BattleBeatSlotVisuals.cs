using Godot;

namespace RoguelikeCardGame.Presentation.Battle;

internal sealed partial class BeatDiamondSlotView : Control
{
    private static readonly Color Ink = new(0f, 0f, 0f, 1f);
    private static readonly Color Paper = new(1f, 1f, 1f, 1f);
    private static readonly Color MutedPaper = new(0.94f, 0.94f, 0.92f, 1f);
    private static readonly Color SlotShadow = new(0f, 0f, 0f, 0.18f);

    private Label? label;
    private string slotText = "";

    public bool Filled { get; set; }

    public bool Emphasized { get; set; }

    public Font? SlotFont { get; set; }

    public int FontSize { get; set; } = 28;

    public string SlotText
    {
        get => slotText;
        set
        {
            slotText = value;
            ApplyLabel();
        }
    }

    public override void _Ready()
    {
        label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(label);
        ApplyLabel();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var points = DiamondPoints(Vector2.Zero);
        var shadowPoints = DiamondPoints(new Vector2(3, 4));
        DrawPolygon(shadowPoints, [SlotShadow, SlotShadow, SlotShadow, SlotShadow]);
        DrawPolygon(points, Filled
            ? [Ink, Ink, Ink, Ink]
            : [Paper, MutedPaper, Paper, MutedPaper]);
        DrawPolyline([points[0], points[1], points[2], points[3], points[0]], Ink, Emphasized ? 5f : 4f, antialiased: true);
    }

    private Vector2[] DiamondPoints(Vector2 offset)
    {
        return
        [
            new Vector2(Size.X * 0.5f, 0f) + offset,
            new Vector2(Size.X, Size.Y * 0.5f) + offset,
            new Vector2(Size.X * 0.5f, Size.Y) + offset,
            new Vector2(0f, Size.Y * 0.5f) + offset
        ];
    }

    private void ApplyLabel()
    {
        if (label is null)
        {
            return;
        }

        label.Text = slotText;
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.AddThemeColorOverride("font_color", Filled ? Paper : Ink);
        label.AddThemeColorOverride("font_outline_color", Filled ? Ink : Paper);
        label.AddThemeConstantOverride("outline_size", Filled ? 2 : 1);
        if (SlotFont is not null)
        {
            label.AddThemeFontOverride("font", SlotFont);
        }
    }
}

internal sealed partial class BeatTargetConnectorOverlay : Control
{
    private static readonly Color Ink = new(0f, 0f, 0f, 1f);
    private static readonly Color Paper = new(1f, 1f, 1f, 1f);

    public IReadOnlyList<BeatTargetConnector> Connections { get; init; } = [];

    public override void _Draw()
    {
        for (var index = 0; index < Connections.Count; index++)
        {
            DrawConnector(Connections[index], index);
        }
    }

    private void DrawConnector(BeatTargetConnector connector, int index)
    {
        var vector = connector.End - connector.Start;
        if (vector.LengthSquared() < 100f)
        {
            return;
        }

        const float ArrowLength = 34f;
        var initialBaseCenters = BeatSlotPresentationGeometry.ArrowBaseCenter(
            connector.Start.X,
            connector.Start.Y,
            connector.End.X,
            connector.End.Y,
            ArrowLength);
        var initialStart = new Vector2(initialBaseCenters.SourceBaseX, initialBaseCenters.SourceBaseY);
        var initialEnd = new Vector2(initialBaseCenters.TargetBaseX, initialBaseCenters.TargetBaseY);
        var initialPoints = BuildCurvePoints(initialStart, initialEnd, index);
        var sourceAwayTangent = (initialPoints[Math.Min(2, initialPoints.Length - 1)] - initialPoints[0]).Normalized();
        var targetTowardTangent = (initialPoints[^1] - initialPoints[Math.Max(0, initialPoints.Length - 3)]).Normalized();
        var sourceTail = BeatSlotPresentationGeometry.ArrowTailFromTip(
            connector.Start.X,
            connector.Start.Y,
            -sourceAwayTangent.X,
            -sourceAwayTangent.Y,
            ArrowLength);
        var targetTail = BeatSlotPresentationGeometry.ArrowTailFromTip(
            connector.End.X,
            connector.End.Y,
            targetTowardTangent.X,
            targetTowardTangent.Y,
            ArrowLength);
        var lineStart = new Vector2(sourceTail.X, sourceTail.Y);
        var lineEnd = new Vector2(targetTail.X, targetTail.Y);
        var points = BuildCurvePoints(lineStart, lineEnd, index);
        DrawCurve(points, Ink, 14f);
        DrawCurve(points, Paper, 7f);
        DrawDoubleArrowHeadFromTip(connector.Start, lineStart);
        DrawDoubleArrowHeadFromTip(connector.End, lineEnd);
    }

    private static Vector2[] BuildCurvePoints(Vector2 start, Vector2 end, int index)
    {
        const int Segments = 28;
        var vector = end - start;
        var distance = vector.Length();
        var lift = Math.Clamp(distance * 0.16f, 54f, 172f) + index * 24f;
        var control = new Vector2(
            (start.X + end.X) * 0.5f,
            BeatSlotPresentationGeometry.RaisedConnectorControlY(start.Y, end.Y, lift));
        var points = new Vector2[Segments + 1];
        for (var i = 0; i <= Segments; i++)
        {
            var t = i / (float)Segments;
            points[i] = QuadraticBezier(start, control, end, t);
        }

        return points;
    }

    private void DrawCurve(IReadOnlyList<Vector2> points, Color color, float width)
    {
        for (var i = 0; i < points.Count - 1; i++)
        {
            DrawLine(points[i], points[i + 1], color, width, antialiased: true);
        }
    }

    private void DrawDoubleArrowHeadFromTip(Vector2 tip, Vector2 baseCenter)
    {
        var axis = tip - baseCenter;
        if (axis.LengthSquared() <= 0f)
        {
            return;
        }

        var tangent = axis.Normalized();
        var normal = new Vector2(-tangent.Y, tangent.X);
        var outer = new[]
        {
            tip,
            baseCenter + normal * 20f,
            baseCenter - normal * 20f
        };
        var innerTip = tip - tangent * 5f;
        var inner = new[]
        {
            innerTip,
            baseCenter + tangent * 10f + normal * 10f,
            baseCenter + tangent * 10f - normal * 10f
        };
        DrawPolygon(outer, [Ink, Ink, Ink]);
        DrawPolygon(inner, [Paper, Paper, Paper]);
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        var inverse = 1f - t;
        return start * (inverse * inverse) + control * (2f * inverse * t) + end * (t * t);
    }
}

internal readonly record struct BeatTargetConnector(Vector2 Start, Vector2 End);
