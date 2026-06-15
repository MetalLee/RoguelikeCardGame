using Godot;

namespace RoguelikeCardGame.Presentation.Shared;

public sealed partial class SketchParallelogramButton : Button
{
    private readonly Label label;
    private readonly Font? font;
    private readonly int fontSize;
    private readonly Color ink = new(0.035f, 0.03f, 0.025f, 1.0f);
    private readonly Color paper = new(0.98f, 0.98f, 0.96f, 1.0f);

    public SketchParallelogramButton(string text, Font? font = null, int fontSize = 28)
    {
        this.font = font;
        this.fontSize = fontSize;

        Text = string.Empty;
        Flat = true;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseEntered += RefreshVisualState;
        MouseExited += RefreshVisualState;
        ButtonDown += RefreshVisualState;
        ButtonUp += RefreshVisualState;

        var empty = new StyleBoxEmpty();
        AddThemeStyleboxOverride("normal", empty);
        AddThemeStyleboxOverride("hover", empty);
        AddThemeStyleboxOverride("pressed", empty);
        AddThemeStyleboxOverride("focus", empty);
        AddThemeStyleboxOverride("disabled", empty);

        label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(label);
        RefreshVisualState();
    }

    public override void _Ready()
    {
        RefreshVisualState();
    }

    public override void _Draw()
    {
        var skew = Math.Min(Size.X * 0.14f, 34f);
        var points = new[]
        {
            new Vector2(skew, 0),
            new Vector2(Size.X, 0),
            new Vector2(Size.X - skew, Size.Y),
            new Vector2(0, Size.Y)
        };

        var isLight = IsHovered() && !Disabled;
        var fill = Disabled
            ? new Color(0.28f, 0.28f, 0.27f, 0.72f)
            : isLight ? paper : ink;
        var line = Disabled
            ? new Color(0.72f, 0.72f, 0.70f, 0.84f)
            : isLight ? ink : paper;

        if (ButtonPressed && !Disabled)
        {
            fill = isLight ? new Color(0.88f, 0.88f, 0.86f, 1.0f) : new Color(0.12f, 0.11f, 0.10f, 1.0f);
        }

        DrawDropShadow(points, isLight);
        DrawPolygon(points, [fill, fill, fill, fill]);
        DrawSketchLine(points[0], points[1], 0f, line);
        DrawSketchLine(points[1], points[2], 1f, line);
        DrawSketchLine(points[2], points[3], -1f, line);
        DrawSketchLine(points[3], points[0], 0.5f, line);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            RefreshVisualState();
        }
    }

    public void RefreshVisualState()
    {
        var isLight = IsHovered() && !Disabled;
        var textColor = Disabled
            ? new Color(0.78f, 0.78f, 0.76f, 0.92f)
            : isLight ? ink : paper;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", textColor);
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        QueueRedraw();
    }

    private void DrawDropShadow(Vector2[] points, bool isHovered)
    {
        var shadow = new Color(0.24f, 0.24f, 0.26f, Disabled ? 0.22f : 0.42f);
        var offset = isHovered && !Disabled ? new Vector2(17, 14) : new Vector2(11, 10);
        DrawOffsetPolygon(points, offset, shadow);
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

    private void DrawSketchLine(Vector2 from, Vector2 to, float offset, Color color)
    {
        var normal = (to - from).Orthogonal().Normalized();
        DrawLine(from + normal * offset, to + normal * offset, color, 3.2f, true);
        DrawLine(from - normal * (offset + 1.4f), to - normal * (offset + 0.2f), new Color(color.R, color.G, color.B, 0.62f), 1.2f, true);
    }
}
