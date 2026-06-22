using System.Text.Json;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed class BeatClashAnimationPlanner
{
    private static readonly JsonSerializerOptions StageJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<BeatClashAnimationStep> Plan(IReadOnlyList<CombatLogEvent> combatLog)
    {
        var energyByCardInstanceTurn = combatLog
            .Where(item => item.EventType == CombatLogEventType.BeatEnergyGenerated)
            .Select(item => new
            {
                item.TurnNumber,
                CardInstanceId = TryGetMetadata(item, "card_instance_id"),
                Color = TryGetMetadata(item, "color"),
                Amount = TryGetNumeric(item, "color_energy_generated")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.CardInstanceId) && item.Amount > 0)
            .GroupBy(item => new BeatClashEnergyKey(item.TurnNumber, item.CardInstanceId!))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<BeatClashEnergyColor>)group
                    .Select(item => new BeatClashEnergyColor(item.Color ?? string.Empty, item.Amount))
                    .ToList());

        var orderedSteps = combatLog
            .Where(item => item.EventType == CombatLogEventType.BeatActionResolved)
            .Select(item => CreateStep(item, energyByCardInstanceTurn))
            .OrderBy(step => step.TurnNumber)
            .ThenBy(step => step.BeatIndex)
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
        IReadOnlyDictionary<BeatClashEnergyKey, IReadOnlyList<BeatClashEnergyColor>> energyByCardInstanceTurn)
    {
        var cardInstanceId = TryGetMetadata(logEvent, "card_instance_id");
        var energyColors = cardInstanceId is not null && energyByCardInstanceTurn.TryGetValue(new BeatClashEnergyKey(logEvent.TurnNumber, cardInstanceId), out var colors)
            ? colors
            : [];
        return new BeatClashAnimationStep
        {
            TurnNumber = logEvent.TurnNumber,
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
            EnergyColors = energyColors,
            ActionStages = CreateActionStages(logEvent)
        };
    }

    private static IReadOnlyList<BeatClashActionStage> CreateActionStages(CombatLogEvent logEvent)
    {
        var stageJson = TryGetMetadata(logEvent, "stage_results");
        if (!string.IsNullOrWhiteSpace(stageJson))
        {
            var stageResults = JsonSerializer.Deserialize<List<BeatClashActionStageLog>>(stageJson, StageJsonOptions) ?? [];
            var parsed = stageResults
                .Select(stage => new BeatClashActionStage
                {
                    StageIndex = stage.StageIndex,
                    PlayerAnimationKind = ToAnimationKind(stage.PlayerActionKind, stage.PlayerAttackType),
                    EnemyDamage = stage.EnemyDamageTaken,
                    PlayerDamage = stage.PlayerDamageTaken,
                    SuccessfulPlayerActions = stage.SuccessfulPlayerActions,
                    SuccessfulEnemyActions = stage.SuccessfulEnemyActions
                })
                .ToList();
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        return
        [
            new BeatClashActionStage
            {
                StageIndex = 0,
                PlayerAnimationKind = logEvent.NumericChanges.TryGetValue("enemy_damage", out var enemyDamage) && enemyDamage > 0
                    ? BeatClashActionAnimationKind.Slash
                    : null,
                EnemyDamage = TryGetNumeric(logEvent, "enemy_damage"),
                PlayerDamage = TryGetNumeric(logEvent, "player_damage"),
                SuccessfulPlayerActions = TryGetNumeric(logEvent, "successful_player_actions"),
                SuccessfulEnemyActions = TryGetNumeric(logEvent, "successful_enemy_actions")
            }
        ];
    }

    private static BeatClashActionAnimationKind? ToAnimationKind(string? actionKind, string? attackType)
    {
        return NormalizeStageToken(actionKind) switch
        {
            "block" => BeatClashActionAnimationKind.Block,
            "dodge" => BeatClashActionAnimationKind.Dodge,
            "attack" => NormalizeStageToken(attackType) switch
            {
                "strike" => BeatClashActionAnimationKind.Strike,
                "projectile" => BeatClashActionAnimationKind.Projectile,
                _ => BeatClashActionAnimationKind.Slash
            },
            _ => null
        };
    }

    private static string NormalizeStageToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
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

internal sealed record BeatClashEnergyKey(int TurnNumber, string CardInstanceId);

public sealed record BeatClashAnimationStep
{
    public int TurnNumber { get; init; }

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

    public IReadOnlyList<BeatClashActionStage> ActionStages { get; init; } = [];

    public bool ContinuesPreviousTarget { get; init; }

    public bool ReturnToStartBeforeStep { get; init; }
}

public sealed record BeatClashEnergyColor(string Color, int Amount);

public sealed record BeatClashActionStage
{
    public int StageIndex { get; init; }

    public BeatClashActionAnimationKind? PlayerAnimationKind { get; init; }

    public int EnemyDamage { get; init; }

    public int PlayerDamage { get; init; }

    public int SuccessfulPlayerActions { get; init; }

    public int SuccessfulEnemyActions { get; init; }
}

internal sealed record BeatClashActionStageLog
{
    public int StageIndex { get; init; }

    public string? PlayerActionKind { get; init; }

    public string? PlayerAttackType { get; init; }

    public int EnemyDamageTaken { get; init; }

    public int PlayerDamageTaken { get; init; }

    public int SuccessfulPlayerActions { get; init; }

    public int SuccessfulEnemyActions { get; init; }
}
