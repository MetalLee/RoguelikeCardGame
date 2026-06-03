using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Effects;

public sealed record EffectDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; }

    [JsonPropertyName("value")]
    public int? Value { get; init; }

    [JsonPropertyName("threshold")]
    public int? Threshold { get; init; }

    [JsonPropertyName("effect")]
    public EffectDefinition? Effect { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; init; } = new();
}
