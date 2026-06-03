using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Runs;

public enum RunStatus
{
    NotStarted,
    InProgress,
    Failed,
    Cleared
}

public sealed record RunState
{
    [JsonPropertyName("run_id")]
    public required string RunId { get; init; }

    [JsonPropertyName("seed")]
    public int Seed { get; init; }

    [JsonPropertyName("status")]
    public RunStatus Status { get; init; }

    [JsonPropertyName("player_max_hp")]
    public int PlayerMaxHp { get; init; }

    [JsonPropertyName("player_hp")]
    public int PlayerHp { get; init; }

    [JsonPropertyName("base_action_points")]
    public int BaseActionPoints { get; init; }

    [JsonPropertyName("cards_per_turn")]
    public int CardsPerTurn { get; init; }

    [JsonPropertyName("master_deck")]
    public List<string> MasterDeck { get; init; } = new();

    [JsonPropertyName("relic_ids")]
    public List<string> RelicIds { get; init; } = new();

    [JsonPropertyName("encounter_sequence")]
    public List<string> EncounterSequence { get; init; } = new();

    [JsonPropertyName("current_encounter_index")]
    public int CurrentEncounterIndex { get; init; }
}
