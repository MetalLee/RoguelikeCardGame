using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Relics;
using RoguelikeCardGame.Domain.Rewards;
using RoguelikeCardGame.Domain.Runs;

namespace RoguelikeCardGame.Application.Rewards;

public sealed class RewardService
{
    public IReadOnlyList<RewardPackDefinition> GetAvailableCardPacks(
        EncounterDefinition encounter,
        IReadOnlyDictionary<string, RewardPackDefinition> rewardPacksById)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(rewardPacksById);

        if (encounter.NodeType == EncounterNodeType.Boss)
        {
            return [];
        }

        return encounter.RewardProfile.CardPackIds
            .Select(packId => OpenRewardPack(packId, rewardPacksById))
            .ToList();
    }

    public RewardPackDefinition OpenRewardPack(
        string packId,
        IReadOnlyDictionary<string, RewardPackDefinition> rewardPacksById)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        ArgumentNullException.ThrowIfNull(rewardPacksById);

        if (!rewardPacksById.TryGetValue(packId, out var pack))
        {
            throw new InvalidOperationException($"Unknown reward pack id '{packId}'.");
        }

        if (pack.CandidateIds.Count != 3)
        {
            throw new InvalidOperationException($"Reward pack '{packId}' must expose exactly 3 candidates.");
        }

        return pack;
    }

    public RunState ClaimCards(
        RunState runState,
        RewardPackDefinition selectedPack,
        IEnumerable<string> selectedCardIds)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(selectedPack);
        ArgumentNullException.ThrowIfNull(selectedCardIds);

        var selected = selectedCardIds.ToList();
        if (selected.Count < selectedPack.MinPick || selected.Count > selectedPack.MaxPick)
        {
            throw new InvalidOperationException(
                $"Reward pack '{selectedPack.Id}' allows {selectedPack.MinPick}-{selectedPack.MaxPick} picks, got {selected.Count}.");
        }

        var candidates = selectedPack.CandidateIds.ToHashSet(StringComparer.Ordinal);
        foreach (var cardId in selected)
        {
            if (!candidates.Contains(cardId))
            {
                throw new InvalidOperationException(
                    $"Card id '{cardId}' is not a candidate in reward pack '{selectedPack.Id}'.");
            }
        }

        return runState with
        {
            MasterDeck = runState.MasterDeck.Concat(selected).ToList()
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
