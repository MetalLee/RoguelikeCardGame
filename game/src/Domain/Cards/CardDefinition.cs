using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Effects;

namespace RoguelikeCardGame.Domain.Cards;

public enum CardType
{
    Action,
    Skill,
    Finisher
}

public enum CardRarity
{
    Starter,
    Common,
    Uncommon,
    Rare
}

public enum TargetRule
{
    SingleEnemy,
    AllEnemies,
    Self,
    None
}

public enum ChainChangeMode
{
    FixedDelta,
    ConsumeAll
}

public sealed record ChainChange
{
    [JsonPropertyName("mode")]
    public ChainChangeMode Mode { get; init; }

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    public static ChainChange Gain(int amount) => new() { Mode = ChainChangeMode.FixedDelta, Amount = amount };

    public static ChainChange None => Gain(0);

    public static ChainChange ConsumeAll => new() { Mode = ChainChangeMode.ConsumeAll, Amount = 0 };
}

public sealed record CardDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public CardType Type { get; init; }

    [JsonPropertyName("cost")]
    public int Cost { get; init; }

    [JsonPropertyName("min_chain")]
    public int? MinChain { get; init; }

    [JsonPropertyName("default_chain_change")]
    public required ChainChange DefaultChainChange { get; init; }

    [JsonPropertyName("target_rule")]
    public TargetRule TargetRule { get; init; }

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; init; } = new();

    [JsonPropertyName("rarity")]
    public CardRarity Rarity { get; init; }

    [JsonPropertyName("vfx_asset")]
    public string? VfxAsset { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}
