using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public sealed record DeckZones
{
    [JsonPropertyName("draw_pile")]
    public List<string> DrawPile { get; init; } = new();

    [JsonPropertyName("hand")]
    public List<string> Hand { get; init; } = new();

    [JsonPropertyName("discard_pile")]
    public List<string> DiscardPile { get; init; } = new();

    [JsonIgnore]
    public int DrawPileCount => DrawPile.Count;

    [JsonIgnore]
    public int HandCount => Hand.Count;

    [JsonIgnore]
    public int DiscardPileCount => DiscardPile.Count;
}
