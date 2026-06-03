using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;

namespace RoguelikeCardGame.Application.Battle;

public enum PlayCardFailureReason
{
    None,
    NotPlayerTurn,
    CardNotInHand,
    InsufficientActionPoints,
    InsufficientChain,
    TargetMissing
}

public sealed record PlayCardResult
{
    public bool Succeeded { get; init; }

    public required CombatState Combat { get; init; }

    public PlayCardFailureReason FailureReason { get; init; }

    public string? FailureMessageKey { get; init; }

    public int? RequiredActionPoints { get; init; }

    public int? CurrentActionPoints { get; init; }

    public int? RequiredChain { get; init; }

    public int? CurrentChain { get; init; }

    public string? RequiredTargetRule { get; init; }

    public List<CombatLogEvent> Events { get; init; } = new();

    public static PlayCardResult Success(CombatState combat, IEnumerable<CombatLogEvent> events) => new()
    {
        Succeeded = true,
        Combat = combat,
        FailureReason = PlayCardFailureReason.None,
        Events = events.ToList()
    };

    public static PlayCardResult Failure(
        CombatState combat,
        PlayCardFailureReason reason,
        string messageKey,
        int? requiredActionPoints = null,
        int? currentActionPoints = null,
        int? requiredChain = null,
        int? currentChain = null,
        TargetRule? requiredTargetRule = null) => new()
    {
        Succeeded = false,
        Combat = combat,
        FailureReason = reason,
        FailureMessageKey = messageKey,
        RequiredActionPoints = requiredActionPoints,
        CurrentActionPoints = currentActionPoints,
        RequiredChain = requiredChain,
        CurrentChain = currentChain,
        RequiredTargetRule = requiredTargetRule?.ToString()
    };
}

public sealed class CardPlayService
{
    private readonly CombatTurnService turnService;

    public CardPlayService(CombatTurnService? turnService = null)
    {
        this.turnService = turnService ?? new CombatTurnService();
    }

    public PlayCardResult CanPlayCard(CombatState combat, CardDefinition card, string? targetEnemyInstanceId = null)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(card);

        if (combat.Status != CombatStatus.PlayerTurn)
        {
            return PlayCardResult.Failure(combat, PlayCardFailureReason.NotPlayerTurn, "ui.play_card.not_player_turn");
        }

        if (!combat.DeckZones.Hand.Contains(card.Id))
        {
            return PlayCardResult.Failure(combat, PlayCardFailureReason.CardNotInHand, "ui.play_card.card_not_in_hand");
        }

        if (card.Type == CardType.Action && combat.ActionPoints < card.Cost)
        {
            return PlayCardResult.Failure(
                combat,
                PlayCardFailureReason.InsufficientActionPoints,
                "ui.play_card.insufficient_action_points",
                requiredActionPoints: card.Cost,
                currentActionPoints: combat.ActionPoints);
        }

        if (card.Type == CardType.Finisher)
        {
            var requiredChain = card.MinChain ?? 0;
            if (requiredChain <= 0 || combat.Chain < requiredChain)
            {
                return PlayCardResult.Failure(
                    combat,
                    PlayCardFailureReason.InsufficientChain,
                    "ui.play_card.insufficient_chain",
                    requiredChain: requiredChain,
                    currentChain: combat.Chain);
            }
        }

        if (!TryResolveTargets(combat, card.TargetRule, targetEnemyInstanceId, out _))
        {
            return PlayCardResult.Failure(
                combat,
                PlayCardFailureReason.TargetMissing,
                "ui.play_card.target_missing",
                requiredTargetRule: card.TargetRule);
        }

        return PlayCardResult.Success(combat, []);
    }

    public PlayCardResult PlayCard(CombatState combat, CardDefinition card, string? targetEnemyInstanceId = null)
    {
        var canPlay = CanPlayCard(combat, card, targetEnemyInstanceId);
        if (!canPlay.Succeeded)
        {
            return canPlay with { Events = [CreateRejectedEvent(combat, card, canPlay)] };
        }

        var logStartIndex = combat.Log.Count;
        var chainBeforePlay = combat.Chain;
        var hand = combat.DeckZones.Hand.ToList();
        hand.Remove(card.Id);

        var working = combat with
        {
            ActionPoints = card.Type == CardType.Action ? combat.ActionPoints - card.Cost : combat.ActionPoints,
            DeckZones = combat.DeckZones with
            {
                Hand = hand
            }
        };

        var log = working.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{working.CombatId}_turn_{working.TurnNumber}_play_{card.Id}_{log.Count + 1}",
            EventType = CombatLogEventType.CardPlayed,
            TurnNumber = working.TurnNumber,
            SourceId = card.Id,
            TargetIds = TargetIdsForLog(working, card.TargetRule, targetEnemyInstanceId),
            NumericChanges = new Dictionary<string, int>
            {
                ["action_points_before"] = combat.ActionPoints,
                ["action_points_after"] = working.ActionPoints,
                ["chain_before"] = combat.Chain
            },
            Metadata = new Dictionary<string, string>
            {
                ["card_type"] = card.Type.ToString()
            }
        });
        working = working with { Log = log };

        foreach (var effect in card.Effects)
        {
            working = ResolveEffect(working, effect, card, targetEnemyInstanceId, chainBeforePlay);
        }

        var chainAfterPlay = ApplyDefaultChainChange(working.Chain, card.DefaultChainChange);
        var discardPile = working.DeckZones.DiscardPile.ToList();
        discardPile.Add(card.Id);
        log = working.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{working.CombatId}_turn_{working.TurnNumber}_chain_{card.Id}_{log.Count + 1}",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = working.TurnNumber,
            SourceId = card.Id,
            NumericChanges = new Dictionary<string, int>
            {
                ["chain_before"] = working.Chain,
                ["chain_after"] = chainAfterPlay
            },
            Metadata = new Dictionary<string, string>
            {
                ["effect_type"] = "default_chain_change"
            }
        });

        var updated = working with
        {
            Chain = chainAfterPlay,
            DeckZones = working.DeckZones with
            {
                DiscardPile = discardPile
            },
            Log = log
        };

        updated = AppendCombatOutcomeLogs(updated);
        return PlayCardResult.Success(updated, updated.Log.Skip(logStartIndex));
    }

    private CombatState ResolveEffect(
        CombatState combat,
        EffectDefinition effect,
        CardDefinition card,
        string? targetEnemyInstanceId,
        int chainBeforePlay)
    {
        return effect.Type switch
        {
            "damage" => ResolveDamage(combat, effect, card, targetEnemyInstanceId),
            "block" or "gain_block" => ResolveBlock(combat, effect, card),
            "draw_cards" => ResolveDrawCards(combat, effect, card),
            "gain_action_points" => ResolveGainActionPoints(combat, effect, card),
            "temporary_discount" => ResolveTemporaryDiscountPlaceholder(combat, effect, card),
            "chain_threshold_bonus" => ResolveThresholdBonus(combat, effect, card, targetEnemyInstanceId, chainBeforePlay),
            _ => ResolveUnsupportedPlaceholder(combat, effect, card)
        };
    }

    private CombatState ResolveDamage(
        CombatState combat,
        EffectDefinition effect,
        CardDefinition card,
        string? targetEnemyInstanceId)
    {
        var damage = Math.Max(0, effect.Value ?? 0);
        var targetIds = ResolveEffectTargets(combat, effect, card.TargetRule, targetEnemyInstanceId);
        var enemies = combat.Enemies.ToList();
        var totalHpDamage = 0;
        var totalBlockedDamage = 0;

        foreach (var targetId in targetIds)
        {
            var index = enemies.FindIndex(enemy => enemy.InstanceId == targetId);
            if (index < 0)
            {
                continue;
            }

            var enemy = enemies[index];
            var blocked = Math.Min(enemy.Block, damage);
            var hpDamage = Math.Max(0, damage - blocked);
            enemies[index] = enemy with
            {
                Block = enemy.Block - blocked,
                CurrentHp = Math.Max(0, enemy.CurrentHp - hpDamage)
            };
            totalBlockedDamage += blocked;
            totalHpDamage += hpDamage;
        }

        return AppendEffectLog(combat with { Enemies = enemies }, card.Id, targetIds, new Dictionary<string, int>
        {
            ["damage"] = damage,
            ["hp_damage"] = totalHpDamage,
            ["blocked_damage"] = totalBlockedDamage
        }, "damage");
    }

    private CombatState ResolveBlock(CombatState combat, EffectDefinition effect, CardDefinition card)
    {
        var block = Math.Max(0, effect.Value ?? 0);
        var updated = combat with
        {
            PlayerBlock = combat.PlayerBlock + block
        };

        return AppendEffectLog(updated, card.Id, ["player"], new Dictionary<string, int>
        {
            ["block_gained"] = block,
            ["player_block_after"] = updated.PlayerBlock
        }, effect.Type);
    }

    private CombatState ResolveDrawCards(CombatState combat, EffectDefinition effect, CardDefinition card)
    {
        var beforeLogCount = combat.Log.Count;
        var updated = turnService.DrawCards(combat, Math.Max(0, effect.Value ?? 0));
        return AppendEffectLog(updated, card.Id, updated.Log.Skip(beforeLogCount).SelectMany(item => item.TargetIds), new Dictionary<string, int>
        {
            ["draw_requested"] = Math.Max(0, effect.Value ?? 0)
        }, "draw_cards");
    }

    private CombatState ResolveGainActionPoints(CombatState combat, EffectDefinition effect, CardDefinition card)
    {
        var actionPoints = Math.Max(0, effect.Value ?? 0);
        var updated = combat with
        {
            ActionPoints = combat.ActionPoints + actionPoints
        };

        return AppendEffectLog(updated, card.Id, ["player"], new Dictionary<string, int>
        {
            ["action_points_gained"] = actionPoints,
            ["action_points_after"] = updated.ActionPoints
        }, "gain_action_points");
    }

    private CombatState ResolveTemporaryDiscountPlaceholder(CombatState combat, EffectDefinition effect, CardDefinition card)
    {
        return AppendEffectLog(combat, card.Id, ["hand"], new Dictionary<string, int>
        {
            ["discount_amount"] = Math.Max(0, effect.Value ?? 0)
        }, "temporary_discount_placeholder");
    }

    private CombatState ResolveThresholdBonus(
        CombatState combat,
        EffectDefinition effect,
        CardDefinition card,
        string? targetEnemyInstanceId,
        int chainBeforePlay)
    {
        var threshold = effect.Threshold ?? int.MaxValue;
        if (chainBeforePlay < threshold || effect.Effect is null)
        {
            return AppendEffectLog(combat, card.Id, [], new Dictionary<string, int>
            {
                ["threshold"] = threshold,
                ["chain_before_play"] = chainBeforePlay,
                ["triggered"] = 0
            }, "chain_threshold_bonus");
        }

        var resolved = ResolveEffect(combat, effect.Effect, card, targetEnemyInstanceId, chainBeforePlay);
        return AppendEffectLog(resolved, card.Id, [], new Dictionary<string, int>
        {
            ["threshold"] = threshold,
            ["chain_before_play"] = chainBeforePlay,
            ["triggered"] = 1
        }, "chain_threshold_bonus");
    }

    private CombatState ResolveUnsupportedPlaceholder(CombatState combat, EffectDefinition effect, CardDefinition card)
    {
        return AppendEffectLog(combat, card.Id, [], new Dictionary<string, int>(), $"unsupported_placeholder:{effect.Type}");
    }

    private CombatState AppendEffectLog(
        CombatState combat,
        string sourceId,
        IEnumerable<string> targetIds,
        Dictionary<string, int> numericChanges,
        string effectType)
    {
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_effect_{log.Count + 1}",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = sourceId,
            TargetIds = targetIds.Distinct().ToList(),
            NumericChanges = numericChanges,
            Metadata = new Dictionary<string, string>
            {
                ["effect_type"] = effectType
            }
        });

        return combat with { Log = log };
    }

    private static CombatState AppendCombatOutcomeLogs(CombatState combat)
    {
        var log = combat.Log.ToList();
        var loggedDeaths = log
            .Where(item => item.EventType == CombatLogEventType.EnemyDied)
            .SelectMany(item => item.TargetIds)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var enemy in combat.Enemies.Where(enemy => enemy.CurrentHp <= 0 && !loggedDeaths.Contains(enemy.InstanceId)))
        {
            log.Add(new CombatLogEvent
            {
                EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_enemy_died_{enemy.InstanceId}_{log.Count + 1}",
                EventType = CombatLogEventType.EnemyDied,
                TurnNumber = combat.TurnNumber,
                SourceId = enemy.EnemyId,
                TargetIds = [enemy.InstanceId],
                NumericChanges = new Dictionary<string, int>
                {
                    ["current_hp"] = enemy.CurrentHp
                }
            });
        }

        if (combat.Enemies.Count > 0 && combat.Enemies.All(enemy => enemy.CurrentHp <= 0) && combat.Status != CombatStatus.Victory)
        {
            log.Add(new CombatLogEvent
            {
                EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_combat_ended_victory",
                EventType = CombatLogEventType.CombatEnded,
                TurnNumber = combat.TurnNumber,
                NumericChanges = new Dictionary<string, int>
                {
                    ["player_hp"] = combat.PlayerHp
                },
                Metadata = new Dictionary<string, string>
                {
                    ["status"] = CombatStatus.Victory.ToString()
                }
            });

            return combat with
            {
                Status = CombatStatus.Victory,
                Log = log
            };
        }

        return combat with { Log = log };
    }

    private static CombatLogEvent CreateRejectedEvent(CombatState combat, CardDefinition card, PlayCardResult failure)
    {
        var numericChanges = new Dictionary<string, int>();
        if (failure.RequiredActionPoints is not null)
        {
            numericChanges["required_action_points"] = failure.RequiredActionPoints.Value;
        }

        if (failure.CurrentActionPoints is not null)
        {
            numericChanges["current_action_points"] = failure.CurrentActionPoints.Value;
        }

        if (failure.RequiredChain is not null)
        {
            numericChanges["required_chain"] = failure.RequiredChain.Value;
        }

        if (failure.CurrentChain is not null)
        {
            numericChanges["current_chain"] = failure.CurrentChain.Value;
        }

        return new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_reject_{card.Id}_{combat.Log.Count + 1}",
            EventType = CombatLogEventType.CardPlayRejected,
            TurnNumber = combat.TurnNumber,
            SourceId = card.Id,
            MessageKey = failure.FailureMessageKey,
            NumericChanges = numericChanges,
            Metadata = new Dictionary<string, string>
            {
                ["failure_reason"] = failure.FailureReason.ToString(),
                ["required_target_rule"] = failure.RequiredTargetRule ?? string.Empty
            }
        };
    }

    private static int ApplyDefaultChainChange(int currentChain, ChainChange chainChange)
    {
        return chainChange.Mode switch
        {
            ChainChangeMode.FixedDelta => Math.Max(0, currentChain + chainChange.Amount),
            ChainChangeMode.ConsumeAll => 0,
            _ => currentChain
        };
    }

    private static bool TryResolveTargets(
        CombatState combat,
        TargetRule targetRule,
        string? targetEnemyInstanceId,
        out List<string> targetIds)
    {
        targetIds = TargetIdsForLog(combat, targetRule, targetEnemyInstanceId);
        return targetRule switch
        {
            TargetRule.SingleEnemy => targetEnemyInstanceId is not null &&
                                      combat.Enemies.Any(enemy => enemy.InstanceId == targetEnemyInstanceId && enemy.CurrentHp > 0),
            TargetRule.AllEnemies => combat.Enemies.Any(enemy => enemy.CurrentHp > 0),
            TargetRule.Self or TargetRule.None => true,
            _ => false
        };
    }

    private static List<string> ResolveEffectTargets(
        CombatState combat,
        EffectDefinition effect,
        TargetRule cardTargetRule,
        string? targetEnemyInstanceId)
    {
        return effect.Target switch
        {
            "all_enemies" => combat.Enemies.Where(enemy => enemy.CurrentHp > 0).Select(enemy => enemy.InstanceId).ToList(),
            "selected_enemy" => targetEnemyInstanceId is null ? [] : [targetEnemyInstanceId],
            "self" => ["player"],
            _ => TargetIdsForLog(combat, cardTargetRule, targetEnemyInstanceId)
        };
    }

    private static List<string> TargetIdsForLog(
        CombatState combat,
        TargetRule targetRule,
        string? targetEnemyInstanceId)
    {
        return targetRule switch
        {
            TargetRule.SingleEnemy => targetEnemyInstanceId is null ? [] : [targetEnemyInstanceId],
            TargetRule.AllEnemies => combat.Enemies.Where(enemy => enemy.CurrentHp > 0).Select(enemy => enemy.InstanceId).ToList(),
            TargetRule.Self => ["player"],
            TargetRule.None => [],
            _ => []
        };
    }
}
