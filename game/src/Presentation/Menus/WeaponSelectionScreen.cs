using Godot;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Cards;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class WeaponSelectionScreen : ComicScreen
{
    public event Action<string, string>? WeaponsConfirmed;
    public event Action? BackRequested;

    private string? selectedMainWeaponId;
    private string? selectedOffHandWeaponId;

    private static readonly IReadOnlyList<string?> WeaponSlots =
    [
        "weapon.revolver_sword",
        "weapon.mechanical_arm",
        null,
        null
    ];

    public void Render()
    {
        var content = RequireContent();
        var root = CreateCanvas();

        AddWeaponSection(
            root,
            content,
            title: "主武器选择",
            selectedWeaponId: selectedMainWeaponId,
            blockedWeaponId: selectedOffHandWeaponId,
            position: new Vector2(56, 126),
            onSelected: weaponId =>
            {
                selectedMainWeaponId = weaponId;
                Render();
            });

        AddWeaponSection(
            root,
            content,
            title: "副武器选择",
            selectedWeaponId: selectedOffHandWeaponId,
            blockedWeaponId: selectedMainWeaponId,
            position: new Vector2(56, 476),
            onSelected: weaponId =>
            {
                selectedOffHandWeaponId = weaponId;
                Render();
            });

        AddStarterDeckPreview(root, content);

        var heavyFont = LoadFont("asset.font.source_han_sans_sc.heavy");
        var back = new SketchParallelogramButton("返回", heavyFont, 24);
        back.Pressed += () => BackRequested?.Invoke();
        AddAt(root, back, new Vector2(720, 930), new Vector2(190, 64));

        var confirm = new SketchParallelogramButton("出发", heavyFont, 24);
        confirm.Disabled = selectedMainWeaponId is null || selectedOffHandWeaponId is null;
        confirm.RefreshVisualState();
        confirm.Pressed += () =>
        {
            if (selectedMainWeaponId is not null && selectedOffHandWeaponId is not null)
            {
                WeaponsConfirmed?.Invoke(selectedMainWeaponId, selectedOffHandWeaponId);
            }
        };
        AddAt(root, confirm, new Vector2(1010, 930), new Vector2(190, 64));
    }

    private void AddWeaponSection(
        Control root,
        GameContent content,
        string title,
        string? selectedWeaponId,
        string? blockedWeaponId,
        Vector2 position,
        Action<string> onSelected)
    {
        AddLabelAt(root, title, position + new Vector2(2, -36), new Vector2(300, 32), 24, InkText, HorizontalAlignment.Left);

        var panel = CreatePaperPanel(new Vector2(700, 270));
        AddAt(root, panel, position, new Vector2(700, 270));

        var tileOrigins = new[]
        {
            new Vector2(56, 34),
            new Vector2(406, 34),
            new Vector2(56, 148),
            new Vector2(406, 148)
        };

        for (var index = 0; index < WeaponSlots.Count; index++)
        {
            var weaponId = WeaponSlots[index];
            var selected = weaponId is not null && string.Equals(selectedWeaponId, weaponId, StringComparison.Ordinal);
            var blocked = weaponId is not null && string.Equals(blockedWeaponId, weaponId, StringComparison.Ordinal);
            var tile = CreateWeaponTile(content, weaponId, selected, blocked);
            if (weaponId is not null && !blocked)
            {
                tile.Pressed += () => onSelected(weaponId);
            }

            AddAt(panel, tile, tileOrigins[index], new Vector2(238, 92));
        }
    }

    private void AddStarterDeckPreview(Control root, GameContent content)
    {
        AddLabelAt(root, "初始卡组浏览", new Vector2(1178, 86), new Vector2(320, 38), 24, InkText, HorizontalAlignment.Center);

        var panel = CreatePaperPanel(new Vector2(1048, 636));
        AddAt(root, panel, new Vector2(820, 126), new Vector2(1048, 636));

        var cardIds = BuildPreviewCardIds(content).ToList();
        if (cardIds.Count == 0)
        {
            AddLabelAt(panel, "选择主武器和副武器后，将在这里预览 10 张初始牌。", new Vector2(204, 284), new Vector2(640, 56), 22, new Color(0.20f, 0.14f, 0.10f), HorizontalAlignment.Center);
            return;
        }

        const float cardWidth = 146f;
        var cardSize = CardPanel.SizeForWidth(cardWidth);
        var start = new Vector2(78, 102);
        var gap = new Vector2(42, 44);
        for (var index = 0; index < cardIds.Count; index++)
        {
            var cardId = cardIds[index];
            var column = index % 5;
            var row = index / 5;
            var card = CardPanel.Create(content.CardsById[cardId], content, LoadTexture, LoadFont, cardWidth);
            AddAt(panel, card, start + new Vector2(column * (cardWidth + gap.X), row * (cardSize.Y + gap.Y)), cardSize);
        }

        var mainName = selectedMainWeaponId is null ? "未选主武器" : content.WeaponName(selectedMainWeaponId);
        var offName = selectedOffHandWeaponId is null ? "未选副武器" : content.WeaponName(selectedOffHandWeaponId);
        AddLabelAt(panel, $"{mainName} 4 行动 + {offName} 4 行动 + {mainName} 2 终结", new Vector2(84, 42), new Vector2(880, 32), 17, new Color(0.20f, 0.14f, 0.10f), HorizontalAlignment.Center);
    }

    private IEnumerable<string> BuildPreviewCardIds(GameContent content)
    {
        if (selectedMainWeaponId is not null)
        {
            foreach (var cardId in StartingCardsByType(content, selectedMainWeaponId, CardType.Action).Take(4))
            {
                yield return cardId;
            }
        }

        if (selectedOffHandWeaponId is not null)
        {
            foreach (var cardId in StartingCardsByType(content, selectedOffHandWeaponId, CardType.Action).Take(4))
            {
                yield return cardId;
            }
        }

        if (selectedMainWeaponId is not null)
        {
            foreach (var cardId in StartingCardsByType(content, selectedMainWeaponId, CardType.Finisher).Take(2))
            {
                yield return cardId;
            }
        }
    }

    private static IEnumerable<string> StartingCardsByType(GameContent content, string weaponId, CardType type)
    {
        return content.ExpandedStartingCardIdsForWeapon(weaponId)
            .Where(cardId => content.CardsById[cardId].Type == type);
    }

    private WeaponChoiceTile CreateWeaponTile(GameContent content, string? weaponId, bool selected, bool blocked)
    {
        if (weaponId is null)
        {
            return new WeaponChoiceTile("?", null, "未来武器", selected: false, blocked: true, isPlaceholder: true);
        }

        return new WeaponChoiceTile(
            content.WeaponName(weaponId),
            LoadTexture(WeaponIconAsset(weaponId)),
            content.WeaponDescription(weaponId),
            selected,
            blocked,
            isPlaceholder: false);
    }

    private static string WeaponIconAsset(string weaponId)
    {
        return weaponId switch
        {
            "weapon.revolver_sword" => "asset.weapon.revolver_sword.large",
            "weapon.mechanical_arm" => "asset.weapon.mechanical_arm.large",
            _ => ""
        };
    }

    private static Panel CreatePaperPanel(Vector2 minSize)
    {
        var panel = new Panel
        {
            CustomMinimumSize = minSize
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.96f),
            BorderColor = Colors.Black,
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0
        });
        return panel;
    }

    private static readonly Color InkText = new(0.05f, 0.05f, 0.05f);

    private sealed partial class WeaponChoiceTile : Control
    {
        private const float Slant = 42f;

        private readonly TextureRect icon = new();
        private readonly Label placeholder = new();
        private readonly Label caption = new();
        private readonly Button hitbox = new();
        private readonly bool selected;
        private readonly bool blocked;
        private readonly bool isPlaceholder;
        private bool hovered;

        public event Action? Pressed;

        public WeaponChoiceTile(
            string title,
            Texture2D? texture,
            string tooltip,
            bool selected,
            bool blocked,
            bool isPlaceholder)
        {
            this.selected = selected;
            this.blocked = blocked;
            this.isPlaceholder = isPlaceholder;

            ClipContents = false;
            MouseFilter = MouseFilterEnum.Ignore;
            TooltipText = tooltip;

            icon.Texture = texture;
            icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            icon.TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps;
            icon.MouseFilter = MouseFilterEnum.Ignore;
            icon.Modulate = blocked ? new Color(0.45f, 0.45f, 0.45f, 0.55f) : Colors.White;
            AddChild(icon);

            placeholder.Text = "?";
            placeholder.HorizontalAlignment = HorizontalAlignment.Center;
            placeholder.VerticalAlignment = VerticalAlignment.Center;
            placeholder.MouseFilter = MouseFilterEnum.Ignore;
            placeholder.Visible = isPlaceholder;
            placeholder.AddThemeFontSizeOverride("font_size", 68);
            placeholder.AddThemeColorOverride("font_color", new Color(0.18f, 0.18f, 0.18f, 0.72f));
            AddChild(placeholder);

            caption.Text = title;
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            caption.VerticalAlignment = VerticalAlignment.Center;
            caption.MouseFilter = MouseFilterEnum.Ignore;
            caption.AddThemeFontSizeOverride("font_size", 18);
            caption.AddThemeColorOverride("font_color", selected ? Colors.White : new Color(0.05f, 0.05f, 0.05f));
            AddChild(caption);

            var empty = new StyleBoxEmpty();
            hitbox.AddThemeStyleboxOverride("normal", empty);
            hitbox.AddThemeStyleboxOverride("hover", empty);
            hitbox.AddThemeStyleboxOverride("pressed", empty);
            hitbox.AddThemeStyleboxOverride("disabled", empty);
            hitbox.AddThemeStyleboxOverride("focus", empty);
            hitbox.Disabled = blocked || isPlaceholder;
            hitbox.MouseEntered += () =>
            {
                hovered = true;
                QueueRedraw();
            };
            hitbox.MouseExited += () =>
            {
                hovered = false;
                QueueRedraw();
            };
            hitbox.Pressed += () => Pressed?.Invoke();
            AddChild(hitbox);
        }

        public override void _Ready()
        {
            LayoutChildren();
        }

        public override void _Notification(int what)
        {
            if (what != NotificationResized)
            {
                return;
            }

            LayoutChildren();
            QueueRedraw();
        }

        private void LayoutChildren()
        {
            var sideMargin = Math.Max(28f, Size.X * 0.15f);
            icon.Position = new Vector2(sideMargin, 8);
            icon.Size = new Vector2(Size.X - sideMargin * 2f, Size.Y * 0.56f);
            placeholder.Position = new Vector2(sideMargin, 4);
            placeholder.Size = new Vector2(Size.X - sideMargin * 2f, Size.Y * 0.62f);
            caption.Position = new Vector2(sideMargin * 0.55f, Size.Y - 32);
            caption.Size = new Vector2(Size.X - sideMargin * 1.1f, 26);
            hitbox.Position = Vector2.Zero;
            hitbox.Size = Size;
        }

        public override void _Draw()
        {
            var slant = Math.Min(Slant, Size.X * 0.16f);
            var points = new[]
            {
                new Vector2(slant, 0),
                new Vector2(Size.X, 0),
                new Vector2(Size.X - slant, Size.Y),
                new Vector2(0, Size.Y)
            };
            var background = selected
                ? Colors.Black
                : blocked || isPlaceholder
                    ? new Color(0.78f, 0.78f, 0.78f, 0.86f)
                    : hovered ? new Color(0.96f, 0.96f, 0.96f, 1f) : Colors.White;

            DrawColoredPolygon(points, background);
            DrawPolyline([points[0], points[1], points[2], points[3], points[0]], Colors.Black, selected ? 7f : 5f, true);

            if (blocked && !isPlaceholder)
            {
                DrawLine(new Vector2(14, Size.Y - 12), new Vector2(Size.X - 14, 12), new Color(0.10f, 0.10f, 0.10f, 0.78f), 5f, true);
                DrawLine(new Vector2(slant + 10, 12), new Vector2(Size.X - slant - 10, Size.Y - 12), new Color(0.10f, 0.10f, 0.10f, 0.42f), 3f, true);
            }
        }
    }
}
