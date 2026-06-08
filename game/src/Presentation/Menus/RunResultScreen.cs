using Godot;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class RunResultScreen : ComicScreen
{
    public event Action? RestartRequested;

    public void RenderRunResult(RunState? run)
    {
        var root = CreateCanvas();
        var cleared = run?.Status == RunStatus.Cleared;
        var failed = run?.Status == RunStatus.Failed;

        AddImageAt(root, cleared ? "asset.vfx.finisher_release_shockwave" : "asset.vfx.enemy_hit_comic_burst", new Vector2(650, 210), new Vector2(620, 260), TextureRect.StretchModeEnum.KeepAspectCentered);
        AddLabelAt(root, cleared ? "MVP 通关" : failed ? "Run 失败" : "Run 结束", new Vector2(710, 500), new Vector2(500, 62), 40, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddLabelAt(root, cleared
            ? "Boss 已击败。当前 MVP 闭环完成。"
            : "生命归零，本次 MVP Run 结束。", new Vector2(610, 575), new Vector2(700, 34), 20, new Color(0.30f, 0.20f, 0.12f), HorizontalAlignment.Center);

        if (run is not null)
        {
            AddLabelAt(root, $"最终卡组数量：{run.MasterDeck.Count}    遗物：{run.RelicIds.Count}", new Vector2(710, 630), new Vector2(500, 30), 18, new Color(0.30f, 0.20f, 0.12f), HorizontalAlignment.Center);
        }

        var restart = CreateArtButton("重新开始", "asset.ui.icon.end_turn", new Vector2(220, 62), GoldLine);
        restart.Pressed += () => RestartRequested?.Invoke();
        AddAt(root, restart, new Vector2(850, 750), new Vector2(220, 62));
    }

    public void RenderFatalError(Exception ex)
    {
        var root = CreateCanvas();
        AddLabelAt(root, "启动失败", new Vector2(710, 160), new Vector2(500, 60), 36, new Color(0.18f, 0.12f, 0.07f), HorizontalAlignment.Center);
        AddAt(root, CreateMessagePanel(ex.ToString()), new Vector2(360, 260), new Vector2(1200, 520));
    }
}
