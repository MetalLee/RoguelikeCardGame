using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Effects;

namespace RoguelikeCardGame.Domain.Enemies;

public enum EnemyIntentType
{
    Attack,
    Defend,
    Buff,
    Debuff,
    Mixed
}

public sealed record EnemyIntentDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("intent_type")]
    public EnemyIntentType IntentType { get; init; }

    [JsonPropertyName("effects")]
    public List<EffectDefinition> Effects { get; init; } = new();
}

public sealed record EnemyDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("max_hp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("intent_sequence")]
    public List<EnemyIntentDefinition> IntentSequence { get; init; } = new();

    [JsonPropertyName("status_immunities")]
    public List<string> StatusImmunities { get; init; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();

}
