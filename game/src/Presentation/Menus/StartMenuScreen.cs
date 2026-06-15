using Godot;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class StartMenuScreen : ComicScreen
{
    public event Action? StartRequested;

    public void Render()
    {
        var root = CreateCanvas();
        var heavyFont = LoadFont("asset.font.source_han_sans_sc.heavy");

        var title = new Label
        {
            Text = "剑与黑塔",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Off
        };
        title.AddThemeFontSizeOverride("font_size", 96);
        title.AddThemeColorOverride("font_color", new Color(0.04f, 0.035f, 0.03f));
        if (heavyFont is not null)
        {
            title.AddThemeFontOverride("font", heavyFont);
        }

        AddImageAt(root, "asset.character.zu.revolver.battle", new Vector2(315, 560), new Vector2(212, 446), TextureRect.StretchModeEnum.KeepAspectCentered);
        AddAt(root, title, new Vector2(540, 198), new Vector2(840, 130));

        var start = new SketchParallelogramButton("开始游戏", heavyFont);
        start.Pressed += () => StartRequested?.Invoke();
        AddAt(root, start, new Vector2(790, 360), new Vector2(340, 88));
    }
}

internal sealed partial class SketchParallelogramButton : Button
{
    private readonly Label label;
    private readonly Color ink = new(0.035f, 0.03f, 0.025f, 1.0f);

    public SketchParallelogramButton(string text, Font? font)
    {
        Text = string.Empty;
        Flat = true;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        CustomMinimumSize = new Vector2(340, 88);
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        ButtonDown += QueueRedraw;
        ButtonUp += QueueRedraw;

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
        label.AddThemeFontSizeOverride("font_size", 30);
        label.AddThemeColorOverride("font_color", ink);
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        AddChild(label);
    }

    public override void _Draw()
    {
        var skew = 28f;
        var points = new[]
        {
            new Vector2(skew, 0),
            new Vector2(Size.X, 0),
            new Vector2(Size.X - skew, Size.Y),
            new Vector2(0, Size.Y)
        };
        var fill = ButtonPressed
            ? new Color(0.90f, 0.90f, 0.88f, 1.0f)
            : IsHovered()
                ? new Color(0.98f, 0.98f, 0.96f, 1.0f)
                : Colors.White;

        DrawPolygon(points, new[] { fill, fill, fill, fill });
        DrawSketchLine(points[0], points[1], 0f);
        DrawSketchLine(points[1], points[2], 1f);
        DrawSketchLine(points[2], points[3], -1f);
        DrawSketchLine(points[3], points[0], 0.5f);
    }


    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            QueueRedraw();
        }
    }

    private void DrawSketchLine(Vector2 from, Vector2 to, float offset)
    {
        var normal = (to - from).Orthogonal().Normalized();
        DrawLine(from + normal * offset, to + normal * offset, ink, 3.2f, true);
        DrawLine(from - normal * (offset + 1.4f), to - normal * (offset + 0.2f), new Color(0.02f, 0.018f, 0.015f, 0.62f), 1.2f, true);
    }
}
