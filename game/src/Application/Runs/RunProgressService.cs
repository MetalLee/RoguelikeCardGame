using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Runs;

namespace RoguelikeCardGame.Application.Runs;

public sealed class RunProgressService
{
    public RunState PrepareForCombat(RunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);
        if (runState.Status != RunStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress run can prepare a new combat.");
        }

        return runState with
        {
            PlayerHp = runState.PlayerMaxHp
        };
    }

    public RunState ApplyCombatResult(RunState runState, CombatState combat, EncounterDefinition? encounter = null)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(combat);

        if (combat.Status == CombatStatus.Defeat)
        {
            return runState with
            {
                Status = RunStatus.Failed,
                PlayerHp = 0
            };
        }

        if (combat.Status == CombatStatus.Victory && encounter?.NodeType == EncounterNodeType.Boss)
        {
            return runState with
            {
                Status = RunStatus.Cleared,
                PlayerHp = Math.Clamp(combat.PlayerHp, 0, runState.PlayerMaxHp)
            };
        }

        return runState with
        {
            PlayerHp = Math.Clamp(combat.PlayerHp, 0, runState.PlayerMaxHp)
        };
    }

    public RunState AdvanceAfterRewards(RunState runState, EncounterDefinition encounter)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(encounter);

        if (runState.Status != RunStatus.InProgress)
        {
            throw new InvalidOperationException("Only an in-progress run can advance to the next encounter.");
        }

        if (encounter.NodeType == EncounterNodeType.Boss)
        {
            return runState with
            {
                Status = RunStatus.Cleared
            };
        }

        var nextIndex = runState.EncounterSequence.Count == 0
            ? runState.CurrentEncounterIndex
            : Math.Min(runState.CurrentEncounterIndex + 1, runState.EncounterSequence.Count - 1);

        return runState with
        {
            CurrentEncounterIndex = nextIndex,
            PlayerHp = runState.PlayerMaxHp
        };
    }
}
