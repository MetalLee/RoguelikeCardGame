using System.Security.Cryptography;

namespace RoguelikeCardGame.Infrastructure.Randomness;

public static class RunSeedGenerator
{
	public static int CreateSeed()
	{
		return RandomNumberGenerator.GetInt32(0, int.MaxValue);
	}
}
