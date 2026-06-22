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

        AddImageAt(root, "asset.character.zu.revolver.battle", new Vector2(315, 360), new Vector2(212, 446), TextureRect.StretchModeEnum.KeepAspectCentered);
        AddAt(root, title, new Vector2(540, 198), new Vector2(840, 130));

        var start = new SketchParallelogramButton("开始游戏", heavyFont);
        start.Pressed += () => StartRequested?.Invoke();
        AddAt(root, start, new Vector2(790, 360), new Vector2(340, 88));
    }
}
