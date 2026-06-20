using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Combat;
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

public sealed record EnemyBeatDefinition
{
    [JsonPropertyName("action_card_id")]
    public required string ActionCardId { get; init; }

    [JsonPropertyName("actions")]
    public List<BeatActionDefinition> Actions { get; init; } = new();

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }
}

public sealed record EnemyBeatSequenceDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("beats")]
    public List<EnemyBeatDefinition> Beats { get; init; } = new();
}

public sealed record EnemyDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("max_hp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("intent_sequence")]
    public List<EnemyIntentDefinition> IntentSequence { get; init; } = new();

    [JsonPropertyName("resistances")]
    public BeatResistanceProfile Resistances { get; init; } = new();

    [JsonPropertyName("beat_sequences")]
    public List<EnemyBeatSequenceDefinition> BeatSequences { get; init; } = new();

    [JsonPropertyName("status_immunities")]
    public List<string> StatusImmunities { get; init; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();

}
