using Godot;

namespace RoguelikeCardGame.Presentation.Battle;

internal sealed partial class ThoughtBubblePanel : Control
{
    private static readonly Color Shadow = new(0.0f, 0.0f, 0.0f, 0.16f);
    private static readonly Color Paper = new(1.0f, 1.0f, 1.0f, 1.0f);
    private static readonly Color Ink = new(0.035f, 0.031f, 0.028f, 0.94f);

    public string ThoughtText { get; init; } = "";

    public Font? ThoughtFont { get; init; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AddChild(CreateLabel());
    }

    public override void _Draw()
    {
        var mainCenter = new Vector2(Size.X * 0.58f, Size.Y * 0.44f);
        var mainRadius = new Vector2(Size.X * 0.38f, Size.Y * 0.30f);

        DrawBubble(mainCenter + new Vector2(9, 11), mainRadius, Shadow, 0.0f, fillOnly: true);
        DrawBubble(mainCenter, mainRadius, Paper, 0.0f);

        DrawBubble(new Vector2(Size.X * 0.13f, Size.Y * 0.79f), new Vector2(17, 11), Paper, -0.15f);
        DrawBubble(new Vector2(Size.X * 0.22f, Size.Y * 0.69f), new Vector2(26, 17), Paper, 0.12f);
    }

    private Label CreateLabel()
    {
        var label = new Label
        {
            Text = ThoughtText,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.Position = new Vector2(Size.X * 0.27f, Size.Y * 0.17f);
        label.Size = new Vector2(Size.X * 0.62f, Size.Y * 0.46f);
        label.CustomMinimumSize = label.Size;
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", Ink);
        label.AddThemeColorOverride("font_outline_color", new Color(1.0f, 1.0f, 0.96f, 0.72f));
        label.AddThemeConstantOverride("outline_size", 3);
        if (ThoughtFont is not null)
        {
            label.AddThemeFontOverride("font", ThoughtFont);
        }

        return label;
    }

    private void DrawBubble(Vector2 center, Vector2 radius, Color fill, float wobble, bool fillOnly = false)
    {
        var points = EllipsePoints(center, radius, wobble);
        DrawPolygon(points, Enumerable.Repeat(fill, points.Length).ToArray());
        if (fillOnly)
        {
            return;
        }

        DrawSketchLoop(points, Ink, 2.2f, Vector2.Zero);
        DrawSketchLoop(points, new Color(Ink.R, Ink.G, Ink.B, 0.38f), 0.75f, new Vector2(1.2f, -0.8f));
    }

    private static Vector2[] EllipsePoints(Vector2 center, Vector2 radius, float wobble)
    {
        const int count = 64;
        var points = new Vector2[count];
        for (var index = 0; index < count; index++)
        {
            var t = index / (float)count * MathF.PI * 2.0f;
            var localRadius = 1.0f + MathF.Sin(t * 3.0f + wobble) * 0.018f + MathF.Cos(t * 5.0f - wobble) * 0.012f;
            points[index] = center + new Vector2(MathF.Cos(t) * radius.X * localRadius, MathF.Sin(t) * radius.Y * localRadius);
        }

        return points;
    }

    private void DrawSketchLoop(IReadOnlyList<Vector2> points, Color color, float width, Vector2 offset)
    {
        for (var index = 0; index < points.Count; index++)
        {
            var from = points[index] + offset;
            var to = points[(index + 1) % points.Count] + offset;
            DrawLine(from, to, color, width, antialiased: true);
        }
    }
}
