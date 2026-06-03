using Godot;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GD.Print($"{ProjectInfo.Name} MVP shell ready.");
    }
}
