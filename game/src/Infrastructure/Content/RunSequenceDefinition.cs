using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Infrastructure.Content;

public sealed record RunSequenceDefinition
{
    public required string Id { get; init; }

    public int PlayerMaxHp { get; init; }

    public int BaseActionPoints { get; init; }

    public int CardsPerTurn { get; init; }

    public List<string> StarterDeck { get; init; } = new();

    public List<string> EncounterSequence { get; init; } = new();

    public required string BossEncounterId { get; init; }
}
