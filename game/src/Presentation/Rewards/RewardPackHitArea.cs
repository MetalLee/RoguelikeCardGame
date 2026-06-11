using Godot;

namespace RoguelikeCardGame.Presentation.Rewards;

public partial class RewardPackHitArea : Control
{
    private const float AlphaThreshold = 0.08f;

    private Image? hitImage;
    private Vector2 drawnTextureSize;
    private Vector2 drawnTextureOffset;
    private string rewardPackId = "";
    private Action<string>? activated;

    public void Configure(
        string packId,
        Image image,
        Vector2 drawAreaSize,
        Action<string> onActivated)
    {
        rewardPackId = packId;
        hitImage = image;
        activated = onActivated;
        Size = drawAreaSize;
        CustomMinimumSize = drawAreaSize;
        MouseFilter = MouseFilterEnum.Stop;
        TooltipText = "打开卡牌包";

        var sourceSize = new Vector2(image.GetWidth(), image.GetHeight());
        var scale = Math.Min(drawAreaSize.X / sourceSize.X, drawAreaSize.Y / sourceSize.Y);
        drawnTextureSize = sourceSize * scale;
        drawnTextureOffset = (drawAreaSize - drawnTextureSize) * 0.5f;
    }

    public override bool _HasPoint(Vector2 point)
    {
        if (hitImage is null)
        {
            return false;
        }

        var texturePoint = point - drawnTextureOffset;
        if (texturePoint.X < 0 ||
            texturePoint.Y < 0 ||
            texturePoint.X >= drawnTextureSize.X ||
            texturePoint.Y >= drawnTextureSize.Y)
        {
            return false;
        }

        var x = Math.Clamp((int)(texturePoint.X / drawnTextureSize.X * hitImage.GetWidth()), 0, hitImage.GetWidth() - 1);
        var y = Math.Clamp((int)(texturePoint.Y / drawnTextureSize.Y * hitImage.GetHeight()), 0, hitImage.GetHeight() - 1);
        return hitImage.GetPixel(x, y).A > AlphaThreshold;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            return;
        }

        activated?.Invoke(rewardPackId);
        AcceptEvent();
    }
}
