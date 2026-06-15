using RoguelikeCardGame.Domain.Cards;

namespace RoguelikeCardGame.Application.Runs;

public sealed record WeaponStartingPoolDefinition
{
    public required string WeaponId { get; init; }

    public List<string> CardIds { get; init; } = new();
}

public sealed record StartingDeckSelection
{
    public required string MainHandWeaponId { get; init; }

    public required string OffHandWeaponId { get; init; }

    public List<string> MainHandCardIds { get; init; } = new();

    public List<string> OffHandCardIds { get; init; } = new();
}

public sealed record StartingDeckValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; init; } = new();

    public List<string> SelectedCardIds { get; init; } = new();
}

public sealed class StartingDeckSelectionService
{
    public const int MainHandPickCount = 6;
    public const int OffHandPickCount = 4;

    public StartingDeckValidationResult BuildAutomaticStarterDeck(
        string mainHandWeaponId,
        string offHandWeaponId,
        IEnumerable<WeaponStartingPoolDefinition> startingPools,
        IReadOnlyDictionary<string, CardDefinition> cardsById)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mainHandWeaponId);
        ArgumentException.ThrowIfNullOrWhiteSpace(offHandWeaponId);
        ArgumentNullException.ThrowIfNull(startingPools);
        ArgumentNullException.ThrowIfNull(cardsById);

        var poolsByWeapon = startingPools.ToDictionary(pool => pool.WeaponId, StringComparer.Ordinal);
        var errors = new List<string>();

        if (!poolsByWeapon.TryGetValue(mainHandWeaponId, out var mainHandPool))
        {
            errors.Add($"main hand: unknown starting weapon '{mainHandWeaponId}'.");
        }

        if (!poolsByWeapon.TryGetValue(offHandWeaponId, out var offHandPool))
        {
            errors.Add($"off hand: unknown starting weapon '{offHandWeaponId}'.");
        }

        if (errors.Count > 0)
        {
            return new StartingDeckValidationResult { Errors = errors };
        }

        var mainHandCardIds = mainHandPool!.CardIds.ToList();
        var offHandCardIds = new List<string>();
        foreach (var cardId in offHandPool!.CardIds)
        {
            if (!cardsById.TryGetValue(cardId, out var card))
            {
                errors.Add($"off hand: unknown card '{cardId}' in starting pool.");
                continue;
            }

            if (card.Type == CardType.Action)
            {
                offHandCardIds.Add(cardId);
            }
        }

        if (errors.Count > 0)
        {
            return new StartingDeckValidationResult
            {
                Errors = errors,
                SelectedCardIds = mainHandCardIds.Concat(offHandCardIds).ToList()
            };
        }

        return Validate(
            new StartingDeckSelection
            {
                MainHandWeaponId = mainHandWeaponId,
                OffHandWeaponId = offHandWeaponId,
                MainHandCardIds = mainHandCardIds,
                OffHandCardIds = offHandCardIds
            },
            poolsByWeapon.Values);
    }

    public StartingDeckValidationResult Validate(
        StartingDeckSelection selection,
        IEnumerable<WeaponStartingPoolDefinition> startingPools)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(startingPools);

        var poolsByWeapon = startingPools.ToDictionary(pool => pool.WeaponId, StringComparer.Ordinal);
        var errors = new List<string>();

        if (string.Equals(selection.MainHandWeaponId, selection.OffHandWeaponId, StringComparison.Ordinal))
        {
            errors.Add("Main hand and off hand weapons must be different.");
        }

        ValidateWeaponPick(
            "main hand",
            selection.MainHandWeaponId,
            selection.MainHandCardIds,
            MainHandPickCount,
            poolsByWeapon,
            errors);
        ValidateWeaponPick(
            "off hand",
            selection.OffHandWeaponId,
            selection.OffHandCardIds,
            OffHandPickCount,
            poolsByWeapon,
            errors);

        return new StartingDeckValidationResult
        {
            Errors = errors,
            SelectedCardIds = selection.MainHandCardIds.Concat(selection.OffHandCardIds).ToList()
        };
    }

    public List<string> BuildStarterDeck(
        StartingDeckSelection selection,
        IEnumerable<WeaponStartingPoolDefinition> startingPools)
    {
        var result = Validate(selection, startingPools);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
        }

        return result.SelectedCardIds;
    }

    private static void ValidateWeaponPick(
        string label,
        string weaponId,
        IReadOnlyList<string> selectedCardIds,
        int requiredCount,
        IReadOnlyDictionary<string, WeaponStartingPoolDefinition> poolsByWeapon,
        List<string> errors)
    {
        if (!poolsByWeapon.TryGetValue(weaponId, out var pool))
        {
            errors.Add($"{label}: unknown starting weapon '{weaponId}'.");
            return;
        }

        if (selectedCardIds.Count != requiredCount)
        {
            errors.Add($"{label}: must select exactly {requiredCount} cards, got {selectedCardIds.Count}.");
        }

        var availableCounts = CountByCardId(pool.CardIds);
        var selectedCounts = CountByCardId(selectedCardIds);
        foreach (var (cardId, selectedCount) in selectedCounts)
        {
            if (!availableCounts.TryGetValue(cardId, out var availableCount))
            {
                errors.Add($"{label}: card '{cardId}' does not belong to weapon '{weaponId}' starting pool.");
                continue;
            }

            if (selectedCount > availableCount)
            {
                errors.Add($"{label}: selected {selectedCount} copies of '{cardId}', but only {availableCount} are available.");
            }
        }
    }

    private static Dictionary<string, int> CountByCardId(IEnumerable<string> cardIds)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cardId in cardIds)
        {
            result[cardId] = result.GetValueOrDefault(cardId) + 1;
        }

        return result;
    }
}
