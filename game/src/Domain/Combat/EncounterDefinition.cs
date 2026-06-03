using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum EncounterNodeType
{
    Normal,
    Elite,
    Boss
}

public sealed record EncounterEnemyDefinition
{
    [JsonPropertyName("instance_id")]
    public required string InstanceId { get; init; }

    [JsonPropertyName("enemy_id")]
    public required string EnemyId { get; init; }
}

public sealed record EncounterRewardProfileDefinition
{
    [JsonPropertyName("card_pack_ids")]
    public List<string> CardPackIds { get; init; } = new();

    [JsonPropertyName("relic_id")]
    public string? RelicId { get; init; }
}

public sealed record EncounterDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("node_type")]
    public EncounterNodeType NodeType { get; init; }

    [JsonPropertyName("enemies")]
    public List<EncounterEnemyDefinition> Enemies { get; init; } = new();

    [JsonPropertyName("reward_profile")]
    public EncounterRewardProfileDefinition RewardProfile { get; init; } = new();

    [JsonPropertyName("teaching_goal_key")]
    public required string TeachingGoalKey { get; init; }

    [JsonPropertyName("difficulty_note")]
    public required string DifficultyNote { get; init; }
}
