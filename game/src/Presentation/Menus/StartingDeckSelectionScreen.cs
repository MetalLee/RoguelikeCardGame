using Godot;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Menus;

public sealed record StartingDeckCardOption
{
    public required string OptionId { get; init; }

    public required string WeaponId { get; init; }

    public required string CardId { get; init; }

    public required string SlotLabel { get; init; }
}

public partial class StartingDeckSelectionScreen : ComicScreen
{
    public event Action<string>? CardOptionToggled;
    public event Action? ConfirmRequested;
    public event Action? BackRequested;

    public void Render(
        string mainHandWeaponId,
        string offHandWeaponId,
        IReadOnlyList<StartingDeckCardOption> mainHandOptions,
        IReadOnlyList<StartingDeckCardOption> offHandOptions,
        IReadOnlySet<string> selectedOptionIds,
        bool canConfirm,
        string? validationMessage = null)
    {
        var content = RequireContent();
        var root = CreateCanvas();

        AddLabelAt(root, "起始牌组预览", new Vector2(600, 70), new Vector2(720, 54), 40, new Color(0.16f, 0.10f, 0.06f), HorizontalAlignment.Center);
        AddLabelAt(root, "当前 MVP 主流程会自动组成 10 张初始牌；此界面保留给未来起始构筑拓展。", new Vector2(450, 132), new Vector2(1020, 34), 18, new Color(0.30f, 0.18f, 0.10f), HorizontalAlignment.Center);

        AddPoolColumn(
            root,
            content,
            title: $"主手：{content.WeaponName(mainHandWeaponId)}",
            requiredCount: 6,
            options: mainHandOptions,
            selectedOptionIds: selectedOptionIds,
            position: new Vector2(170, 215));
        AddPoolColumn(
            root,
            content,
            title: $"副手：{content.WeaponName(offHandWeaponId)}",
            requiredCount: 4,
            options: offHandOptions,
            selectedOptionIds: selectedOptionIds,
            position: new Vector2(990, 215));

        var selectedMain = mainHandOptions.Count(option => selectedOptionIds.Contains(option.OptionId));
        var selectedOff = offHandOptions.Count(option => selectedOptionIds.Contains(option.OptionId));
        var summaryColor = canConfirm ? new Color(0.08f, 0.34f, 0.16f) : new Color(0.54f, 0.16f, 0.10f);
        var summary = validationMessage ?? $"当前选择：主手 {selectedMain}/6，副手 {selectedOff}/4";
        AddLabelAt(root, summary, new Vector2(500, 900), new Vector2(920, 38), 20, summaryColor, HorizontalAlignment.Center);

        var back = CreateArtButton("返回武器选择", "asset.ui.icon.discard_pile", new Vector2(230, 62), GoldLine);
        back.Pressed += () => BackRequested?.Invoke();
        AddAt(root, back, new Vector2(630, 962), new Vector2(230, 62));

        var confirm = CreateArtButton("开始第一战", "asset.ui.icon.playable_highlight", new Vector2(240, 62), GoldLine);
        confirm.Disabled = !canConfirm;
        confirm.Pressed += () => ConfirmRequested?.Invoke();
        AddAt(root, confirm, new Vector2(930, 962), new Vector2(240, 62));
    }

    private void AddPoolColumn(
        Control root,
        GameContent content,
        string title,
        int requiredCount,
        IReadOnlyList<StartingDeckCardOption> options,
        IReadOnlySet<string> selectedOptionIds,
        Vector2 position)
    {
        var selectedCount = options.Count(option => selectedOptionIds.Contains(option.OptionId));
        var panel = CreateFramedPanel(new Vector2(760, 640), selectedCount == requiredCount ? GoldLine : CyanLine);
        AddAt(root, panel, position, new Vector2(760, 640));

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 10);
        panel.AddChild(stack);

        var header = new Label
        {
            Text = $"{title}    {selectedCount}/{requiredCount}",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        header.AddThemeFontSizeOverride("font_size", 24);
        header.AddThemeColorOverride("font_color", new Color(1.0f, 0.82f, 0.48f));
        stack.AddChild(header);

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 10);
        stack.AddChild(grid);

        foreach (var option in options)
        {
            grid.AddChild(CreateCardOptionButton(content, option, selectedOptionIds.Contains(option.OptionId)));
        }
    }

    private Control CreateCardOptionButton(GameContent content, StartingDeckCardOption option, bool selected)
    {
        var card = content.CardsById[option.CardId];
        var button = new Button
        {
            Text = $"{option.SlotLabel}  {content.CardName(option.CardId)}\n{CardTypeText(card)}  {CostText(card)}\n{content.CardRules(option.CardId)}",
            CustomMinimumSize = new Vector2(356, 130),
            Alignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", selected ? new Color(0.15f, 0.08f, 0.03f) : new Color(0.98f, 0.86f, 0.64f));
        button.AddThemeStyleboxOverride("normal", CreateSelectionStyle(selected, hover: false));
        button.AddThemeStyleboxOverride("hover", CreateSelectionStyle(selected, hover: true));
        button.AddThemeStyleboxOverride("pressed", CreateSelectionStyle(!selected, hover: true));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.Pressed += () => CardOptionToggled?.Invoke(option.OptionId);
        return button;
    }

    private static string CardTypeText(CardDefinition card)
    {
        return card.Type == CardType.Finisher ? "终结牌" : "行动牌";
    }

    private static string CostText(CardDefinition card)
    {
        if (card.Type == CardType.Action)
        {
            var energy = card.ColorEnergyGeneration is null ? "" : $" / 生成 {card.ColorEnergyGeneration.Amount} 彩能";
            return $"{card.Cost} 费{energy}";
        }

        if (card.ColorEnergyCost is null)
        {
            return "彩能 ?";
        }

        return card.ColorEnergyCost.Mode switch
        {
            ColorEnergySpendMode.Fixed => $"消耗 {card.ColorEnergyCost.Amount} 彩能",
            ColorEnergySpendMode.X => $"消耗 X 彩能，至少 {card.ColorEnergyCost.MinAmount}",
            ColorEnergySpendMode.All => $"消耗全部彩能，至少 {card.ColorEnergyCost.MinAmount}",
            _ => "彩能 ?"
        };
    }

    private StyleBoxFlat CreateSelectionStyle(bool selected, bool hover)
    {
        return new StyleBoxFlat
        {
            BgColor = selected
                ? new Color(0.95f, 0.78f, 0.42f, hover ? 0.98f : 0.90f)
                : new Color(0.07f, 0.045f, 0.035f, hover ? 0.98f : 0.88f),
            BorderColor = selected ? GoldLine : new Color(0.38f, 0.30f, 0.22f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
    }
}
