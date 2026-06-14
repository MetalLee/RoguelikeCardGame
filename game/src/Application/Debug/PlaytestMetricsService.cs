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

    public int PeakColorEnergy { get; init; }

    public Dictionary<string, int> ColorEnergyGeneratedByColor { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> FinisherColorSpendByColor { get; init; } = new(StringComparer.Ordinal);

    public int ActionUseCount { get; init; }

    public int EnchantedActionUseCount { get; init; }

    public int FinisherUseCount { get; init; }
}

public sealed record PlaytestRewardChoiceMetric
{
    public int NodeOrder { get; init; }

    public required string EncounterId { get; init; }

    public required string ColorShard { get; init; }

    public string? EnchantedCardInstanceId { get; init; }

    public string? EnchantedCardDefinitionId { get; init; }

    public List<string> WeaponCardCandidateIds { get; init; } = new();

    public required string SelectedWeaponCardId { get; init; }
}

public sealed record PlaytestBuildRouteMetrics
{
    public int BlueGreenHeavyCannonSignals { get; init; }

    public int RedMechanicalCounterSignals { get; init; }

    public int YellowPurpleBarrageSignals { get; init; }
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

    public double EnchantmentUsageRate { get; init; }

    public required PlaytestBuildRouteMetrics BuildRoutes { get; init; }

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
            EnchantmentUsageRate = CalculateEnchantmentUsageRate(combatMetrics),
            BuildRoutes = BuildRouteMetrics(runState, combatMetrics),
            FinalState = runState.Status.ToString(),
            FinalNodeEncounterId = ResolveFinalNode(runState, combats),
            TotalDurationSeconds = Math.Max(0, (endedAt - startedAt).TotalSeconds)
        };
    }

    private static PlaytestCombatMetrics BuildCombatMetrics(CombatState combat, IReadOnlyList<string> nodeOrder)
    {
        var turnCount = Math.Max(combat.TurnNumber, combat.Log.Select(item => item.TurnNumber).DefaultIfEmpty(0).Max());
        var actionUseCount = combat.Log.Count(IsActionPlayed);
        var enchantedActionUseCount = combat.Log.Count(IsEnchantedActionPlayed);

        return new PlaytestCombatMetrics
        {
            EncounterId = combat.EncounterId,
            NodeOrder = Math.Max(0, nodeOrder.ToList().FindIndex(id => string.Equals(id, combat.EncounterId, StringComparison.Ordinal))) + 1,
            TurnCount = turnCount,
            DamageTaken = combat.Log
                .Where(item => item.EventType == CombatLogEventType.EnemyIntentResolved)
                .Sum(item => TryGetNumeric(item, "hp_damage") ?? 0),
            Outcome = combat.Status.ToString(),
            PeakColorEnergy = PeakColorEnergy(combat),
            ColorEnergyGeneratedByColor = ColorEnergyGeneratedByColor(combat),
            FinisherColorSpendByColor = FinisherColorSpendByColor(combat),
            ActionUseCount = actionUseCount,
            EnchantedActionUseCount = enchantedActionUseCount,
            FinisherUseCount = combat.Log.Count(IsFinisherPlayed)
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

    private static bool IsActionPlayed(CombatLogEvent logEvent)
    {
        return logEvent.EventType == CombatLogEventType.CardPlayed &&
               logEvent.Metadata.TryGetValue("card_type", out var cardType) &&
               string.Equals(cardType, "Action", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnchantedActionPlayed(CombatLogEvent logEvent)
    {
        return IsActionPlayed(logEvent) &&
               logEvent.Metadata.TryGetValue("enchantment_color", out var color) &&
               !string.IsNullOrWhiteSpace(color) &&
               !string.Equals(color, "Colorless", StringComparison.OrdinalIgnoreCase);
    }

    private static int PeakColorEnergy(CombatState combat)
    {
        return combat.Log
            .SelectMany(item => item.NumericChanges)
            .Where(item => item.Key.StartsWith("color_energy_", StringComparison.Ordinal) &&
                           (item.Key.Contains("after", StringComparison.Ordinal) ||
                            item.Key.Contains("before", StringComparison.Ordinal)))
            .Select(item => item.Value)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static Dictionary<string, int> ColorEnergyGeneratedByColor(CombatState combat)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var logEvent in combat.Log.Where(item =>
                     item.EventType == CombatLogEventType.EffectResolved &&
                     item.Metadata.TryGetValue("effect_type", out var effectType) &&
                     string.Equals(effectType, "gain_color_energy", StringComparison.Ordinal)))
        {
            var color = logEvent.Metadata.TryGetValue("color", out var value) ? value : "Colorless";
            result[color] = result.GetValueOrDefault(color) + (TryGetNumeric(logEvent, "color_energy_generated") ?? 0);
        }

        return result;
    }

    private static Dictionary<string, int> FinisherColorSpendByColor(CombatState combat)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var logEvent in combat.Log.Where(IsFinisherPlayed))
        {
            if (!logEvent.Metadata.TryGetValue("spent_colors", out var spentColors))
            {
                continue;
            }

            foreach (var color in spentColors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                result[color] = result.GetValueOrDefault(color) + 1;
            }
        }

        return result;
    }

    private static double CalculateEnchantmentUsageRate(IReadOnlyList<PlaytestCombatMetrics> combats)
    {
        var actionUses = combats.Sum(combat => combat.ActionUseCount);
        if (actionUses <= 0)
        {
            return 0;
        }

        return combats.Sum(combat => combat.EnchantedActionUseCount) / (double)actionUses;
    }

    private static PlaytestBuildRouteMetrics BuildRouteMetrics(
        RunState runState,
        IReadOnlyList<PlaytestCombatMetrics> combats)
    {
        var enchantmentColors = runState.MasterDeckInstances
            .Select(instance => instance.Enchantment?.Color.ToString())
            .Where(color => !string.IsNullOrWhiteSpace(color))
            .Cast<string>()
            .ToList();
        var spentColors = combats
            .SelectMany(combat => combat.FinisherColorSpendByColor)
            .SelectMany(item => Enumerable.Repeat(item.Key, item.Value))
            .ToList();
        var colors = enchantmentColors.Concat(spentColors).ToList();

        return new PlaytestBuildRouteMetrics
        {
            BlueGreenHeavyCannonSignals = colors.Count(color => color is "Blue" or "Green"),
            RedMechanicalCounterSignals = colors.Count(color => color == "Red"),
            YellowPurpleBarrageSignals = colors.Count(color => color is "Yellow" or "Purple")
        };
    }
}
