using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Colors;

public enum ColorType
{
    Colorless,
    Red,
    Yellow,
    Blue,
    Green,
    Purple
}

public sealed record ColorDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("color")]
    public ColorType Color { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("base_effect_template")]
    public required ColorEffectTemplateDefinition BaseEffectTemplate { get; init; }

    [JsonPropertyName("stack_rule")]
    public required ColorStackRuleDefinition StackRule { get; init; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}

public sealed record ColorEffectTemplateDefinition
{
    [JsonPropertyName("op")]
    public required string Op { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; init; } = new();
}

public sealed record ColorStackRuleDefinition
{
    [JsonPropertyName("mode")]
    public required string Mode { get; init; }

    [JsonPropertyName("max_per_card")]
    public int MaxPerCard { get; init; }
}
