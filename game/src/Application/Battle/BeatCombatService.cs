using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Application.Battle;

public sealed record BeatCollisionResult
{
    public int PlayerDamageTaken { get; init; }
    public int EnemyDamageTaken { get; init; }
    public int SuccessfulPlayerActions { get; init; }
    public int SuccessfulEnemyActions { get; init; }
}

public sealed record BeatRoundResolveResult
{
    public required CombatState Combat { get; init; }
    public List<CombatLogEvent> Events { get; init; } = new();
}

public sealed class BeatCombatService
{
    private readonly Func<int> dodgeRollPercent;

    public BeatCombatService(Func<int>? dodgeRollPercent = null)
    {
        this.dodgeRollPercent = dodgeRollPercent ?? (() => 100);
    }

    public BeatCollisionResult ResolveActionCollision(
        IReadOnlyList<BeatActionDefinition> playerActions,
        IReadOnlyList<BeatActionDefinition> enemyActions,
        BeatResistanceProfile enemyResistance,
        BeatResistanceProfile playerResistance)
    {
        var playerQueue = playerActions.SelectMany(action => action.ExpandRepeats()).ToList();
        var enemyQueue = enemyActions.SelectMany(action => action.ExpandRepeats()).ToList();
        var max = Math.Max(playerQueue.Count, enemyQueue.Count);
        var playerDamage = 0;
        var enemyDamage = 0;
        var successfulPlayer = 0;
        var successfulEnemy = 0;

        for (var index = 0; index < max; index++)
        {
            var player = index < playerQueue.Count ? playerQueue[index] : null;
            var enemy = index < enemyQueue.Count ? enemyQueue[index] : null;
            var exchange = ResolveActionPair(player, enemy, enemyResistance, playerResistance);
            playerDamage += exchange.PlayerDamageTaken;
            enemyDamage += exchange.EnemyDamageTaken;
            successfulPlayer += exchange.SuccessfulPlayerActions;
            successfulEnemy += exchange.SuccessfulEnemyActions;
        }

        return new BeatCollisionResult
        {
            PlayerDamageTaken = playerDamage,
            EnemyDamageTaken = enemyDamage,
            SuccessfulPlayerActions = successfulPlayer,
            SuccessfulEnemyActions = successfulEnemy
        };
    }

    private BeatCollisionResult ResolveActionPair(
        BeatActionDefinition? player,
        BeatActionDefinition? enemy,
        BeatResistanceProfile enemyResistance,
        BeatResistanceProfile playerResistance)
    {
        if (player is null && enemy is null)
        {
            return new BeatCollisionResult();
        }

        if (player is null)
        {
            return ResolveUnopposedEnemy(enemy!, playerResistance);
        }

        if (enemy is null)
        {
            return ResolveUnopposedPlayer(player, enemyResistance);
        }

        if (player.Kind == BeatActionKind.Attack && enemy.Kind == BeatActionKind.Attack)
        {
            return ResolveAttackVsAttack(player, enemy, enemyResistance, playerResistance);
        }

        if (player.Kind == BeatActionKind.Attack && enemy.Kind == BeatActionKind.Block)
        {
            var remaining = DamageAfterBlock(player, enemyResistance, enemy);
            return new BeatCollisionResult
            {
                EnemyDamageTaken = remaining,
                SuccessfulPlayerActions = remaining > 0 ? 1 : 0,
                SuccessfulEnemyActions = enemy.Value > 0 ? 1 : 0
            };
        }

        if (player.Kind == BeatActionKind.Block && enemy.Kind == BeatActionKind.Attack)
        {
            var remaining = DamageAfterBlock(enemy, playerResistance, player);
            return new BeatCollisionResult
            {
                PlayerDamageTaken = remaining,
                SuccessfulPlayerActions = player.Value > 0 ? 1 : 0,
                SuccessfulEnemyActions = remaining > 0 ? 1 : 0
            };
        }

        if (player.Kind == BeatActionKind.Dodge && enemy.Kind == BeatActionKind.Attack)
        {
            var dodged = dodgeRollPercent() <= player.DodgeChancePercent;
            return new BeatCollisionResult
            {
                PlayerDamageTaken = dodged ? 0 : DamageAgainstResistance(enemy, playerResistance),
                SuccessfulPlayerActions = dodged ? 1 : 0,
                SuccessfulEnemyActions = dodged ? 0 : 1
            };
        }

        if (player.Kind == BeatActionKind.Attack && enemy.Kind == BeatActionKind.Dodge)
        {
            var dodged = dodgeRollPercent() <= enemy.DodgeChancePercent;
            return new BeatCollisionResult
            {
                EnemyDamageTaken = dodged ? 0 : DamageAgainstResistance(player, enemyResistance),
                SuccessfulPlayerActions = dodged ? 0 : 1,
                SuccessfulEnemyActions = dodged ? 1 : 0
            };
        }

        return new BeatCollisionResult();
    }

    private static BeatCollisionResult ResolveAttackVsAttack(
        BeatActionDefinition player,
        BeatActionDefinition enemy,
        BeatResistanceProfile enemyResistance,
        BeatResistanceProfile playerResistance)
    {
        return new BeatCollisionResult
        {
            EnemyDamageTaken = DamageAgainstResistance(player, enemyResistance),
            PlayerDamageTaken = DamageAgainstResistance(enemy, playerResistance),
            SuccessfulPlayerActions = 1,
            SuccessfulEnemyActions = 1
        };
    }

    private static BeatCollisionResult ResolveUnopposedPlayer(BeatActionDefinition player, BeatResistanceProfile enemyResistance)
    {
        if (player.Kind != BeatActionKind.Attack)
        {
            return new BeatCollisionResult();
        }

        return new BeatCollisionResult
        {
            EnemyDamageTaken = DamageAgainstResistance(player, enemyResistance),
            SuccessfulPlayerActions = 1
        };
    }

    private static BeatCollisionResult ResolveUnopposedEnemy(BeatActionDefinition enemy, BeatResistanceProfile playerResistance)
    {
        if (enemy.Kind != BeatActionKind.Attack)
        {
            return new BeatCollisionResult();
        }

        return new BeatCollisionResult
        {
            PlayerDamageTaken = DamageAgainstResistance(enemy, playerResistance),
            SuccessfulEnemyActions = 1
        };
    }

    private static int DamageAgainstResistance(BeatActionDefinition action, BeatResistanceProfile resistance)
    {
        var attackType = action.AttackType ?? BeatAttackType.Slash;
        return BeatResistanceProfile.Apply(resistance.GradeFor(attackType), action.Value);
    }

    private static int DamageAfterBlock(
        BeatActionDefinition attack,
        BeatResistanceProfile defenderResistance,
        BeatActionDefinition block)
    {
        return Math.Max(0, DamageAgainstResistance(attack, defenderResistance) - block.Value);
    }
}
