using RoguelikeCardGame.Domain.Cards;

namespace RoguelikeCardGame.Presentation.Cards;

public static class CardPanelLayout
{
    private static readonly CardPanelLayoutSpec Action = new(
        TemplateSize: new CardPanelSize(600, 802),
        ArtRect: new CardPanelRect(66, 156, 496, 410),
        CostRect: new CardPanelRect(64, 58, 58, 64),
        NameRect: new CardPanelRect(148, 62, 292, 52),
        RulesRect: new CardPanelRect(82, 606, 436, 118),
        MetaRect: new CardPanelRect(150, 156, 390, 34),
        CostFontSize: 54,
        NameFontSize: 34,
        RulesFontSize: 26,
        MetaFontSize: 18);

    private static readonly CardPanelLayoutSpec Finisher = new(
        TemplateSize: new CardPanelSize(600, 802),
        ArtRect: new CardPanelRect(53, 127, 493, 369),
        CostRect: new CardPanelRect(63, 46, 92, 72),
        NameRect: new CardPanelRect(182, 52, 347, 63),
        RulesRect: new CardPanelRect(74, 540, 452, 157),
        MetaRect: new CardPanelRect(74, 497, 452, 36),
        CostFontSize: 54,
        NameFontSize: 34,
        RulesFontSize: 26,
        MetaFontSize: 18);

    public static CardPanelLayoutSpec For(CardType type) =>
        type == CardType.Finisher ? Finisher : Action;
}

public readonly record struct CardPanelLayoutSpec(
    CardPanelSize TemplateSize,
    CardPanelRect ArtRect,
    CardPanelRect CostRect,
    CardPanelRect NameRect,
    CardPanelRect RulesRect,
    CardPanelRect MetaRect,
    int CostFontSize,
    int NameFontSize,
    int RulesFontSize,
    int MetaFontSize);

public readonly record struct CardPanelSize(float Width, float Height);

public readonly record struct CardPanelRect(float X, float Y, float Width, float Height);
