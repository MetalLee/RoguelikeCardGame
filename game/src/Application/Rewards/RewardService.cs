using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Relics;
using RoguelikeCardGame.Domain.Runs;

namespace RoguelikeCardGame.Application.Rewards;

public sealed record WeaponRewardPoolDefinition
{
    public required string WeaponId { get; init; }

    public Dictionary<CardRarity, List<string>> CardIdsByRarity { get; init; } = new();
}

public sealed class RewardService
{
    private static readonly ColorType[] RewardShardColors =
    [
        ColorType.Red,
        ColorType.Yellow,
        ColorType.Blue,
        ColorType.Green,
        ColorType.Purple
    ];

    public ColorType GenerateColorShard(Func<int, int> nextInt)
    {
        ArgumentNullException.ThrowIfNull(nextInt);
        return RewardShardColors[nextInt(RewardShardColors.Length)];
    }

    public RunState AddPendingColorShard(RunState runState, ColorType color)
    {
        ArgumentNullException.ThrowIfNull(runState);
        if (color == ColorType.Colorless)
        {
            throw new InvalidOperationException("Colorless shards cannot be granted as reward color shards.");
        }

        return runState with
        {
            PendingColorShards = runState.PendingColorShards.Concat([color]).ToList()
        };
    }

    public IReadOnlyList<CardInstance> ListEnchantableActionCards(
        RunState runState,
        IReadOnlyDictionary<string, CardDefinition> cardsById)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(cardsById);

        return runState.MasterDeckInstances
            .Where(instance => instance.Enchantment is null)
            .Where(instance =>
                cardsById.TryGetValue(instance.DefinitionId, out var card) &&
                card.Type == CardType.Action)
            .ToList();
    }

    public RunState ApplyColorShard(
        RunState runState,
        ColorType color,
        string cardInstanceId,
        IReadOnlyDictionary<string, CardDefinition> cardsById)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardInstanceId);
        ArgumentNullException.ThrowIfNull(cardsById);

        if (color == ColorType.Colorless)
        {
            throw new InvalidOperationException("Colorless shards cannot enchant cards.");
        }

        var pendingShards = runState.PendingColorShards.ToList();
        var pendingIndex = pendingShards.FindIndex(item => item == color);
        if (pendingIndex < 0)
        {
            throw new InvalidOperationException($"Run does not have a pending {color} color shard.");
        }

        var instances = runState.MasterDeckInstances.ToList();
        var instanceIndex = instances.FindIndex(instance => instance.InstanceId == cardInstanceId);
        if (instanceIndex < 0)
        {
            throw new InvalidOperationException($"Unknown card instance id '{cardInstanceId}'.");
        }

        var instance = instances[instanceIndex];
        if (!cardsById.TryGetValue(instance.DefinitionId, out var card))
        {
            throw new InvalidOperationException($"Unknown card definition id '{instance.DefinitionId}'.");
        }

        if (card.Type != CardType.Action)
        {
            throw new InvalidOperationException($"Only action cards can be enchanted by color shards; '{card.Id}' is {card.Type}.");
        }

        if (instance.Enchantment is not null)
        {
            throw new InvalidOperationException($"Card instance '{cardInstanceId}' is already enchanted.");
        }

        var enchantment = new CardEnchantment { CardInstanceId = cardInstanceId, Color = color };
        instances[instanceIndex] = instance with { Enchantment = enchantment };
        pendingShards.RemoveAt(pendingIndex);

        var enchantments = instances
            .Select(item => item.Enchantment)
            .Where(item => item is not null)
            .Cast<CardEnchantment>()
            .ToList();

        return runState with
        {
            MasterDeckInstances = instances,
            CardEnchantments = enchantments,
            PendingColorShards = pendingShards
        };
    }

    public IReadOnlyList<string> GenerateWeaponCardCandidates(
        RunState runState,
        IEnumerable<WeaponRewardPoolDefinition> rewardPools,
        IReadOnlyDictionary<string, CardDefinition> cardsById,
        Func<int, int> nextInt)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(rewardPools);
        ArgumentNullException.ThrowIfNull(cardsById);
        ArgumentNullException.ThrowIfNull(nextInt);

        var weaponIds = new[] { runState.MainHandWeaponId, runState.OffHandWeaponId }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        if (weaponIds.Count == 0)
        {
            throw new InvalidOperationException("Run has no main-hand or off-hand weapon for reward card generation.");
        }

        var pools = rewardPools
            .Where(pool => weaponIds.Contains(pool.WeaponId))
            .ToList();
        var allCandidates = pools
            .SelectMany(pool => pool.CardIdsByRarity.Values.SelectMany(cardIds => cardIds))
            .Where(cardsById.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (allCandidates.Count < 3)
        {
            throw new InvalidOperationException("Weapon reward pools must expose at least 3 distinct card candidates.");
        }

        var result = new List<string>();
        var attempts = 0;
        while (result.Count < 3 && attempts < 60)
        {
            attempts++;
            var rarity = RollRewardRarity(nextInt);
            var rarityCandidates = pools
                .SelectMany(pool => pool.CardIdsByRarity.TryGetValue(rarity, out var ids) ? ids : [])
                .Where(cardsById.ContainsKey)
                .Where(cardId => !result.Contains(cardId, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (rarityCandidates.Count == 0)
            {
                continue;
            }

            result.Add(rarityCandidates[nextInt(rarityCandidates.Count)]);
        }

        foreach (var cardId in allCandidates.Where(cardId => !result.Contains(cardId, StringComparer.Ordinal)))
        {
            if (result.Count >= 3)
            {
                break;
            }

            result.Add(cardId);
        }

        return result;
    }

    public RunState ClaimWeaponCardChoice(
        RunState runState,
        IReadOnlyList<string> candidateCardIds,
        IEnumerable<string> selectedCardIds)
    {
        ArgumentNullException.ThrowIfNull(selectedCardIds);
        var selected = selectedCardIds.ToList();
        if (selected.Count != 1)
        {
            throw new InvalidOperationException($"Weapon card reward requires exactly 1 pick, got {selected.Count}.");
        }

        return ClaimWeaponCard(runState, candidateCardIds, selected[0]);
    }

    public RunState ClaimWeaponCard(
        RunState runState,
        IReadOnlyList<string> candidateCardIds,
        string selectedCardId)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(candidateCardIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedCardId);

        if (candidateCardIds.Count != 3)
        {
            throw new InvalidOperationException($"Weapon card reward must expose exactly 3 candidates, got {candidateCardIds.Count}.");
        }

        if (!candidateCardIds.Contains(selectedCardId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"Card id '{selectedCardId}' is not one of the weapon reward candidates.");
        }

        var nextInstanceIndex = runState.MasterDeckInstances.Count + 1;
        var instance = new CardInstance
        {
            InstanceId = $"{runState.RunId}.card.{nextInstanceIndex:000}",
            DefinitionId = selectedCardId
        };

        return runState with
        {
            MasterDeck = runState.MasterDeck.Concat([selectedCardId]).ToList(),
            MasterDeckInstances = runState.MasterDeckInstances.Concat([instance]).ToList()
        };
    }

    private static CardRarity RollRewardRarity(Func<int, int> nextInt)
    {
        var roll = nextInt(100);
        return roll switch
        {
            < 60 => CardRarity.Common,
            < 90 => CardRarity.Uncommon,
            _ => CardRarity.Rare
        };
    }

    public RunState GrantEncounterRelic(
        RunState runState,
        EncounterDefinition encounter,
        IReadOnlyDictionary<string, RelicDefinition> relicsById)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(relicsById);

        var relicId = encounter.RewardProfile.RelicId;
        if (string.IsNullOrWhiteSpace(relicId))
        {
            return runState;
        }

        if (!relicsById.ContainsKey(relicId))
        {
            throw new InvalidOperationException($"Unknown relic id '{relicId}'.");
        }

        if (runState.RelicIds.Contains(relicId))
        {
            return runState;
        }

        return runState with
        {
            RelicIds = runState.RelicIds.Concat([relicId]).ToList()
        };
    }
}
