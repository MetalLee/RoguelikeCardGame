using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Runs;

namespace RoguelikeCardGame.Application.Battle;

public sealed class CombatStateFactory
{
    public CombatState CreateCombat(
        string combatId,
        RunState runState,
        EncounterDefinition encounter,
        IReadOnlyDictionary<string, EnemyDefinition> enemiesById)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(combatId);
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(enemiesById);

        var enemyStates = encounter.Enemies.Select(enemyEntry =>
        {
            if (!enemiesById.TryGetValue(enemyEntry.EnemyId, out var enemyDefinition))
            {
                throw new InvalidOperationException($"Unknown enemy definition id '{enemyEntry.EnemyId}'.");
            }

            return new CombatEnemyState
            {
                InstanceId = enemyEntry.InstanceId,
                EnemyId = enemyEntry.EnemyId,
                MaxHp = enemyDefinition.MaxHp,
                CurrentHp = enemyDefinition.MaxHp,
                Block = 0,
                IntentIndex = 0
            };
        }).ToList();

        return new CombatState
        {
            CombatId = combatId,
            EncounterId = encounter.Id,
            Status = CombatStatus.NotStarted,
            TurnNumber = 0,
            PlayerMaxHp = runState.PlayerMaxHp,
            PlayerHp = runState.PlayerMaxHp,
            PlayerBlock = 0,
            BaseActionPoints = runState.BaseActionPoints,
            CardsPerTurn = runState.CardsPerTurn,
            ActionPoints = 0,
            Chain = 0,
            DeckZones = new DeckZones
            {
                DrawPile = runState.MasterDeck.ToList()
            },
            Enemies = enemyStates,
            Log =
            [
                new CombatLogEvent
                {
                    EventId = $"{combatId}_started",
                    EventType = CombatLogEventType.CombatStarted,
                    TurnNumber = 1,
                    SourceId = encounter.Id,
                    Metadata = new Dictionary<string, string>
                    {
                        ["run_id"] = runState.RunId
                    }
                }
            ]
        };
    }
}
