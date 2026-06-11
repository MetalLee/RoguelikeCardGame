namespace RoguelikeCardGame.Infrastructure.Randomness;

public sealed class DeterministicRandom
{
	private const ulong Increment = 0x9E3779B97F4A7C15UL;

	private ulong state;

	public DeterministicRandom(ulong seed)
	{
		state = seed;
	}

	public ulong State => state;

	public int Calls { get; private set; }

	public ulong NextUInt64()
	{
		state += Increment;
		var value = state;
		value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
		value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
		Calls++;
		return value ^ (value >> 31);
	}

	public int NextInt(int maxExclusive)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
		return (int)(NextUInt64() % (uint)maxExclusive);
	}

	public IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		var result = items.ToList();
		for (var i = result.Count - 1; i > 0; i--)
		{
			var swapIndex = NextInt(i + 1);
			(result[i], result[swapIndex]) = (result[swapIndex], result[i]);
		}

		return result;
	}
}
