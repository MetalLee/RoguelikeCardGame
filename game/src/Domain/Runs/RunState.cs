using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;

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

    [JsonPropertyName("main_hand_weapon_id")]
    public string? MainHandWeaponId { get; init; }

    [JsonPropertyName("off_hand_weapon_id")]
    public string? OffHandWeaponId { get; init; }

    [JsonPropertyName("master_deck")]
    public List<string> MasterDeck { get; init; } = new();

    [JsonPropertyName("master_deck_instances")]
    public List<CardInstance> MasterDeckInstances { get; init; } = new();

    [JsonPropertyName("card_enchantments")]
    public List<CardEnchantment> CardEnchantments { get; init; } = new();

    [JsonPropertyName("held_color_shards")]
    public List<ColorType> HeldColorShards { get; init; } = new();

    [JsonPropertyName("pending_color_shards")]
    public List<ColorType> PendingColorShards { get; init; } = new();

    [JsonPropertyName("relic_ids")]
    public List<string> RelicIds { get; init; } = new();

    [JsonPropertyName("encounter_sequence")]
    public List<string> EncounterSequence { get; init; } = new();

    [JsonPropertyName("current_encounter_index")]
    public int CurrentEncounterIndex { get; init; }
}
