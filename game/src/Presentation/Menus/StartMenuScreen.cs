using Godot;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class StartMenuScreen : ComicScreen
{
    public event Action? StartRequested;

    public void Render()
    {
        var root = CreateCanvas();

        AddImageAt(root, "asset.character.swordsman.battle", new Vector2(70, 285), new Vector2(520, 620), TextureRect.StretchModeEnum.KeepAspectCentered);
        AddLabelAt(root, "连锁终章", new Vector2(710, 150), new Vector2(500, 62), 42, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, "第一版 MVP", new Vector2(790, 216), new Vector2(340, 34), 20, new Color(0.46f, 0.28f, 0.12f), HorizontalAlignment.Center);
        AddLabelAt(root, "6 场固定战斗  /  连锁层数  /  终结牌  /  卡牌包奖励  /  精英遗物", new Vector2(610, 265), new Vector2(700, 42), 18, new Color(0.24f, 0.17f, 0.10f), HorizontalAlignment.Center);

        var start = CreateArtButton("开始 MVP Run", "asset.ui.icon.chain_count", new Vector2(280, 74), GoldLine);
        start.Pressed += () => StartRequested?.Invoke();
        AddAt(root, start, new Vector2(820, 760), new Vector2(280, 74));

        AddLabelAt(root, "先点敌人选择目标，再点手牌出牌。行动牌 +1 连锁；终结牌满足连锁后释放。", new Vector2(610, 850), new Vector2(700, 32), 16, new Color(0.26f, 0.18f, 0.12f), HorizontalAlignment.Center);
    }
}
