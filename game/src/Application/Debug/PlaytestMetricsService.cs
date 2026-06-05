using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Runs;

namespace RoguelikeCardGame.Application.Debug;

public sealed record PlaytestCombatMetrics
{
    public required string EncounterId { get; init; }

    public int NodeOrder { get; init; }

    public int TurnCount { get; init; }

    public int DamageTaken { get; init; }

    public required string Outcome { get; init; }

    public int HighestChain { get; init; }

    public int ChainThreshold3ReachedCount { get; init; }

    public int ChainThreshold5ReachedCount { get; init; }

    public int ChainThreshold8ReachedCount { get; init; }

    public int FinisherUseCount { get; init; }

    public int FinisherBonusTriggerCount { get; init; }
}

public sealed record PlaytestRewardChoiceMetric
{
    public int NodeOrder { get; init; }

    public required string EncounterId { get; init; }

    public required string RewardPackId { get; init; }

    public required string RewardPackType { get; init; }

    public int PickedCount { get; init; }

    public List<string> CardIds { get; init; } = new();
}

public sealed record PlaytestRelicGrantMetric
{
    public int NodeOrder { get; init; }

    public required string EncounterId { get; init; }

    public required string RelicId { get; init; }
}

public sealed record PlaytestMetricsReport
{
    public int RunSeed { get; init; }

    public List<string> NodeOrder { get; init; } = new();

    public List<PlaytestCombatMetrics> Combats { get; init; } = new();

    public List<PlaytestRewardChoiceMetric> Rewards { get; init; } = new();

    public List<PlaytestRelicGrantMetric> Relics { get; init; } = new();

    public required string FinalState { get; init; }

    public string? FinalNodeEncounterId { get; init; }

    public double TotalDurationSeconds { get; init; }
}

public sealed class PlaytestMetricsService
{
    public PlaytestMetricsReport BuildReport(
        RunState runState,
        IReadOnlyList<string> nodeOrder,
        IReadOnlyList<CombatState> combats,
        IEnumerable<PlaytestRewardChoiceMetric> rewardChoices,
        IEnumerable<PlaytestRelicGrantMetric> relicGrants,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(nodeOrder);
        ArgumentNullException.ThrowIfNull(combats);
        ArgumentNullException.ThrowIfNull(rewardChoices);
        ArgumentNullException.ThrowIfNull(relicGrants);

        var combatMetrics = combats
            .Select(combat => BuildCombatMetrics(combat, nodeOrder))
            .ToList();

        return new PlaytestMetricsReport
        {
            RunSeed = runState.Seed,
            NodeOrder = nodeOrder.ToList(),
            Combats = combatMetrics,
            Rewards = rewardChoices.ToList(),
            Relics = relicGrants.ToList(),
            FinalState = runState.Status.ToString(),
            FinalNodeEncounterId = ResolveFinalNode(runState, combats),
            TotalDurationSeconds = Math.Max(0, (endedAt - startedAt).TotalSeconds)
        };
    }

    private static PlaytestCombatMetrics BuildCombatMetrics(CombatState combat, IReadOnlyList<string> nodeOrder)
    {
        var chainTransitions = combat.Log
            .Select(item => (
                Before: TryGetNumeric(item, "chain_before") ?? TryGetNumeric(item, "chain_before_play") ?? 0,
                After: TryGetNumeric(item, "chain_after") ?? TryGetNumeric(item, "chain_before") ?? TryGetNumeric(item, "chain_before_play") ?? 0))
            .ToList();
        var highestChain = chainTransitions.Count == 0
            ? combat.Chain
            : Math.Max(combat.Chain, chainTransitions.Max(item => Math.Max(item.Before, item.After)));
        var turnCount = Math.Max(combat.TurnNumber, combat.Log.Select(item => item.TurnNumber).DefaultIfEmpty(0).Max());

        return new PlaytestCombatMetrics
        {
            EncounterId = combat.EncounterId,
            NodeOrder = Math.Max(0, nodeOrder.ToList().FindIndex(id => string.Equals(id, combat.EncounterId, StringComparison.Ordinal))) + 1,
            TurnCount = turnCount,
            DamageTaken = combat.Log
                .Where(item => item.EventType == CombatLogEventType.EnemyIntentResolved)
                .Sum(item => TryGetNumeric(item, "hp_damage") ?? 0),
            Outcome = combat.Status.ToString(),
            HighestChain = highestChain,
            ChainThreshold3ReachedCount = CountThresholdCrossings(chainTransitions, 3),
            ChainThreshold5ReachedCount = CountThresholdCrossings(chainTransitions, 5),
            ChainThreshold8ReachedCount = CountThresholdCrossings(chainTransitions, 8),
            FinisherUseCount = combat.Log.Count(IsFinisherPlayed),
            FinisherBonusTriggerCount = combat.Log.Count(IsTriggeredFinisherBonus)
        };
    }

    private static string? ResolveFinalNode(RunState runState, IReadOnlyList<CombatState> combats)
    {
        var failedCombat = combats.FirstOrDefault(combat => combat.Status == CombatStatus.Defeat);
        if (failedCombat is not null)
        {
            return failedCombat.EncounterId;
        }

        if (runState.Status == RunStatus.Cleared)
        {
            return combats.LastOrDefault(combat => combat.Status == CombatStatus.Victory)?.EncounterId;
        }

        return null;
    }

    private static int CountThresholdCrossings(IEnumerable<(int Before, int After)> transitions, int threshold)
    {
        return transitions.Count(item => item.Before < threshold && item.After >= threshold);
    }

    private static int? TryGetNumeric(CombatLogEvent logEvent, string key)
    {
        return logEvent.NumericChanges.TryGetValue(key, out var value) ? value : null;
    }

    private static bool IsFinisherPlayed(CombatLogEvent logEvent)
    {
        return logEvent.EventType == CombatLogEventType.CardPlayed &&
               logEvent.Metadata.TryGetValue("card_type", out var cardType) &&
               string.Equals(cardType, "Finisher", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTriggeredFinisherBonus(CombatLogEvent logEvent)
    {
        return logEvent.EventType == CombatLogEventType.EffectResolved &&
               logEvent.Metadata.TryGetValue("effect_type", out var effectType) &&
               string.Equals(effectType, "chain_threshold_bonus", StringComparison.Ordinal) &&
               logEvent.NumericChanges.TryGetValue("triggered", out var triggered) &&
               triggered == 1;
    }
}
