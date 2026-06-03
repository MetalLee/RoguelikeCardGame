using System.Text.Json;
using System.Text.Json.Serialization;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Relics;
using RoguelikeCardGame.Domain.Rewards;
using RoguelikeCardGame.Domain.Runs;

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    WriteIndented = true
};
options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

var strike = new CardDefinition
{
    Id = "card_basic_strike",
    Type = CardType.Action,
    Cost = 1,
    DefaultChainChange = ChainChange.Gain(1),
    TargetRule = TargetRule.SingleEnemy,
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 6 }],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "attack"],
    TextKey = "card.basic_strike",
    ArtKey = "art.card.placeholder.strike"
};

var finisher = new CardDefinition
{
    Id = "card_burst_finish",
    Type = CardType.Finisher,
    Cost = 0,
    MinChain = 3,
    DefaultChainChange = ChainChange.ConsumeAll,
    TargetRule = TargetRule.SingleEnemy,
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 18 }],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "finisher"],
    TextKey = "card.burst_finish",
    ArtKey = "art.card.placeholder.burst_finish"
};

var relic = new RelicDefinition
{
    Id = "relic_mvp_chain_spark",
    Rarity = RelicRarity.Common,
    Trigger = "first_action_card_each_turn",
    Conditions = [new RelicConditionDefinition { Type = "combat_turn", Value = "player_turn" }],
    Effects = [new EffectDefinition { Type = "gain_block", Target = "self", Value = 2 }],
    StackRule = RelicStackRule.Unique,
    TextKey = "relic.mvp_chain_spark",
    IconKey = "icon.relic.placeholder.chain_spark"
};

var enemy = new EnemyDefinition
{
    Id = "enemy_training_dummy",
    MaxHp = 24,
    IntentSequence =
    [
        new EnemyIntentDefinition
        {
            Id = "intent_dummy_attack",
            IntentType = EnemyIntentType.Attack,
            UiTextKey = "enemy.intent.training_dummy.attack",
            Effects = [new EffectDefinition { Type = "damage", Target = "player", Value = 5 }]
        }
    ],
    Tags = ["normal", "teaching"],
    ArtKey = "art.enemy.placeholder.training_dummy",
    UiNameKey = "enemy.training_dummy"
};

var encounter = new EncounterDefinition
{
    Id = "encounter_mvp_normal_01",
    NodeType = EncounterNodeType.Normal,
    Enemies = [new EncounterEnemyDefinition { InstanceId = "enemy_01", EnemyId = enemy.Id }],
    RewardProfile = new EncounterRewardProfileDefinition
    {
        CardPackIds = ["reward_pack_mvp_action"],
        RelicId = null
    },
    TeachingGoalKey = "encounter.mvp_normal_01.goal",
    DifficultyNote = "Smoke test encounter."
};

var rewardPack = new RewardPackDefinition
{
    Id = "reward_pack_mvp_action",
    PackType = CardType.Action,
    CandidateIds = ["card_basic_strike", "card_quick_jab", "card_heavy_strike"],
    MinPick = 0,
    MaxPick = 3,
    GuaranteeRule = "fixed_three_candidates",
    RepeatRule = RewardRepeatRule.Repeatable,
    TextKey = "reward_pack.mvp_action"
};

var serializedCard = JsonSerializer.Serialize(strike, options);
var deserializedCard = JsonSerializer.Deserialize<CardDefinition>(serializedCard, options);
AssertEqual(strike.Id, deserializedCard?.Id, "CardDefinition serializes and deserializes");
AssertEqual(ChainChangeMode.FixedDelta, deserializedCard?.DefaultChainChange.Mode, "CardDefinition keeps chain change mode");

var runFactory = new RunStateFactory();
var run = runFactory.CreateNewRun(
    runId: "run_smoke_001",
    seed: 12345,
    playerMaxHp: 60,
    baseActionPoints: 3,
    cardsPerTurn: 5,
    starterDeck: [strike.Id, strike.Id, strike.Id, strike.Id, finisher.Id],
    encounterSequence: [encounter.Id]);

AssertEqual(RunStatus.InProgress, run.Status, "RunStateFactory starts run in progress");
AssertEqual(60, run.PlayerHp, "RunStateFactory starts at full HP");
AssertEqual(5, run.MasterDeck.Count, "RunStateFactory copies starter deck");

var combatFactory = new CombatStateFactory();
var combat = combatFactory.CreateCombat(
    combatId: "combat_smoke_001",
    runState: run,
    encounter: encounter,
    enemiesById: new Dictionary<string, EnemyDefinition> { [enemy.Id] = enemy });

AssertEqual(CombatStatus.PlayerTurn, combat.Status, "CombatStateFactory starts on player turn");
AssertEqual(run.BaseActionPoints, combat.ActionPoints, "CombatStateFactory restores base action points");
AssertEqual(run.MasterDeck.Count, combat.DeckZones.DrawPileCount, "CombatStateFactory places run deck in draw pile");
AssertEqual(enemy.MaxHp, combat.Enemies[0].CurrentHp, "CombatStateFactory creates enemy state at full HP");
AssertEqual(CombatLogEventType.CombatStarted, combat.Log[0].EventType, "CombatStateFactory records combat start event");

var serializedBundle = JsonSerializer.Serialize(new
{
    Cards = new[] { strike, finisher },
    Relics = new[] { relic },
    Enemies = new[] { enemy },
    Encounters = new[] { encounter },
    Rewards = new[] { rewardPack },
    Run = run,
    Combat = combat
}, options);

if (string.IsNullOrWhiteSpace(serializedBundle))
{
    throw new InvalidOperationException("Serialized model bundle should not be empty.");
}

Console.WriteLine("Domain model smoke tests passed.");

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}. Expected: {expected}; Actual: {actual}");
    }
}
