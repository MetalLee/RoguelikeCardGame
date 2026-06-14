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

public sealed record ResourceAmountDefinition
{
    [JsonPropertyName("resource")]
    public required string Resource { get; init; }

    [JsonPropertyName("amount")]
    public int Amount { get; init; }
}

public sealed record CardTargetingDefinition
{
    public static CardTargetingDefinition None { get; } = new()
    {
        Mode = "none",
        Side = "none",
        Required = false
    };

    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("side")]
    public required string Side { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }
}

public sealed record CardEnchantmentRulesDefinition
{
    [JsonPropertyName("can_be_enchanted")]
    public bool CanBeEnchanted { get; init; }

    [JsonPropertyName("generated_energy_color")]
    public required string GeneratedEnergyColor { get; init; }

    [JsonPropertyName("self_effects")]
    public List<EffectDefinition> SelfEffects { get; init; } = new();
}

public sealed record FinisherColorEffectsDefinition
{
    [JsonPropertyName("color_id")]
    public required string ColorId { get; init; }

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; init; } = new();

    [JsonPropertyName("stack_limit")]
    public int StackLimit { get; init; }
}

public sealed record CardColorInteractionsDefinition
{
    [JsonPropertyName("enchantment")]
    public CardEnchantmentRulesDefinition Enchantment { get; init; } = new()
    {
        CanBeEnchanted = false,
        GeneratedEnergyColor = "none"
    };

    [JsonPropertyName("finisher_color_effects")]
    public List<FinisherColorEffectsDefinition> FinisherColorEffects { get; init; } = new();
}

public sealed record CardBalanceDefinition
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("tier")]
    public int Tier { get; init; }
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

    [JsonPropertyName("costs")]
    public List<ResourceAmountDefinition> Costs { get; init; } = new();

    [JsonPropertyName("requirements")]
    public List<EffectDefinition> Requirements { get; init; } = new();

    [JsonPropertyName("targeting")]
    public CardTargetingDefinition Targeting { get; init; } = CardTargetingDefinition.None;

    [JsonPropertyName("color_energy_generation")]
    public ColorEnergyGeneration? ColorEnergyGeneration { get; init; }

    [JsonPropertyName("color_energy_cost")]
    public ColorEnergyCost? ColorEnergyCost { get; init; }

    [JsonPropertyName("target_rule")]
    public TargetRule TargetRule { get; init; }

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; init; } = new();

    [JsonPropertyName("color_interactions")]
    public CardColorInteractionsDefinition ColorInteractions { get; init; } = new();

    [JsonPropertyName("after_play")]
    public List<EffectDefinition> AfterPlay { get; init; } = new();

    [JsonPropertyName("rarity")]
    public CardRarity Rarity { get; init; }

    [JsonPropertyName("balance")]
    public CardBalanceDefinition Balance { get; init; } = new()
    {
        Role = string.Empty,
        Tier = 0
    };

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

    [JsonPropertyName("fixed_color_id")]
    public string? FixedColorId { get; init; }

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

    [JsonPropertyName("color_filter")]
    public string ColorFilter { get; init; } = "any";
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
