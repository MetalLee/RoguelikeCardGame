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
    public required string BaseEffectTemplate { get; init; }

    [JsonPropertyName("max_stacks_per_card")]
    public int MaxStacksPerCard { get; init; } = 1;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}
