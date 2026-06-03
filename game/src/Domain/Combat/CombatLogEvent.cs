using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum CombatLogEventType
{
    CombatStarted,
    TurnStarted,
    CardPlayed,
    EffectResolved,
    EnemyIntentResolved,
    CombatEnded
}

public sealed record CombatLogEvent
{
    [JsonPropertyName("event_id")]
    public required string EventId { get; init; }

    [JsonPropertyName("event_type")]
    public CombatLogEventType EventType { get; init; }

    [JsonPropertyName("turn_number")]
    public int TurnNumber { get; init; }

    [JsonPropertyName("source_id")]
    public string? SourceId { get; init; }

    [JsonPropertyName("target_ids")]
    public List<string> TargetIds { get; init; } = new();

    [JsonPropertyName("message_key")]
    public string? MessageKey { get; init; }

    [JsonPropertyName("numeric_changes")]
    public Dictionary<string, int> NumericChanges { get; init; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();
}
