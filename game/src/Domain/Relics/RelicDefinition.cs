using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Effects;

namespace RoguelikeCardGame.Domain.Relics;

public enum RelicRarity
{
    Common,
    Uncommon,
    Rare,
    Boss
}

public enum RelicStackRule
{
    Unique,
    Stackable
}

public sealed record RelicConditionDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public sealed record RelicDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("rarity")]
    public RelicRarity Rarity { get; init; }

    [JsonPropertyName("trigger")]
    public required string Trigger { get; init; }

    [JsonPropertyName("conditions")]
    public List<RelicConditionDefinition> Conditions { get; init; } = new();

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; init; } = new();

    [JsonPropertyName("stack_rule")]
    public RelicStackRule StackRule { get; init; }

    [JsonPropertyName("text_key")]
    public required string TextKey { get; init; }

    [JsonPropertyName("icon_key")]
    public required string IconKey { get; init; }
}
