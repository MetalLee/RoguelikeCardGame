using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Application.Battle;

public sealed class CombatTurnService
{
    private readonly Func<IReadOnlyList<string>, IReadOnlyList<string>> shuffleDiscardIntoDraw;

    public CombatTurnService(Func<IReadOnlyList<string>, IReadOnlyList<string>>? shuffleDiscardIntoDraw = null)
    {
        this.shuffleDiscardIntoDraw = shuffleDiscardIntoDraw ?? (cards => cards.ToList());
    }

    public CombatState StartCombat(CombatState combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        if (combat.Status != CombatStatus.NotStarted)
        {
            throw new InvalidOperationException("Combat can only be started from NotStarted status.");
        }

        return StartPlayerTurn(combat, turnNumber: 1, includeNewTurnPreparedEvent: false);
    }

    public CombatState DrawCards(CombatState combat, int count)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var log = combat.Log.ToList();
        var (deckZones, drawEvents) = DrawCards(combat.DeckZones, count, combat.CombatId, combat.TurnNumber);
        log.AddRange(drawEvents);

        return combat with
        {
            DeckZones = deckZones,
            Log = log
        };
    }

    public CombatState EndPlayerTurn(CombatState combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        if (combat.Status != CombatStatus.PlayerTurn)
        {
            throw new InvalidOperationException("Only an active player turn can be ended.");
        }

        var hand = combat.DeckZones.Hand.ToList();
        var discardPile = combat.DeckZones.DiscardPile.Concat(hand).ToList();
        var log = combat.Log.ToList();

        if (hand.Count > 0)
        {
            log.Add(new CombatLogEvent
            {
                EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_discard_hand",
                EventType = CombatLogEventType.CardsDiscarded,
                TurnNumber = combat.TurnNumber,
                TargetIds = hand,
                NumericChanges = new Dictionary<string, int>
                {
                    ["discarded_count"] = hand.Count
                }
            });
        }

        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_player_turn_ended",
            EventType = CombatLogEventType.PlayerTurnEnded,
            TurnNumber = combat.TurnNumber,
            NumericChanges = new Dictionary<string, int>
            {
                ["action_points_before_clear"] = combat.ActionPoints,
                ["chain_before_clear"] = combat.Chain
            }
        });

        return combat with
        {
            Status = CombatStatus.EnemyTurn,
            ActionPoints = 0,
            Chain = 0,
            DeckZones = combat.DeckZones with
            {
                Hand = [],
                DiscardPile = discardPile
            },
            Log = log
        };
    }

    public CombatState ResolveEnemyTurnPlaceholder(CombatState combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        if (combat.Status != CombatStatus.EnemyTurn)
        {
            throw new InvalidOperationException("Enemy turn placeholder can only resolve during EnemyTurn status.");
        }

        var enemies = combat.Enemies
            .Select(enemy => enemy with { IntentIndex = enemy.IntentIndex + 1 })
            .ToList();
        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_enemy_placeholder",
            EventType = CombatLogEventType.EnemyTurnPlaceholderResolved,
            TurnNumber = combat.TurnNumber,
            Metadata = new Dictionary<string, string>
            {
                ["note"] = "Enemy action resolution is a placeholder for MVP task 4."
            }
        });

        return combat with
        {
            Enemies = enemies,
            Log = log
        };
    }

    public CombatState PrepareNextPlayerTurn(CombatState combat)
    {
        ArgumentNullException.ThrowIfNull(combat);
        if (combat.Status != CombatStatus.EnemyTurn)
        {
            throw new InvalidOperationException("Next player turn can only be prepared after player turn has ended.");
        }

        return StartPlayerTurn(combat, combat.TurnNumber + 1, includeNewTurnPreparedEvent: true);
    }

    private CombatState StartPlayerTurn(CombatState combat, int turnNumber, bool includeNewTurnPreparedEvent)
    {
        var log = combat.Log.ToList();
        if (includeNewTurnPreparedEvent)
        {
            log.Add(new CombatLogEvent
            {
                EventId = $"{combat.CombatId}_turn_{turnNumber}_prepared",
                EventType = CombatLogEventType.NewTurnPrepared,
                TurnNumber = turnNumber,
                NumericChanges = new Dictionary<string, int>
                {
                    ["cleared_player_block"] = combat.PlayerBlock
                }
            });
        }

        var stateBeforeDraw = combat with
        {
            Status = CombatStatus.PlayerTurn,
            TurnNumber = turnNumber,
            PlayerBlock = 0,
            ActionPoints = combat.BaseActionPoints,
            Chain = 0,
            Log = log
        };

        var drawn = DrawCards(stateBeforeDraw, stateBeforeDraw.CardsPerTurn);
        var updatedLog = drawn.Log.ToList();
        updatedLog.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{turnNumber}_started",
            EventType = CombatLogEventType.TurnStarted,
            TurnNumber = turnNumber,
            NumericChanges = new Dictionary<string, int>
            {
                ["action_points"] = drawn.ActionPoints,
                ["cards_per_turn"] = drawn.CardsPerTurn
            }
        });

        return drawn with
        {
            Log = updatedLog
        };
    }

    private (DeckZones DeckZones, List<CombatLogEvent> Events) DrawCards(
        DeckZones deckZones,
        int count,
        string combatId,
        int turnNumber)
    {
        var drawPile = deckZones.DrawPile.ToList();
        var hand = deckZones.Hand.ToList();
        var discardPile = deckZones.DiscardPile.ToList();
        var drawnCards = new List<string>();
        var events = new List<CombatLogEvent>();

        for (var i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    break;
                }

                drawPile = shuffleDiscardIntoDraw(discardPile).ToList();
                discardPile.Clear();
                events.Add(new CombatLogEvent
                {
                    EventId = $"{combatId}_turn_{turnNumber}_reshuffle_{events.Count + 1}",
                    EventType = CombatLogEventType.DeckReshuffled,
                    TurnNumber = turnNumber,
                    NumericChanges = new Dictionary<string, int>
                    {
                        ["reshuffled_count"] = drawPile.Count
                    }
                });
            }

            var cardId = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(cardId);
            drawnCards.Add(cardId);
        }

        if (drawnCards.Count > 0)
        {
            events.Add(new CombatLogEvent
            {
                EventId = $"{combatId}_turn_{turnNumber}_draw_{Guid.NewGuid():N}",
                EventType = CombatLogEventType.CardsDrawn,
                TurnNumber = turnNumber,
                TargetIds = drawnCards,
                NumericChanges = new Dictionary<string, int>
                {
                    ["drawn_count"] = drawnCards.Count
                }
            });
        }

        return (
            new DeckZones
            {
                DrawPile = drawPile,
                Hand = hand,
                DiscardPile = discardPile
            },
            events);
    }
}
