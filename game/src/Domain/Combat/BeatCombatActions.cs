using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum BeatActionKind
{
    Attack,
    Block,
    Dodge
}

public enum BeatAttackType
{
    Slash,
    Strike,
    Projectile
}

public enum BeatResistanceGrade
{
    Resist,
    Standard,
    Weakness
}

public sealed record BeatActionDefinition
{
    [JsonPropertyName("kind")]
    public BeatActionKind Kind { get; init; }

    [JsonPropertyName("attack_type")]
    public BeatAttackType? AttackType { get; init; }

    [JsonPropertyName("value")]
    public int Value { get; init; }

    [JsonPropertyName("repeat")]
    public int Repeat { get; init; } = 1;

    [JsonPropertyName("dodge_chance_percent")]
    public int DodgeChancePercent { get; init; } = 50;

    public IEnumerable<BeatActionDefinition> ExpandRepeats()
    {
        var repeat = Math.Max(1, Repeat);
        for (var index = 0; index < repeat; index++)
        {
            yield return this with { Repeat = 1 };
        }
    }
}

public sealed record BeatResistanceProfile
{
    [JsonPropertyName("slash")]
    public BeatResistanceGrade Slash { get; init; } = BeatResistanceGrade.Standard;

    [JsonPropertyName("strike")]
    public BeatResistanceGrade Strike { get; init; } = BeatResistanceGrade.Standard;

    [JsonPropertyName("projectile")]
    public BeatResistanceGrade Projectile { get; init; } = BeatResistanceGrade.Standard;

    public BeatResistanceGrade GradeFor(BeatAttackType attackType)
    {
        return attackType switch
        {
            BeatAttackType.Slash => Slash,
            BeatAttackType.Strike => Strike,
            BeatAttackType.Projectile => Projectile,
            _ => BeatResistanceGrade.Standard
        };
    }

    public static int Apply(BeatResistanceGrade grade, int value)
    {
        return grade switch
        {
            BeatResistanceGrade.Resist => value / 2,
            BeatResistanceGrade.Weakness => value * 3 / 2,
            _ => value
        };
    }
}
