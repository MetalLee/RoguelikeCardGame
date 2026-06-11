using System.Text;

namespace RoguelikeCardGame.Infrastructure.Randomness;

public sealed class RunRandomStreams
{
	private RunRandomStreams(int runSeed)
	{
		RunSeed = runSeed;
		Deck = CreateStream(runSeed, RandomStreamName.Deck);
		Map = CreateStream(runSeed, RandomStreamName.Map);
		Reward = CreateStream(runSeed, RandomStreamName.Reward);
		Encounter = CreateStream(runSeed, RandomStreamName.Encounter);
	}

	public int RunSeed { get; }

	public DeterministicRandom Deck { get; }

	public DeterministicRandom Map { get; }

	public DeterministicRandom Reward { get; }

	public DeterministicRandom Encounter { get; }

	public static RunRandomStreams FromRunSeed(int runSeed)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(runSeed);
		return new RunRandomStreams(runSeed);
	}

	private static DeterministicRandom CreateStream(int runSeed, string streamName)
	{
		return new DeterministicRandom(DeriveStreamSeed(runSeed, streamName));
	}

	private static ulong DeriveStreamSeed(int runSeed, string streamName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

		const ulong offsetBasis = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;

		var hash = offsetBasis;
		foreach (var value in BitConverter.GetBytes(runSeed))
		{
			hash ^= value;
			hash *= prime;
		}

		foreach (var value in Encoding.UTF8.GetBytes(streamName))
		{
			hash ^= value;
			hash *= prime;
		}

		return hash;
	}
}
