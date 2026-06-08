using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Enemies;

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

	public IReadOnlyList<EnemyIntentView> GetEnemyIntentViews(
		CombatState combat,
		IReadOnlyDictionary<string, EnemyDefinition> enemiesById)
	{
		ArgumentNullException.ThrowIfNull(combat);
		ArgumentNullException.ThrowIfNull(enemiesById);

		return combat.Enemies
			.Where(enemy => enemy.CurrentHp > 0)
			.Select(enemy =>
			{
				var definition = GetEnemyDefinition(enemy, enemiesById);
				var intent = GetCurrentIntent(definition, enemy.IntentIndex);
				return new EnemyIntentView
				{
					EnemyInstanceId = enemy.InstanceId,
					EnemyId = enemy.EnemyId,
					IntentIndex = enemy.IntentIndex,
					IntentId = intent.Id,
					IntentType = intent.IntentType,
					EffectPreviews = intent.Effects.Select(effect => new EnemyIntentEffectPreview
					{
						Type = effect.Type,
						Target = effect.Target,
						Value = effect.Value
					}).ToList()
				};
			})
			.ToList();
	}

	public CombatState ResolveEnemyTurn(
		CombatState combat,
		IReadOnlyDictionary<string, EnemyDefinition> enemiesById)
	{
		ArgumentNullException.ThrowIfNull(combat);
		ArgumentNullException.ThrowIfNull(enemiesById);
		if (combat.Status != CombatStatus.EnemyTurn)
		{
			throw new InvalidOperationException("Enemy turn can only resolve during EnemyTurn status.");
		}

		var working = combat;
		foreach (var enemy in combat.Enemies.Where(enemy => enemy.CurrentHp > 0))
		{
			var latestEnemy = working.Enemies.FirstOrDefault(item => item.InstanceId == enemy.InstanceId);
			if (latestEnemy is null || latestEnemy.CurrentHp <= 0)
			{
				continue;
			}

			var definition = GetEnemyDefinition(latestEnemy, enemiesById);
			var intent = GetCurrentIntent(definition, latestEnemy.IntentIndex);

			foreach (var effect in intent.Effects)
			{
				working = ResolveEnemyIntentEffect(working, latestEnemy.InstanceId, intent, effect);
				if (working.Status == CombatStatus.Defeat)
				{
					return AdvanceEnemyIntent(working, latestEnemy.InstanceId);
				}
			}

			working = AdvanceEnemyIntent(working, latestEnemy.InstanceId);
		}

		return working;
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

	private static CombatState ResolveEnemyIntentEffect(
		CombatState combat,
		string enemyInstanceId,
		EnemyIntentDefinition intent,
		EffectDefinition effect)
	{
		return effect.Type switch
		{
			"damage" when effect.Target == "player" => ResolveEnemyDamage(combat, enemyInstanceId, intent, effect),
			"block" or "gain_block" when effect.Target == "self" => ResolveEnemyBlock(combat, enemyInstanceId, intent, effect),
			_ => AppendEnemyIntentLog(combat, enemyInstanceId, intent, [], new Dictionary<string, int>(), $"unsupported_placeholder:{effect.Type}")
		};
	}

	private static CombatState ResolveEnemyDamage(
		CombatState combat,
		string enemyInstanceId,
		EnemyIntentDefinition intent,
		EffectDefinition effect)
	{
		var incomingDamage = Math.Max(0, effect.Value ?? 0);
		var blockedDamage = Math.Min(combat.PlayerBlock, incomingDamage);
		var hpDamage = Math.Max(0, incomingDamage - blockedDamage);
		var playerHpAfter = Math.Max(0, combat.PlayerHp - hpDamage);
		var updated = combat with
		{
			PlayerBlock = combat.PlayerBlock - blockedDamage,
			PlayerHp = playerHpAfter
		};

		updated = AppendEnemyIntentLog(updated, enemyInstanceId, intent, ["player"], new Dictionary<string, int>
		{
			["incoming_damage"] = incomingDamage,
			["blocked_damage"] = blockedDamage,
			["hp_damage"] = hpDamage,
			["player_block_after"] = updated.PlayerBlock,
			["player_hp_after"] = updated.PlayerHp
		}, "damage");

		return updated.PlayerHp <= 0
			? AppendCombatEndedLog(updated with { Status = CombatStatus.Defeat }, CombatStatus.Defeat)
			: updated;
	}

	private static CombatState ResolveEnemyBlock(
		CombatState combat,
		string enemyInstanceId,
		EnemyIntentDefinition intent,
		EffectDefinition effect)
	{
		var block = Math.Max(0, effect.Value ?? 0);
		var enemies = combat.Enemies.ToList();
		var index = enemies.FindIndex(enemy => enemy.InstanceId == enemyInstanceId);
		if (index < 0)
		{
			return combat;
		}

		enemies[index] = enemies[index] with
		{
			Block = enemies[index].Block + block
		};
		var updated = combat with { Enemies = enemies };

		return AppendEnemyIntentLog(updated, enemyInstanceId, intent, [enemyInstanceId], new Dictionary<string, int>
		{
			["block_gained"] = block,
			["enemy_block_after"] = enemies[index].Block
		}, effect.Type);
	}

	private static CombatState AdvanceEnemyIntent(CombatState combat, string enemyInstanceId)
	{
		var enemies = combat.Enemies
			.Select(enemy => enemy.InstanceId == enemyInstanceId ? enemy with { IntentIndex = enemy.IntentIndex + 1 } : enemy)
			.ToList();

		return combat with { Enemies = enemies };
	}

	private static CombatState AppendEnemyIntentLog(
		CombatState combat,
		string enemyInstanceId,
		EnemyIntentDefinition intent,
		IEnumerable<string> targetIds,
		Dictionary<string, int> numericChanges,
		string effectType)
	{
		var log = combat.Log.ToList();
		log.Add(new CombatLogEvent
		{
			EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_enemy_intent_{log.Count + 1}",
			EventType = CombatLogEventType.EnemyIntentResolved,
			TurnNumber = combat.TurnNumber,
			SourceId = enemyInstanceId,
			TargetIds = targetIds.ToList(),
			MessageKey = intent.Id,
			NumericChanges = numericChanges,
			Metadata = new Dictionary<string, string>
			{
				["intent_id"] = intent.Id,
				["intent_type"] = intent.IntentType.ToString(),
				["effect_type"] = effectType
			}
		});

		return combat with { Log = log };
	}

	private static CombatState AppendCombatEndedLog(CombatState combat, CombatStatus status)
	{
		var log = combat.Log.ToList();
		log.Add(new CombatLogEvent
		{
			EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_combat_ended_{status.ToString().ToLowerInvariant()}",
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

	private static EnemyDefinition GetEnemyDefinition(
		CombatEnemyState enemy,
		IReadOnlyDictionary<string, EnemyDefinition> enemiesById)
	{
		if (!enemiesById.TryGetValue(enemy.EnemyId, out var definition))
		{
			throw new InvalidOperationException($"Unknown enemy definition id '{enemy.EnemyId}'.");
		}

		return definition;
	}

	private static EnemyIntentDefinition GetCurrentIntent(EnemyDefinition definition, int intentIndex)
	{
		if (definition.IntentSequence.Count == 0)
		{
			throw new InvalidOperationException($"Enemy definition '{definition.Id}' has no intent sequence.");
		}

		var normalizedIndex = intentIndex % definition.IntentSequence.Count;
		if (normalizedIndex < 0)
		{
			normalizedIndex += definition.IntentSequence.Count;
		}

		return definition.IntentSequence[normalizedIndex];
	}
}
