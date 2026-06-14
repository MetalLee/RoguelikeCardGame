using Godot;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class WeaponSelectionScreen : ComicScreen
{
    public event Action<string, string>? WeaponsConfirmed;
    public event Action? BackRequested;

    private string? selectedMainWeaponId;

    public void Render()
    {
        var content = RequireContent();
        selectedMainWeaponId ??= content.WeaponsById.Keys.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();

        var root = CreateCanvas();
        AddLabelAt(root, "选择主手武器", new Vector2(610, 110), new Vector2(700, 58), 40, new Color(0.16f, 0.10f, 0.06f), HorizontalAlignment.Center);
        AddLabelAt(root, "主手稍后选择 6 张起始牌；副手选择 4 张。两把武器都可以作为主手。", new Vector2(520, 176), new Vector2(880, 36), 18, new Color(0.28f, 0.18f, 0.10f), HorizontalAlignment.Center);

        var weaponIds = content.WeaponsById.Values
            .Where(weapon => weapon.MainHandAllowed && weapon.OffHandAllowed)
            .Select(weapon => weapon.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        for (var index = 0; index < weaponIds.Count; index++)
        {
            var weaponId = weaponIds[index];
            var isMain = string.Equals(selectedMainWeaponId, weaponId, StringComparison.Ordinal);
            var panel = CreateWeaponPanel(content, weaponId, isMain);
            AddAt(root, panel, new Vector2(410 + index * 560, 300), new Vector2(500, 330));
        }

        var offHandWeaponId = weaponIds.FirstOrDefault(id => !string.Equals(id, selectedMainWeaponId, StringComparison.Ordinal));
        var summary = selectedMainWeaponId is null || offHandWeaponId is null
            ? "请选择主手武器"
            : $"主手：{content.WeaponName(selectedMainWeaponId)}    副手：{content.WeaponName(offHandWeaponId)}";
        AddLabelAt(root, summary, new Vector2(560, 680), new Vector2(800, 40), 22, new Color(0.18f, 0.10f, 0.05f), HorizontalAlignment.Center);

        var back = CreateArtButton("返回", "asset.ui.icon.discard_pile", new Vector2(190, 66), GoldLine);
        back.Pressed += () => BackRequested?.Invoke();
        AddAt(root, back, new Vector2(650, 780), new Vector2(190, 66));

        var confirm = CreateArtButton("确认武器", "asset.ui.icon.playable_highlight", new Vector2(240, 66), GoldLine);
        confirm.Disabled = selectedMainWeaponId is null || offHandWeaponId is null;
        confirm.Pressed += () =>
        {
            if (selectedMainWeaponId is not null && offHandWeaponId is not null)
            {
                WeaponsConfirmed?.Invoke(selectedMainWeaponId, offHandWeaponId);
            }
        };
        AddAt(root, confirm, new Vector2(910, 780), new Vector2(240, 66));
    }

    private Control CreateWeaponPanel(GameContent content, string weaponId, bool isMain)
    {
        var panel = CreateFramedPanel(new Vector2(500, 330), isMain ? GoldLine : new Color(0.30f, 0.22f, 0.16f));
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 12);
        panel.AddChild(stack);

        var title = new Label
        {
            Text = content.WeaponName(weaponId),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", new Color(1.0f, 0.84f, 0.48f));
        stack.AddChild(title);

        var role = new Label
        {
            Text = isMain ? "当前主手" : "当前副手",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        role.AddThemeFontSizeOverride("font_size", 18);
        role.AddThemeColorOverride("font_color", isMain ? new Color(1.0f, 0.70f, 0.26f) : new Color(0.78f, 0.68f, 0.56f));
        stack.AddChild(role);

        var description = CreateBodyLabel(content.WeaponDescription(weaponId));
        description.AddThemeFontSizeOverride("font_size", 17);
        description.AddThemeColorOverride("font_color", new Color(0.96f, 0.82f, 0.62f));
        stack.AddChild(description);

        var count = content.ExpandedStartingCardIdsForWeapon(weaponId).Count;
        var pool = CreateSmallLabel($"起始池：{count} 张；奖励池按武器与稀有度生成三选一。");
        pool.AddThemeColorOverride("font_color", new Color(0.88f, 0.76f, 0.58f));
        stack.AddChild(pool);

        var choose = CreateArtButton(isMain ? "已选择" : "设为主手", "asset.ui.icon.deck_library", new Vector2(210, 52), isMain ? GoldLine : CyanLine);
        choose.Disabled = isMain;
        choose.Pressed += () =>
        {
            selectedMainWeaponId = weaponId;
            Render();
        };
        stack.AddChild(choose);
        return panel;
    }
}
