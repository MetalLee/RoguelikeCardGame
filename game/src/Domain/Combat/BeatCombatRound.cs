using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum BeatTargetKind
{
    EnemyBeat,
    EnemyBody
}

public sealed record BeatTarget
{
    [JsonPropertyName("kind")]
    public BeatTargetKind Kind { get; init; }

    [JsonPropertyName("enemy_instance_id")]
    public required string EnemyInstanceId { get; init; }

    [JsonPropertyName("enemy_beat_index")]
    public int? EnemyBeatIndex { get; init; }
}

public sealed record PlayerBeatSlot
{
    [JsonPropertyName("beat_index")]
    public int BeatIndex { get; init; }

    [JsonPropertyName("card_instance_id")]
    public string? CardInstanceId { get; init; }

    [JsonPropertyName("card_id")]
    public string? CardId { get; init; }

    [JsonPropertyName("target")]
    public BeatTarget? Target { get; init; }
}

public sealed record EnemyBeatSlot
{
    [JsonPropertyName("enemy_instance_id")]
    public required string EnemyInstanceId { get; init; }

    [JsonPropertyName("beat_index")]
    public int BeatIndex { get; init; }

    [JsonPropertyName("action_card_id")]
    public required string ActionCardId { get; init; }

    [JsonPropertyName("actions")]
    public List<BeatActionDefinition> Actions { get; init; } = new();

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }
}

public sealed record FinisherSlotState
{
    [JsonPropertyName("card_instance_id")]
    public string? CardInstanceId { get; init; }

    [JsonPropertyName("card_id")]
    public string? CardId { get; init; }

    [JsonPropertyName("preserve_after_round")]
    public bool PreserveAfterRound { get; init; }
}

public sealed record BeatRoundState
{
    [JsonPropertyName("beat_count")]
    public int BeatCount { get; init; } = 3;

    [JsonPropertyName("player_beats")]
    public List<PlayerBeatSlot> PlayerBeats { get; init; } = new();

    [JsonPropertyName("enemy_beats")]
    public List<EnemyBeatSlot> EnemyBeats { get; init; } = new();

    [JsonPropertyName("finisher_slot")]
    public FinisherSlotState FinisherSlot { get; init; } = new();
}
