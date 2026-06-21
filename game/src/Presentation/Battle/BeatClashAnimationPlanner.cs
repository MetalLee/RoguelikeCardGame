using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed class BeatClashAnimationPlanner
{
    public IReadOnlyList<BeatClashAnimationStep> Plan(IReadOnlyList<CombatLogEvent> combatLog)
    {
        var energyByCardInstanceId = combatLog
            .Where(item => item.EventType == CombatLogEventType.BeatEnergyGenerated)
            .Select(item => new
            {
                CardInstanceId = TryGetMetadata(item, "card_instance_id"),
                Color = TryGetMetadata(item, "color"),
                Amount = TryGetNumeric(item, "color_energy_generated")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.CardInstanceId) && item.Amount > 0)
            .GroupBy(item => item.CardInstanceId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<BeatClashEnergyColor>)group
                    .Select(item => new BeatClashEnergyColor(item.Color ?? string.Empty, item.Amount))
                    .ToList(),
                StringComparer.Ordinal);

        var orderedSteps = combatLog
            .Where(item => item.EventType == CombatLogEventType.BeatActionResolved)
            .Select(item => CreateStep(item, energyByCardInstanceId))
            .OrderBy(step => step.BeatIndex)
            .ToList();

        for (var index = 0; index < orderedSteps.Count; index++)
        {
            if (index == 0)
            {
                orderedSteps[index] = orderedSteps[index] with
                {
                    ContinuesPreviousTarget = false,
                    ReturnToStartBeforeStep = false
                };
                continue;
            }

            var previousTargetId = orderedSteps[index - 1].TargetId;
            var currentTargetId = orderedSteps[index].TargetId;
            var continuesPreviousTarget =
                !string.IsNullOrWhiteSpace(currentTargetId) &&
                string.Equals(previousTargetId, currentTargetId, StringComparison.Ordinal);
            orderedSteps[index] = orderedSteps[index] with
            {
                ContinuesPreviousTarget = continuesPreviousTarget,
                ReturnToStartBeforeStep = !continuesPreviousTarget
            };
        }

        return orderedSteps;
    }

    private static BeatClashAnimationStep CreateStep(
        CombatLogEvent logEvent,
        IReadOnlyDictionary<string, IReadOnlyList<BeatClashEnergyColor>> energyByCardInstanceId)
    {
        var cardInstanceId = TryGetMetadata(logEvent, "card_instance_id");
        var energyColors = cardInstanceId is not null && energyByCardInstanceId.TryGetValue(cardInstanceId, out var colors)
            ? colors
            : [];
        return new BeatClashAnimationStep
        {
            BeatIndex = TryGetNumeric(logEvent, "beat_index"),
            CardId = logEvent.SourceId,
            SourceId = logEvent.SourceId,
            CardInstanceId = cardInstanceId,
            TargetId = logEvent.TargetIds.FirstOrDefault(),
            TargetKind = TryGetMetadata(logEvent, "target_kind"),
            EnemyBeatIndex = TryGetMetadataInt(logEvent, "enemy_beat_index"),
            EnemyDamage = TryGetNumeric(logEvent, "enemy_damage"),
            PlayerDamage = TryGetNumeric(logEvent, "player_damage"),
            SuccessfulPlayerActions = TryGetNumeric(logEvent, "successful_player_actions"),
            SuccessfulEnemyActions = TryGetNumeric(logEvent, "successful_enemy_actions"),
            EnergyGeneratedTotal = energyColors.Sum(item => item.Amount),
            EnergyColors = energyColors
        };
    }

    private static int TryGetNumeric(CombatLogEvent logEvent, string key)
    {
        return logEvent.NumericChanges.TryGetValue(key, out var value) ? value : 0;
    }

    private static string? TryGetMetadata(CombatLogEvent logEvent, string key)
    {
        return logEvent.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static int? TryGetMetadataInt(CombatLogEvent logEvent, string key)
    {
        return int.TryParse(TryGetMetadata(logEvent, key), out var value) ? value : null;
    }
}

public sealed record BeatClashAnimationStep
{
    public int BeatIndex { get; init; }

    public string? CardId { get; init; }

    public string? SourceId { get; init; }

    public string? CardInstanceId { get; init; }

    public string? TargetId { get; init; }

    public string? TargetKind { get; init; }

    public int? EnemyBeatIndex { get; init; }

    public int EnemyDamage { get; init; }

    public int PlayerDamage { get; init; }

    public int SuccessfulPlayerActions { get; init; }

    public int SuccessfulEnemyActions { get; init; }

    public int EnergyGeneratedTotal { get; init; }

    public IReadOnlyList<BeatClashEnergyColor> EnergyColors { get; init; } = [];

    public bool ContinuesPreviousTarget { get; init; }

    public bool ReturnToStartBeforeStep { get; init; }
}

public sealed record BeatClashEnergyColor(string Color, int Amount);
