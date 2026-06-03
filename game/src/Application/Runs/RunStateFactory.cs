using RoguelikeCardGame.Domain.Runs;

namespace RoguelikeCardGame.Application.Runs;

public sealed class RunStateFactory
{
    public RunState CreateNewRun(
        string runId,
        int seed,
        int playerMaxHp,
        int baseActionPoints,
        int cardsPerTurn,
        IEnumerable<string> starterDeck,
        IEnumerable<string> encounterSequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentOutOfRangeException.ThrowIfNegative(seed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerMaxHp);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseActionPoints);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cardsPerTurn);

        return new RunState
        {
            RunId = runId,
            Seed = seed,
            Status = RunStatus.InProgress,
            PlayerMaxHp = playerMaxHp,
            PlayerHp = playerMaxHp,
            BaseActionPoints = baseActionPoints,
            CardsPerTurn = cardsPerTurn,
            MasterDeck = starterDeck.ToList(),
            EncounterSequence = encounterSequence.ToList(),
            CurrentEncounterIndex = 0
        };
    }
}
