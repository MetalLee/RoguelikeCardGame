using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Colors;

namespace RoguelikeCardGame.Domain.Combat;

public sealed record ColorEnergySlot
{
    [JsonPropertyName("color")]
    public ColorType Color { get; init; } = ColorType.Colorless;

    public static ColorEnergySlot Colorless => new();

    public static ColorEnergySlot FromColor(ColorType color) => new() { Color = color };
}
