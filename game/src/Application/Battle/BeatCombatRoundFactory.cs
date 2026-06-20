using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Enemies;

namespace RoguelikeCardGame.Application.Battle;

public sealed class BeatCombatRoundFactory
{
	public BeatRoundState CreateRound(
		CombatState combat,
		IReadOnlyDictionary<string, EnemyDefinition> enemiesById,
		int playerBeatCount = 3)
	{
		ArgumentNullException.ThrowIfNull(combat);
		ArgumentNullException.ThrowIfNull(enemiesById);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playerBeatCount);

		var enemyBeats = new List<EnemyBeatSlot>();
		foreach (var enemy in combat.Enemies.Where(enemy => enemy.CurrentHp > 0))
		{
			if (!enemiesById.TryGetValue(enemy.EnemyId, out var definition))
			{
				throw new InvalidOperationException($"Unknown enemy definition id '{enemy.EnemyId}'.");
			}

			var sequence = definition.BeatSequences.FirstOrDefault();
			if (sequence is null)
			{
				continue;
			}

			for (var index = 0; index < sequence.Beats.Count; index++)
			{
				var beat = sequence.Beats[index];
				enemyBeats.Add(new EnemyBeatSlot
				{
					EnemyInstanceId = enemy.InstanceId,
					BeatIndex = index,
					ActionCardId = beat.ActionCardId,
					Actions = beat.Actions,
					Hidden = beat.Hidden
				});
			}
		}

		return new BeatRoundState
		{
			BeatCount = playerBeatCount,
			PlayerBeats = Enumerable.Range(0, playerBeatCount)
				.Select(index => new PlayerBeatSlot { BeatIndex = index })
				.ToList(),
			EnemyBeats = enemyBeats
		};
	}
}
