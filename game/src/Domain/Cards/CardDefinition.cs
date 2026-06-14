using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Effects;

namespace RoguelikeCardGame.Domain.Cards;

public enum CardType
{
    Action,
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

    [JsonPropertyName("weapon_id")]
    public string WeaponId { get; init; } = string.Empty;

    [JsonPropertyName("cost")]
    public int Cost { get; init; }

    [JsonPropertyName("min_chain")]
    public int? MinChain { get; init; }

    [JsonPropertyName("default_chain_change")]
    public ChainChange DefaultChainChange { get; init; } = ChainChange.None;

    [JsonPropertyName("color_energy_generation")]
    public ColorEnergyGeneration? ColorEnergyGeneration { get; init; }

    [JsonPropertyName("color_energy_cost")]
    public ColorEnergyCost? ColorEnergyCost { get; init; }

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

public enum ColorEnergyColorSource
{
    Colorless,
    Enchantment,
    FixedColor
}

public sealed record ColorEnergyGeneration
{
    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("color_source")]
    public ColorEnergyColorSource ColorSource { get; init; } = ColorEnergyColorSource.Enchantment;

    [JsonPropertyName("fixed_color")]
    public ColorType? FixedColor { get; init; }

    public ColorType ResolveColor(CardEnchantment? enchantment)
    {
        return ColorSource switch
        {
            ColorEnergyColorSource.Enchantment => enchantment?.Color ?? ColorType.Colorless,
            ColorEnergyColorSource.FixedColor => FixedColor ?? ColorType.Colorless,
            _ => ColorType.Colorless
        };
    }
}

public sealed record ColorEnergyCost
{
    [JsonPropertyName("mode")]
    public ColorEnergySpendMode Mode { get; init; }

    [JsonPropertyName("amount")]
    public int Amount { get; init; }

    [JsonPropertyName("min_amount")]
    public int MinAmount { get; init; }
}

public sealed record CardEnchantment
{
    [JsonPropertyName("card_instance_id")]
    public required string CardInstanceId { get; init; }

    [JsonPropertyName("color")]
    public ColorType Color { get; init; }
}

public sealed record CardInstance
{
    [JsonPropertyName("instance_id")]
    public required string InstanceId { get; init; }

    [JsonPropertyName("definition_id")]
    public required string DefinitionId { get; init; }

    [JsonPropertyName("enchantment")]
    public CardEnchantment? Enchantment { get; init; }
}
