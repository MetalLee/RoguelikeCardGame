using Godot;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Shared;

public sealed class SceneScreenHost
{
    private readonly Control parent;
    private readonly GameContent content;
    private readonly Dictionary<string, Texture2D> textureCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Font> fontCache = new(StringComparer.Ordinal);

    public SceneScreenHost(Control parent, GameContent content)
    {
        this.parent = parent;
        this.content = content;
    }

    public Control? ActiveScreen { get; private set; }

    public T ShowScreen<T>(string scenePath)
        where T : Control
    {
        ClearActiveScreen();
        var packedScene = GD.Load<PackedScene>(scenePath)
            ?? throw new InvalidOperationException($"Scene '{scenePath}' could not be loaded.");
        var screen = packedScene.Instantiate<T>();
        if (screen is ComicScreen comicScreen)
        {
            comicScreen.Initialize(content, textureCache, fontCache);
        }

        screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(screen);
        ActiveScreen = screen;
        return screen;
    }

    public void ShowFatalError(Exception ex)
    {
        ClearActiveScreen();
        var label = CreateFatalErrorLabel(ex);
        parent.AddChild(label);
        ActiveScreen = label;
    }

    public void ClearActiveScreen()
    {
        if (ActiveScreen is null)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child is Node node)
                {
                    parent.RemoveChild(node);
                    node.QueueFree();
                }
            }

            return;
        }

        parent.RemoveChild(ActiveScreen);
        ActiveScreen.QueueFree();
        ActiveScreen = null;
    }

    public static Label CreateFatalErrorLabel(Exception ex)
    {
        var label = new Label
        {
            Text = ex.ToString(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.AddThemeColorOverride("font_color", Colors.Red);
        return label;
    }
}
