using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Presentation.Battle;

public enum BeatClashActionAnimationKind
{
    Slash,
    Strike,
    Projectile,
    Dodge,
    Block
}

public sealed record BeatClashSpriteSheet(string AssetId, int Columns = 1, int Rows = 1);

public sealed record BeatClashSpriteSequence(
    string Id,
    IReadOnlyList<BeatClashSpriteSheet> Sheets,
    double FrameDurationSeconds = 0.055,
    bool Loop = false);

public static class BeatClashPresentationTiming
{
    public const double PreActionPauseSeconds = 0.5;

    public const double ActionIntervalSeconds = 0.5;
}

public sealed class BeatClashActionAnimationCatalog
{
    public static BeatClashActionAnimationCatalog Default { get; } = new(
        new BeatClashSpriteSequence(
            "zu.run.sword",
            [
                new BeatClashSpriteSheet("asset.character.zu.action.run.sword_1_4", Columns: 4),
                new BeatClashSpriteSheet("asset.character.zu.action.run.sword_5_8", Columns: 4),
                new BeatClashSpriteSheet("asset.character.zu.action.run.sword_9_12", Columns: 4)
            ],
            FrameDurationSeconds: 0.09,
            Loop: true),
        new Dictionary<BeatClashActionAnimationKind, IReadOnlyList<BeatClashSpriteSequence>>
        {
            [BeatClashActionAnimationKind.Slash] =
            [
                new BeatClashSpriteSequence(
                    "zu.attack.slash.combo_1",
                    [new BeatClashSpriteSheet("asset.character.zu.action.attack.combo_1", Columns: 4)],
                    FrameDurationSeconds: 0.11),
                new BeatClashSpriteSequence(
                    "zu.attack.slash.combo_2",
                    [new BeatClashSpriteSheet("asset.character.zu.action.attack.combo_2", Columns: 3)],
                    FrameDurationSeconds: 0.11),
                new BeatClashSpriteSequence(
                    "zu.attack.slash.combo_3",
                    [new BeatClashSpriteSheet("asset.character.zu.action.attack.combo_3", Columns: 3)],
                    FrameDurationSeconds: 0.11)
            ],
            [BeatClashActionAnimationKind.Strike] = [],
            [BeatClashActionAnimationKind.Projectile] = [],
            [BeatClashActionAnimationKind.Dodge] = [],
            [BeatClashActionAnimationKind.Block] = []
        });

    private readonly IReadOnlyDictionary<BeatClashActionAnimationKind, IReadOnlyList<BeatClashSpriteSequence>> sequencesByKind;

    public BeatClashActionAnimationCatalog(
        BeatClashSpriteSequence runWithSword,
        IReadOnlyDictionary<BeatClashActionAnimationKind, IReadOnlyList<BeatClashSpriteSequence>> sequencesByKind)
    {
        RunWithSword = runWithSword;
        this.sequencesByKind = sequencesByKind;
    }

    public BeatClashSpriteSequence RunWithSword { get; }

    public IReadOnlyList<BeatClashSpriteSequence> SequencesFor(BeatClashActionAnimationKind kind)
    {
        return sequencesByKind.TryGetValue(kind, out var sequences) ? sequences : [];
    }

    public IReadOnlyList<BeatClashActionAnimationKind> KindsForCard(CardDefinition card)
    {
        return card.BeatActions
            .SelectMany(action => action.ExpandRepeats())
            .Select(ToAnimationKind)
            .ToList();
    }

    private static BeatClashActionAnimationKind ToAnimationKind(BeatActionDefinition action)
    {
        if (action.Kind == BeatActionKind.Block)
        {
            return BeatClashActionAnimationKind.Block;
        }

        if (action.Kind == BeatActionKind.Dodge)
        {
            return BeatClashActionAnimationKind.Dodge;
        }

        return action.AttackType switch
        {
            BeatAttackType.Strike => BeatClashActionAnimationKind.Strike,
            BeatAttackType.Projectile => BeatClashActionAnimationKind.Projectile,
            _ => BeatClashActionAnimationKind.Slash
        };
    }
}
