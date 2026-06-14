using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Randomness;

namespace RoguelikeCardGame.Application.Debug;

public sealed record DebugRunDefaults
{
    public int PlayerMaxHp { get; init; }

    public int BaseActionPoints { get; init; }

    public int CardsPerTurn { get; init; }

    public List<string> StarterDeck { get; init; } = new();

    public List<string> EncounterSequence { get; init; } = new();
}

public sealed record DebugRunConfiguration
{
    public string RunId { get; init; } = "debug_run";

    public int Seed { get; init; }

    public string? EncounterId { get; init; }

    public List<string>? StarterDeckOverride { get; init; }

    public List<string> AdditionalCardIds { get; init; } = new();

    public string? MainHandWeaponId { get; init; }

    public string? OffHandWeaponId { get; init; }

    public int ColorShardPreviewRoll { get; init; }

    public List<string> WeaponCardPreviewIds { get; init; } = new();
}

public sealed record DebugRunStartResult
{
    public required RunState RunState { get; init; }

    public required EncounterDefinition Encounter { get; init; }

    public required CombatState Combat { get; init; }

    public ColorType ColorShardPreview { get; init; }

    public List<string> WeaponCardPreviews { get; init; } = new();
}

public sealed class DebugRunService
{
    private readonly RunStateFactory runStateFactory;
    private readonly CombatStateFactory? combatStateFactory;
    private readonly RewardService rewardService;

    public DebugRunService(
        RunStateFactory? runStateFactory = null,
        CombatStateFactory? combatStateFactory = null,
        RewardService? rewardService = null)
    {
        this.runStateFactory = runStateFactory ?? new RunStateFactory();
        this.combatStateFactory = combatStateFactory;
        this.rewardService = rewardService ?? new RewardService();
    }

    public DebugRunStartResult StartDebugEncounter(
        DebugRunDefaults defaults,
        DebugRunConfiguration configuration,
        IReadOnlyDictionary<string, EncounterDefinition> encountersById,
        IReadOnlyDictionary<string, EnemyDefinition> enemiesById,
        IReadOnlyDictionary<string, CardDefinition> cardsById,
        IEnumerable<WeaponRewardPoolDefinition> weaponRewardPools)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(encountersById);
        ArgumentNullException.ThrowIfNull(enemiesById);
        ArgumentNullException.ThrowIfNull(cardsById);
        ArgumentNullException.ThrowIfNull(weaponRewardPools);

        var encounterId = configuration.EncounterId ?? defaults.EncounterSequence.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(encounterId) || !encountersById.TryGetValue(encounterId, out var encounter))
        {
            throw new InvalidOperationException($"Unknown debug encounter id '{encounterId}'.");
        }

        var starterDeck = configuration.StarterDeckOverride?.ToList() ?? defaults.StarterDeck.ToList();
        starterDeck.AddRange(configuration.AdditionalCardIds);

        var run = runStateFactory.CreateNewRun(
            runId: configuration.RunId,
            seed: configuration.Seed,
            playerMaxHp: defaults.PlayerMaxHp,
            baseActionPoints: defaults.BaseActionPoints,
            cardsPerTurn: defaults.CardsPerTurn,
            starterDeck: starterDeck,
            encounterSequence: defaults.EncounterSequence,
            mainHandWeaponId: configuration.MainHandWeaponId,
            offHandWeaponId: configuration.OffHandWeaponId);

        var encounterIndex = defaults.EncounterSequence.FindIndex(id => string.Equals(id, encounter.Id, StringComparison.Ordinal));
        run = run with
        {
            CurrentEncounterIndex = Math.Max(0, encounterIndex)
        };

        var activeCombatFactory = combatStateFactory ?? new CombatStateFactory(RunRandomStreams.FromRunSeed(run.Seed).Deck.Shuffle);
        var combat = activeCombatFactory.CreateCombat(
            combatId: $"{run.RunId}_{encounter.Id}",
            runState: run,
            encounter: encounter,
            enemiesById: enemiesById);

        var colorShardPreview = rewardService.GenerateColorShard(max =>
            Math.Clamp(configuration.ColorShardPreviewRoll, 0, Math.Max(0, max - 1)));
        var weaponPreviewIds = configuration.WeaponCardPreviewIds.Count == 3
            ? configuration.WeaponCardPreviewIds.ToList()
            : rewardService.GenerateWeaponCardCandidates(
                run,
                weaponRewardPools,
                cardsById,
                _ => 0).ToList();

        return new DebugRunStartResult
        {
            RunState = run,
            Encounter = encounter,
            Combat = combat,
            ColorShardPreview = colorShardPreview,
            WeaponCardPreviews = weaponPreviewIds
        };
    }
}
