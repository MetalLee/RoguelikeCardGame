using System.Text.Json.Serialization;
using RoguelikeCardGame.Domain.Colors;

namespace RoguelikeCardGame.Domain.Combat;

public enum ColorEnergySpendMode
{
    Fixed,
    X,
    All
}

public sealed record ColorEnergyPool
{
    public const int DefaultCapacity = 6;

    [JsonPropertyName("capacity")]
    public int Capacity { get; init; } = DefaultCapacity;

    [JsonPropertyName("slots")]
    public List<ColorEnergySlot> Slots { get; init; } = new();

    [JsonIgnore]
    public int Count => Slots.Count;

    [JsonIgnore]
    public bool IsFull => Count >= Capacity;

    public static ColorEnergyPool Empty(int capacity = DefaultCapacity) => new() { Capacity = capacity };

    public ColorEnergyPool Clear() => this with { Slots = [] };

    public ColorEnergyPool Add(ColorType color, int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (amount == 0)
        {
            return this;
        }

        var slots = Slots.ToList();
        var accepted = Math.Min(amount, Math.Max(0, Capacity - slots.Count));
        for (var index = 0; index < accepted; index++)
        {
            slots.Add(ColorEnergySlot.FromColor(color));
        }

        return this with { Slots = slots };
    }

    public bool CanSpend(ColorEnergySpendMode mode, int amount, int minAmount = 0)
    {
        return mode switch
        {
            ColorEnergySpendMode.Fixed => amount > 0 && Count >= amount,
            ColorEnergySpendMode.X => minAmount > 0 && Count >= minAmount,
            ColorEnergySpendMode.All => minAmount > 0 && Count >= minAmount,
            _ => false
        };
    }

    public (ColorEnergyPool Pool, List<ColorEnergySlot> Spent) Spend(ColorEnergySpendMode mode, int amount, int minAmount = 0)
    {
        if (!CanSpend(mode, amount, minAmount))
        {
            throw new InvalidOperationException("Not enough color energy to spend.");
        }

        var spendCount = mode switch
        {
            ColorEnergySpendMode.Fixed => amount,
            ColorEnergySpendMode.X => Math.Clamp(amount, minAmount, Count),
            ColorEnergySpendMode.All => Count,
            _ => 0
        };

        var spent = Slots.Take(spendCount).ToList();
        var remaining = Slots.Skip(spendCount).ToList();
        return (this with { Slots = remaining }, spent);
    }
}
