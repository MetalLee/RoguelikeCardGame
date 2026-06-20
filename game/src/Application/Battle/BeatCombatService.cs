using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;

namespace RoguelikeCardGame.Application.Battle;

public sealed record BeatCollisionResult
{
    public int PlayerDamageTaken { get; init; }
    public int EnemyDamageTaken { get; init; }
    public int SuccessfulPlayerActions { get; init; }
    public int SuccessfulEnemyActions { get; init; }
}

public sealed record BeatRoundResolveResult
{
    public required CombatState Combat { get; init; }
    public List<CombatLogEvent> Events { get; init; } = new();
}

public sealed record BeatTargetValidationResult
{
    public bool Succeeded { get; init; }
    public BeatTargetValidationFailureReason? FailureReason { get; init; }
    public string? Message { get; init; }

    public static BeatTargetValidationResult Success() => new() { Succeeded = true };

    public static BeatTargetValidationResult Failure(BeatTargetValidationFailureReason reason, string message) => new()
    {
        FailureReason = reason,
        Message = message
    };
}

public enum BeatTargetValidationFailureReason
{
    PlayerBeatIndexOutOfRange,
    DuplicatePlayerBeatIndex,
    TargetMissing,
    TargetWithoutCard,
    EnemyMissing,
    EnemyBeatIndexMissing,
    EnemyBeatMissing,
    DuplicateEnemyBeatTarget,
    CardIdMissing,
    BodyTargetRequiresAllEnemyBeatsLocked
}

public sealed class BeatCombatService
{
    private readonly Func<int> dodgeRollPercent;

    public BeatCombatService(Func<int>? dodgeRollPercent = null)
    {
        this.dodgeRollPercent = dodgeRollPercent ?? (() => 100);
    }

    public BeatTargetValidationResult ValidatePlayerBeatTargets(BeatRoundState round, CombatState combat)
    {
        var livingEnemyIds = combat.Enemies
            .Where(enemy => enemy.CurrentHp > 0)
            .Select(enemy => enemy.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        var playerBeatIndexes = new HashSet<int>();
        var lockedEnemyBeats = new HashSet<(string EnemyInstanceId, int BeatIndex)>();

        foreach (var playerBeat in round.PlayerBeats)
        {
            if (playerBeat.BeatIndex < 0 || playerBeat.BeatIndex >= round.BeatCount)
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.PlayerBeatIndexOutOfRange,
                    $"Player beat index {playerBeat.BeatIndex} is outside beat count {round.BeatCount}.");
            }

            if (!playerBeatIndexes.Add(playerBeat.BeatIndex))
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.DuplicatePlayerBeatIndex,
                    $"Player beat index {playerBeat.BeatIndex} is assigned more than once.");
            }

            var hasCardInstance = !string.IsNullOrWhiteSpace(playerBeat.CardInstanceId);
            var hasCardId = !string.IsNullOrWhiteSpace(playerBeat.CardId);
            if (hasCardInstance && !hasCardId)
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.CardIdMissing,
                    $"Player beat index {playerBeat.BeatIndex} has a card instance but no card id.");
            }

            var hasCard = hasCardInstance || hasCardId;
            if (!hasCard)
            {
                if (playerBeat.Target is not null)
                {
                    return BeatTargetValidationResult.Failure(
                        BeatTargetValidationFailureReason.TargetWithoutCard,
                        $"Player beat index {playerBeat.BeatIndex} has a target but no card.");
                }

                continue;
            }

            if (playerBeat.Target is null)
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.TargetMissing,
                    $"Player beat index {playerBeat.BeatIndex} has a card but no target.");
            }

            var target = playerBeat.Target;
            if (!livingEnemyIds.Contains(target.EnemyInstanceId))
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.EnemyMissing,
                    $"Enemy '{target.EnemyInstanceId}' is missing or defeated.");
            }

            if (target.Kind != BeatTargetKind.EnemyBeat)
            {
                continue;
            }

            if (target.EnemyBeatIndex is null)
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.EnemyBeatIndexMissing,
                    $"Player beat index {playerBeat.BeatIndex} targets an enemy beat without a beat index.");
            }

            var enemyBeatKey = (target.EnemyInstanceId, target.EnemyBeatIndex.Value);
            if (!round.EnemyBeats.Any(enemyBeat =>
                    string.Equals(enemyBeat.EnemyInstanceId, target.EnemyInstanceId, StringComparison.Ordinal) &&
                    enemyBeat.BeatIndex == target.EnemyBeatIndex.Value))
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.EnemyBeatMissing,
                    $"Enemy '{target.EnemyInstanceId}' has no beat index {target.EnemyBeatIndex.Value} in this round.");
            }

            if (!lockedEnemyBeats.Add(enemyBeatKey))
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.DuplicateEnemyBeatTarget,
                    $"Enemy '{target.EnemyInstanceId}' beat index {target.EnemyBeatIndex.Value} is targeted more than once.");
            }
        }

        foreach (var playerBeat in round.PlayerBeats)
        {
            if (playerBeat.Target?.Kind != BeatTargetKind.EnemyBody)
            {
                continue;
            }

            var enemyBeatKeys = round.EnemyBeats
                .Where(enemyBeat => string.Equals(enemyBeat.EnemyInstanceId, playerBeat.Target.EnemyInstanceId, StringComparison.Ordinal))
                .Select(enemyBeat => (enemyBeat.EnemyInstanceId, enemyBeat.BeatIndex));
            if (enemyBeatKeys.Any(enemyBeatKey => !lockedEnemyBeats.Contains(enemyBeatKey)))
            {
                return BeatTargetValidationResult.Failure(
                    BeatTargetValidationFailureReason.BodyTargetRequiresAllEnemyBeatsLocked,
                    $"Enemy '{playerBeat.Target.EnemyInstanceId}' body cannot be targeted before all enemy beats are locked.");
            }
        }

        return BeatTargetValidationResult.Success();
    }

    public BeatRoundResolveResult ResolveBeatRound(
        CombatState combat,
        IReadOnlyDictionary<string, CardDefinition> cardsById,
        IReadOnlyDictionary<string, EnemyDefinition> enemiesById)
    {
        EnsureCombatNotEnded(combat);

        var round = combat.BeatRound ?? throw new InvalidOperationException("Combat has no beat round to resolve.");
        var validation = ValidatePlayerBeatTargets(round, combat);
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException(validation.Message ?? "Beat round target validation failed.");
        }

        var logStartIndex = combat.Log.Count;
        var working = combat;
        var lockedEnemyBeats = round.PlayerBeats
            .Where(playerBeat =>
                !string.IsNullOrWhiteSpace(playerBeat.CardId) &&
                playerBeat.Target?.Kind == BeatTargetKind.EnemyBeat &&
                playerBeat.Target.EnemyBeatIndex is not null)
            .Select(playerBeat => (playerBeat.Target!.EnemyInstanceId, playerBeat.Target.EnemyBeatIndex!.Value))
            .ToHashSet();

        foreach (var playerBeat in round.PlayerBeats.OrderBy(beat => beat.BeatIndex))
        {
            if (string.IsNullOrWhiteSpace(playerBeat.CardId) || playerBeat.Target is null)
            {
                continue;
            }

            if (!cardsById.TryGetValue(playerBeat.CardId, out var card))
            {
                throw new InvalidOperationException($"Unknown beat card id '{playerBeat.CardId}'.");
            }

            var target = playerBeat.Target;
            var enemyIndex = working.Enemies.FindIndex(enemy => string.Equals(enemy.InstanceId, target.EnemyInstanceId, StringComparison.Ordinal));
            if (enemyIndex < 0 || working.Enemies[enemyIndex].CurrentHp <= 0)
            {
                continue;
            }

            var enemyState = working.Enemies[enemyIndex];
            if (!enemiesById.TryGetValue(enemyState.EnemyId, out var enemyDefinition))
            {
                throw new InvalidOperationException($"Unknown enemy definition id '{enemyState.EnemyId}'.");
            }

            var playerActions = PlayerActionsForTarget(card, target);
            var enemyActions = target.Kind == BeatTargetKind.EnemyBeat
                ? EnemyActionsForBeat(round, target)
                : [];
            var collision = ResolveActionCollision(
                playerActions,
                enemyActions,
                enemyDefinition.Resistances,
                new BeatResistanceProfile());

            working = ApplyBeatCollision(working, enemyIndex, collision);
            working = AppendBeatActionResolvedLog(working, playerBeat, card, target, collision);

            if (collision.SuccessfulPlayerActions > 0)
            {
                var before = working.ColorEnergy.Count;
                var afterPool = working.ColorEnergy.Add(ColorType.Colorless, collision.SuccessfulPlayerActions);
                var accepted = afterPool.Count - before;
                working = working with { ColorEnergy = afterPool };
                if (accepted > 0)
                {
                    working = AppendBeatEnergyGeneratedLog(working, playerBeat, card, accepted, afterPool.Count);
                }
            }
        }

        foreach (var enemyBeat in round.EnemyBeats
            .Where(enemyBeat => !lockedEnemyBeats.Contains((enemyBeat.EnemyInstanceId, enemyBeat.BeatIndex)))
            .OrderBy(enemyBeat => enemyBeat.BeatIndex))
        {
            var enemyIndex = working.Enemies.FindIndex(enemy => string.Equals(enemy.InstanceId, enemyBeat.EnemyInstanceId, StringComparison.Ordinal));
            if (enemyIndex < 0 || working.Enemies[enemyIndex].CurrentHp <= 0)
            {
                continue;
            }

            var enemyState = working.Enemies[enemyIndex];
            if (!enemiesById.TryGetValue(enemyState.EnemyId, out var enemyDefinition))
            {
                throw new InvalidOperationException($"Unknown enemy definition id '{enemyState.EnemyId}'.");
            }

            var collision = ResolveActionCollision(
                [],
                enemyBeat.Actions,
                enemyDefinition.Resistances,
                new BeatResistanceProfile());

            working = ApplyBeatCollision(working, enemyIndex, collision);
            working = AppendUnblockedEnemyBeatResolvedLog(working, enemyBeat, collision);
        }

        working = ApplyBeatOutcome(working);
        if (working.Status is not CombatStatus.Victory and not CombatStatus.Defeat)
        {
            working = working with { Status = CombatStatus.EnemyTurn };
        }

        return new BeatRoundResolveResult
        {
            Combat = working,
            Events = working.Log.Skip(logStartIndex).ToList()
        };
    }

    public BeatRoundResolveResult ReleaseSlottedFinisher(
        CombatState combat,
        CardDefinition finisher,
        string targetEnemyInstanceId)
    {
        EnsureCombatNotEnded(combat);

        var round = combat.BeatRound ?? throw new InvalidOperationException("Combat has no beat round for slotted finisher release.");
        if (!string.Equals(round.FinisherSlot.CardId, finisher.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Slotted finisher '{round.FinisherSlot.CardId}' does not match requested finisher '{finisher.Id}'.");
        }

        if (finisher.Type != CardType.Finisher)
        {
            throw new InvalidOperationException($"Slotted card '{finisher.Id}' must be a finisher to release from the finisher slot.");
        }

        if (finisher.ColorEnergyCost is null)
        {
            throw new InvalidOperationException($"Finisher '{finisher.Id}' has no color energy cost.");
        }

        var spendAmount = SpendAmount(finisher.ColorEnergyCost, combat.ColorEnergy);
        if (!combat.ColorEnergy.CanSpend(finisher.ColorEnergyCost.Mode, spendAmount, finisher.ColorEnergyCost.MinAmount))
        {
            throw new InvalidOperationException($"Not enough color energy to release finisher '{finisher.Id}'.");
        }

        var targetIndex = combat.Enemies.FindIndex(enemy => string.Equals(enemy.InstanceId, targetEnemyInstanceId, StringComparison.Ordinal));
        if (targetIndex < 0 || combat.Enemies[targetIndex].CurrentHp <= 0)
        {
            throw new InvalidOperationException($"Target enemy '{targetEnemyInstanceId}' is missing or defeated.");
        }

        var logStartIndex = combat.Log.Count;
        var spent = combat.ColorEnergy.Spend(finisher.ColorEnergyCost.Mode, spendAmount, finisher.ColorEnergyCost.MinAmount);
        var damage = finisher.Effects
            .Where(effect => string.Equals(effect.Type, "damage", StringComparison.Ordinal))
            .Sum(effect => effect.Value ?? 0);
        var enemies = combat.Enemies.ToList();
        var target = enemies[targetIndex];
        enemies[targetIndex] = target with
        {
            CurrentHp = Math.Max(0, target.CurrentHp - damage)
        };

        var working = combat with
        {
            ColorEnergy = spent.Pool,
            Enemies = enemies
        };
        working = AppendFinisherReleasedLog(working, finisher, round.FinisherSlot, targetEnemyInstanceId, damage, spent.Spent.Count);
        working = ApplyBeatOutcome(working);

        return new BeatRoundResolveResult
        {
            Combat = working,
            Events = working.Log.Skip(logStartIndex).ToList()
        };
    }

    public BeatCollisionResult ResolveActionCollision(
        IReadOnlyList<BeatActionDefinition> playerActions,
        IReadOnlyList<BeatActionDefinition> enemyActions,
        BeatResistanceProfile enemyResistance,
        BeatResistanceProfile playerResistance)
    {
        var playerQueue = playerActions.SelectMany(action => action.ExpandRepeats()).ToList();
        var enemyQueue = enemyActions.SelectMany(action => action.ExpandRepeats()).ToList();
        var max = Math.Max(playerQueue.Count, enemyQueue.Count);
        var playerDamage = 0;
        var enemyDamage = 0;
        var successfulPlayer = 0;
        var successfulEnemy = 0;

        for (var index = 0; index < max; index++)
        {
            var player = index < playerQueue.Count ? playerQueue[index] : null;
            var enemy = index < enemyQueue.Count ? enemyQueue[index] : null;
            var exchange = ResolveActionPair(player, enemy, enemyResistance, playerResistance);
            playerDamage += exchange.PlayerDamageTaken;
            enemyDamage += exchange.EnemyDamageTaken;
            successfulPlayer += exchange.SuccessfulPlayerActions;
            successfulEnemy += exchange.SuccessfulEnemyActions;
        }

        return new BeatCollisionResult
        {
            PlayerDamageTaken = playerDamage,
            EnemyDamageTaken = enemyDamage,
            SuccessfulPlayerActions = successfulPlayer,
            SuccessfulEnemyActions = successfulEnemy
        };
    }

    private BeatCollisionResult ResolveActionPair(
        BeatActionDefinition? player,
        BeatActionDefinition? enemy,
        BeatResistanceProfile enemyResistance,
        BeatResistanceProfile playerResistance)
    {
        if (player is null && enemy is null)
        {
            return new BeatCollisionResult();
        }

        if (player is null)
        {
            return ResolveUnopposedEnemy(enemy!, playerResistance);
        }

        if (enemy is null)
        {
            return ResolveUnopposedPlayer(player, enemyResistance);
        }

        if (player.Kind == BeatActionKind.Attack && enemy.Kind == BeatActionKind.Attack)
        {
            return ResolveAttackVsAttack(player, enemy, enemyResistance, playerResistance);
        }

        if (player.Kind == BeatActionKind.Attack && enemy.Kind == BeatActionKind.Block)
        {
            var remaining = DamageAfterBlock(player, enemyResistance, enemy);
            return new BeatCollisionResult
            {
                EnemyDamageTaken = remaining,
                SuccessfulPlayerActions = remaining > 0 ? 1 : 0,
                SuccessfulEnemyActions = enemy.Value > 0 ? 1 : 0
            };
        }

        if (player.Kind == BeatActionKind.Block && enemy.Kind == BeatActionKind.Attack)
        {
            var remaining = DamageAfterBlock(enemy, playerResistance, player);
            return new BeatCollisionResult
            {
                PlayerDamageTaken = remaining,
                SuccessfulPlayerActions = player.Value > 0 ? 1 : 0,
                SuccessfulEnemyActions = remaining > 0 ? 1 : 0
            };
        }

        if (player.Kind == BeatActionKind.Dodge && enemy.Kind == BeatActionKind.Attack)
        {
            var dodged = dodgeRollPercent() <= player.DodgeChancePercent;
            return new BeatCollisionResult
            {
                PlayerDamageTaken = dodged ? 0 : DamageAgainstResistance(enemy, playerResistance),
                SuccessfulPlayerActions = dodged ? 1 : 0,
                SuccessfulEnemyActions = dodged ? 0 : 1
            };
        }

        if (player.Kind == BeatActionKind.Attack && enemy.Kind == BeatActionKind.Dodge)
        {
            var dodged = dodgeRollPercent() <= enemy.DodgeChancePercent;
            return new BeatCollisionResult
            {
                EnemyDamageTaken = dodged ? 0 : DamageAgainstResistance(player, enemyResistance),
                SuccessfulPlayerActions = dodged ? 0 : 1,
                SuccessfulEnemyActions = dodged ? 1 : 0
            };
        }

        return new BeatCollisionResult();
    }

    private static BeatCollisionResult ResolveAttackVsAttack(
        BeatActionDefinition player,
        BeatActionDefinition enemy,
        BeatResistanceProfile enemyResistance,
        BeatResistanceProfile playerResistance)
    {
        return new BeatCollisionResult
        {
            EnemyDamageTaken = DamageAgainstResistance(player, enemyResistance),
            PlayerDamageTaken = DamageAgainstResistance(enemy, playerResistance),
            SuccessfulPlayerActions = 1,
            SuccessfulEnemyActions = 1
        };
    }

    private static BeatCollisionResult ResolveUnopposedPlayer(BeatActionDefinition player, BeatResistanceProfile enemyResistance)
    {
        if (player.Kind != BeatActionKind.Attack)
        {
            return new BeatCollisionResult();
        }

        return new BeatCollisionResult
        {
            EnemyDamageTaken = DamageAgainstResistance(player, enemyResistance),
            SuccessfulPlayerActions = 1
        };
    }

    private static BeatCollisionResult ResolveUnopposedEnemy(BeatActionDefinition enemy, BeatResistanceProfile playerResistance)
    {
        if (enemy.Kind != BeatActionKind.Attack)
        {
            return new BeatCollisionResult();
        }

        return new BeatCollisionResult
        {
            PlayerDamageTaken = DamageAgainstResistance(enemy, playerResistance),
            SuccessfulEnemyActions = 1
        };
    }

    private static int DamageAgainstResistance(BeatActionDefinition action, BeatResistanceProfile resistance)
    {
        var attackType = action.AttackType ?? BeatAttackType.Slash;
        return BeatResistanceProfile.Apply(resistance.GradeFor(attackType), action.Value);
    }

    private static int DamageAfterBlock(
        BeatActionDefinition attack,
        BeatResistanceProfile defenderResistance,
        BeatActionDefinition block)
    {
        return Math.Max(0, DamageAgainstResistance(attack, defenderResistance) - block.Value);
    }

    private static IReadOnlyList<BeatActionDefinition> PlayerActionsForTarget(CardDefinition card, BeatTarget target)
    {
        return target.Kind == BeatTargetKind.EnemyBody
            ? card.BeatActions.Where(action => action.Kind == BeatActionKind.Attack).ToList()
            : card.BeatActions;
    }

    private static IReadOnlyList<BeatActionDefinition> EnemyActionsForBeat(BeatRoundState round, BeatTarget target)
    {
        return round.EnemyBeats
            .FirstOrDefault(enemyBeat =>
                string.Equals(enemyBeat.EnemyInstanceId, target.EnemyInstanceId, StringComparison.Ordinal) &&
                enemyBeat.BeatIndex == target.EnemyBeatIndex)
            ?.Actions ?? [];
    }

    private static CombatState ApplyBeatCollision(CombatState combat, int enemyIndex, BeatCollisionResult collision)
    {
        var enemies = combat.Enemies.ToList();
        var target = enemies[enemyIndex];
        enemies[enemyIndex] = target with
        {
            CurrentHp = Math.Max(0, target.CurrentHp - collision.EnemyDamageTaken)
        };

        return combat with
        {
            PlayerHp = Math.Max(0, combat.PlayerHp - collision.PlayerDamageTaken),
            Enemies = enemies
        };
    }

    private static CombatState AppendBeatActionResolvedLog(
        CombatState combat,
        PlayerBeatSlot playerBeat,
        CardDefinition card,
        BeatTarget target,
        BeatCollisionResult collision)
    {
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_beat_{playerBeat.BeatIndex}_resolved_{log.Count + 1}",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = card.Id,
            TargetIds = [target.EnemyInstanceId],
            NumericChanges = new Dictionary<string, int>
            {
                ["beat_index"] = playerBeat.BeatIndex,
                ["enemy_damage"] = collision.EnemyDamageTaken,
                ["player_damage"] = collision.PlayerDamageTaken,
                ["successful_player_actions"] = collision.SuccessfulPlayerActions,
                ["successful_enemy_actions"] = collision.SuccessfulEnemyActions
            },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = playerBeat.CardInstanceId ?? string.Empty,
                ["target_kind"] = target.Kind.ToString(),
                ["enemy_beat_index"] = target.EnemyBeatIndex?.ToString() ?? string.Empty
            }
        });

        return combat with { Log = log };
    }

    private static CombatState AppendUnblockedEnemyBeatResolvedLog(
        CombatState combat,
        EnemyBeatSlot enemyBeat,
        BeatCollisionResult collision)
    {
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_enemy_beat_{enemyBeat.EnemyInstanceId}_{enemyBeat.BeatIndex}_resolved_{log.Count + 1}",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = enemyBeat.ActionCardId,
            TargetIds = ["player"],
            NumericChanges = new Dictionary<string, int>
            {
                ["beat_index"] = enemyBeat.BeatIndex,
                ["player_damage"] = collision.PlayerDamageTaken,
                ["successful_enemy_actions"] = collision.SuccessfulEnemyActions
            },
            Metadata = new Dictionary<string, string>
            {
                ["enemy_instance_id"] = enemyBeat.EnemyInstanceId,
                ["target_kind"] = "Player"
            }
        });

        return combat with { Log = log };
    }

    private static CombatState AppendBeatEnergyGeneratedLog(
        CombatState combat,
        PlayerBeatSlot playerBeat,
        CardDefinition card,
        int accepted,
        int colorEnergyAfter)
    {
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_beat_{playerBeat.BeatIndex}_energy_{log.Count + 1}",
            EventType = CombatLogEventType.BeatEnergyGenerated,
            TurnNumber = combat.TurnNumber,
            SourceId = card.Id,
            TargetIds = ["color_energy_pool"],
            NumericChanges = new Dictionary<string, int>
            {
                ["color_energy_generated"] = accepted,
                ["color_energy_after"] = colorEnergyAfter
            },
            Metadata = new Dictionary<string, string>
            {
                ["color"] = ColorType.Colorless.ToString(),
                ["card_instance_id"] = playerBeat.CardInstanceId ?? string.Empty
            }
        });

        return combat with { Log = log };
    }

    private static CombatState AppendFinisherReleasedLog(
        CombatState combat,
        CardDefinition finisher,
        FinisherSlotState slot,
        string targetEnemyInstanceId,
        int damage,
        int spentEnergy)
    {
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_finisher_{finisher.Id}_{log.Count + 1}",
            EventType = CombatLogEventType.FinisherReleased,
            TurnNumber = combat.TurnNumber,
            SourceId = finisher.Id,
            TargetIds = [targetEnemyInstanceId],
            NumericChanges = new Dictionary<string, int>
            {
                ["damage"] = damage,
                ["color_energy_spent"] = spentEnergy,
                ["color_energy_after"] = combat.ColorEnergy.Count
            },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = slot.CardInstanceId ?? string.Empty,
                ["finisher_attack_type"] = finisher.FinisherAttackType?.ToString() ?? string.Empty
            }
        });

        return combat with { Log = log };
    }

    private static int SpendAmount(ColorEnergyCost cost, ColorEnergyPool pool)
    {
        return cost.Mode switch
        {
            ColorEnergySpendMode.Fixed => cost.Amount,
            ColorEnergySpendMode.X => cost.Amount > 0 ? cost.Amount : pool.Count,
            ColorEnergySpendMode.All => pool.Count,
            _ => 0
        };
    }

    private static void EnsureCombatNotEnded(CombatState combat)
    {
        if (combat.Status is CombatStatus.Victory or CombatStatus.Defeat)
        {
            throw new InvalidOperationException($"Combat '{combat.CombatId}' has already ended with status {combat.Status}.");
        }
    }

    private static CombatState ApplyBeatOutcome(CombatState combat)
    {
        if (combat.PlayerHp <= 0 && combat.Status != CombatStatus.Defeat)
        {
            return AppendCombatEndedLog(combat with { Status = CombatStatus.Defeat }, CombatStatus.Defeat);
        }

        if (combat.Enemies.Count > 0 && combat.Enemies.All(enemy => enemy.CurrentHp <= 0) && combat.Status != CombatStatus.Victory)
        {
            return AppendCombatEndedLog(combat with { Status = CombatStatus.Victory }, CombatStatus.Victory);
        }

        return combat;
    }

    private static CombatState AppendCombatEndedLog(CombatState combat, CombatStatus status)
    {
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_combat_ended_{status.ToString().ToLowerInvariant()}_{log.Count + 1}",
            EventType = CombatLogEventType.CombatEnded,
            TurnNumber = combat.TurnNumber,
            NumericChanges = new Dictionary<string, int>
            {
                ["player_hp"] = combat.PlayerHp
            },
            Metadata = new Dictionary<string, string>
            {
                ["status"] = status.ToString()
            }
        });

        return combat with { Log = log };
    }
}
