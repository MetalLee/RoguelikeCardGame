using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Weapons;

public sealed record WeaponDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("starting_pool_id")]
    public required string StartingPoolId { get; init; }

    [JsonPropertyName("reward_pool_id")]
    public required string RewardPoolId { get; init; }

    [JsonPropertyName("main_hand_allowed")]
    public bool MainHandAllowed { get; init; } = true;

    [JsonPropertyName("off_hand_allowed")]
    public bool OffHandAllowed { get; init; } = true;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
}
