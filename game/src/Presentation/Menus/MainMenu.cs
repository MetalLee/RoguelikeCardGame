using Godot;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Flow;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class MainMenu : Control
{
    private MvpRunFlowController? flowController;
    private SceneScreenHost? screenHost;

    public override void _Ready()
    {
        try
        {
            var content = GameContent.LoadFromProject();
            screenHost = new SceneScreenHost(this, content);
            flowController = new MvpRunFlowController(content, screenHost);
            flowController.ShowStartMenu();
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
        }
    }

    private void ShowFatalError(Exception ex)
    {
        if (screenHost is not null)
        {
            screenHost.ShowFatalError(ex);
            return;
        }

        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                RemoveChild(node);
                node.QueueFree();
            }
        }

        AddChild(SceneScreenHost.CreateFatalErrorLabel(ex));
    }
}
