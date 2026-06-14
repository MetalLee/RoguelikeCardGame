using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;

namespace RoguelikeCardGame.Application.Runs;

public sealed class RunStateFactory
{
    public RunState CreateNewRunFromWeaponSelection(
        string runId,
        int seed,
        int playerMaxHp,
        int baseActionPoints,
        int cardsPerTurn,
        string mainHandWeaponId,
        string offHandWeaponId,
        IEnumerable<string> selectedCardIds,
        IEnumerable<string> encounterSequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mainHandWeaponId);
        ArgumentException.ThrowIfNullOrWhiteSpace(offHandWeaponId);
        if (string.Equals(mainHandWeaponId, offHandWeaponId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Main hand and off hand weapons must be different.");
        }

        return CreateNewRun(
            runId,
            seed,
            playerMaxHp,
            baseActionPoints,
            cardsPerTurn,
            selectedCardIds,
            encounterSequence,
            mainHandWeaponId,
            offHandWeaponId);
    }

    public RunState CreateNewRun(
        string runId,
        int seed,
        int playerMaxHp,
        int baseActionPoints,
        int cardsPerTurn,
        IEnumerable<string> starterDeck,
        IEnumerable<string> encounterSequence,
        string? mainHandWeaponId = null,
        string? offHandWeaponId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentOutOfRangeException.ThrowIfNegative(seed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerMaxHp);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseActionPoints);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cardsPerTurn);

        var starterDeckList = starterDeck.ToList();
        var cardInstances = CreateCardInstances(runId, starterDeckList);

        return new RunState
        {
            RunId = runId,
            Seed = seed,
            Status = RunStatus.InProgress,
            PlayerMaxHp = playerMaxHp,
            PlayerHp = playerMaxHp,
            BaseActionPoints = baseActionPoints,
            CardsPerTurn = cardsPerTurn,
            MainHandWeaponId = mainHandWeaponId,
            OffHandWeaponId = offHandWeaponId,
            MasterDeck = starterDeckList,
            MasterDeckInstances = cardInstances,
            EncounterSequence = encounterSequence.ToList(),
            CurrentEncounterIndex = 0
        };
    }

    public RunState EnchantCard(RunState runState, string cardInstanceId, ColorType color)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardInstanceId);

        var updatedInstances = runState.MasterDeckInstances.Select(instance =>
            instance.InstanceId == cardInstanceId
                ? instance with { Enchantment = new CardEnchantment { CardInstanceId = cardInstanceId, Color = color } }
                : instance).ToList();

        if (updatedInstances.All(instance => instance.InstanceId != cardInstanceId))
        {
            throw new InvalidOperationException($"Unknown card instance id '{cardInstanceId}'.");
        }

        var enchantments = updatedInstances
            .Select(instance => instance.Enchantment)
            .Where(enchantment => enchantment is not null)
            .Cast<CardEnchantment>()
            .ToList();

        return runState with
        {
            MasterDeckInstances = updatedInstances,
            CardEnchantments = enchantments
        };
    }

    private static List<CardInstance> CreateCardInstances(string runId, IReadOnlyList<string> starterDeck)
    {
        return starterDeck
            .Select((cardId, index) => new CardInstance
            {
                InstanceId = $"{runId}.card.{index + 1:000}",
                DefinitionId = cardId
            })
            .ToList();
    }
}
