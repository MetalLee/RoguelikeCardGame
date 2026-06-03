using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Enemies;

namespace RoguelikeCardGame.Domain.Combat;

public sealed record EnemyIntentView
{
    [JsonPropertyName("enemy_instance_id")]
    public required string EnemyInstanceId { get; init; }

    [JsonPropertyName("enemy_id")]
    public required string EnemyId { get; init; }

    [JsonPropertyName("intent_index")]
    public int IntentIndex { get; init; }

    [JsonPropertyName("intent_id")]
    public required string IntentId { get; init; }

    [JsonPropertyName("intent_type")]
    public EnemyIntentType IntentType { get; init; }

    [JsonPropertyName("ui_text_key")]
    public required string UiTextKey { get; init; }

    [JsonPropertyName("effect_previews")]
    public List<EnemyIntentEffectPreview> EffectPreviews { get; init; } = new();
}

public sealed record EnemyIntentEffectPreview
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; }

    [JsonPropertyName("value")]
    public int? Value { get; init; }
}
