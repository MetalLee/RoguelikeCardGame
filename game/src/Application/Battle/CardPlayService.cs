using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;

namespace RoguelikeCardGame.Application.Battle;

public enum PlayCardFailureReason
{
	None,
	NotPlayerTurn,
	CardNotInHand,
	InsufficientActionPoints,
	InsufficientColorEnergy,
	TargetMissing
}

public sealed record CardEffectPreview
{
	public required string EffectType { get; init; }

	public int Value { get; init; }

	public string? Target { get; init; }
}

public sealed record ColorEffectPreview
{
	public ColorType Color { get; init; }

	public required string EffectType { get; init; }

	public int Value { get; init; }
}

public sealed record CardPlayPreview
{
	public required string CardId { get; init; }

	public CardType CardType { get; init; }

	public List<CardEffectPreview> BaseEffects { get; init; } = new();

	public ColorType? EnchantmentColor { get; init; }

	public int GeneratedColorEnergyAmount { get; init; }

	public ColorType GeneratedColorEnergyColor { get; init; } = ColorType.Colorless;

	public int ColorEnergyCost { get; init; }

	public List<ColorType> ConsumedColors { get; init; } = new();

	public List<ColorEffectPreview> ColorEffects { get; init; } = new();

	public int EstimatedDamage { get; init; }

	public int EstimatedBlock { get; init; }

	public int EstimatedHealing { get; init; }

	public int EstimatedExtraCasts { get; init; }
}

public sealed record PlayCardResult
{
	public bool Succeeded { get; init; }

	public required CombatState Combat { get; init; }

	public PlayCardFailureReason FailureReason { get; init; }

	public string? FailureMessageKey { get; init; }

	public int? RequiredActionPoints { get; init; }

	public int? CurrentActionPoints { get; init; }

	public int? RequiredColorEnergy { get; init; }

	public int? CurrentColorEnergy { get; init; }

	public string? RequiredTargetRule { get; init; }

	public CardPlayPreview? Preview { get; init; }

	public List<CombatLogEvent> Events { get; init; } = new();

	public static PlayCardResult Success(CombatState combat, IEnumerable<CombatLogEvent> events, CardPlayPreview? preview = null) => new()
	{
		Succeeded = true,
		Combat = combat,
		FailureReason = PlayCardFailureReason.None,
		Events = events.ToList(),
		Preview = preview
	};

	public static PlayCardResult Failure(
		CombatState combat,
		PlayCardFailureReason reason,
		string messageKey,
		int? requiredActionPoints = null,
		int? currentActionPoints = null,
		int? requiredColorEnergy = null,
		int? currentColorEnergy = null,
		TargetRule? requiredTargetRule = null) => new()
	{
		Succeeded = false,
		Combat = combat,
		FailureReason = reason,
		FailureMessageKey = messageKey,
		RequiredActionPoints = requiredActionPoints,
		CurrentActionPoints = currentActionPoints,
		RequiredColorEnergy = requiredColorEnergy,
		CurrentColorEnergy = currentColorEnergy,
		RequiredTargetRule = requiredTargetRule?.ToString()
	};
}

internal sealed record ResolutionTotals
{
	public int Damage { get; init; }

	public int Block { get; init; }

	public int Healing { get; init; }

	public int ExtraCasts { get; init; }

	public ResolutionTotals Add(ResolutionTotals other) => this with
	{
		Damage = Damage + other.Damage,
		Block = Block + other.Block,
		Healing = Healing + other.Healing,
		ExtraCasts = ExtraCasts + other.ExtraCasts
	};
}

public sealed class CardPlayService
{
	private const int MaxYellowExtraCasts = 1;
	private const int MaxPurpleDoublings = 1;
	private const int RedDamageBonusPerEnergy = 3;
	private const int RedBlockToDamagePercent = 50;
	private const int BlueBlockPercent = 50;
	private const int GreenHealPercent = 50;

	private readonly CombatTurnService turnService;

	public CardPlayService(CombatTurnService? turnService = null)
	{
		this.turnService = turnService ?? new CombatTurnService();
	}

	public PlayCardResult CanPlayCard(CombatState combat, CardDefinition card, string? targetEnemyInstanceId = null)
	{
		return CanPlayCard(combat, card, targetEnemyInstanceId, handIndex: null, enchantment: null);
	}

	public PlayCardResult CanPlayCard(CombatState combat, CardDefinition card, string? targetEnemyInstanceId, int? handIndex)
	{
		return CanPlayCard(combat, card, targetEnemyInstanceId, handIndex, enchantment: null);
	}

	public PlayCardResult CanPlayCard(
		CombatState combat,
		CardDefinition card,
		string? targetEnemyInstanceId,
		int? handIndex,
		CardEnchantment? enchantment)
	{
		ArgumentNullException.ThrowIfNull(combat);
		ArgumentNullException.ThrowIfNull(card);

		if (combat.Status != CombatStatus.PlayerTurn)
		{
			return PlayCardResult.Failure(combat, PlayCardFailureReason.NotPlayerTurn, "ui.play_card.not_player_turn");
		}

		if (!IsCardInHandSlot(combat, card, handIndex))
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
			var requiredColorEnergy = RequiredColorEnergy(card, combat.ColorEnergy);
			if (card.ColorEnergyCost is null || !combat.ColorEnergy.CanSpend(card.ColorEnergyCost.Mode, card.ColorEnergyCost.Amount, card.ColorEnergyCost.MinAmount))
			{
				return PlayCardResult.Failure(
					combat,
					PlayCardFailureReason.InsufficientColorEnergy,
					"ui.play_card.insufficient_color_energy",
					requiredColorEnergy: requiredColorEnergy,
					currentColorEnergy: combat.ColorEnergy.Count);
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

		return PlayCardResult.Success(combat, [], BuildPreview(combat, card, targetEnemyInstanceId, enchantment));
	}

	public CardPlayPreview PreviewCard(
		CombatState combat,
		CardDefinition card,
		string? targetEnemyInstanceId = null,
		CardEnchantment? enchantment = null)
	{
		ArgumentNullException.ThrowIfNull(combat);
		ArgumentNullException.ThrowIfNull(card);
		return BuildPreview(combat, card, targetEnemyInstanceId, enchantment);
	}

	public PlayCardResult PlayCard(CombatState combat, CardDefinition card, string? targetEnemyInstanceId = null)
	{
		return PlayCard(combat, card, targetEnemyInstanceId, handIndex: null, enchantment: null);
	}

	public PlayCardResult PlayCard(CombatState combat, CardDefinition card, string? targetEnemyInstanceId, int? handIndex)
	{
		return PlayCard(combat, card, targetEnemyInstanceId, handIndex, enchantment: null);
	}

	public PlayCardResult PlayCard(
		CombatState combat,
		CardDefinition card,
		string? targetEnemyInstanceId,
		int? handIndex,
		CardEnchantment? enchantment)
	{
		var canPlay = CanPlayCard(combat, card, targetEnemyInstanceId, handIndex, enchantment);
		if (!canPlay.Succeeded)
		{
			return canPlay with { Events = [CreateRejectedEvent(combat, card, canPlay)] };
		}

		var logStartIndex = combat.Log.Count;
		var hand = combat.DeckZones.Hand.ToList();
		var resolvedHandIndex = ResolveHandIndex(hand, card.Id, handIndex);
		hand.RemoveAt(resolvedHandIndex);

		var spentColors = new List<ColorEnergySlot>();
		var colorEnergy = combat.ColorEnergy;
		if (card.Type == CardType.Finisher && card.ColorEnergyCost is not null)
		{
			var spent = colorEnergy.Spend(card.ColorEnergyCost.Mode, SpendAmount(card.ColorEnergyCost, colorEnergy), card.ColorEnergyCost.MinAmount);
			colorEnergy = spent.Pool;
			spentColors = spent.Spent;
		}

		var working = combat with
		{
			ActionPoints = card.Type == CardType.Action ? combat.ActionPoints - card.Cost : combat.ActionPoints,
			ColorEnergy = colorEnergy,
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
				["color_energy_before"] = combat.ColorEnergy.Count,
				["color_energy_after_spend"] = working.ColorEnergy.Count,
				["color_energy_spent"] = spentColors.Count,
				["hand_index"] = resolvedHandIndex
			},
			Metadata = new Dictionary<string, string>
			{
				["card_type"] = card.Type.ToString(),
				["spent_colors"] = string.Join(",", spentColors.Select(slot => slot.Color.ToString())),
				["enchantment_color"] = enchantment?.Color.ToString() ?? string.Empty
			}
		});
		working = working with { Log = log };

		var playColors = card.Type == CardType.Action
			? EnchantmentColors(enchantment)
			: spentColors.Select(slot => slot.Color).Where(color => color != ColorType.Colorless).ToList();
		var resolution = ResolveCardRelease(working, card, targetEnemyInstanceId, playColors);
		working = resolution.Combat;

		if (card.Type == CardType.Action && card.ColorEnergyGeneration is not null)
		{
			var generationColor = card.ColorEnergyGeneration.ResolveColor(enchantment);
			var before = working.ColorEnergy.Count;
			var afterPool = working.ColorEnergy.Add(generationColor, card.ColorEnergyGeneration.Amount);
			var accepted = afterPool.Count - before;
			working = working with { ColorEnergy = afterPool };
			working = AppendEffectLog(working, card.Id, ["color_energy_pool"], new Dictionary<string, int>
			{
				["color_energy_generated"] = accepted,
				["color_energy_requested"] = card.ColorEnergyGeneration.Amount,
				["color_energy_after"] = afterPool.Count
			}, "gain_color_energy", new Dictionary<string, string>
			{
				["color"] = generationColor.ToString()
			});
		}

		var discardPile = working.DeckZones.DiscardPile.ToList();
		discardPile.Add(card.Id);
		var updated = working with
		{
			DeckZones = working.DeckZones with
			{
				DiscardPile = discardPile
			}
		};

		updated = AppendCombatOutcomeLogs(updated);
		var preview = BuildPreview(combat, card, targetEnemyInstanceId, enchantment);
		return PlayCardResult.Success(updated, updated.Log.Skip(logStartIndex), preview);
	}

	private (CombatState Combat, ResolutionTotals Totals) ResolveCardRelease(
		CombatState combat,
		CardDefinition card,
		string? targetEnemyInstanceId,
		IReadOnlyList<ColorType> colors)
	{
		var colorCounts = colors
			.Where(color => color != ColorType.Colorless)
			.GroupBy(color => color)
			.ToDictionary(group => group.Key, group => group.Count());
		var extraCasts = Math.Min(MaxYellowExtraCasts, colorCounts.GetValueOrDefault(ColorType.Yellow));
		var releases = 1 + extraCasts;
		var working = combat;
		var totals = new ResolutionTotals { ExtraCasts = extraCasts };

		for (var releaseIndex = 0; releaseIndex < releases; releaseIndex++)
		{
			var allowUtilityEffects = releaseIndex == 0;
			foreach (var effect in card.Effects)
			{
				if (!allowUtilityEffects && IsYellowForbiddenRepeatEffect(effect))
				{
					working = AppendEffectLog(working, card.Id, [], new Dictionary<string, int>
					{
						["skipped_extra_release"] = releaseIndex,
					}, "yellow_extra_release_skipped_utility");
					continue;
				}

				var resolved = ResolveEffect(working, effect, card, targetEnemyInstanceId, colorCounts, releaseIndex);
				working = resolved.Combat;
				totals = totals.Add(resolved.Totals);
			}
		}

		if (extraCasts > 0)
		{
			working = AppendEffectLog(working, card.Id, [card.Id], new Dictionary<string, int>
			{
				["extra_casts"] = extraCasts,
				["yellow_count"] = colorCounts.GetValueOrDefault(ColorType.Yellow)
			}, "yellow_extra_casts");
		}

		return (working, totals);
	}

	private (CombatState Combat, ResolutionTotals Totals) ResolveEffect(
		CombatState combat,
		EffectDefinition effect,
		CardDefinition card,
		string? targetEnemyInstanceId,
		IReadOnlyDictionary<ColorType, int> colorCounts,
		int releaseIndex)
	{
		return effect.Type switch
		{
			"damage" => ResolveDamage(combat, effect, card, targetEnemyInstanceId, colorCounts),
			"block" or "gain_block" => ResolveBlock(combat, effect, card, colorCounts),
			"heal" => ResolveHeal(combat, effect, card, colorCounts),
			"draw_cards" => (ResolveDrawCards(combat, effect, card), new ResolutionTotals()),
			"gain_action_points" => (ResolveGainActionPoints(combat, effect, card), new ResolutionTotals()),
			"temporary_discount" => (ResolveTemporaryDiscountPlaceholder(combat, effect, card), new ResolutionTotals()),
			_ => (ResolveUnsupportedPlaceholder(combat, effect, card), new ResolutionTotals())
		};
	}

	private (CombatState Combat, ResolutionTotals Totals) ResolveDamage(
		CombatState combat,
		EffectDefinition effect,
		CardDefinition card,
		string? targetEnemyInstanceId,
		IReadOnlyDictionary<ColorType, int> colorCounts)
	{
		var baseDamage = Math.Max(0, effect.Value ?? 0);
		var redBonus = RedDamageBonusPerEnergy * colorCounts.GetValueOrDefault(ColorType.Red);
		var purpleMultiplier = 1 + Math.Min(MaxPurpleDoublings, colorCounts.GetValueOrDefault(ColorType.Purple));
		var damage = (baseDamage + redBonus) * purpleMultiplier;
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

		var updated = AppendEffectLog(combat with { Enemies = enemies }, card.Id, targetIds, new Dictionary<string, int>
		{
			["base_damage"] = baseDamage,
			["red_bonus"] = redBonus,
			["purple_multiplier"] = purpleMultiplier,
			["damage"] = damage,
			["hp_damage"] = totalHpDamage,
			["blocked_damage"] = totalBlockedDamage
		}, "damage");

		updated = ApplyBlueAndGreenFollowUps(updated, card.Id, totalHpDamage, colorCounts, basis: "final_damage");
		return (updated, new ResolutionTotals { Damage = totalHpDamage });
	}

	private (CombatState Combat, ResolutionTotals Totals) ResolveBlock(
		CombatState combat,
		EffectDefinition effect,
		CardDefinition card,
		IReadOnlyDictionary<ColorType, int> colorCounts)
	{
		var baseBlock = Math.Max(0, effect.Value ?? 0);
		var purpleMultiplier = 1 + Math.Min(MaxPurpleDoublings, colorCounts.GetValueOrDefault(ColorType.Purple));
		var block = baseBlock * purpleMultiplier;
		var updated = combat with
		{
			PlayerBlock = combat.PlayerBlock + block
		};

		updated = AppendEffectLog(updated, card.Id, ["player"], new Dictionary<string, int>
		{
			["base_block"] = baseBlock,
			["purple_multiplier"] = purpleMultiplier,
			["block_gained"] = block,
			["player_block_after"] = updated.PlayerBlock
		}, effect.Type);

		var redDamage = block * RedBlockToDamagePercent / 100 * colorCounts.GetValueOrDefault(ColorType.Red);
		if (redDamage > 0)
		{
			var redEffect = new EffectDefinition { Type = "damage", Target = "all_enemies", Value = redDamage };
			var redResolved = ResolveDamage(updated, redEffect, card, targetEnemyInstanceId: null, new Dictionary<ColorType, int>());
			updated = AppendEffectLog(redResolved.Combat, card.Id, redResolved.Combat.Enemies.Where(enemy => enemy.CurrentHp > 0).Select(enemy => enemy.InstanceId), new Dictionary<string, int>
			{
				["red_block_to_damage"] = redDamage
			}, "red_block_to_damage");
		}

		updated = ApplyBlueAndGreenFollowUps(updated, card.Id, block, colorCounts, basis: "final_effect_value");
		return (updated, new ResolutionTotals { Block = block });
	}

	private (CombatState Combat, ResolutionTotals Totals) ResolveHeal(
		CombatState combat,
		EffectDefinition effect,
		CardDefinition card,
		IReadOnlyDictionary<ColorType, int> colorCounts)
	{
		var baseHealing = Math.Max(0, effect.Value ?? 0);
		var purpleMultiplier = 1 + Math.Min(MaxPurpleDoublings, colorCounts.GetValueOrDefault(ColorType.Purple));
		var healing = baseHealing * purpleMultiplier;
		var updated = HealPlayer(combat, card.Id, healing, "heal");
		return (updated, new ResolutionTotals { Healing = updated.PlayerHp - combat.PlayerHp });
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

	private CombatState ResolveUnsupportedPlaceholder(CombatState combat, EffectDefinition effect, CardDefinition card, string? effectType = null)
	{
		return AppendEffectLog(combat, card.Id, [], new Dictionary<string, int>(), effectType ?? $"unsupported_placeholder:{effect.Type}");
	}

	private CombatState ApplyBlueAndGreenFollowUps(
		CombatState combat,
		string cardId,
		int finalValue,
		IReadOnlyDictionary<ColorType, int> colorCounts,
		string basis)
	{
		var working = combat;
		var blueCount = colorCounts.GetValueOrDefault(ColorType.Blue);
		if (blueCount > 0 && finalValue > 0)
		{
			var block = finalValue * BlueBlockPercent / 100 * blueCount;
			working = working with { PlayerBlock = working.PlayerBlock + block };
			working = AppendEffectLog(working, cardId, ["player"], new Dictionary<string, int>
			{
				["blue_count"] = blueCount,
				["basis_value"] = finalValue,
				["block_gained"] = block,
				["player_block_after"] = working.PlayerBlock
			}, "blue_gain_block", new Dictionary<string, string> { ["basis"] = basis });
		}

		var greenCount = colorCounts.GetValueOrDefault(ColorType.Green);
		if (greenCount > 0 && finalValue > 0)
		{
			var healing = finalValue * GreenHealPercent / 100 * greenCount;
			working = HealPlayer(working, cardId, healing, "green_heal", new Dictionary<string, int>
			{
				["green_count"] = greenCount,
				["basis_value"] = finalValue
			});
		}

		return working;
	}

	private CombatState HealPlayer(
		CombatState combat,
		string cardId,
		int healing,
		string effectType,
		Dictionary<string, int>? extra = null)
	{
		var clampedHealing = Math.Max(0, Math.Min(healing, combat.PlayerMaxHp - combat.PlayerHp));
		var updated = combat with
		{
			PlayerHp = combat.PlayerHp + clampedHealing
		};
		var changes = extra is null ? new Dictionary<string, int>() : new Dictionary<string, int>(extra);
		changes["heal_requested"] = healing;
		changes["healed"] = clampedHealing;
		changes["player_hp_after"] = updated.PlayerHp;

		return AppendEffectLog(updated, cardId, ["player"], changes, effectType);
	}

	private CombatState AppendEffectLog(
		CombatState combat,
		string sourceId,
		IEnumerable<string> targetIds,
		Dictionary<string, int> numericChanges,
		string effectType,
		Dictionary<string, string>? metadata = null)
	{
		var log = combat.Log.ToList();
		var eventMetadata = metadata is null ? new Dictionary<string, string>() : new Dictionary<string, string>(metadata);
		eventMetadata["effect_type"] = effectType;
		log.Add(new CombatLogEvent
		{
			EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_effect_{log.Count + 1}",
			EventType = CombatLogEventType.EffectResolved,
			TurnNumber = combat.TurnNumber,
			SourceId = sourceId,
			TargetIds = targetIds.Distinct().ToList(),
			NumericChanges = numericChanges,
			Metadata = eventMetadata
		});

		return combat with { Log = log };
	}

	private CardPlayPreview BuildPreview(
		CombatState combat,
		CardDefinition card,
		string? targetEnemyInstanceId,
		CardEnchantment? enchantment)
	{
		var spentColors = card.Type == CardType.Finisher && card.ColorEnergyCost is not null && combat.ColorEnergy.CanSpend(card.ColorEnergyCost.Mode, card.ColorEnergyCost.Amount, card.ColorEnergyCost.MinAmount)
			? combat.ColorEnergy.Spend(card.ColorEnergyCost.Mode, SpendAmount(card.ColorEnergyCost, combat.ColorEnergy), card.ColorEnergyCost.MinAmount).Spent
			: [];
		var colors = card.Type == CardType.Action
			? EnchantmentColors(enchantment)
			: spentColors.Select(slot => slot.Color).Where(color => color != ColorType.Colorless).ToList();
		var colorCounts = colors.GroupBy(color => color).ToDictionary(group => group.Key, group => group.Count());
		var baseDamage = card.Effects.Where(effect => effect.Type == "damage").Sum(effect => Math.Max(0, effect.Value ?? 0));
		var baseBlock = card.Effects.Where(effect => effect.Type is "block" or "gain_block").Sum(effect => Math.Max(0, effect.Value ?? 0));
		var baseHealing = card.Effects.Where(effect => effect.Type == "heal").Sum(effect => Math.Max(0, effect.Value ?? 0));
		var extraCasts = Math.Min(MaxYellowExtraCasts, colorCounts.GetValueOrDefault(ColorType.Yellow));
		var purpleMultiplier = 1 + Math.Min(MaxPurpleDoublings, colorCounts.GetValueOrDefault(ColorType.Purple));
		var estimatedDamage = (baseDamage + RedDamageBonusPerEnergy * colorCounts.GetValueOrDefault(ColorType.Red)) * purpleMultiplier * (1 + extraCasts);
		var estimatedBlock = baseBlock * purpleMultiplier * (1 + extraCasts);
		var estimatedHealing = baseHealing * purpleMultiplier * (1 + extraCasts);
		var blueBlock = estimatedDamage * BlueBlockPercent / 100 * colorCounts.GetValueOrDefault(ColorType.Blue);
		var greenHealing = estimatedDamage * GreenHealPercent / 100 * colorCounts.GetValueOrDefault(ColorType.Green);

		var generation = card.ColorEnergyGeneration;
		var generationColor = generation?.ResolveColor(enchantment) ?? ColorType.Colorless;
		return new CardPlayPreview
		{
			CardId = card.Id,
			CardType = card.Type,
			BaseEffects = card.Effects.Select(effect => new CardEffectPreview
			{
				EffectType = effect.Type,
				Value = Math.Max(0, effect.Value ?? 0),
				Target = effect.Target
			}).ToList(),
			EnchantmentColor = enchantment?.Color,
			GeneratedColorEnergyAmount = generation?.Amount ?? 0,
			GeneratedColorEnergyColor = generationColor,
			ColorEnergyCost = card.Type == CardType.Finisher ? RequiredColorEnergy(card, combat.ColorEnergy) : 0,
			ConsumedColors = spentColors.Select(slot => slot.Color).ToList(),
			ColorEffects = BuildColorPreviews(colorCounts, estimatedDamage, estimatedBlock),
			EstimatedDamage = estimatedDamage,
			EstimatedBlock = estimatedBlock + blueBlock,
			EstimatedHealing = Math.Min(combat.PlayerMaxHp - combat.PlayerHp, estimatedHealing + greenHealing),
			EstimatedExtraCasts = extraCasts
		};
	}

	private static List<ColorEffectPreview> BuildColorPreviews(
		IReadOnlyDictionary<ColorType, int> colorCounts,
		int estimatedDamage,
		int estimatedBlock)
	{
		var previews = new List<ColorEffectPreview>();
		foreach (var (color, count) in colorCounts)
		{
			switch (color)
			{
				case ColorType.Red:
					previews.Add(new ColorEffectPreview { Color = color, EffectType = "red_damage_bonus", Value = RedDamageBonusPerEnergy * count });
					break;
				case ColorType.Yellow:
					previews.Add(new ColorEffectPreview { Color = color, EffectType = "extra_casts", Value = Math.Min(MaxYellowExtraCasts, count) });
					break;
				case ColorType.Blue:
					previews.Add(new ColorEffectPreview { Color = color, EffectType = "gain_block", Value = estimatedDamage * BlueBlockPercent / 100 * count });
					break;
				case ColorType.Green:
					previews.Add(new ColorEffectPreview { Color = color, EffectType = "heal", Value = estimatedDamage * GreenHealPercent / 100 * count });
					break;
				case ColorType.Purple:
					previews.Add(new ColorEffectPreview { Color = color, EffectType = "double_final_value", Value = Math.Min(MaxPurpleDoublings, count) });
					break;
			}
		}

		return previews;
	}

	private static IReadOnlyList<ColorType> EnchantmentColors(CardEnchantment? enchantment)
	{
		return enchantment?.Color is null or ColorType.Colorless ? [] : [enchantment.Color];
	}

	private static int RequiredColorEnergy(CardDefinition card, ColorEnergyPool pool)
	{
		if (card.ColorEnergyCost is null)
		{
			return 0;
		}

		return card.ColorEnergyCost.Mode switch
		{
			ColorEnergySpendMode.Fixed => card.ColorEnergyCost.Amount,
			ColorEnergySpendMode.X => card.ColorEnergyCost.MinAmount,
			ColorEnergySpendMode.All => card.ColorEnergyCost.MinAmount,
			_ => 0
		};
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

	private static bool IsYellowForbiddenRepeatEffect(EffectDefinition effect)
	{
		return effect.Type is "draw_cards" or "gain_action_points" or "gain_resource" or "gain_color_energy";
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

		if (failure.RequiredColorEnergy is not null)
		{
			numericChanges["required_color_energy"] = failure.RequiredColorEnergy.Value;
		}

		if (failure.CurrentColorEnergy is not null)
		{
			numericChanges["current_color_energy"] = failure.CurrentColorEnergy.Value;
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

	private static bool IsCardInHandSlot(CombatState combat, CardDefinition card, int? handIndex)
	{
		if (handIndex is null)
		{
			return combat.DeckZones.Hand.Contains(card.Id);
		}

		return handIndex.Value >= 0 &&
			   handIndex.Value < combat.DeckZones.Hand.Count &&
			   combat.DeckZones.Hand[handIndex.Value] == card.Id;
	}

	private static int ResolveHandIndex(IReadOnlyList<string> hand, string cardId, int? handIndex)
	{
		if (handIndex is not null)
		{
			if (handIndex.Value < 0 || handIndex.Value >= hand.Count || hand[handIndex.Value] != cardId)
			{
				throw new InvalidOperationException($"Card '{cardId}' is not in hand slot {handIndex.Value}.");
			}

			return handIndex.Value;
		}

		var resolvedIndex = hand.ToList().IndexOf(cardId);
		if (resolvedIndex < 0)
		{
			throw new InvalidOperationException($"Card '{cardId}' is not in hand.");
		}

		return resolvedIndex;
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
			"selected_target" => targetEnemyInstanceId is null ? [] : [targetEnemyInstanceId],
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
