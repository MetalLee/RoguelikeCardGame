using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Cards;

namespace RoguelikeCardGame.Domain.Rewards;

public enum RewardRepeatRule
{
    Repeatable,
    UniqueUntilSeen
}

public sealed record RewardPackDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("pack_type")]
    public CardType PackType { get; init; }

    [JsonPropertyName("candidate_ids")]
    public List<string> CandidateIds { get; init; } = new();

    [JsonPropertyName("min_pick")]
    public int MinPick { get; init; }

    [JsonPropertyName("max_pick")]
    public int MaxPick { get; init; }

    [JsonPropertyName("guarantee_rule")]
    public required string GuaranteeRule { get; init; }

    [JsonPropertyName("repeat_rule")]
    public RewardRepeatRule RepeatRule { get; init; }

    [JsonPropertyName("text_key")]
    public required string TextKey { get; init; }
}
