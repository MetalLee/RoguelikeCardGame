using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Application.Battle;

public sealed class BeatRoundPlanningService
{
    public CombatState PlaceActionCardInBeat(
        CombatState combat,
        string cardInstanceId,
        string cardId,
        int handIndex,
        int beatIndex,
        CardDefinition card)
    {
        EnsurePlayerTurn(combat);
        if (card.Type != CardType.Action)
        {
            throw new InvalidOperationException("Only action cards can be placed in player beat slots.");
        }

        if (!string.Equals(card.Id, cardId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Card definition '{card.Id}' does not match requested card id '{cardId}'.");
        }

        var round = RequireBeatRound(combat);
        if (handIndex < 0 ||
            handIndex >= combat.DeckZones.Hand.Count ||
            !string.Equals(combat.DeckZones.Hand[handIndex], cardInstanceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Card instance '{cardInstanceId}' is not in hand slot {handIndex}.");
        }

        var playerBeats = round.PlayerBeats.ToList();
        var playerBeatIndex = playerBeats.FindIndex(beat => beat.BeatIndex == beatIndex);
        if (playerBeatIndex < 0)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} does not exist.");
        }

        var playerBeat = playerBeats[playerBeatIndex];
        if (!string.IsNullOrWhiteSpace(playerBeat.CardInstanceId) ||
            !string.IsNullOrWhiteSpace(playerBeat.CardId) ||
            playerBeat.Target is not null)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} is already occupied.");
        }

        playerBeats[playerBeatIndex] = playerBeat with
        {
            CardInstanceId = cardInstanceId,
            CardId = cardId,
            Target = null
        };

        var hand = combat.DeckZones.Hand.ToList();
        hand.RemoveAt(handIndex);

        return combat with
        {
            BeatRound = round with { PlayerBeats = playerBeats },
            DeckZones = combat.DeckZones with { Hand = hand }
        };
    }

    public CombatState PlaceActionCardIntoNextPlayerBeatAndTarget(
        CombatState combat,
        string cardInstanceId,
        string cardId,
        int handIndex,
        CardDefinition card,
        BeatTarget requestedTarget,
        BeatCombatService beatCombatService)
    {
        EnsurePlayerTurn(combat);
        var round = RequireBeatRound(combat);
        var playerBeat = round.PlayerBeats
            .OrderBy(beat => beat.BeatIndex)
            .FirstOrDefault(IsEmptyPlayerBeat);
        if (playerBeat is null)
        {
            throw new InvalidOperationException("No empty player beat slot is available for deployment.");
        }

        var resolvedTarget = ResolveDeploymentTarget(round, requestedTarget);
        var placed = PlaceActionCardInBeat(
            combat,
            cardInstanceId,
            cardId,
            handIndex,
            playerBeat.BeatIndex,
            card);
        return SetTarget(placed, playerBeat.BeatIndex, resolvedTarget, beatCombatService);
    }

    public CombatState SetEnemyBeatTarget(
        CombatState combat,
        int beatIndex,
        string enemyInstanceId,
        int enemyBeatIndex,
        BeatCombatService beatCombatService)
    {
        return SetTarget(
            combat,
            beatIndex,
            new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBeat,
                EnemyInstanceId = enemyInstanceId,
                EnemyBeatIndex = enemyBeatIndex
            },
            beatCombatService);
    }

    public CombatState SetEnemyBodyTarget(
        CombatState combat,
        int beatIndex,
        string enemyInstanceId,
        BeatCombatService beatCombatService)
    {
        return SetTarget(
            combat,
            beatIndex,
            new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBody,
                EnemyInstanceId = enemyInstanceId
            },
            beatCombatService);
    }

    public CombatState DiscardSlottedActionCards(CombatState combat)
    {
        var round = RequireBeatRound(combat);
        var discardPile = combat.DeckZones.DiscardPile.ToList();
        discardPile.AddRange(round.PlayerBeats
            .Select(beat => beat.CardInstanceId)
            .Where(cardInstanceId => !string.IsNullOrWhiteSpace(cardInstanceId))
            .Select(cardInstanceId => cardInstanceId!));

        return combat with
        {
            DeckZones = combat.DeckZones with { DiscardPile = discardPile }
        };
    }

    public CombatState CancelUntargetedBeatPlacement(
        CombatState combat,
        int beatIndex,
        string cardInstanceId)
    {
        EnsurePlayerTurn(combat);
        var round = RequireBeatRound(combat);
        var playerBeats = round.PlayerBeats.ToList();
        var playerBeatIndex = playerBeats.FindIndex(beat => beat.BeatIndex == beatIndex);
        if (playerBeatIndex < 0)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} does not exist.");
        }

        var playerBeat = playerBeats[playerBeatIndex];
        if (!string.Equals(playerBeat.CardInstanceId, cardInstanceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} does not contain card instance '{cardInstanceId}'.");
        }

        if (playerBeat.Target is not null)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} already has a target.");
        }

        playerBeats[playerBeatIndex] = playerBeat with
        {
            CardInstanceId = null,
            CardId = null,
            Target = null
        };

        var hand = combat.DeckZones.Hand.ToList();
        hand.Add(cardInstanceId);

        return combat with
        {
            BeatRound = round with { PlayerBeats = playerBeats },
            DeckZones = combat.DeckZones with { Hand = hand }
        };
    }

    private static CombatState SetTarget(
        CombatState combat,
        int beatIndex,
        BeatTarget target,
        BeatCombatService beatCombatService)
    {
        EnsurePlayerTurn(combat);
        var round = RequireBeatRound(combat);
        var playerBeats = round.PlayerBeats.ToList();
        var playerBeatIndex = playerBeats.FindIndex(beat => beat.BeatIndex == beatIndex);
        if (playerBeatIndex < 0)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} does not exist.");
        }

        var playerBeat = playerBeats[playerBeatIndex];
        if (string.IsNullOrWhiteSpace(playerBeat.CardInstanceId) && string.IsNullOrWhiteSpace(playerBeat.CardId))
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} has no card to target.");
        }

        playerBeats[playerBeatIndex] = playerBeat with { Target = target };
        var updatedRound = round with { PlayerBeats = playerBeats };
        var validation = beatCombatService.ValidatePlayerBeatTargets(updatedRound, combat);
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException(validation.Message ?? "Beat round target validation failed.");
        }

        return combat with { BeatRound = updatedRound };
    }

    private static BeatTarget ResolveDeploymentTarget(BeatRoundState round, BeatTarget requestedTarget)
    {
        if (requestedTarget.Kind == BeatTargetKind.EnemyBeat)
        {
            return requestedTarget;
        }

        if (requestedTarget.Kind != BeatTargetKind.EnemyBody)
        {
            throw new InvalidOperationException($"Unsupported beat target kind '{requestedTarget.Kind}'.");
        }

        var lockedEnemyBeatIndices = round.PlayerBeats
            .Where(beat =>
                beat.Target?.Kind == BeatTargetKind.EnemyBeat &&
                string.Equals(beat.Target.EnemyInstanceId, requestedTarget.EnemyInstanceId, StringComparison.Ordinal) &&
                beat.Target.EnemyBeatIndex is not null)
            .Select(beat => beat.Target!.EnemyBeatIndex!.Value)
            .ToHashSet();

        var firstUnlockedEnemyBeat = round.EnemyBeats
            .Where(beat => string.Equals(beat.EnemyInstanceId, requestedTarget.EnemyInstanceId, StringComparison.Ordinal))
            .OrderBy(beat => beat.BeatIndex)
            .FirstOrDefault(beat => !lockedEnemyBeatIndices.Contains(beat.BeatIndex));

        if (firstUnlockedEnemyBeat is not null)
        {
            return new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBeat,
                EnemyInstanceId = requestedTarget.EnemyInstanceId,
                EnemyBeatIndex = firstUnlockedEnemyBeat.BeatIndex
            };
        }

        return new BeatTarget
        {
            Kind = BeatTargetKind.EnemyBody,
            EnemyInstanceId = requestedTarget.EnemyInstanceId
        };
    }

    private static bool IsEmptyPlayerBeat(PlayerBeatSlot beat)
    {
        return string.IsNullOrWhiteSpace(beat.CardInstanceId) &&
            string.IsNullOrWhiteSpace(beat.CardId) &&
            beat.Target is null;
    }

    private static void EnsurePlayerTurn(CombatState combat)
    {
        if (combat.Status != CombatStatus.PlayerTurn)
        {
            throw new InvalidOperationException("Beat round planning is only allowed during the player turn.");
        }
    }

    private static BeatRoundState RequireBeatRound(CombatState combat)
    {
        return combat.BeatRound ?? throw new InvalidOperationException("Combat has no beat round to plan.");
    }
}
