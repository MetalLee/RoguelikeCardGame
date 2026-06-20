# 三拍拆招战斗重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现第一版“三拍拆招 + 成功产彩 + 终结分镜切入”战斗重构 MVP 的规则核心、数据入口和最小 Godot 接入。

**Architecture:** 先新增独立 `BeatCombat` 规则模型与服务，避免直接破坏现有 `CardPlayService` / `CombatTurnService`。规则层稳定后，再把 `CardDefinition`、`EnemyDefinition`、数据 Schema 和 Battle UI 逐步接入新模型。第一版只使用无色彩能，不实现色彩碎片、行动牌附魔、五色终结转译和魔物卡奖励。

**Tech Stack:** Godot 4.6.x .NET / C#，自定义 `game/tests/Unit/Program.cs` 单元测试，JSON 数据与 Schema，现有 `GameContent` 内容加载器。

---

## 范围检查

本计划只覆盖 spec 中的第一版战斗重构 MVP：

- 包含：三拍编排、动作对撞、无色彩能、终结槽、手牌保留、每轮抽 2、最多 2 个魔物的最小 UI 接入。
- 不包含：彩能附魔、五色终结转译、隐藏魔物拍、魔物卡奖励、完整 6 战 Run 替换、多个终结槽。

如果执行过程中发现 UI 接入过大，应先完成规则层和一个本地调试 / 可视化验证入口，再单独拆 UI polish 计划。

## 文件结构

新增文件：

- `game/src/Domain/Combat/BeatCombatActions.cs`  
  定义动作类型、攻击类型、抗性档位和动作数据结构。

- `game/src/Domain/Combat/BeatCombatRound.cs`  
  定义玩家拍位、魔物拍位、目标分配、终结槽和回合状态。

- `game/src/Application/Battle/BeatCombatService.cs`  
  负责拍位编排校验、动作对撞、成功动作产彩、终结槽释放和轮末清理。

- `game/src/Application/Battle/BeatCombatRoundFactory.cs`  
  从 `CombatState`、卡牌定义和魔物定义创建一轮三拍拆招状态。

修改文件：

- `game/src/Domain/Cards/CardDefinition.cs`  
  增加行动牌动作列表、卡牌来源标签、终结牌攻击类型。

- `game/src/Domain/Enemies/EnemyDefinition.cs`  
  增加魔物抗性和魔物动作序列字段。

- `game/src/Domain/Combat/CombatState.cs`  
  增加 `BeatRound`、终结槽和新战斗模式需要的状态字段。

- `game/src/Domain/Combat/CombatLogEvent.cs`  
  增加三拍拆招相关日志事件类型。

- `game/src/Infrastructure/Content/GameContent.cs`  
  解析新卡牌动作字段、魔物抗性和魔物拍位字段。

- `game/data/schemas/gameplay/cards.schema.json`  
  增加 `actions`、`source`、终结牌 `attack_type` 字段。

- `game/data/schemas/gameplay/enemies.schema.json`  
  增加 `resistances` 和 `beat_sequences` 字段。

- `game/data/gameplay/cards/cards.json`  
  给 MVP 卡牌补最小动作数据，费用字段保留兼容但不驱动新战斗。

- `game/data/gameplay/enemies/enemies.json`  
  给验证切片魔物补抗性和动作序列。

- `game/src/Presentation/Battle/BattleHudView.cs`  
  隐藏或弱化行动点显示，保留 6 格彩能与终结槽位置。

- `game/src/Presentation/Battle/BattleScreen.cs`  
  加入最小拍位区、魔物拍位显示和终结槽事件。

- `game/src/Presentation/Cards/CardPanel.cs`  
  显示行动牌动作串和终结牌攻击类型 / 彩能需求。

- `game/tests/Unit/Program.cs`  
  追加三拍拆招规则测试、内容加载测试和回归测试。

## 任务 1：新增动作、抗性和回合模型

**Files:**

- Create: `game/src/Domain/Combat/BeatCombatActions.cs`
- Create: `game/src/Domain/Combat/BeatCombatRound.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖动作模型序列化与基础值**

在 `game/tests/Unit/Program.cs` 的现有序列化测试后追加：

```csharp
var slashAction = new BeatActionDefinition
{
    Kind = BeatActionKind.Attack,
    AttackType = BeatAttackType.Slash,
    Value = 6,
    Repeat = 2
};
var blockAction = new BeatActionDefinition
{
    Kind = BeatActionKind.Block,
    Value = 4,
    Repeat = 1
};
var serializedBeatActions = JsonSerializer.Serialize(new[] { slashAction, blockAction }, options);
var deserializedBeatActions = JsonSerializer.Deserialize<List<BeatActionDefinition>>(serializedBeatActions, options);
AssertEqual(2, deserializedBeatActions?.Count, "Beat actions serialize and deserialize");
AssertEqual(BeatActionKind.Attack, deserializedBeatActions?[0].Kind, "Beat action keeps attack kind");
AssertEqual(BeatAttackType.Slash, deserializedBeatActions?[0].AttackType, "Beat action keeps slash type");
AssertEqual(2, deserializedBeatActions?[0].Repeat, "Beat action keeps repeat count");
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 编译失败，提示 `BeatActionDefinition` 或相关 enum 未定义。

- [ ] **Step 3: 新增 `BeatCombatActions.cs`**

Create `game/src/Domain/Combat/BeatCombatActions.cs`:

```csharp
using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum BeatActionKind
{
    Attack,
    Block,
    Dodge
}

public enum BeatAttackType
{
    Slash,
    Strike,
    Projectile
}

public enum BeatResistanceGrade
{
    Resist,
    Standard,
    Weakness
}

public sealed record BeatActionDefinition
{
    [JsonPropertyName("kind")]
    public BeatActionKind Kind { get; init; }

    [JsonPropertyName("attack_type")]
    public BeatAttackType? AttackType { get; init; }

    [JsonPropertyName("value")]
    public int Value { get; init; }

    [JsonPropertyName("repeat")]
    public int Repeat { get; init; } = 1;

    [JsonPropertyName("dodge_chance_percent")]
    public int DodgeChancePercent { get; init; } = 50;

    public IEnumerable<BeatActionDefinition> ExpandRepeats()
    {
        var repeat = Math.Max(1, Repeat);
        for (var index = 0; index < repeat; index++)
        {
            yield return this with { Repeat = 1 };
        }
    }
}

public sealed record BeatResistanceProfile
{
    [JsonPropertyName("slash")]
    public BeatResistanceGrade Slash { get; init; } = BeatResistanceGrade.Standard;

    [JsonPropertyName("strike")]
    public BeatResistanceGrade Strike { get; init; } = BeatResistanceGrade.Standard;

    [JsonPropertyName("projectile")]
    public BeatResistanceGrade Projectile { get; init; } = BeatResistanceGrade.Standard;

    public BeatResistanceGrade GradeFor(BeatAttackType attackType)
    {
        return attackType switch
        {
            BeatAttackType.Slash => Slash,
            BeatAttackType.Strike => Strike,
            BeatAttackType.Projectile => Projectile,
            _ => BeatResistanceGrade.Standard
        };
    }

    public static int Apply(BeatResistanceGrade grade, int value)
    {
        return grade switch
        {
            BeatResistanceGrade.Resist => value / 2,
            BeatResistanceGrade.Weakness => value * 3 / 2,
            _ => value
        };
    }
}
```

- [ ] **Step 4: 新增 `BeatCombatRound.cs`**

Create `game/src/Domain/Combat/BeatCombatRound.cs`:

```csharp
using System.Text.Json.Serialization;

namespace RoguelikeCardGame.Domain.Combat;

public enum BeatTargetKind
{
    EnemyBeat,
    EnemyBody
}

public sealed record BeatTarget
{
    [JsonPropertyName("kind")]
    public BeatTargetKind Kind { get; init; }

    [JsonPropertyName("enemy_instance_id")]
    public required string EnemyInstanceId { get; init; }

    [JsonPropertyName("enemy_beat_index")]
    public int? EnemyBeatIndex { get; init; }
}

public sealed record PlayerBeatSlot
{
    [JsonPropertyName("beat_index")]
    public int BeatIndex { get; init; }

    [JsonPropertyName("card_instance_id")]
    public string? CardInstanceId { get; init; }

    [JsonPropertyName("card_id")]
    public string? CardId { get; init; }

    [JsonPropertyName("target")]
    public BeatTarget? Target { get; init; }
}

public sealed record EnemyBeatSlot
{
    [JsonPropertyName("enemy_instance_id")]
    public required string EnemyInstanceId { get; init; }

    [JsonPropertyName("beat_index")]
    public int BeatIndex { get; init; }

    [JsonPropertyName("action_card_id")]
    public required string ActionCardId { get; init; }

    [JsonPropertyName("actions")]
    public List<BeatActionDefinition> Actions { get; init; } = new();

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }
}

public sealed record FinisherSlotState
{
    [JsonPropertyName("card_instance_id")]
    public string? CardInstanceId { get; init; }

    [JsonPropertyName("card_id")]
    public string? CardId { get; init; }

    [JsonPropertyName("preserve_after_round")]
    public bool PreserveAfterRound { get; init; }
}

public sealed record BeatRoundState
{
    [JsonPropertyName("beat_count")]
    public int BeatCount { get; init; } = 3;

    [JsonPropertyName("player_beats")]
    public List<PlayerBeatSlot> PlayerBeats { get; init; } = new();

    [JsonPropertyName("enemy_beats")]
    public List<EnemyBeatSlot> EnemyBeats { get; init; } = new();

    [JsonPropertyName("finisher_slot")]
    public FinisherSlotState FinisherSlot { get; init; } = new();
}
```

- [ ] **Step 5: 运行测试确认通过**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS，或只剩后续尚未实现测试失败。

- [ ] **Step 6: 提交**

```bash
git add game/src/Domain/Combat/BeatCombatActions.cs game/src/Domain/Combat/BeatCombatRound.cs game/tests/Unit/Program.cs
git commit -m "feat: add beat combat domain model"
```

## 任务 2：扩展卡牌与魔物定义

**Files:**

- Modify: `game/src/Domain/Cards/CardDefinition.cs`
- Modify: `game/src/Domain/Enemies/EnemyDefinition.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖卡牌动作与魔物抗性**

在 `Program.cs` 中卡牌定义序列化断言附近追加：

```csharp
var beatSlashCard = strike with
{
    Id = "card.beat_slash",
    BeatActions =
    [
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 6, Repeat = 1 }
    ],
    CardSource = "weapon",
    FinisherAttackType = null
};
var beatFinisher = finisher with
{
    Id = "card.beat_finisher",
    FinisherAttackType = BeatAttackType.Projectile
};
var beatEnemy = enemy with
{
    Id = "enemy.beat_dummy",
    Resistances = new BeatResistanceProfile
    {
        Slash = BeatResistanceGrade.Weakness,
        Strike = BeatResistanceGrade.Standard,
        Projectile = BeatResistanceGrade.Resist
    },
    BeatSequences =
    [
        new EnemyBeatSequenceDefinition
        {
            Id = "sequence.opening",
            Beats =
            [
                new EnemyBeatDefinition
                {
                    ActionCardId = "enemy_card.dummy_slash",
                    Actions =
                    [
                        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 4 }
                    ]
                }
            ]
        }
    ]
};
AssertEqual(BeatAttackType.Slash, beatSlashCard.BeatActions[0].AttackType, "CardDefinition stores beat action type");
AssertEqual(BeatAttackType.Projectile, beatFinisher.FinisherAttackType, "CardDefinition stores finisher attack type");
AssertEqual(BeatResistanceGrade.Weakness, beatEnemy.Resistances.Slash, "EnemyDefinition stores slash weakness");
AssertEqual(1, beatEnemy.BeatSequences[0].Beats.Count, "EnemyDefinition stores beat sequence");
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 编译失败，提示 `BeatActions`、`CardSource`、`FinisherAttackType`、`Resistances` 或 `BeatSequences` 不存在。

- [ ] **Step 3: 修改 `CardDefinition.cs`**

在 `CardDefinition` 内 `Effects` 后加入：

```csharp
[JsonPropertyName("beat_actions")]
public List<BeatActionDefinition> BeatActions { get; init; } = new();

[JsonPropertyName("card_source")]
public string CardSource { get; init; } = "weapon";

[JsonPropertyName("finisher_attack_type")]
public BeatAttackType? FinisherAttackType { get; init; }
```

确保文件顶部已有：

```csharp
using RoguelikeCardGame.Domain.Combat;
```

该 using 已存在；不要重复添加。

- [ ] **Step 4: 修改 `EnemyDefinition.cs`**

在 `EnemyIntentDefinition` 后、`EnemyDefinition` 前加入：

```csharp
public sealed record EnemyBeatDefinition
{
    [JsonPropertyName("action_card_id")]
    public required string ActionCardId { get; init; }

    [JsonPropertyName("actions")]
    public List<BeatActionDefinition> Actions { get; init; } = new();

    [JsonPropertyName("hidden")]
    public bool Hidden { get; init; }
}

public sealed record EnemyBeatSequenceDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("beats")]
    public List<EnemyBeatDefinition> Beats { get; init; } = new();
}
```

在 `EnemyDefinition` 内 `IntentSequence` 后加入：

```csharp
[JsonPropertyName("resistances")]
public BeatResistanceProfile Resistances { get; init; } = new();

[JsonPropertyName("beat_sequences")]
public List<EnemyBeatSequenceDefinition> BeatSequences { get; init; } = new();
```

在文件顶部加入：

```csharp
using RoguelikeCardGame.Domain.Combat;
```

- [ ] **Step 5: 运行测试确认通过**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS，或只剩后续尚未实现测试失败。

- [ ] **Step 6: 提交**

```bash
git add game/src/Domain/Cards/CardDefinition.cs game/src/Domain/Enemies/EnemyDefinition.cs game/tests/Unit/Program.cs
git commit -m "feat: add beat actions to cards and enemies"
```

## 任务 3：实现对撞结算服务

**Files:**

- Create: `game/src/Application/Battle/BeatCombatService.cs`
- Modify: `game/src/Domain/Combat/CombatLogEvent.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖格挡、剩余动作和抗性**

在 `Program.cs` 中追加：

```csharp
var beatService = new BeatCombatService(() => 100);
var clash = beatService.ResolveActionCollision(
    playerActions:
    [
        new BeatActionDefinition { Kind = BeatActionKind.Block, Value = 4 },
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Strike, Value = 6 }
    ],
    enemyActions:
    [
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 5 }
    ],
    enemyResistance: new BeatResistanceProfile { Strike = BeatResistanceGrade.Weakness },
    playerResistance: new BeatResistanceProfile());
AssertEqual(0, clash.PlayerDamageTaken, "Block reduces incoming attack to zero when value is high enough");
AssertEqual(9, clash.EnemyDamageTaken, "Remaining strike action hits weakness for 150 percent damage");
AssertEqual(2, clash.SuccessfulPlayerActions, "Effective block and successful attack both count as successful actions");

var resisted = beatService.ResolveActionCollision(
    playerActions:
    [
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Projectile, Value = 10 }
    ],
    enemyActions: [],
    enemyResistance: new BeatResistanceProfile { Projectile = BeatResistanceGrade.Resist },
    playerResistance: new BeatResistanceProfile());
AssertEqual(5, resisted.EnemyDamageTaken, "Projectile resistance halves direct damage");
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 编译失败，提示 `BeatCombatService` 或 `ResolveActionCollision` 不存在。

- [ ] **Step 3: 扩展日志事件类型**

在 `CombatLogEventType` 中追加：

```csharp
BeatActionResolved,
BeatEnergyGenerated,
FinisherSlotted,
FinisherReleased,
BeatRoundResolved
```

- [ ] **Step 4: 新增 `BeatCombatService.cs` 的对撞模型和方法**

Create `game/src/Application/Battle/BeatCombatService.cs`:

```csharp
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
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
            var remaining = Math.Max(0, DamageAgainstResistance(player, enemyResistance) - enemy.Value);
            return new BeatCollisionResult
            {
                EnemyDamageTaken = remaining,
                SuccessfulPlayerActions = remaining > 0 ? 1 : 0,
                SuccessfulEnemyActions = enemy.Value > 0 ? 1 : 0
            };
        }

        if (player.Kind == BeatActionKind.Block && enemy.Kind == BeatActionKind.Attack)
        {
            var remaining = Math.Max(0, DamageAgainstResistance(enemy, playerResistance) - player.Value);
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
}
```

- [ ] **Step 5: 运行测试确认通过**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS，或只剩后续尚未实现测试失败。

- [ ] **Step 6: 提交**

```bash
git add game/src/Application/Battle/BeatCombatService.cs game/src/Domain/Combat/CombatLogEvent.cs game/tests/Unit/Program.cs
git commit -m "feat: resolve beat action collisions"
```

## 任务 4：实现三拍目标分配校验

**Files:**

- Modify: `game/src/Application/Battle/BeatCombatService.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖重复锁定与空门规则**

追加：

```csharp
var enemyBeatsForTargeting = new List<EnemyBeatSlot>
{
    new()
    {
        EnemyInstanceId = "enemy_01",
        BeatIndex = 0,
        ActionCardId = "enemy_card.a",
        Actions = [new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 3 }]
    },
    new()
    {
        EnemyInstanceId = "enemy_01",
        BeatIndex = 1,
        ActionCardId = "enemy_card.b",
        Actions = [new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 3 }]
    }
};
var duplicateTargets = new List<PlayerBeatSlot>
{
    new() { BeatIndex = 0, CardInstanceId = "card_001", CardId = strike.Id, Target = new BeatTarget { Kind = BeatTargetKind.EnemyBeat, EnemyInstanceId = "enemy_01", EnemyBeatIndex = 0 } },
    new() { BeatIndex = 1, CardInstanceId = "card_002", CardId = strike.Id, Target = new BeatTarget { Kind = BeatTargetKind.EnemyBeat, EnemyInstanceId = "enemy_01", EnemyBeatIndex = 0 } }
};
Assert(!beatService.ValidateTargets(duplicateTargets, enemyBeatsForTargeting).Succeeded, "Duplicate enemy beat target is rejected");

var exposedBodyTargets = new List<PlayerBeatSlot>
{
    new() { BeatIndex = 0, CardInstanceId = "card_001", CardId = strike.Id, Target = new BeatTarget { Kind = BeatTargetKind.EnemyBeat, EnemyInstanceId = "enemy_01", EnemyBeatIndex = 0 } },
    new() { BeatIndex = 1, CardInstanceId = "card_002", CardId = strike.Id, Target = new BeatTarget { Kind = BeatTargetKind.EnemyBeat, EnemyInstanceId = "enemy_01", EnemyBeatIndex = 1 } },
    new() { BeatIndex = 2, CardInstanceId = "card_003", CardId = strike.Id, Target = new BeatTarget { Kind = BeatTargetKind.EnemyBody, EnemyInstanceId = "enemy_01" } }
};
Assert(beatService.ValidateTargets(exposedBodyTargets, enemyBeatsForTargeting).Succeeded, "Enemy body can be targeted after all beats are intercepted");
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 编译失败，提示 `ValidateTargets` 不存在。

- [ ] **Step 3: 在 `BeatCombatService.cs` 增加校验结果与方法**

在 `BeatRoundResolveResult` 后加入：

```csharp
public sealed record BeatTargetValidationResult
{
    public bool Succeeded { get; init; }

    public string? FailureReason { get; init; }

    public static BeatTargetValidationResult Success() => new() { Succeeded = true };

    public static BeatTargetValidationResult Failure(string reason) => new()
    {
        Succeeded = false,
        FailureReason = reason
    };
}
```

在 `BeatCombatService` 内加入：

```csharp
public BeatTargetValidationResult ValidateTargets(
    IReadOnlyList<PlayerBeatSlot> playerBeats,
    IReadOnlyList<EnemyBeatSlot> enemyBeats)
{
    var enemyBeatKeys = enemyBeats
        .Select(beat => $"{beat.EnemyInstanceId}:{beat.BeatIndex}")
        .ToHashSet(StringComparer.Ordinal);
    var usedEnemyBeatKeys = new HashSet<string>(StringComparer.Ordinal);
    var targetedByEnemy = playerBeats
        .Where(beat => beat.Target?.Kind == BeatTargetKind.EnemyBeat && beat.Target.EnemyBeatIndex is not null)
        .GroupBy(beat => beat.Target!.EnemyInstanceId, StringComparer.Ordinal)
        .ToDictionary(
            group => group.Key,
            group => group.Select(beat => beat.Target!.EnemyBeatIndex!.Value).ToHashSet(),
            StringComparer.Ordinal);

    foreach (var playerBeat in playerBeats)
    {
        if (playerBeat.Target is null)
        {
            continue;
        }

        if (playerBeat.Target.Kind == BeatTargetKind.EnemyBeat)
        {
            if (playerBeat.Target.EnemyBeatIndex is null)
            {
                return BeatTargetValidationResult.Failure("enemy_beat_target_requires_index");
            }

            var key = $"{playerBeat.Target.EnemyInstanceId}:{playerBeat.Target.EnemyBeatIndex.Value}";
            if (!enemyBeatKeys.Contains(key))
            {
                return BeatTargetValidationResult.Failure("enemy_beat_target_not_found");
            }

            if (!usedEnemyBeatKeys.Add(key))
            {
                return BeatTargetValidationResult.Failure("enemy_beat_target_duplicated");
            }

            continue;
        }

        var allEnemyBeatIndexes = enemyBeats
            .Where(beat => string.Equals(beat.EnemyInstanceId, playerBeat.Target.EnemyInstanceId, StringComparison.Ordinal))
            .Select(beat => beat.BeatIndex)
            .ToHashSet();
        targetedByEnemy.TryGetValue(playerBeat.Target.EnemyInstanceId, out var targetedIndexes);
        if (targetedIndexes is null || !allEnemyBeatIndexes.IsSubsetOf(targetedIndexes))
        {
            return BeatTargetValidationResult.Failure("enemy_body_requires_all_beats_targeted");
        }
    }

    return BeatTargetValidationResult.Success();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS，或只剩后续尚未实现测试失败。

- [ ] **Step 5: 提交**

```bash
git add game/src/Application/Battle/BeatCombatService.cs game/tests/Unit/Program.cs
git commit -m "feat: validate beat targeting"
```

## 任务 5：实现整轮结算、产彩与终结槽释放

**Files:**

- Modify: `game/src/Domain/Combat/CombatState.cs`
- Modify: `game/src/Application/Battle/BeatCombatService.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖成功动作产彩与终结释放**

追加：

```csharp
var beatCombat = CreatePlayableCombat(
    [strike.Id, finisher.Id],
    actionPoints: 0,
    colorEnergy: ColorEnergyPool.Empty(),
    enemies: [CreateEnemyState("enemy_01", currentHp: 30, maxHp: 30)]) with
{
    BeatRound = new BeatRoundState
    {
        BeatCount = 3,
        PlayerBeats =
        [
            new PlayerBeatSlot
            {
                BeatIndex = 0,
                CardInstanceId = beatCombatCardInstanceId,
                CardId = beatSlashCard.Id,
                Target = new BeatTarget { Kind = BeatTargetKind.EnemyBeat, EnemyInstanceId = "enemy_01", EnemyBeatIndex = 0 }
            }
        ],
        EnemyBeats =
        [
            new EnemyBeatSlot
            {
                EnemyInstanceId = "enemy_01",
                BeatIndex = 0,
                ActionCardId = "enemy_card.dummy",
                Actions = []
            }
        ],
        FinisherSlot = new FinisherSlotState
        {
            CardInstanceId = "finisher_001",
            CardId = finisher.Id
        }
    }
};
var beatCards = new Dictionary<string, CardDefinition>
{
    [beatSlashCard.Id] = beatSlashCard,
    [finisher.Id] = finisher
};
var beatEnemies = new Dictionary<string, EnemyDefinition>
{
    ["enemy.beat_dummy"] = beatEnemy
};
var resolvedBeatRound = beatService.ResolveBeatRound(beatCombat, beatCards, beatEnemies);
AssertEqual(1, resolvedBeatRound.Combat.ColorEnergy.Count, "Successful unopposed attack generates one colorless energy");
AssertEqual(21, resolvedBeatRound.Combat.Enemies[0].CurrentHp, "Weakness-adjusted beat damage is applied to enemy HP");
Assert(resolvedBeatRound.Events.Any(item => item.EventType == CombatLogEventType.BeatEnergyGenerated), "Beat energy generation is logged");
```

在同一区块前定义缺失的 card instance id：

```csharp
const string beatCombatCardInstanceId = "card_beat_slash_001";
```

如果现有 `CreatePlayableCombat` 不允许指定新卡实例，先用 `beatCombat = beatCombat with { DeckZones = beatCombat.DeckZones with { Hand = [beatCombatCardInstanceId] } };` 并保证 `beatSlashCard.Id` 能从字典解析。

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 编译失败，提示 `CombatState.BeatRound` 或 `ResolveBeatRound` 不存在。

- [ ] **Step 3: 修改 `CombatState.cs`**

在 `CombatState` 内 `ColorEnergy` 后加入：

```csharp
[JsonPropertyName("beat_round")]
public BeatRoundState? BeatRound { get; init; }
```

- [ ] **Step 4: 在 `BeatCombatService.cs` 增加 `ResolveBeatRound`**

在 `BeatCombatService` 内加入：

```csharp
public BeatRoundResolveResult ResolveBeatRound(
    CombatState combat,
    IReadOnlyDictionary<string, CardDefinition> cardsById,
    IReadOnlyDictionary<string, EnemyDefinition> enemiesById)
{
    ArgumentNullException.ThrowIfNull(combat);
    ArgumentNullException.ThrowIfNull(cardsById);
    ArgumentNullException.ThrowIfNull(enemiesById);
    if (combat.BeatRound is null)
    {
        throw new InvalidOperationException("Beat round state is required.");
    }

    var validation = ValidateTargets(combat.BeatRound.PlayerBeats, combat.BeatRound.EnemyBeats);
    if (!validation.Succeeded)
    {
        throw new InvalidOperationException($"Invalid beat targets: {validation.FailureReason}");
    }

    var working = combat;
    var events = new List<CombatLogEvent>();
    foreach (var playerBeat in combat.BeatRound.PlayerBeats.OrderBy(beat => beat.BeatIndex))
    {
        if (playerBeat.CardId is null || playerBeat.Target is null || !cardsById.TryGetValue(playerBeat.CardId, out var playerCard))
        {
            continue;
        }

        var enemy = working.Enemies.FirstOrDefault(item => string.Equals(item.InstanceId, playerBeat.Target.EnemyInstanceId, StringComparison.Ordinal));
        if (enemy is null || enemy.CurrentHp <= 0)
        {
            continue;
        }

        var enemyDefinition = enemiesById[enemy.EnemyId];
        var playerActions = playerBeat.Target.Kind == BeatTargetKind.EnemyBody
            ? playerCard.BeatActions.Where(action => action.Kind == BeatActionKind.Attack).ToList()
            : playerCard.BeatActions;
        var enemyActions = playerBeat.Target.Kind == BeatTargetKind.EnemyBeat
            ? combat.BeatRound.EnemyBeats
                .First(beat =>
                    string.Equals(beat.EnemyInstanceId, playerBeat.Target.EnemyInstanceId, StringComparison.Ordinal) &&
                    beat.BeatIndex == playerBeat.Target.EnemyBeatIndex)
                .Actions
            : [];

        var collision = ResolveActionCollision(
            playerActions,
            enemyActions,
            enemyDefinition.Resistances,
            new BeatResistanceProfile());
        working = ApplyBeatCollision(working, enemy.InstanceId, collision);
        if (collision.SuccessfulPlayerActions > 0)
        {
            working = AddSuccessfulActionEnergy(working, collision.SuccessfulPlayerActions);
        }

        events.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_beat_{playerBeat.BeatIndex}_resolved",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = playerBeat.CardId,
            TargetIds = [enemy.InstanceId],
            NumericChanges = new Dictionary<string, int>
            {
                ["enemy_damage"] = collision.EnemyDamageTaken,
                ["player_damage"] = collision.PlayerDamageTaken,
                ["successful_player_actions"] = collision.SuccessfulPlayerActions
            }
        });

        if (collision.SuccessfulPlayerActions > 0)
        {
            events.Add(new CombatLogEvent
            {
                EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_beat_{playerBeat.BeatIndex}_energy",
                EventType = CombatLogEventType.BeatEnergyGenerated,
                TurnNumber = combat.TurnNumber,
                SourceId = playerBeat.CardId,
                NumericChanges = new Dictionary<string, int>
                {
                    ["generated"] = collision.SuccessfulPlayerActions
                }
            });
        }
    }

    return new BeatRoundResolveResult
    {
        Combat = working with { Log = working.Log.Concat(events).ToList() },
        Events = events
    };
}

private static CombatState ApplyBeatCollision(CombatState combat, string enemyInstanceId, BeatCollisionResult collision)
{
    var enemies = combat.Enemies
        .Select(enemy => string.Equals(enemy.InstanceId, enemyInstanceId, StringComparison.Ordinal)
            ? enemy with { CurrentHp = Math.Max(0, enemy.CurrentHp - collision.EnemyDamageTaken) }
            : enemy)
        .ToList();
    var playerHp = Math.Max(0, combat.PlayerHp - collision.PlayerDamageTaken);
    var status = playerHp <= 0 ? CombatStatus.Defeat : combat.Status;
    if (enemies.All(enemy => enemy.CurrentHp <= 0))
    {
        status = CombatStatus.Victory;
    }

    return combat with
    {
        Enemies = enemies,
        PlayerHp = playerHp,
        Status = status
    };
}

private static CombatState AddSuccessfulActionEnergy(CombatState combat, int count)
{
    var energy = combat.ColorEnergy;
    for (var index = 0; index < count; index++)
    {
        energy = energy.Add(ColorType.Colorless, 1);
    }

    return combat with { ColorEnergy = energy };
}
```

- [ ] **Step 5: 添加终结槽释放测试**

追加：

```csharp
var finisherReadyCombat = resolvedBeatRound.Combat with
{
    ColorEnergy = ColorEnergyPool.Empty().Add(ColorType.Colorless, 3),
    BeatRound = resolvedBeatRound.Combat.BeatRound! with
    {
        FinisherSlot = new FinisherSlotState
        {
            CardInstanceId = "finisher_001",
            CardId = finisher.Id
        }
    }
};
var releasedFinisher = beatService.ReleaseSlottedFinisher(finisherReadyCombat, finisher, "enemy_01");
AssertEqual(0, releasedFinisher.Combat.ColorEnergy.Count, "Slotted finisher consumes required colorless energy");
AssertEqual("finisher_001", releasedFinisher.Combat.BeatRound?.FinisherSlot.CardInstanceId, "Finisher release does not consume the finisher card from slot");
Assert(releasedFinisher.Events.Any(item => item.EventType == CombatLogEventType.FinisherReleased), "Finisher release is logged");
```

- [ ] **Step 6: 实现 `ReleaseSlottedFinisher`**

在 `BeatCombatService` 内加入：

```csharp
public BeatRoundResolveResult ReleaseSlottedFinisher(
    CombatState combat,
    CardDefinition finisher,
    string targetEnemyInstanceId)
{
    ArgumentNullException.ThrowIfNull(combat);
    ArgumentNullException.ThrowIfNull(finisher);
    if (combat.BeatRound?.FinisherSlot.CardId is null ||
        !string.Equals(combat.BeatRound.FinisherSlot.CardId, finisher.Id, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("The requested finisher is not in the finisher slot.");
    }

    if (finisher.ColorEnergyCost is null ||
        !combat.ColorEnergy.CanSpend(finisher.ColorEnergyCost.Mode, finisher.ColorEnergyCost.Amount, finisher.ColorEnergyCost.MinAmount))
    {
        throw new InvalidOperationException("Not enough color energy for slotted finisher.");
    }

    var spentEnergy = combat.ColorEnergy.Spend(finisher.ColorEnergyCost.Mode, finisher.ColorEnergyCost.Amount, finisher.ColorEnergyCost.MinAmount);
    var damage = finisher.Effects
        .Where(effect => effect.Type == "damage")
        .Sum(effect => effect.Value);
    var enemies = combat.Enemies
        .Select(enemy => string.Equals(enemy.InstanceId, targetEnemyInstanceId, StringComparison.Ordinal)
            ? enemy with { CurrentHp = Math.Max(0, enemy.CurrentHp - damage) }
            : enemy)
        .ToList();
    var status = enemies.All(enemy => enemy.CurrentHp <= 0) ? CombatStatus.Victory : combat.Status;
    var updated = combat with
    {
        ColorEnergy = spentEnergy.Pool,
        Enemies = enemies,
        Status = status
    };
    var releaseEvent = new CombatLogEvent
    {
        EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_finisher_released",
        EventType = CombatLogEventType.FinisherReleased,
        TurnNumber = combat.TurnNumber,
        SourceId = finisher.Id,
        TargetIds = [targetEnemyInstanceId],
        NumericChanges = new Dictionary<string, int>
        {
            ["damage"] = damage,
            ["energy_spent"] = spentEnergy.Spent.Count
        }
    };

    return new BeatRoundResolveResult
    {
        Combat = updated with { Log = updated.Log.Append(releaseEvent).ToList() },
        Events = [releaseEvent]
    };
}
```

- [ ] **Step 7: 运行测试确认通过**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS.

- [ ] **Step 8: 提交**

```bash
git add game/src/Domain/Combat/CombatState.cs game/src/Application/Battle/BeatCombatService.cs game/tests/Unit/Program.cs
git commit -m "feat: resolve beat rounds and slotted finishers"
```

## 任务 6：实现新抽牌与轮末清理规则

**Files:**

- Create: `game/src/Application/Battle/BeatCombatRoundFactory.cs`
- Modify: `game/src/Application/Battle/CombatTurnService.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖开局抽 5、跨轮保留、每轮抽 2**

追加：

```csharp
var beatTurnService = new CombatTurnService();
var beatStartCombat = new CombatState
{
    CombatId = "combat_beat_draw",
    EncounterId = "encounter.test",
    Status = CombatStatus.NotStarted,
    TurnNumber = 0,
    PlayerMaxHp = 60,
    PlayerHp = 60,
    CardsPerTurn = 2,
    DeckZones = new DeckZones
    {
        DrawPile = ["c1", "c2", "c3", "c4", "c5", "c6", "c7"]
    },
    Enemies = []
};
var startedBeatCombat = beatTurnService.StartBeatCombat(beatStartCombat);
AssertEqual(5, startedBeatCombat.DeckZones.Hand.Count, "Beat combat starts by drawing five cards");
var withKeptHand = startedBeatCombat with
{
    Status = CombatStatus.EnemyTurn,
    DeckZones = startedBeatCombat.DeckZones with
    {
        Hand = ["c1", "c2", "kept"],
        DrawPile = ["c6", "c7"]
    }
};
var nextBeatRound = beatTurnService.PrepareNextBeatRound(withKeptHand);
AssertEqual(5, nextBeatRound.DeckZones.Hand.Count, "Beat combat preserves hand and draws two more cards");
Assert(nextBeatRound.DeckZones.Hand.Contains("kept"), "Beat combat keeps unplayed hand cards");
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 编译失败，提示 `StartBeatCombat` / `PrepareNextBeatRound` 不存在。

- [ ] **Step 3: 修改 `CombatTurnService.cs`**

在 `StartCombat` 后加入：

```csharp
public CombatState StartBeatCombat(CombatState combat)
{
    ArgumentNullException.ThrowIfNull(combat);
    if (combat.Status != CombatStatus.NotStarted)
    {
        throw new InvalidOperationException("Beat combat can only be started from NotStarted status.");
    }

    var stateBeforeDraw = combat with
    {
        Status = CombatStatus.PlayerTurn,
        TurnNumber = 1,
        ActionPoints = 0,
        ColorEnergy = ColorEnergyPool.Empty()
    };
    var drawn = DrawCards(stateBeforeDraw, 5);
    return drawn with
    {
        Log = drawn.Log.Append(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_beat_turn_1_started",
            EventType = CombatLogEventType.TurnStarted,
            TurnNumber = 1,
            NumericChanges = new Dictionary<string, int>
            {
                ["cards_drawn"] = 5
            }
        }).ToList()
    };
}

public CombatState PrepareNextBeatRound(CombatState combat)
{
    ArgumentNullException.ThrowIfNull(combat);
    if (combat.Status != CombatStatus.EnemyTurn)
    {
        throw new InvalidOperationException("Next beat round can only be prepared after resolution.");
    }

    var stateBeforeDraw = combat with
    {
        Status = CombatStatus.PlayerTurn,
        TurnNumber = combat.TurnNumber + 1,
        PlayerBlock = 0,
        ActionPoints = 0,
        ColorEnergy = combat.ColorEnergy.Clear(),
        BeatRound = null
    };
    return DrawCards(stateBeforeDraw, 2);
}
```

- [ ] **Step 4: 新增 `BeatCombatRoundFactory.cs`**

Create `game/src/Application/Battle/BeatCombatRoundFactory.cs`:

```csharp
using RoguelikeCardGame.Domain.Cards;
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
        var enemyBeats = new List<EnemyBeatSlot>();
        foreach (var enemy in combat.Enemies.Where(enemy => enemy.CurrentHp > 0))
        {
            var definition = enemiesById[enemy.EnemyId];
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
```

- [ ] **Step 5: 运行测试确认通过**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS.

- [ ] **Step 6: 提交**

```bash
git add game/src/Application/Battle/CombatTurnService.cs game/src/Application/Battle/BeatCombatRoundFactory.cs game/tests/Unit/Program.cs
git commit -m "feat: add beat combat draw flow"
```

## 任务 7：扩展 JSON Schema 与内容加载器

**Files:**

- Modify: `game/data/schemas/gameplay/cards.schema.json`
- Modify: `game/data/schemas/gameplay/enemies.schema.json`
- Modify: `game/src/Infrastructure/Content/GameContent.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: 写失败测试，覆盖真实 `game/data` 加载动作字段**

在 `GameContent.LoadFromDataRoot` 测试后追加：

```csharp
var loadedBeatActionCard = loadedContent.CardsById["card.revolver_slash"];
Assert(loadedBeatActionCard.BeatActions.Count > 0, "GameContent maps action card beat actions");
AssertEqual(BeatActionKind.Attack, loadedBeatActionCard.BeatActions[0].Kind, "GameContent maps beat action kind");
AssertEqual(BeatAttackType.Slash, loadedBeatActionCard.BeatActions[0].AttackType, "GameContent maps beat attack type");
var loadedBeatEnemy = loadedContent.EnemiesById.Values.First(enemy => enemy.BeatSequences.Count > 0);
AssertEqual(BeatResistanceGrade.Standard, loadedBeatEnemy.Resistances.Strike, "GameContent maps default enemy strike resistance");
Assert(loadedBeatEnemy.BeatSequences[0].Beats.Count > 0, "GameContent maps enemy beat sequences");
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: 断言失败，因为真实数据尚未包含 `beat_actions` / `beat_sequences`。

- [ ] **Step 3: 修改 `cards.schema.json`**

在卡牌 item `properties` 中加入：

```json
"card_source": { "type": "string", "enum": ["weapon", "enemy", "special"] },
"beat_actions": {
  "type": "array",
  "items": { "$ref": "#/$defs/beat_action" }
},
"finisher_attack_type": { "type": "string", "enum": ["slash", "strike", "projectile"] }
```

在 `$defs` 中加入：

```json
"beat_action": {
  "type": "object",
  "required": ["kind", "value"],
  "additionalProperties": false,
  "properties": {
    "kind": { "type": "string", "enum": ["attack", "block", "dodge"] },
    "attack_type": { "type": "string", "enum": ["slash", "strike", "projectile"] },
    "value": { "type": "integer", "minimum": 0 },
    "repeat": { "type": "integer", "minimum": 1 },
    "dodge_chance_percent": { "type": "integer", "minimum": 0, "maximum": 100 }
  }
}
```

不要把这些字段加入 `required`，保持旧数据迁移可渐进。

- [ ] **Step 4: 修改 `enemies.schema.json`**

在 enemy item `properties` 中加入：

```json
"resistances": { "$ref": "#/$defs/resistance_profile" },
"beat_sequences": {
  "type": "array",
  "items": { "$ref": "#/$defs/beat_sequence" }
}
```

在 `$defs` 中加入：

```json
"resistance_grade": { "type": "string", "enum": ["resist", "standard", "weakness"] },
"resistance_profile": {
  "type": "object",
  "required": ["slash", "strike", "projectile"],
  "additionalProperties": false,
  "properties": {
    "slash": { "$ref": "#/$defs/resistance_grade" },
    "strike": { "$ref": "#/$defs/resistance_grade" },
    "projectile": { "$ref": "#/$defs/resistance_grade" }
  }
},
"beat_action": {
  "type": "object",
  "required": ["kind", "value"],
  "additionalProperties": false,
  "properties": {
    "kind": { "type": "string", "enum": ["attack", "block", "dodge"] },
    "attack_type": { "type": "string", "enum": ["slash", "strike", "projectile"] },
    "value": { "type": "integer", "minimum": 0 },
    "repeat": { "type": "integer", "minimum": 1 },
    "dodge_chance_percent": { "type": "integer", "minimum": 0, "maximum": 100 }
  }
},
"beat": {
  "type": "object",
  "required": ["action_card_id", "actions"],
  "additionalProperties": false,
  "properties": {
    "action_card_id": { "type": "string" },
    "actions": { "type": "array", "items": { "$ref": "#/$defs/beat_action" } },
    "hidden": { "type": "boolean" }
  }
},
"beat_sequence": {
  "type": "object",
  "required": ["id", "beats"],
  "additionalProperties": false,
  "properties": {
    "id": { "type": "string" },
    "beats": { "type": "array", "items": { "$ref": "#/$defs/beat" } }
  }
}
```

- [ ] **Step 5: 修改 `GameContent.cs` 解析**

在 `ParseCard` 的 object initializer 中加入：

```csharp
BeatActions = ReadBeatActions(item, "beat_actions"),
CardSource = item.TryGetProperty("card_source", out var cardSource)
    ? cardSource.GetStringRequired("card_source")
    : "weapon",
FinisherAttackType = TryParseBeatAttackType(item, "finisher_attack_type")
```

在 `ParseEnemy` 的 object initializer 中加入：

```csharp
Resistances = ReadResistanceProfile(item),
BeatSequences = ReadBeatSequences(item),
```

新增 helper：

```csharp
private static List<BeatActionDefinition> ReadBeatActions(JsonElement element, string propertyName)
{
    return element.TryGetProperty(propertyName, out var actions) && actions.ValueKind == JsonValueKind.Array
        ? actions.EnumerateArray().Select(ReadBeatAction).ToList()
        : [];
}

private static BeatActionDefinition ReadBeatAction(JsonElement element)
{
    return new BeatActionDefinition
    {
        Kind = ParseBeatActionKind(element.GetProperty("kind").GetStringRequired("beat_action.kind")),
        AttackType = TryParseBeatAttackType(element, "attack_type"),
        Value = element.TryGetProperty("value", out var value) ? value.GetInt32() : 0,
        Repeat = element.TryGetProperty("repeat", out var repeat) ? repeat.GetInt32() : 1,
        DodgeChancePercent = element.TryGetProperty("dodge_chance_percent", out var dodge) ? dodge.GetInt32() : 50
    };
}

private static BeatResistanceProfile ReadResistanceProfile(JsonElement element)
{
    if (!element.TryGetProperty("resistances", out var resistances) || resistances.ValueKind != JsonValueKind.Object)
    {
        return new BeatResistanceProfile();
    }

    return new BeatResistanceProfile
    {
        Slash = ParseBeatResistanceGrade(resistances, "slash"),
        Strike = ParseBeatResistanceGrade(resistances, "strike"),
        Projectile = ParseBeatResistanceGrade(resistances, "projectile")
    };
}

private static List<EnemyBeatSequenceDefinition> ReadBeatSequences(JsonElement element)
{
    if (!element.TryGetProperty("beat_sequences", out var sequences) || sequences.ValueKind != JsonValueKind.Array)
    {
        return [];
    }

    return sequences.EnumerateArray()
        .Select(sequence => new EnemyBeatSequenceDefinition
        {
            Id = sequence.GetProperty("id").GetStringRequired("beat_sequence.id"),
            Beats = sequence.GetProperty("beats").EnumerateArray()
                .Select(beat => new EnemyBeatDefinition
                {
                    ActionCardId = beat.GetProperty("action_card_id").GetStringRequired("beat.action_card_id"),
                    Actions = ReadBeatActions(beat, "actions"),
                    Hidden = beat.TryGetProperty("hidden", out var hidden) && hidden.GetBoolean()
                })
                .ToList()
        })
        .ToList();
}

private static BeatActionKind ParseBeatActionKind(string value) => value switch
{
    "attack" => BeatActionKind.Attack,
    "block" => BeatActionKind.Block,
    "dodge" => BeatActionKind.Dodge,
    _ => throw new InvalidOperationException($"Unknown beat action kind '{value}'.")
};

private static BeatAttackType? TryParseBeatAttackType(JsonElement item, string propertyName)
{
    if (!item.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
    {
        return null;
    }

    return value.GetStringRequired(propertyName) switch
    {
        "slash" => BeatAttackType.Slash,
        "strike" => BeatAttackType.Strike,
        "projectile" => BeatAttackType.Projectile,
        var unknown => throw new InvalidOperationException($"Unknown beat attack type '{unknown}'.")
    };
}

private static BeatResistanceGrade ParseBeatResistanceGrade(JsonElement item, string propertyName)
{
    if (!item.TryGetProperty(propertyName, out var value))
    {
        return BeatResistanceGrade.Standard;
    }

    return value.GetStringRequired(propertyName) switch
    {
        "resist" => BeatResistanceGrade.Resist,
        "standard" => BeatResistanceGrade.Standard,
        "weakness" => BeatResistanceGrade.Weakness,
        var unknown => throw new InvalidOperationException($"Unknown beat resistance grade '{unknown}'.")
    };
}
```

- [ ] **Step 6: 给真实数据补最小字段**

Patch `game/data/gameplay/cards/cards.json` for at least:

```json
"beat_actions": [
  { "kind": "attack", "attack_type": "slash", "value": 6, "repeat": 1 }
],
"card_source": "weapon"
```

Patch finisher cards with:

```json
"finisher_attack_type": "projectile"
```

Patch `game/data/gameplay/enemies/enemies.json` for at least one tutorial enemy:

```json
"resistances": {
  "slash": "standard",
  "strike": "standard",
  "projectile": "standard"
},
"beat_sequences": [
  {
    "id": "opening",
    "beats": [
      {
        "action_card_id": "enemy_card.tutorial_slash",
        "actions": [
          { "kind": "attack", "attack_type": "slash", "value": 4, "repeat": 1 }
        ],
        "hidden": false
      }
    ]
  }
]
```

For MVP validation, add beat sequences only to the three validation encounters first; do not convert every enemy unless needed.

- [ ] **Step 7: 运行验证**

Run:

```bash
./game/tools/data_validator/validate_data.ps1
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: data validator PASS and unit tests PASS.

- [ ] **Step 8: 提交**

```bash
git add game/data/schemas/gameplay/cards.schema.json game/data/schemas/gameplay/enemies.schema.json game/src/Infrastructure/Content/GameContent.cs game/data/gameplay/cards/cards.json game/data/gameplay/enemies/enemies.json game/tests/Unit/Program.cs
git commit -m "feat: load beat combat data"
```

## 任务 8：最小 Godot UI 接入

**Files:**

- Modify: `game/src/Presentation/Battle/BattleHudView.cs`
- Modify: `game/src/Presentation/Battle/BattleScreen.cs`
- Modify: `game/src/Presentation/Cards/CardPanel.cs`
- Modify: `game/src/Presentation/Flow/MvpRunFlowController.cs`

- [ ] **Step 1: 更新卡牌面板显示动作串**

Modify `CardPanel.cs` so the rules text appended for cards with beat actions uses:

```csharp
private static string BeatActionSummary(CardDefinition card)
{
    if (card.BeatActions.Count == 0)
    {
        return string.Empty;
    }

    return string.Join(" -> ", card.BeatActions.Select(action =>
    {
        var repeat = action.Repeat > 1 ? $" x{action.Repeat}" : string.Empty;
        return action.Kind switch
        {
            BeatActionKind.Attack => $"{AttackTypeLabel(action.AttackType)} {action.Value}{repeat}",
            BeatActionKind.Block => $"格挡 {action.Value}{repeat}",
            BeatActionKind.Dodge => $"闪避 {action.DodgeChancePercent}%{repeat}",
            _ => action.Kind.ToString()
        };
    }));
}

private static string AttackTypeLabel(BeatAttackType? attackType)
{
    return attackType switch
    {
        BeatAttackType.Slash => "斩击",
        BeatAttackType.Strike => "钝击",
        BeatAttackType.Projectile => "弹射",
        _ => "攻击"
    };
}
```

Use this summary below existing card rules. If `CardPanel` does not currently receive `CardDefinition`, extend the render method signature and update callers in `BattleHandView.cs`.

- [ ] **Step 2: 在 HUD 隐藏行动点并预留终结槽**

In `BattleHudView.cs`, make `CreateActionPointBadge` skip rendering when beat mode is active. Add a method:

```csharp
private Control CreateFinisherSlot(CombatState combat, GameContent content)
{
    var panel = new PanelContainer
    {
        CustomMinimumSize = new Vector2(170, 96),
        TooltipText = "终结槽"
    };
    var label = new Label
    {
        Text = combat.BeatRound?.FinisherSlot.CardId is { } cardId
            ? content.CardName(cardId)
            : "终结槽",
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        AutowrapMode = TextServer.AutowrapMode.WordSmart
    };
    panel.AddChild(label);
    return panel;
}
```

Add it near the existing color energy panel:

```csharp
var finisherSlot = CreateFinisherSlot(combatState, gameContent);
AddAt(rootControl, finisherSlot, new Vector2(724, 126), new Vector2(180, 104));
```

- [ ] **Step 3: 在 BattleScreen 添加三拍区最小显示**

In `BattleScreen.cs`, add a render helper:

```csharp
private Control CreateBeatLane(CombatState combat)
{
    var lane = new HBoxContainer
    {
        Name = "BeatLane",
        CustomMinimumSize = new Vector2(520, 118)
    };
    var beatCount = combat.BeatRound?.BeatCount ?? 3;
    for (var index = 0; index < beatCount; index++)
    {
        var slot = new PanelContainer
        {
            CustomMinimumSize = new Vector2(160, 104),
            TooltipText = $"第 {index + 1} 拍"
        };
        slot.AddChild(new Label
        {
            Text = combat.BeatRound?.PlayerBeats.FirstOrDefault(beat => beat.BeatIndex == index)?.CardId ?? $"第 {index + 1} 拍",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        lane.AddChild(slot);
    }

    return lane;
}
```

Place it above the hand area:

```csharp
var beatLane = CreateBeatLane(combatState);
AddChild(beatLane);
beatLane.Position = new Vector2(520, 690);
```

- [ ] **Step 4: 在 FlowController 暂接新开始抽牌**

In `MvpRunFlowController.cs`, where combat is started, keep the old flow behind a local flag:

```csharp
private const bool UseBeatCombatPrototype = true;
```

When starting combat:

```csharp
combat = UseBeatCombatPrototype
    ? turnService.StartBeatCombat(combatFactory.CreateCombat(combatId, run, encounter, content.EnemiesById))
    : turnService.StartCombat(combatFactory.CreateCombat(combatId, run, encounter, content.EnemiesById));
```

Also assign a `BeatRound`:

```csharp
if (UseBeatCombatPrototype && combat is not null)
{
    var beatRoundFactory = new BeatCombatRoundFactory();
    combat = combat with
    {
        BeatRound = beatRoundFactory.CreateRound(combat, content.EnemiesById)
    };
}
```

- [ ] **Step 5: 构建验证**

Run:

```bash
dotnet build game/RoguelikeCardGame.csproj
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
./game/tools/check_project.ps1
```

Expected: build PASS, unit tests PASS, Godot headless check PASS.

- [ ] **Step 6: 提交**

```bash
git add game/src/Presentation/Battle/BattleHudView.cs game/src/Presentation/Battle/BattleScreen.cs game/src/Presentation/Cards/CardPanel.cs game/src/Presentation/Flow/MvpRunFlowController.cs
git commit -m "feat: prototype beat combat UI"
```

## 任务 9：更新设计文档与变更日志

**Files:**

- Modify: `design/01_core_gameplay/02_combat_system.md`
- Modify: `design/01_core_gameplay/03_card_system.md`
- Modify: `design/03_experience/00_ui_ux.md`
- Modify: `design/08_governance/01_change_log.md`

- [ ] **Step 1: 更新战斗系统文档**

Add a section to `design/01_core_gameplay/02_combat_system.md` after “战斗回合结构”:

```markdown
## 三拍拆招战斗重构方向

后续战斗核心卖点调整为“三拍拆招 + 成功产彩 + 终结分镜切入”。第一版重构 MVP 先不实现彩能附魔和五色终结转译，只验证读招、拆招、成功动作产彩和终结槽切入。

每轮魔物公开动作序列，玩家用武器提供的拍位编排行动牌。初始左轮剑和机械臂均为 3 拍。每拍放 1 张行动牌，行动牌内部可以包含斩击、钝击、弹射、格挡和闪避等多个动作。玩家每拍选择一个魔物的未锁定拍作为对撞目标；若某个魔物全部拍位都已被锁定，剩余拍可以直接攻击该魔物本体。

成功造成伤害、有效格挡或闪避成功时生成 1 点无色彩能。彩能上限仍为 6，默认轮末清空。终结牌放入终结槽，不占拍；成功动作后若满足彩能条件，玩家可以手动释放终结牌并插入漫画分镜演出。
```

- [ ] **Step 2: 更新卡牌系统文档**

Add to `design/01_core_gameplay/03_card_system.md` after “卡牌类型”:

```markdown
### 三拍拆招下的行动牌与终结牌

行动牌是动作容器，放入玩家拍位。玩家可见类型仍为行动牌 / 终结牌，不新增“格挡牌”“闪避牌”等基础类型。攻击、格挡和闪避都属于动作。一张行动牌可以混合多个动作，例如“格挡 -> 钝击”或“闪避 -> 斩击 x2”。

终结牌放入终结槽，不作为普通拍位牌打出。终结牌在彩能满足条件后可由玩家手动释放，释放消耗对应彩能，但不消耗终结牌本身；同一轮内若玩家再次生成足够彩能，可以再次释放同一张终结牌。

第一版战斗重构 MVP 暂不做色彩碎片与附魔。后续色彩系统回归时，色彩仍绑定整张行动牌，而不是绑定单个动作。
```

- [ ] **Step 3: 更新 UI 文档**

Add to `design/03_experience/00_ui_ux.md` after “战斗界面信息优先级”:

```markdown
## 三拍拆招界面方向

战斗界面需要支持三拍分镜拆招：玩家侧显示拍位，魔物侧显示每个魔物本轮动作序列。玩家将行动牌放入拍位后，选择某个魔物未锁定拍作为对撞目标。已锁定魔物拍显示“已接招”，未锁定魔物拍显示危险提示；当某个魔物全部拍位被锁定后，该魔物显示“空门”，剩余玩家拍可直接攻击本体。

行动牌需要显示内部动作串，例如“格挡 4 -> 钝击 6”或“闪避 50% -> 斩击 3 x2”。终结槽位于彩能槽附近，满足彩能条件后高亮或短暂停顿提示玩家可手动切入终结分镜。
```

- [ ] **Step 4: 更新变更日志**

Append to `design/08_governance/01_change_log.md` under `### 2026-06-20` or create the section:

```markdown
### 2026-06-20

- 根据 [[inspiration/2026-06-20_combat_identity_and_animation_brainstorm_qa|战斗特色化与动画爽感头脑风暴 Q&A]]，确认战斗重构核心卖点为“三拍拆招 + 成功产彩 + 终结分镜切入”。
- 新增 [[docs/superpowers/specs/2026-06-20-combat-rework-design|战斗重构设计 spec]]，明确第一版战斗重构 MVP 先不实现彩能附魔和五色终结转译，只验证三拍拆招、成功动作产彩和终结槽切入。
```

- [ ] **Step 5: 文档检查**

Run:

```bash
rg -n "三拍拆招|成功产彩|终结分镜切入" design docs/superpowers/specs inspiration
```

Expected: 新术语出现在 design、spec 和 inspiration 中。

- [ ] **Step 6: 提交**

```bash
git add design/01_core_gameplay/02_combat_system.md design/01_core_gameplay/03_card_system.md design/03_experience/00_ui_ux.md design/08_governance/01_change_log.md
git commit -m "docs: record beat combat rework direction"
```

## 任务 10：最终验证

**Files:**

- Verify only.

- [ ] **Step 1: 数据校验**

Run:

```bash
./game/tools/data_validator/validate_data.ps1
```

Expected: PASS.

- [ ] **Step 2: 单元测试**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: PASS.

- [ ] **Step 3: 主工程构建**

Run:

```bash
dotnet build game/RoguelikeCardGame.csproj
```

Expected: build succeeds with 0 errors.

- [ ] **Step 4: Godot headless 检查**

Run:

```bash
./game/tools/check_project.ps1
```

Expected: Godot project check PASS.

- [ ] **Step 5: 提交最终验证记录**

If all verification passes, create a small note in the final implementation summary rather than a file. Commit any remaining implementation changes:

```bash
git status --short
```

Expected: no uncommitted changes after all task commits.

## Self-Review

- Spec coverage: tasks cover action model, card/enemy data, collision, targeting, energy, finisher slot, hand flow, minimal UI, docs, and verification. Deferred spec items are explicitly excluded from first MVP.
- Placeholder scan: the plan avoids unfinished placeholder wording and uses concrete file paths, commands, and code snippets.
- Type consistency: plan consistently uses `BeatActionDefinition`, `BeatActionKind`, `BeatAttackType`, `BeatResistanceGrade`, `BeatRoundState`, `PlayerBeatSlot`, `EnemyBeatSlot`, `FinisherSlotState`, and `BeatCombatService`.
