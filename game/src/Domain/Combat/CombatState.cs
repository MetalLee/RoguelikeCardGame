using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum CombatStatus
{
    NotStarted,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat
}

public sealed record CombatEnemyState
{
    [JsonPropertyName("instance_id")]
    public required string InstanceId { get; init; }

    [JsonPropertyName("enemy_id")]
    public required string EnemyId { get; init; }

    [JsonPropertyName("max_hp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("current_hp")]
    public int CurrentHp { get; init; }

    [JsonPropertyName("block")]
    public int Block { get; init; }

    [JsonPropertyName("intent_index")]
    public int IntentIndex { get; init; }
}

public sealed record CombatState
{
    [JsonPropertyName("combat_id")]
    public required string CombatId { get; init; }

    [JsonPropertyName("encounter_id")]
    public required string EncounterId { get; init; }

    [JsonPropertyName("status")]
    public CombatStatus Status { get; init; }

    [JsonPropertyName("turn_number")]
    public int TurnNumber { get; init; }

    [JsonPropertyName("player_max_hp")]
    public int PlayerMaxHp { get; init; }

    [JsonPropertyName("player_hp")]
    public int PlayerHp { get; init; }

    [JsonPropertyName("player_block")]
    public int PlayerBlock { get; init; }

    [JsonPropertyName("action_points")]
    public int ActionPoints { get; init; }

    [JsonPropertyName("chain")]
    public int Chain { get; init; }

    [JsonPropertyName("deck_zones")]
    public DeckZones DeckZones { get; init; } = new();

    [JsonPropertyName("enemies")]
    public List<CombatEnemyState> Enemies { get; init; } = new();

    [JsonPropertyName("log")]
    public List<CombatLogEvent> Log { get; init; } = new();
}
