using System.Text.Json;
using System.Text.Json.Serialization;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Debug;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Relics;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Infrastructure.Randomness;
using RoguelikeCardGame.Presentation.Cards;

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    WriteIndented = true
};
options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

var strike = new CardDefinition
{
    Id = "card.basic_strike",
    WeaponId = "weapon.revolver_sword",
    Type = CardType.Action,
    Cost = 1,
    ColorEnergyGeneration = new ColorEnergyGeneration { Amount = 1, ColorSource = ColorEnergyColorSource.Enchantment },
    TargetRule = TargetRule.SingleEnemy,
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 6 }],
    Rarity = CardRarity.Starter,
    VfxAsset = "asset.vfx.slash_speed_lines",
    Tags = ["starter", "attack"]
};

var finisher = new CardDefinition
{
    Id = "card.burst_finish",
    WeaponId = "weapon.revolver_sword",
    Type = CardType.Finisher,
    Cost = 0,
    ColorEnergyCost = new ColorEnergyCost { Mode = ColorEnergySpendMode.Fixed, Amount = 3, MinAmount = 3 },
    TargetRule = TargetRule.SingleEnemy,
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 18 }],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "finisher"]
};

var guardAction = new CardDefinition
{
    Id = "card.basic_guard",
    WeaponId = "weapon.mechanical_arm",
    Type = CardType.Action,
    Cost = 0,
    ColorEnergyGeneration = new ColorEnergyGeneration { Amount = 1, ColorSource = ColorEnergyColorSource.Enchantment },
    TargetRule = TargetRule.Self,
    Effects =
    [
        new EffectDefinition { Type = "block", Target = "self", Value = 5 },
        new EffectDefinition { Type = "gain_action_points", Target = "self", Value = 1 }
    ],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "defense"]
};

var drawAction = new CardDefinition
{
    Id = "card.focus_draw",
    WeaponId = "weapon.mechanical_arm",
    Type = CardType.Action,
    Cost = 0,
    ColorEnergyGeneration = new ColorEnergyGeneration { Amount = 1, ColorSource = ColorEnergyColorSource.Enchantment },
    TargetRule = TargetRule.Self,
    Effects = [new EffectDefinition { Type = "draw_cards", Target = "self", Value = 1 }],
    Rarity = CardRarity.Common,
    Tags = ["action", "draw"]
};

var discountAction = new CardDefinition
{
    Id = "card.setup_discount",
    WeaponId = "weapon.mechanical_arm",
    Type = CardType.Action,
    Cost = 0,
    ColorEnergyGeneration = new ColorEnergyGeneration { Amount = 1, ColorSource = ColorEnergyColorSource.Enchantment },
    TargetRule = TargetRule.Self,
    Effects = [new EffectDefinition { Type = "temporary_discount", Target = "hand", Value = 1 }],
    Rarity = CardRarity.Common,
    Tags = ["action", "discount"]
};

var arcSweepFinisher = new CardDefinition
{
    Id = "card.arc_sweep_finish",
    WeaponId = "weapon.mechanical_arm",
    Type = CardType.Finisher,
    Cost = 0,
    ColorEnergyCost = new ColorEnergyCost { Mode = ColorEnergySpendMode.Fixed, Amount = 3, MinAmount = 3 },
    TargetRule = TargetRule.AllEnemies,
    Effects = [new EffectDefinition { Type = "damage", Target = "all_enemies", Value = 4 }],
    Rarity = CardRarity.Common,
    Tags = ["finisher", "attack", "aoe"]
};

var refundFinisher = new CardDefinition
{
    Id = "card.refund_finish",
    WeaponId = "weapon.revolver_sword",
    Type = CardType.Finisher,
    Cost = 0,
    ColorEnergyCost = new ColorEnergyCost { Mode = ColorEnergySpendMode.Fixed, Amount = 3, MinAmount = 3 },
    TargetRule = TargetRule.SingleEnemy,
    Effects =
    [
        new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 14 },
        new EffectDefinition { Type = "gain_action_points", Target = "self", Value = 1 }
    ],
    Rarity = CardRarity.Uncommon,
    Tags = ["finisher", "resource"]
};

var relic = new RelicDefinition
{
    Id = "relic.mvp_color_spark",
    Rarity = RelicRarity.Common,
    Trigger = "first_action_card_each_turn",
    Conditions = [new RelicConditionDefinition { Type = "combat_turn", Value = "player_turn" }],
    Effects = [new EffectDefinition { Type = "gain_block", Target = "self", Value = 2 }],
    StackRule = RelicStackRule.Unique
};

var enemy = new EnemyDefinition
{
    Id = "enemy.training_dummy",
    MaxHp = 24,
    IntentSequence =
    [
        new EnemyIntentDefinition
        {
            Id = "intent.attack",
            IntentType = EnemyIntentType.Attack,
            Effects = [new EffectDefinition { Type = "damage", Target = "player", Value = 5 }]
        }
    ],
    Tags = ["normal", "teaching"]
};

var sequenceEnemy = new EnemyDefinition
{
    Id = "enemy.sequence_tester",
    MaxHp = 20,
    IntentSequence =
    [
        new EnemyIntentDefinition
        {
            Id = "intent.attack",
            IntentType = EnemyIntentType.Attack,
            Effects = [new EffectDefinition { Type = "damage", Target = "player", Value = 6 }]
        },
        new EnemyIntentDefinition
        {
            Id = "intent.guard",
            IntentType = EnemyIntentType.Defend,
            Effects = [new EffectDefinition { Type = "gain_block", Target = "self", Value = 4 }]
        }
    ],
    Tags = ["test", "sequence"]
};

var encounter = new EncounterDefinition
{
    Id = "encounter.mvp.normal_01",
    NodeType = EncounterNodeType.Normal,
    Enemies = [new EncounterEnemyDefinition { InstanceId = "enemy_01", EnemyId = enemy.Id }],
    RewardProfile = new EncounterRewardProfileDefinition
    {
        CardPackIds = [],
        RelicId = null
    },
    TeachingGoalKey = "encounter.mvp.normal_01.goal",
    DifficultyNote = "Smoke test encounter."
};

var eliteEncounter = new EncounterDefinition
{
    Id = "encounter.mvp.elite_01",
    NodeType = EncounterNodeType.Elite,
    Enemies = [new EncounterEnemyDefinition { InstanceId = "enemy_01", EnemyId = sequenceEnemy.Id }],
    RewardProfile = new EncounterRewardProfileDefinition
    {
        CardPackIds = [],
        RelicId = relic.Id
    },
    TeachingGoalKey = "encounter.mvp.elite_01.goal",
    DifficultyNote = "Smoke test elite encounter."
};

var bossEncounter = new EncounterDefinition
{
    Id = "encounter.mvp.boss_01",
    NodeType = EncounterNodeType.Boss,
    Enemies = [new EncounterEnemyDefinition { InstanceId = "enemy_01", EnemyId = sequenceEnemy.Id }],
    RewardProfile = new EncounterRewardProfileDefinition
    {
        CardPackIds = [],
        RelicId = null
    },
    TeachingGoalKey = "encounter.mvp.boss_01.goal",
    DifficultyNote = "Smoke test boss encounter."
};

var enemiesById = new Dictionary<string, EnemyDefinition>
{
    [enemy.Id] = enemy,
    [sequenceEnemy.Id] = sequenceEnemy
};

var relicsById = new Dictionary<string, RelicDefinition>
{
    [relic.Id] = relic
};

var serializedCard = JsonSerializer.Serialize(strike, options);
var deserializedCard = JsonSerializer.Deserialize<CardDefinition>(serializedCard, options);
AssertEqual(strike.Id, deserializedCard?.Id, "CardDefinition serializes and deserializes");
AssertEqual(strike.WeaponId, deserializedCard?.WeaponId, "CardDefinition keeps weapon ownership");
AssertEqual(1, deserializedCard?.ColorEnergyGeneration?.Amount, "CardDefinition keeps action color energy generation");
AssertEqual(strike.VfxAsset, deserializedCard?.VfxAsset, "CardDefinition keeps card VFX asset");

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
AssertEqual(1, clash.PlayerDamageTaken, "Block subtracts its value from incoming attack damage");
AssertEqual(9, clash.EnemyDamageTaken, "Remaining strike action hits weakness for 150 percent damage");
AssertEqual(2, clash.SuccessfulPlayerActions, "Effective block and successful attack both count as successful actions");
AssertEqual(2, clash.Stages.Count, "Beat collision keeps per-action stage results");
AssertEqual(BeatActionKind.Block, clash.Stages[0].PlayerActionKind, "First stage records player block");
AssertEqual(BeatActionKind.Attack, clash.Stages[0].EnemyActionKind, "First stage records enemy attack");
AssertEqual(1, clash.Stages[0].PlayerDamageTaken, "First stage records damage after block");
AssertEqual(0, clash.Stages[0].EnemyDamageTaken, "First stage does not assign later attack damage");
AssertEqual(BeatActionKind.Attack, clash.Stages[1].PlayerActionKind, "Second stage records player counter attack");
AssertEqual(BeatAttackType.Strike, clash.Stages[1].PlayerAttackType, "Second stage records strike attack type");
AssertEqual(9, clash.Stages[1].EnemyDamageTaken, "Second stage records counter attack damage");

var fullyBlocked = beatService.ResolveActionCollision(
    playerActions:
    [
        new BeatActionDefinition { Kind = BeatActionKind.Block, Value = 5 }
    ],
    enemyActions:
    [
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 5 }
    ],
    enemyResistance: new BeatResistanceProfile(),
    playerResistance: new BeatResistanceProfile());
AssertEqual(0, fullyBlocked.PlayerDamageTaken, "Equal block value reduces incoming attack to zero");

var resisted = beatService.ResolveActionCollision(
    playerActions:
    [
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Projectile, Value = 10 }
    ],
    enemyActions: [],
    enemyResistance: new BeatResistanceProfile { Projectile = BeatResistanceGrade.Resist },
    playerResistance: new BeatResistanceProfile());
AssertEqual(5, resisted.EnemyDamageTaken, "Projectile resistance halves direct damage");

var beatTargetCombat = CreatePlayableCombat([], enemies: [CreateEnemyState("enemy_01")]);
var threeBeatRound = CreateBeatTargetRound(
    beatCount: 3,
    enemyBeatIndexes: [0, 1, 2],
    playerBeats:
    [
        CreatePlayerBeat(0, "card.beat_slash", CreateEnemyBeatTarget(0)),
        CreatePlayerBeat(1, "card.beat_slash", CreateEnemyBeatTarget(1)),
        CreatePlayerBeat(2, "card.beat_slash", CreateEnemyBeatTarget(2))
    ]);
Assert(beatService.ValidatePlayerBeatTargets(threeBeatRound, beatTargetCombat).Succeeded, "Three player beats can lock three different enemy beats");

var duplicateEnemyBeatTarget = beatService.ValidatePlayerBeatTargets(
    CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats:
        [
            CreatePlayerBeat(0, "card.beat_slash", CreateEnemyBeatTarget(1)),
            CreatePlayerBeat(1, "card.beat_slash", CreateEnemyBeatTarget(1))
        ]),
    beatTargetCombat);
Assert(!duplicateEnemyBeatTarget.Succeeded, "Duplicate enemy beat targets are rejected");
AssertEqual(BeatTargetValidationFailureReason.DuplicateEnemyBeatTarget, duplicateEnemyBeatTarget.FailureReason, "Duplicate enemy beat target exposes failure reason");

var bodyTargetBeforeLocks = beatService.ValidatePlayerBeatTargets(
    CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats:
        [
            CreatePlayerBeat(0, "card.beat_slash", CreateEnemyBeatTarget(0)),
            CreatePlayerBeat(1, "card.beat_slash", CreateEnemyBodyTarget())
        ]),
    beatTargetCombat);
Assert(!bodyTargetBeforeLocks.Succeeded, "Body targets require all enemy beats to be locked first");
AssertEqual(BeatTargetValidationFailureReason.BodyTargetRequiresAllEnemyBeatsLocked, bodyTargetBeforeLocks.FailureReason, "Body target lock failure exposes failure reason");

var bodyTargetAfterLocks = beatService.ValidatePlayerBeatTargets(
    CreateBeatTargetRound(
        beatCount: 4,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats:
        [
            CreatePlayerBeat(0, "card.beat_slash", CreateEnemyBeatTarget(0)),
            CreatePlayerBeat(1, "card.beat_slash", CreateEnemyBeatTarget(1)),
            CreatePlayerBeat(2, "card.beat_slash", CreateEnemyBeatTarget(2)),
            CreatePlayerBeat(3, "card.beat_slash", CreateEnemyBodyTarget())
        ]),
    beatTargetCombat);
Assert(bodyTargetAfterLocks.Succeeded, "Body targets are valid once all enemy beats are locked");

var missingBeatTarget = beatService.ValidatePlayerBeatTargets(
    CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats: [CreatePlayerBeat(0, "card.beat_slash", null)]),
    beatTargetCombat);
Assert(!missingBeatTarget.Succeeded, "Player beats with cards require a target");
AssertEqual(BeatTargetValidationFailureReason.TargetMissing, missingBeatTarget.FailureReason, "Missing target exposes failure reason");

var emptyBeat = beatService.ValidatePlayerBeatTargets(
    CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats: [CreatePlayerBeat(0, null, null)]),
    beatTargetCombat);
Assert(emptyBeat.Succeeded, "Empty player beats without targets are allowed");

var beatPlanning = new BeatRoundPlanningService();
const string planningBeatSlashInstanceA = "card_instance.beat_slash_001";
const string planningGuardInstance = "card_instance.guard_001";
const string planningBeatSlashInstanceB = "card_instance.beat_slash_002";
var planningCombat = CreatePlayableCombat(
    [planningBeatSlashInstanceA, planningGuardInstance, planningBeatSlashInstanceB],
    actionPoints: 0,
    enemies: [CreateEnemyState("enemy_01", currentHp: 30, maxHp: 30, enemyId: beatEnemy.Id)]) with
{
    BeatRound = new BeatRoundFactoryForTest().CreateEmptyRound(enemyInstanceId: "enemy_01")
};
var placedBeat = beatPlanning.PlaceActionCardInBeat(
    planningCombat,
    cardInstanceId: planningCombat.DeckZones.Hand[0],
    cardId: beatSlashCard.Id,
    handIndex: 0,
    beatIndex: 1,
    beatSlashCard);
AssertEqual(beatSlashCard.Id, placedBeat.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 1).CardId, "Beat planning places the action card in the selected beat");
Assert(!placedBeat.DeckZones.Hand.Contains(planningCombat.DeckZones.Hand[0]), "Beat planning removes the slotted action card instance from hand");

var targetedBeat = beatPlanning.SetEnemyBeatTarget(
    placedBeat,
    beatIndex: 1,
    enemyInstanceId: "enemy_01",
    enemyBeatIndex: 0,
    new BeatCombatService());
AssertEqual(BeatTargetKind.EnemyBeat, targetedBeat.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 1).Target?.Kind, "Beat planning assigns an enemy beat target");
AssertEqual(0, targetedBeat.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 1).Target?.EnemyBeatIndex, "Beat planning stores the enemy beat index");

var cancelledPlacement = beatPlanning.CancelUntargetedBeatPlacement(
    placedBeat,
    beatIndex: 1,
    cardInstanceId: planningBeatSlashInstanceA);
AssertEqual(null, cancelledPlacement.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 1).CardId, "Cancelling beat placement clears the player beat card id");
AssertEqual(null, cancelledPlacement.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 1).CardInstanceId, "Cancelling beat placement clears the player beat card instance id");
Assert(cancelledPlacement.DeckZones.Hand.Contains(planningBeatSlashInstanceA), "Cancelling beat placement returns the card instance to hand");
AssertThrows(
    () => beatPlanning.CancelUntargetedBeatPlacement(targetedBeat, beatIndex: 1, cardInstanceId: planningBeatSlashInstanceA),
    "Targeted beat placements cannot be cancelled back to hand");

var secondPlacedBeat = beatPlanning.PlaceActionCardInBeat(
    targetedBeat,
    cardInstanceId: targetedBeat.DeckZones.Hand[0],
    cardId: guardAction.Id,
    handIndex: 0,
    beatIndex: 2,
    guardAction);
AssertThrows(
    () => beatPlanning.SetEnemyBodyTarget(secondPlacedBeat, beatIndex: 2, enemyInstanceId: "enemy_01", new BeatCombatService()),
    "Enemy body cannot be targeted until all enemy beats are locked");

var allBeatsLocked = beatPlanning.SetEnemyBeatTarget(
    secondPlacedBeat,
    beatIndex: 2,
    enemyInstanceId: "enemy_01",
    enemyBeatIndex: 1,
    new BeatCombatService());
var thirdPlacedBeat = beatPlanning.PlaceActionCardInBeat(
    allBeatsLocked,
    cardInstanceId: allBeatsLocked.DeckZones.Hand[0],
    cardId: beatSlashCard.Id,
    handIndex: 0,
    beatIndex: 0,
    beatSlashCard);
var bodyTargetedBeat = beatPlanning.SetEnemyBodyTarget(thirdPlacedBeat, beatIndex: 0, enemyInstanceId: "enemy_01", new BeatCombatService());
AssertEqual(BeatTargetKind.EnemyBody, bodyTargetedBeat.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 0).Target?.Kind, "Enemy body can be targeted after all enemy beats are locked");

var directEnemyBeatDeployment = beatPlanning.PlaceActionCardIntoNextPlayerBeatAndTarget(
    planningCombat,
    cardInstanceId: planningCombat.DeckZones.Hand[0],
    cardId: beatSlashCard.Id,
    handIndex: 0,
    beatSlashCard,
    CreateEnemyBeatTarget(1),
    new BeatCombatService());
var directEnemyBeat = directEnemyBeatDeployment.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 0);
AssertEqual(beatSlashCard.Id, directEnemyBeat?.CardId, "Direct beat deployment uses the first empty player beat");
AssertEqual(BeatTargetKind.EnemyBeat, directEnemyBeat?.Target?.Kind, "Direct beat deployment locks the requested enemy beat");
AssertEqual(1, directEnemyBeat?.Target?.EnemyBeatIndex, "Direct beat deployment stores the requested enemy beat index");
Assert(!directEnemyBeatDeployment.DeckZones.Hand.Contains(planningBeatSlashInstanceA), "Direct beat deployment removes the action card from hand");

var bodyDeploymentLocksNextBeatCombat = CreatePlayableCombat(
    [planningGuardInstance],
    actionPoints: 0,
    enemies: [CreateEnemyState("enemy_01", currentHp: 30, maxHp: 30, enemyId: beatEnemy.Id)]) with
{
    BeatRound = CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1],
        playerBeats:
        [
            new PlayerBeatSlot
            {
                BeatIndex = 0,
                CardInstanceId = "card_instance.already_slotted",
                CardId = beatSlashCard.Id,
                Target = CreateEnemyBeatTarget(0)
            },
            new PlayerBeatSlot { BeatIndex = 1 },
            new PlayerBeatSlot { BeatIndex = 2 }
        ])
};
var bodyDeploymentLocksNextBeat = beatPlanning.PlaceActionCardIntoNextPlayerBeatAndTarget(
    bodyDeploymentLocksNextBeatCombat,
    cardInstanceId: planningGuardInstance,
    cardId: guardAction.Id,
    handIndex: 0,
    guardAction,
    CreateEnemyBodyTarget(),
    new BeatCombatService());
var bodyResolvedToEnemyBeat = bodyDeploymentLocksNextBeat.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 1);
AssertEqual(guardAction.Id, bodyResolvedToEnemyBeat?.CardId, "Body deployment fills the first empty player beat");
AssertEqual(BeatTargetKind.EnemyBeat, bodyResolvedToEnemyBeat?.Target?.Kind, "Body deployment locks the first unlocked enemy beat before targeting the body");
AssertEqual(1, bodyResolvedToEnemyBeat?.Target?.EnemyBeatIndex, "Body deployment resolves to the lowest unlocked enemy beat index");

var bodyDeploymentAfterAllBeatsLockedCombat = CreatePlayableCombat(
    [planningBeatSlashInstanceB],
    actionPoints: 0,
    enemies: [CreateEnemyState("enemy_01", currentHp: 30, maxHp: 30, enemyId: beatEnemy.Id)]) with
{
    BeatRound = CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1],
        playerBeats:
        [
            new PlayerBeatSlot
            {
                BeatIndex = 0,
                CardInstanceId = "card_instance.already_slotted_0",
                CardId = beatSlashCard.Id,
                Target = CreateEnemyBeatTarget(0)
            },
            new PlayerBeatSlot
            {
                BeatIndex = 1,
                CardInstanceId = "card_instance.already_slotted_1",
                CardId = guardAction.Id,
                Target = CreateEnemyBeatTarget(1)
            },
            new PlayerBeatSlot { BeatIndex = 2 }
        ])
};
var bodyDeploymentAfterAllBeatsLocked = beatPlanning.PlaceActionCardIntoNextPlayerBeatAndTarget(
    bodyDeploymentAfterAllBeatsLockedCombat,
    cardInstanceId: planningBeatSlashInstanceB,
    cardId: beatSlashCard.Id,
    handIndex: 0,
    beatSlashCard,
    CreateEnemyBodyTarget(),
    new BeatCombatService());
var bodyResolvedToEnemyBody = bodyDeploymentAfterAllBeatsLocked.BeatRound?.PlayerBeats.Single(beat => beat.BeatIndex == 2);
AssertEqual(BeatTargetKind.EnemyBody, bodyResolvedToEnemyBody?.Target?.Kind, "Body deployment targets the enemy body after all enemy beats are locked");

var discardAfterBeat = beatPlanning.DiscardSlottedActionCards(bodyTargetedBeat);
Assert(discardAfterBeat.DeckZones.DiscardPile.Contains(planningCombat.DeckZones.Hand[0]), "Beat planning discards slotted action card instances after beat resolution");

AssertEqual("I", RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.RomanBeatNumber(0), "Beat UI renders first beat as roman numeral I");
AssertEqual("II", RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.RomanBeatNumber(1), "Beat UI renders second beat as roman numeral II");
AssertEqual("III", RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.RomanBeatNumber(2), "Beat UI renders third beat as roman numeral III");
Assert(RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.IsPointInsideDiamond(36, 36, 72, 72), "Beat UI accepts the center of a diamond slot");
Assert(!RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.IsPointInsideDiamond(4, 4, 72, 72), "Beat UI rejects rectangular corners outside a diamond slot");
var insetConnector = RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.InsetConnectorLine(0, 0, 100, 0, 20);
AssertEqual(20f, insetConnector.StartX, "Beat connector starts after the source arrow head");
AssertEqual(80f, insetConnector.EndX, "Beat connector ends before the target arrow head");
var raisedControlY = RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.RaisedConnectorControlY(120, 80, 30);
AssertEqual(50f, raisedControlY, "Beat connector control point stays above both endpoints");
var arrowBase = RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.ArrowBaseCenter(0, 0, 100, 0, 34);
AssertEqual(34f, arrowBase.SourceBaseX, "Beat connector source line endpoint matches the source arrow tail center");
AssertEqual(66f, arrowBase.TargetBaseX, "Beat connector target line endpoint matches the target arrow tail center");
var arrowTip = RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.ArrowTipFromBaseCenter(34, 0, 0.6f, -0.8f, 10);
AssertEqual(40f, arrowTip.X, "Beat connector arrow tip follows the curve endpoint tangent X");
AssertEqual(-8f, arrowTip.Y, "Beat connector arrow tip follows the curve endpoint tangent Y");
var arrowTail = RoguelikeCardGame.Presentation.Battle.BeatSlotPresentationGeometry.ArrowTailFromTip(100, 0, 1, 0, 34);
AssertEqual(66f, arrowTail.X, "Beat connector arrow tip remains attached to the diamond vertex while the tail backs into the curve");
AssertEqual(-62f, RoguelikeCardGame.Presentation.Battle.BeatCardDragPresentation.CardLiftOffsetY, "Beat card deployment lifts the card instead of dragging the card body to a beat slot");
var beatClashPlanner = new RoguelikeCardGame.Presentation.Battle.BeatClashAnimationPlanner();
var beatClashSteps = beatClashPlanner.Plan(
    [
        new CombatLogEvent
        {
            EventId = "beat_iii_resolved",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = 1,
            SourceId = "card.third",
            TargetIds = ["enemy_b"],
            NumericChanges = new Dictionary<string, int>
            {
                ["beat_index"] = 2,
                ["enemy_damage"] = 5,
                ["player_damage"] = 1,
                ["successful_player_actions"] = 1,
                ["successful_enemy_actions"] = 1
            },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "card_instance.third",
                ["target_kind"] = "EnemyBody"
            }
        },
        new CombatLogEvent
        {
            EventId = "beat_i_blue_energy",
            EventType = CombatLogEventType.BeatEnergyGenerated,
            TurnNumber = 1,
            SourceId = "card.first",
            NumericChanges = new Dictionary<string, int> { ["color_energy_generated"] = 2 },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "card_instance.first",
                ["color"] = "Blue"
            }
        },
        new CombatLogEvent
        {
            EventId = "beat_ii_resolved",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = 1,
            SourceId = "card.second",
            TargetIds = ["enemy_a"],
            NumericChanges = new Dictionary<string, int>
            {
                ["beat_index"] = 1,
                ["enemy_damage"] = 3,
                ["successful_player_actions"] = 1
            },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "card_instance.second",
                ["target_kind"] = "EnemyBeat",
                ["enemy_beat_index"] = "1"
            }
        },
        new CombatLogEvent
        {
            EventId = "beat_i_resolved",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = 1,
            SourceId = "card.first",
            TargetIds = ["enemy_a"],
            NumericChanges = new Dictionary<string, int>
            {
                ["beat_index"] = 0,
                ["enemy_damage"] = 7,
                ["successful_player_actions"] = 1
            },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "card_instance.first",
                ["target_kind"] = "EnemyBody",
                ["stage_results"] = """
                    [
                      {
                        "stage_index": 0,
                        "player_action_kind": "Attack",
                        "player_attack_type": "Slash",
                        "enemy_damage_taken": 4,
                        "player_damage_taken": 0,
                        "successful_player_actions": 1,
                        "successful_enemy_actions": 0
                      },
                      {
                        "stage_index": 1,
                        "player_action_kind": "Block",
                        "enemy_damage_taken": 3,
                        "player_damage_taken": 0,
                        "successful_player_actions": 1,
                        "successful_enemy_actions": 0
                      },
                      {
                        "stage_index": 2,
                        "player_action_kind": "Attack",
                        "player_attack_type": "Strike",
                        "enemy_damage_taken": 0,
                        "player_damage_taken": 0,
                        "successful_player_actions": 1,
                        "successful_enemy_actions": 0
                      }
                    ]
                    """
            }
        },
        new CombatLogEvent
        {
            EventId = "beat_i_red_energy",
            EventType = CombatLogEventType.BeatEnergyGenerated,
            TurnNumber = 1,
            SourceId = "card.first",
            NumericChanges = new Dictionary<string, int> { ["color_energy_generated"] = 1 },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "card_instance.first",
                ["color"] = "Red"
            }
        }
    ]);
AssertSequenceEqual([0, 1, 2], beatClashSteps.Select(step => step.BeatIndex), "Beat clash planner orders steps by beat index");
AssertEqual("card.first", beatClashSteps[0].CardId, "Beat clash planner keeps the resolved action source card");
AssertEqual("card_instance.first", beatClashSteps[0].CardInstanceId, "Beat clash planner keeps the card instance id");
AssertEqual("enemy_a", beatClashSteps[0].TargetId, "Beat clash planner uses the first target id");
AssertEqual("EnemyBody", beatClashSteps[0].TargetKind, "Beat clash planner keeps the target kind");
AssertEqual(7, beatClashSteps[0].EnemyDamage, "Beat clash planner keeps enemy damage");
AssertEqual(3, beatClashSteps[0].ActionStages.Count, "Beat clash planner parses per-action stage results");
AssertSequenceEqual([4, 3, 0], beatClashSteps[0].ActionStages.Select(stage => stage.EnemyDamage), "Beat clash planner keeps per-stage enemy damage");
AssertSequenceEqual([RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Slash, RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Block, RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Strike], beatClashSteps[0].ActionStages.Select(stage => stage.PlayerAnimationKind), "Beat clash planner maps stage action types to animation kinds");
AssertEqual(3, beatClashSteps[0].EnergyGeneratedTotal, "Beat clash planner aggregates generated energy for the action card instance");
AssertSequenceEqual(["Blue", "Red"], beatClashSteps[0].EnergyColors.Select(color => color.Color), "Beat clash planner keeps generated energy colors");
AssertSequenceEqual([2, 1], beatClashSteps[0].EnergyColors.Select(color => color.Amount), "Beat clash planner keeps generated energy amounts");
Assert(!beatClashSteps[0].ReturnToStartBeforeStep, "Beat clash planner does not return before the first step");
Assert(!beatClashSteps[0].ContinuesPreviousTarget, "Beat clash planner does not continue before the first step");
AssertEqual("enemy_a", beatClashSteps[1].TargetId, "Beat clash planner keeps the second beat target id");
AssertEqual(1, beatClashSteps[1].EnemyBeatIndex, "Beat clash planner parses enemy beat index");
Assert(beatClashSteps[1].ContinuesPreviousTarget, "Beat clash planner marks consecutive beats against the same target");
Assert(!beatClashSteps[1].ReturnToStartBeforeStep, "Beat clash planner keeps the camera on consecutive target beats");
AssertEqual("enemy_b", beatClashSteps[2].TargetId, "Beat clash planner keeps the changed target id");
Assert(!beatClashSteps[2].ContinuesPreviousTarget, "Beat clash planner does not continue when the target changes");
Assert(beatClashSteps[2].ReturnToStartBeforeStep, "Beat clash planner returns before switching targets");
var dashPosition = RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPresentation.DashPosition(
    playerPosition: new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayFrame(200, 500, 300, 630),
    targetPosition: new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayFrame(1100, 120, 420, 500));
AssertEqual(700f, dashPosition.X, "Beat clash overlay dashes to exactly 100px before the target");
AssertEqual(500f, dashPosition.Y, "Beat clash overlay dash keeps the player's original vertical position");
var overlayHpFrames = RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPresentation.HealthAfterStages(
    currentHp: 11,
    maxHp: 20,
    beatClashSteps[0].ActionStages);
AssertSequenceEqual([7, 4, 4], overlayHpFrames.Select(frame => frame.CurrentHp), "Beat clash overlay health strip updates after each stage damage");
AssertSequenceEqual([0.35f, 0.2f, 0.2f], overlayHpFrames.Select(frame => frame.HealthRatio), "Beat clash overlay health ratio follows stage damage");
Assert(!RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPresentation.ShouldMove(
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(700, 500),
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(702, 503)),
    "Beat clash overlay skips dash movement between consecutive beats on the same target");
Assert(!RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPresentation.ShouldPlayDash(
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(650, 500),
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(700, 500),
    continuesPreviousTarget: true),
    "Beat clash overlay never replays dash for consecutive beats on the same target");
Assert(RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPresentation.ShouldPlayDash(
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(650, 500),
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(700, 500),
    continuesPreviousTarget: false),
    "Beat clash overlay still dashes for a new target segment");
Assert(RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPresentation.ShouldFaceLeft(
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(700, 500),
    new RoguelikeCardGame.Presentation.Battle.BeatClashOverlayPoint(200, 500)),
    "Beat clash overlay faces left when returning to the start position");
var multiTurnBeatClashSteps = beatClashPlanner.Plan(
    [
        new CombatLogEvent
        {
            EventId = "turn_2_energy",
            EventType = CombatLogEventType.BeatEnergyGenerated,
            TurnNumber = 2,
            NumericChanges = new Dictionary<string, int> { ["color_energy_generated"] = 3 },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "reused_card_instance",
                ["color"] = "Yellow"
            }
        },
        new CombatLogEvent
        {
            EventId = "turn_2_resolved",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = 2,
            SourceId = "card.reused",
            TargetIds = ["enemy_b"],
            NumericChanges = new Dictionary<string, int> { ["beat_index"] = 0 },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "reused_card_instance",
                ["target_kind"] = "EnemyBody"
            }
        },
        new CombatLogEvent
        {
            EventId = "turn_1_resolved",
            EventType = CombatLogEventType.BeatActionResolved,
            TurnNumber = 1,
            SourceId = "card.reused",
            TargetIds = ["enemy_a"],
            NumericChanges = new Dictionary<string, int> { ["beat_index"] = 2 },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "reused_card_instance",
                ["target_kind"] = "EnemyBody"
            }
        },
        new CombatLogEvent
        {
            EventId = "turn_1_energy",
            EventType = CombatLogEventType.BeatEnergyGenerated,
            TurnNumber = 1,
            NumericChanges = new Dictionary<string, int> { ["color_energy_generated"] = 1 },
            Metadata = new Dictionary<string, string>
            {
                ["card_instance_id"] = "reused_card_instance",
                ["color"] = "Red"
            }
        }
    ]);
AssertSequenceEqual([1, 2], multiTurnBeatClashSteps.Select(step => step.TurnNumber), "Beat clash planner keeps full combat log input ordered by turn before beat");
AssertSequenceEqual([1, 3], multiTurnBeatClashSteps.Select(step => step.EnergyGeneratedTotal), "Beat clash planner matches generated energy by turn and card instance");
AssertSequenceEqual(["Red", "Yellow"], multiTurnBeatClashSteps.Select(step => step.EnergyColors.Single().Color), "Beat clash planner does not mix energy colors across turns for a reused card instance");
var actionAnimationCatalog = RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationCatalog.Default;
var runSequence = actionAnimationCatalog.RunWithSword;
AssertEqual(3, runSequence.Sheets.Count, "Beat clash run animation uses the three supplied sword-run sprite sheets");
Assert(runSequence.Sheets.All(sheet => sheet.Columns == 4), "Beat clash run animation treats each sword-run sheet as four frames");
AssertEqual(0.09, runSequence.FrameDurationSeconds, "Beat clash run animation frame duration is slowed down to double the initial MVP speed");
AssertEqual(0.5, RoguelikeCardGame.Presentation.Battle.BeatClashPresentationTiming.PreActionPauseSeconds, "Beat clash cut-in waits half a second before playing the current beat action sequence");
AssertEqual(0.5, RoguelikeCardGame.Presentation.Battle.BeatClashPresentationTiming.ActionIntervalSeconds, "Beat clash cut-in waits half a second between beat actions");
var slashSequences = actionAnimationCatalog.SequencesFor(RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Slash);
AssertEqual(3, slashSequences.Count, "Beat clash slash animation exposes the three supplied combo groups");
Assert(slashSequences.All(sequence => Math.Abs(sequence.FrameDurationSeconds - 0.11) < 0.0001), "Beat clash slash animation frame duration is slowed down to double the initial MVP speed");
AssertSequenceEqual(
    [
        "asset.character.zu.action.attack.combo_1",
        "asset.character.zu.action.attack.combo_2",
        "asset.character.zu.action.attack.combo_3"
    ],
    slashSequences.Select(sequence => sequence.Sheets.Single().AssetId),
    "Beat clash slash animation groups keep stable asset ids");
AssertSequenceEqual([4, 3, 3], slashSequences.Select(sequence => sequence.Sheets.Single().Columns), "Beat clash slash animation reads pre-cropped spritesheets by their authored frame columns");
Assert(slashSequences.All(sequence => sequence.SfxAssetId == "asset.sfx.slash_light"), "Beat clash slash animations all reuse the light slash SFX");
var actionAnimationKinds = actionAnimationCatalog.KindsForCard(
    beatSlashCard with
    {
        BeatActions =
        [
            new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 4 },
            new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Strike, Value = 3 },
            new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Projectile, Value = 2 },
            new BeatActionDefinition { Kind = BeatActionKind.Block, Value = 4 },
            new BeatActionDefinition { Kind = BeatActionKind.Dodge, DodgeChancePercent = 50 }
        ]
    });
AssertSequenceEqual(
    [
        RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Slash,
        RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Strike,
        RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Projectile,
        RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Block,
        RoguelikeCardGame.Presentation.Battle.BeatClashActionAnimationKind.Dodge
    ],
    actionAnimationKinds,
    "Beat clash animation catalog reserves animation interfaces for every current beat action type");
var beatTargetingOperations = new List<string>();
RoguelikeCardGame.Presentation.Battle.BeatTargetingInputPresentation.CompleteTargetSelection(
    markInputHandled: () => beatTargetingOperations.Add("handled"),
    hideArrow: () => beatTargetingOperations.Add("hidden"),
    clearTargetingState: () => beatTargetingOperations.Add("cleared"),
    publishSelection: () => beatTargetingOperations.Add("published"));
AssertEqual("handled,hidden,cleared,published", string.Join(",", beatTargetingOperations), "Beat target selection marks input handled before publishing callbacks that may replace the screen");

var missingCardIdWithInstance = beatService.ValidatePlayerBeatTargets(
    CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats:
        [
            new PlayerBeatSlot
            {
                BeatIndex = 0,
                CardInstanceId = "card_instance_without_definition_id",
                Target = CreateEnemyBeatTarget(0)
            }
        ]),
    beatTargetCombat);
Assert(!missingCardIdWithInstance.Succeeded, "Player beats with a card instance require a card id for resolution");
AssertEqual(BeatTargetValidationFailureReason.CardIdMissing, missingCardIdWithInstance.FailureReason, "Missing beat card id exposes failure reason");

const string beatCombatCardInstanceId = "card_beat_slash_001";
var beatCombat = CreatePlayableCombat(
    [beatSlashCard.Id, finisher.Id],
    actionPoints: 0,
    colorEnergy: ColorEnergyPool.Empty(),
    enemies: [CreateEnemyState("enemy_01", currentHp: 30, maxHp: 30, enemyId: beatEnemy.Id)]) with
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
    ["card.beat_slash_i"] = beatSlashCard with { Id = "card.beat_slash_i" },
    ["card.beat_slash_ii"] = beatSlashCard with { Id = "card.beat_slash_ii" },
    ["card.beat_slash_iii"] = beatSlashCard with { Id = "card.beat_slash_iii" },
    [finisher.Id] = finisher
};
var beatEnemies = new Dictionary<string, EnemyDefinition>
{
    [beatEnemy.Id] = beatEnemy
};

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
AssertEqual(CombatStatus.PlayerTurn, startedBeatCombat.Status, "Beat combat starts in player turn");
AssertEqual(0, startedBeatCombat.ActionPoints, "Beat combat does not use action points");
var withKeptHand = startedBeatCombat with
{
    Status = CombatStatus.EnemyTurn,
    ColorEnergy = ColorEnergyPool.Empty().Add(ColorType.Colorless, 2),
    BeatRound = new BeatRoundState { BeatCount = 3 },
    DeckZones = startedBeatCombat.DeckZones with
    {
        Hand = ["c1", "c2", "kept"],
        DrawPile = ["c6", "c7"]
    }
};
var nextBeatRound = beatTurnService.PrepareNextBeatRound(withKeptHand);
AssertEqual(5, nextBeatRound.DeckZones.Hand.Count, "Beat combat preserves hand and draws two more cards");
Assert(nextBeatRound.DeckZones.Hand.Contains("kept"), "Beat combat keeps unplayed hand cards");
AssertEqual(0, nextBeatRound.ColorEnergy.Count, "Beat combat clears color energy between rounds");
AssertEqual(null, nextBeatRound.BeatRound, "Beat combat clears the resolved beat round before the next planning phase");

var beatRoundCombat = new CombatState
{
    CombatId = "combat_beat_round_factory",
    EncounterId = "encounter.test",
    Status = CombatStatus.PlayerTurn,
    TurnNumber = 1,
    PlayerMaxHp = 60,
    PlayerHp = 60,
    Enemies = [CreateEnemyState("enemy_01", currentHp: 20, maxHp: 20, enemyId: beatEnemy.Id)]
};
var createdBeatRound = new BeatCombatRoundFactory().CreateRound(beatRoundCombat, beatEnemies, playerBeatCount: 3);
AssertEqual(3, createdBeatRound.PlayerBeats.Count, "Beat round factory creates requested player beat slots");
AssertEqual(beatEnemy.BeatSequences[0].Beats.Count, createdBeatRound.EnemyBeats.Count, "Beat round factory maps first enemy beat sequence");
AssertEqual("enemy_card.dummy_slash", createdBeatRound.EnemyBeats[0].ActionCardId, "Beat round factory maps enemy action card id");
AssertEqual(beatEnemy.BeatSequences[0].Beats[0].Actions, createdBeatRound.EnemyBeats[0].Actions, "Beat round factory maps enemy beat actions");

var beatFlowStartCombat = new CombatState
{
    CombatId = "combat_beat_flow",
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
    Enemies = [CreateEnemyState("enemy_01", currentHp: 20, maxHp: 20, enemyId: beatEnemy.Id)]
};
var startedBeatFlow = beatTurnService.StartBeatCombat(beatFlowStartCombat);
var resolvingBeatFlow = startedBeatFlow with
{
    BeatRound = new BeatCombatRoundFactory().CreateRound(startedBeatFlow, beatEnemies, playerBeatCount: 3)
};
var resolvedBeatFlow = beatService.ResolveBeatRound(resolvingBeatFlow, beatCards, beatEnemies);
AssertEqual(CombatStatus.EnemyTurn, resolvedBeatFlow.Combat.Status, "Resolved beat round advances to between-round cleanup status");
var nextBeatFlowRound = beatTurnService.PrepareNextBeatRound(resolvedBeatFlow.Combat);
AssertEqual(7, nextBeatFlowRound.DeckZones.Hand.Count, "Beat flow preserves starting hand and draws two cards after resolution");
Assert(nextBeatFlowRound.DeckZones.Hand.Contains("c1"), "Beat flow keeps cards drawn at combat start");
Assert(nextBeatFlowRound.DeckZones.Hand.Contains("c7"), "Beat flow draws cards for the next round");
var resolvedBeatRound = beatService.ResolveBeatRound(beatCombat, beatCards, beatEnemies);
AssertEqual(1, resolvedBeatRound.Combat.ColorEnergy.Count, "Successful unopposed attack generates one colorless energy");
AssertEqual(21, resolvedBeatRound.Combat.Enemies[0].CurrentHp, "Weakness-adjusted beat damage is applied to enemy HP");
Assert(resolvedBeatRound.Events.Any(item => item.EventType == CombatLogEventType.BeatEnergyGenerated), "Beat energy generation is logged");

var hundredCutsCard = beatSlashCard with
{
    Id = "card.hundred_cuts_test",
    BeatActions =
    [
        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 3, Repeat = 3 }
    ]
};
var lethalMultiStageCombat = beatCombat with
{
    CombatId = "combat_lethal_multi_stage",
    Enemies = [CreateEnemyState("enemy_01", currentHp: 5, maxHp: 5, enemyId: beatEnemy.Id)],
    ColorEnergy = ColorEnergyPool.Empty(),
    BeatRound = beatCombat.BeatRound! with
    {
        PlayerBeats =
        [
            beatCombat.BeatRound.PlayerBeats[0] with
            {
                CardId = hundredCutsCard.Id,
                CardInstanceId = "card_hundred_cuts_001"
            }
        ]
    }
};
var lethalResult = beatService.ResolveBeatRound(
    lethalMultiStageCombat,
    beatCards.Append(new KeyValuePair<string, CardDefinition>(hundredCutsCard.Id, hundredCutsCard)).ToDictionary(item => item.Key, item => item.Value),
    new Dictionary<string, EnemyDefinition> { [beatEnemy.Id] = beatEnemy with { Resistances = new BeatResistanceProfile() } });
var lethalLog = lethalResult.Events.Single(item => item.EventType == CombatLogEventType.BeatActionResolved && item.SourceId == hundredCutsCard.Id);
AssertEqual(0, lethalResult.Combat.Enemies[0].CurrentHp, "Lethal multi-stage beat stops after reducing the enemy to zero HP");
AssertEqual(2, lethalResult.Combat.ColorEnergy.Count, "Only successful stages before target death generate beat energy");
AssertEqual(5, lethalLog.NumericChanges["enemy_damage"], "Lethal multi-stage beat caps total applied damage at remaining HP");
var lethalStages = JsonSerializer.Deserialize<List<BeatActionStageResult>>(lethalLog.Metadata["stage_results"], options) ?? [];
AssertEqual(2, lethalStages.Count, "Lethal multi-stage beat omits stages after target death");
AssertSequenceEqual([3, 2], lethalStages.Select(stage => stage.EnemyDamageTaken), "Lethal multi-stage beat logs per-stage applied damage");

var unorderedBeatCombat = CreatePlayableCombat(
    [],
    actionPoints: 0,
    colorEnergy: ColorEnergyPool.Empty(),
    enemies: [CreateEnemyState("enemy_01", currentHp: 40, maxHp: 40, enemyId: beatEnemy.Id)]) with
{
    CombatId = "combat_unordered_player_beats",
    BeatRound = CreateBeatTargetRound(
        beatCount: 3,
        enemyBeatIndexes: [0, 1, 2],
        playerBeats:
        [
            new PlayerBeatSlot
            {
                BeatIndex = 2,
                CardInstanceId = "card_instance.beat_iii",
                CardId = "card.beat_slash_iii",
                Target = CreateEnemyBeatTarget(0)
            },
            new PlayerBeatSlot
            {
                BeatIndex = 0,
                CardInstanceId = "card_instance.beat_i",
                CardId = "card.beat_slash_i",
                Target = CreateEnemyBeatTarget(2)
            },
            new PlayerBeatSlot
            {
                BeatIndex = 1,
                CardInstanceId = "card_instance.beat_ii",
                CardId = "card.beat_slash_ii",
                Target = CreateEnemyBeatTarget(1)
            }
        ])
};
var resolvedUnorderedBeatRound = new BeatCombatService().ResolveBeatRound(unorderedBeatCombat, beatCards, beatEnemies);
var resolvedPlayerBeatIndexes = resolvedUnorderedBeatRound.Events
    .Where(item => item.EventType == CombatLogEventType.BeatActionResolved)
    .Select(item => item.NumericChanges["beat_index"]);
AssertSequenceEqual([0, 1, 2], resolvedPlayerBeatIndexes, "Beat round resolves player actions by left-to-right beat index, not PlayerBeats list order");

var unblockedEnemyBeatCombat = CreatePlayableCombat(
    [beatSlashCard.Id, finisher.Id],
    actionPoints: 0,
    colorEnergy: ColorEnergyPool.Empty(),
    enemies: [CreateEnemyState("enemy_01", currentHp: 30, maxHp: 30, enemyId: beatEnemy.Id)]) with
{
    BeatRound = new BeatRoundState
    {
        BeatCount = 3,
        PlayerBeats = [],
        EnemyBeats =
        [
            new EnemyBeatSlot
            {
                EnemyInstanceId = "enemy_01",
                BeatIndex = 1,
                ActionCardId = "enemy_card.unblocked_slash",
                Actions =
                [
                    new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 4 }
                ]
            }
        ]
    }
};
var resolvedUnblockedEnemyBeat = beatService.ResolveBeatRound(unblockedEnemyBeatCombat, beatCards, beatEnemies);
AssertEqual(56, resolvedUnblockedEnemyBeat.Combat.PlayerHp, "Unblocked enemy beat action damages the player");
AssertEqual(0, resolvedUnblockedEnemyBeat.Combat.ColorEnergy.Count, "Unblocked enemy beat action does not generate player color energy");
Assert(
    resolvedUnblockedEnemyBeat.Events.Any(item =>
        item.EventType == CombatLogEventType.BeatActionResolved &&
        item.SourceId == "enemy_card.unblocked_slash" &&
        item.TargetIds.Contains("player")),
    "Unblocked enemy beat action is logged");

AssertThrows(
    () => beatService.ResolveBeatRound(beatCombat with { Status = CombatStatus.Defeat }, beatCards, beatEnemies),
    "Defeated combat cannot resolve another beat round");
AssertThrows(
    () => beatService.ResolveBeatRound(beatCombat with { Status = CombatStatus.Victory }, beatCards, beatEnemies),
    "Victorious combat cannot resolve another beat round");

var finisherReadyCombat = resolvedBeatRound.Combat with
{
    Status = CombatStatus.PlayerTurn,
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
AssertEqual(CombatStatus.PlayerTurn, releasedFinisher.Combat.Status, "Slotted finisher release keeps the current beat round status");
Assert(releasedFinisher.Events.Any(item => item.EventType == CombatLogEventType.FinisherReleased), "Finisher release is logged");

AssertThrows(
    () => beatService.ReleaseSlottedFinisher(finisherReadyCombat with { Status = CombatStatus.Defeat }, finisher, "enemy_01"),
    "Defeated combat cannot release a slotted finisher");
AssertThrows(
    () => beatService.ReleaseSlottedFinisher(finisherReadyCombat with { Status = CombatStatus.Victory }, finisher, "enemy_01"),
    "Victorious combat cannot release a slotted finisher");

var actionCardInFinisherSlot = strike with
{
    Id = "card.action_in_finisher_slot",
    Type = CardType.Action,
    Cost = 0,
    ColorEnergyCost = new ColorEnergyCost { Mode = ColorEnergySpendMode.Fixed, Amount = 3, MinAmount = 3 },
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 9 }]
};
var actionSlottedCombat = finisherReadyCombat with
{
    BeatRound = finisherReadyCombat.BeatRound! with
    {
        FinisherSlot = new FinisherSlotState
        {
            CardInstanceId = "action_slot_001",
            CardId = actionCardInFinisherSlot.Id
        }
    }
};
AssertThrows(
    () => beatService.ReleaseSlottedFinisher(actionSlottedCombat, actionCardInFinisherSlot, "enemy_01"),
    "Slotted finisher release rejects non-finisher card definitions");

var runFactory = new RunStateFactory();
var run = runFactory.CreateNewRun(
    runId: "run_smoke_001",
    seed: 12345,
    playerMaxHp: 60,
    baseActionPoints: 3,
    cardsPerTurn: 5,
    starterDeck: [strike.Id, strike.Id, strike.Id, strike.Id, finisher.Id],
    encounterSequence: [encounter.Id, eliteEncounter.Id, bossEncounter.Id],
    mainHandWeaponId: "weapon.revolver_sword",
    offHandWeaponId: "weapon.mechanical_arm");

AssertEqual(RunStatus.InProgress, run.Status, "RunStateFactory starts run in progress");
AssertEqual(60, run.PlayerHp, "RunStateFactory starts at full HP");
AssertEqual(5, run.MasterDeck.Count, "RunStateFactory copies starter deck");
AssertEqual(5, run.MasterDeckInstances.Count, "RunStateFactory creates card instances for the master deck");
AssertEqual(12345, run.Seed, "RunStateFactory records the run seed");
AssertEqual("weapon.revolver_sword", run.MainHandWeaponId, "RunState records main-hand weapon");
AssertEqual("weapon.mechanical_arm", run.OffHandWeaponId, "RunState records off-hand weapon");

var enchantedRun = runFactory.EnchantCard(run, run.MasterDeckInstances[0].InstanceId, ColorType.Blue);
var blueEnchantedStrike = enchantedRun.MasterDeckInstances[0];
AssertEqual(ColorType.Blue, blueEnchantedStrike.Enchantment?.Color, "CardInstance records blue enchantment");
AssertEqual(1, enchantedRun.CardEnchantments.Count, "RunState exposes card enchantment state");
var generatedColor = strike.ColorEnergyGeneration?.ResolveColor(blueEnchantedStrike.Enchantment);
AssertEqual(ColorType.Blue, generatedColor, "Enchanted action card can theoretically generate blue color energy");
var blueEnergyPool = ColorEnergyPool.Empty().Add(generatedColor ?? ColorType.Colorless, strike.ColorEnergyGeneration?.Amount ?? 0);
AssertEqual(1, blueEnergyPool.Count, "ColorEnergyPool accepts generated color energy");
AssertEqual(ColorType.Blue, blueEnergyPool.Slots[0].Color, "Generated color energy keeps blue color");
Assert(finisher.ColorEnergyCost is { Mode: ColorEnergySpendMode.Fixed, Amount: 3, MinAmount: 3 }, "Finisher can define fixed 3 color energy cost");
var serializedRun = JsonSerializer.Serialize(enchantedRun, options);
var deserializedRun = JsonSerializer.Deserialize<RunState>(serializedRun, options);
AssertEqual(ColorType.Blue, deserializedRun?.MasterDeckInstances[0].Enchantment?.Color, "RunState serializes card instance enchantment");

var startingDeckSelectionService = new StartingDeckSelectionService();
var revolverStartingPool = new WeaponStartingPoolDefinition
{
    WeaponId = "weapon.revolver_sword",
    CardIds =
    [
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_hundred_blades"
    ]
};
var armStartingPool = new WeaponStartingPoolDefinition
{
    WeaponId = "weapon.mechanical_arm",
    CardIds =
    [
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_counter"
    ]
};
var startingPools = new[] { revolverStartingPool, armStartingPool };
var revolverMainSelection = new StartingDeckSelection
{
    MainHandWeaponId = revolverStartingPool.WeaponId,
    OffHandWeaponId = armStartingPool.WeaponId,
    MainHandCardIds =
    [
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_hundred_blades"
    ],
    OffHandCardIds =
    [
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_counter"
    ]
};
var revolverMainValidation = startingDeckSelectionService.Validate(revolverMainSelection, startingPools);
Assert(revolverMainValidation.IsValid, "Revolver sword main-hand 3 actions plus mechanical arm off-hand 3 actions is a valid temporary start");
AssertEqual(6, revolverMainValidation.SelectedCardIds.Count, "Starting deck selection creates a temporary 6-card action-only starter deck");
var automaticStarterCardsById = new Dictionary<string, CardDefinition>
{
    ["card.revolver_slash"] = strike with { Id = "card.revolver_slash" },
    ["card.revolver_hundred_blades"] = strike with { Id = "card.revolver_hundred_blades" },
    ["card.revolver_planetary_poem"] = finisher with { Id = "card.revolver_planetary_poem" },
    ["card.arm_guard"] = guardAction with { Id = "card.arm_guard" },
    ["card.arm_counter"] = guardAction with { Id = "card.arm_counter", WeaponId = "weapon.mechanical_arm" },
    ["card.arm_sweep"] = finisher with { Id = "card.arm_sweep", WeaponId = "weapon.mechanical_arm" }
};
var automaticRevolverMain = startingDeckSelectionService.BuildAutomaticStarterDeck(
    revolverStartingPool.WeaponId,
    armStartingPool.WeaponId,
    startingPools,
    automaticStarterCardsById);
Assert(automaticRevolverMain.IsValid, "Automatic starter deck accepts both temporary 3-action weapon pools");
AssertSequenceEqual(revolverMainValidation.SelectedCardIds, automaticRevolverMain.SelectedCardIds, "Automatic starter deck matches the temporary action-only pool contents");
Assert(automaticRevolverMain.SelectedCardIds.All(cardId => automaticStarterCardsById[cardId].Type == CardType.Action), "Automatic starter deck excludes finisher cards");
var revolverMainRun = runFactory.CreateNewRunFromWeaponSelection(
    "run_revolver_main",
    22222,
    60,
    3,
    5,
    revolverMainSelection.MainHandWeaponId,
    revolverMainSelection.OffHandWeaponId,
    revolverMainValidation.SelectedCardIds,
    [encounter.Id]);
AssertEqual("weapon.revolver_sword", revolverMainRun.MainHandWeaponId, "Run can start with revolver sword as main hand");
AssertEqual("weapon.mechanical_arm", revolverMainRun.OffHandWeaponId, "Run can start with mechanical arm as off hand");
AssertEqual(6, revolverMainRun.MasterDeckInstances.Count, "Weapon-selected run creates card instances for the temporary action-only starter cards");

var armMainSelection = new StartingDeckSelection
{
    MainHandWeaponId = armStartingPool.WeaponId,
    OffHandWeaponId = revolverStartingPool.WeaponId,
    MainHandCardIds =
    [
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_counter"
    ],
    OffHandCardIds =
    [
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_hundred_blades",
    ]
};
var armMainValidation = startingDeckSelectionService.Validate(armMainSelection, startingPools);
Assert(armMainValidation.IsValid, "Mechanical arm main-hand 3 actions plus revolver sword off-hand 3 actions is a valid temporary start");
var armMainRun = runFactory.CreateNewRunFromWeaponSelection(
    "run_arm_main",
    22223,
    60,
    3,
    5,
    armMainSelection.MainHandWeaponId,
    armMainSelection.OffHandWeaponId,
    armMainValidation.SelectedCardIds,
    [encounter.Id]);
AssertEqual("weapon.mechanical_arm", armMainRun.MainHandWeaponId, "Run can start with mechanical arm as main hand");
AssertEqual("weapon.revolver_sword", armMainRun.OffHandWeaponId, "Run can start with revolver sword as off hand");
AssertEqual(6, armMainRun.MasterDeckInstances.Count, "Automatic-compatible arm main run also creates 6 temporary action-only starter card instances");

var loadedContent = GameContent.LoadFromDataRoot(FindGameDataRoot());
AssertEqual(5, loadedContent.ColorsById.Count, "GameContent loads all MVP colors");
AssertEqual("tempo_conversion", loadedContent.ColorsById["color.yellow"].Role, "GameContent maps color roles");
AssertEqual("extra_casts", loadedContent.ColorsById["color.yellow"].BaseEffectTemplate.Op, "GameContent maps color effect templates");
AssertEqual(1, loadedContent.ColorsById["color.yellow"].StackRule.MaxPerCard, "GameContent maps color stack rules");
AssertEqual(2, loadedContent.WeaponsById.Count, "GameContent loads MVP weapons");
AssertEqual("card_pool.starting.revolver_sword", loadedContent.WeaponsById["weapon.revolver_sword"].StartingPoolId, "GameContent maps weapon starting pools");
AssertEqual(6, loadedContent.CardsById.Count, "GameContent loads the trimmed 6-card MVP weapon card set");
var loadedActionCard = loadedContent.CardsById["card.revolver_slash"];
AssertEqual(CardType.Action, loadedActionCard.Type, "GameContent maps card type");
AssertEqual("action_point", loadedActionCard.Costs[0].Resource, "GameContent maps structured card costs");
Assert(loadedActionCard.Targeting.Required, "GameContent maps targeting.required");
AssertEqual(5, loadedActionCard.ColorInteractions.Enchantment.SelfEffects.Count, "GameContent maps action enchantment color effects");
AssertEqual("move_card", loadedActionCard.AfterPlay[0].Type, "GameContent maps after_play operations");
AssertEqual("single_slash_action", loadedActionCard.Balance.Role, "GameContent maps card balance metadata");
var loadedBeatActionCard = loadedContent.CardsById["card.revolver_slash"];
Assert(loadedBeatActionCard.BeatActions.Count > 0, "GameContent maps action card beat actions");
AssertEqual(BeatActionKind.Attack, loadedBeatActionCard.BeatActions[0].Kind, "GameContent maps beat action kind");
AssertEqual(BeatAttackType.Slash, loadedBeatActionCard.BeatActions[0].AttackType, "GameContent maps beat attack type");
AssertEqual("weapon", loadedBeatActionCard.CardSource, "GameContent maps card source");
var loadedFinisher = loadedContent.CardsById["card.revolver_planetary_poem"];
AssertEqual("any", loadedFinisher.ColorEnergyCost?.ColorFilter, "GameContent maps finisher color filter");
AssertEqual(5, loadedFinisher.ColorInteractions.FinisherColorEffects.Count, "GameContent maps finisher color effects");
var loadedBeatFinisher = loadedContent.CardsById["card.revolver_planetary_poem"];
AssertEqual(BeatAttackType.Projectile, loadedBeatFinisher.FinisherAttackType, "GameContent maps finisher attack type");
var loadedBeatEnemy = loadedContent.EnemiesById.Values.First(enemy => enemy.BeatSequences.Count > 0);
AssertEqual(BeatResistanceGrade.Standard, loadedBeatEnemy.Resistances.Strike, "GameContent maps default enemy strike resistance");
Assert(loadedBeatEnemy.BeatSequences[0].Beats.Count > 0, "GameContent maps enemy beat sequences");
AssertEqual(3, loadedContent.ExpandedStartingCardIdsForWeapon("weapon.revolver_sword").Count, "GameContent expands temporary 3-action weapon starting pools");
Assert(loadedContent.ExpandedStartingCardIdsForWeapon("weapon.revolver_sword").All(cardId => loadedContent.CardsById[cardId].Type == CardType.Action), "Revolver sword starting pool temporarily excludes finishers");
Assert(loadedContent.ExpandedStartingCardIdsForWeapon("weapon.mechanical_arm").All(cardId => loadedContent.CardsById[cardId].Type == CardType.Action), "Mechanical arm starting pool temporarily excludes finishers");
AssertEqual(CardRarity.Starter, loadedContent.CardsById["card.revolver_hundred_blades"].Rarity, "Hundred Blades is part of the starter-sized MVP card set");
AssertEqual(CardRarity.Starter, loadedContent.CardsById["card.arm_counter"].Rarity, "Counter is part of the starter-sized MVP card set");
Assert(!loadedContent.CardPoolsById.Values
    .SelectMany(pool => pool.RewardByRarity.Values.SelectMany(cardIds => cardIds))
    .Any(cardId => loadedContent.CardsById[cardId].Type == CardType.Finisher), "Active weapon reward pools temporarily exclude finishers");
var loadedAutomaticStarterDeck = startingDeckSelectionService.BuildAutomaticStarterDeck(
    "weapon.revolver_sword",
    "weapon.mechanical_arm",
    loadedContent.WeaponsById.Values.Select(weapon => new WeaponStartingPoolDefinition
    {
        WeaponId = weapon.Id,
        CardIds = loadedContent.ExpandedStartingCardIdsForWeapon(weapon.Id).ToList()
    }),
    loadedContent.CardsById);
Assert(loadedAutomaticStarterDeck.IsValid, "Loaded temporary action-only starter deck can be built automatically");
AssertEqual(6, loadedAutomaticStarterDeck.SelectedCardIds.Count, "Loaded temporary automatic starter deck contains 6 cards");
Assert(loadedAutomaticStarterDeck.SelectedCardIds.All(cardId => loadedContent.CardsById[cardId].Type == CardType.Action), "Loaded temporary automatic starter deck contains only action cards");
AssertEqual(3, loadedContent.EncountersById.Count, "GameContent loads the trimmed MVP encounter sequence");
AssertSequenceEqual(
    ["encounter.mvp.normal_01", "encounter.mvp.normal_03", "encounter.mvp.boss_01"],
    loadedContent.MvpRun.EncounterSequence,
    "MVP run keeps one single-target normal, one multi-target normal, and one boss encounter");
AssertEqual(0, loadedContent.EncountersById["encounter.mvp.normal_01"].RewardProfile.CardPackIds.Count, "GameContent preserves empty card_pack_ids compatibility field");
AssertEqual("左轮剑", loadedContent.WeaponName("weapon.revolver_sword"), "GameContent maps localization for weapons");
Assert(loadedContent.AssetsById.ContainsKey("asset.card.template.action"), "GameContent loads presentation asset manifest");
AssertEqual("audio", loadedContent.AssetsById["asset.sfx.slash_light"].Type, "GameContent loads the slash SFX audio asset");
Assert(loadedContent.AssetsById.ContainsKey("asset.background.mvp_battle"), "GameContent keeps the single active MVP battle background asset");
AssertEqual(1, loadedContent.AssetsById.Keys.Count(id => id.StartsWith("asset.background.mvp_battle", StringComparison.Ordinal)), "GameContent exposes only one active MVP battle background asset");
Assert(!loadedContent.AssetsById.ContainsKey("asset.background.mvp_battle.back"), "GameContent no longer exposes a split back battle background asset");
Assert(!loadedContent.AssetsById.ContainsKey("asset.background.mvp_battle.front"), "GameContent no longer exposes a split front battle background asset");
var actionPanelLayout = CardPanelLayout.For(CardType.Action);
AssertEqual(600f, actionPanelLayout.TemplateSize.Width, "Action card panel uses the new template width");
AssertEqual(802f, actionPanelLayout.TemplateSize.Height, "Action card panel uses the new template height");
AssertEqual(148f, actionPanelLayout.NameRect.X, "Action card title starts after the cost medallion");
AssertEqual(62f, actionPanelLayout.NameRect.Y, "Action card title aligns with the new top banner");
AssertEqual(64f, actionPanelLayout.CostRect.X, "Action card cost is centered in the new circular medallion");
AssertEqual(58f, actionPanelLayout.CostRect.Y, "Action card cost follows the new circular medallion");
Assert(actionPanelLayout.RulesRect.Y >= 590f, "Action card rules text sits in the lower rules panel");
Assert(actionPanelLayout.RulesRect.Y + actionPanelLayout.RulesRect.Height <= 760f, "Action card rules text stays inside the lower rules panel");
var actionTemplatePath = Path.Combine(Directory.GetParent(FindGameDataRoot())!.FullName, "assets", "art", "cards", "templates", "action_card_template.png");
var actionTemplatePngSize = PngSize(actionTemplatePath);
AssertEqual(600, actionTemplatePngSize.Width, "Action card template PNG is resized to 600 px wide");
AssertEqual(802, actionTemplatePngSize.Height, "Action card template PNG is resized to 802 px tall");

var underPicked = startingDeckSelectionService.Validate(revolverMainSelection with
{
    MainHandCardIds = revolverMainSelection.MainHandCardIds.Take(2).ToList()
}, startingPools);
Assert(!underPicked.IsValid, "Under-picking main-hand starting cards cannot start a run");

var overPicked = startingDeckSelectionService.Validate(revolverMainSelection with
{
    OffHandCardIds = revolverMainSelection.OffHandCardIds.Concat(["card.arm_guard"]).ToList()
}, startingPools);
Assert(!overPicked.IsValid, "Over-picking off-hand starting cards cannot start a run");

var outsidePoolPick = startingDeckSelectionService.Validate(revolverMainSelection with
{
    MainHandCardIds =
    [
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_hundred_blades",
        "card.arm_guard"
    ]
}, startingPools);
Assert(!outsidePoolPick.IsValid, "A card outside the selected weapon starting pool cannot be selected");

var combatFactory = new CombatStateFactory();
var damagedRunReadyForCombat = run with { PlayerHp = 1 };
var fullHealCombat = combatFactory.CreateCombat(
    combatId: "combat_full_heal_001",
    runState: damagedRunReadyForCombat,
    encounter: encounter,
    enemiesById: enemiesById);
AssertEqual(run.PlayerMaxHp, fullHealCombat.PlayerHp, "CombatStateFactory starts each combat at full player HP");

var initialCombat = combatFactory.CreateCombat(
    combatId: "combat_smoke_001",
    runState: run,
    encounter: encounter,
    enemiesById: enemiesById);

AssertEqual(CombatStatus.NotStarted, initialCombat.Status, "CombatStateFactory creates a not-started combat");
AssertEqual(0, initialCombat.ActionPoints, "CombatStateFactory does not restore action points before combat starts");
AssertEqual(run.MasterDeck.Count, initialCombat.DeckZones.DrawPileCount, "CombatStateFactory places run deck in draw pile");
AssertEqual(run.MasterDeckInstances[0].InstanceId, initialCombat.DeckZones.DrawPile[0], "CombatStateFactory tracks card instances in combat deck zones");
AssertEqual(CombatLogEventType.CombatStarted, initialCombat.Log[0].EventType, "CombatStateFactory records combat start event");

var turnService = new CombatTurnService();
var combat = turnService.StartCombat(initialCombat);

AssertEqual(CombatStatus.PlayerTurn, combat.Status, "CombatStateFactory starts on player turn");
AssertEqual(run.BaseActionPoints, combat.ActionPoints, "CombatStateFactory restores base action points");
AssertEqual(run.CardsPerTurn, combat.DeckZones.HandCount, "StartCombat draws the opening hand");
AssertEqual(0, combat.DeckZones.DrawPileCount, "StartCombat removes drawn cards from draw pile");
AssertEqual(enemy.MaxHp, combat.Enemies[0].CurrentHp, "CombatStateFactory creates enemy state at full HP");
Assert(combat.Log.Any(item => item.EventType == CombatLogEventType.CardsDrawn), "StartCombat records a draw event");
Assert(combat.Log.Any(item => item.EventType == CombatLogEventType.TurnStarted), "StartCombat records a turn-start event");

var openingIntentViews = turnService.GetEnemyIntentViews(combat, enemiesById);
AssertEqual(1, openingIntentViews.Count, "GetEnemyIntentViews returns one view per living enemy");
AssertEqual("intent.attack", openingIntentViews[0].IntentId, "GetEnemyIntentViews exposes the current fixed intent id");
AssertEqual(EnemyIntentType.Attack, openingIntentViews[0].IntentType, "GetEnemyIntentViews exposes intent type");
AssertEqual(5, openingIntentViews[0].EffectPreviews[0].Value, "GetEnemyIntentViews exposes damage preview value");

var endedTurn = turnService.EndPlayerTurn(combat with
{
    ActionPoints = 2,
    ColorEnergy = ColorEnergyPool.Empty().Add(ColorType.Blue, 2),
    PlayerBlock = 7
});
AssertEqual(CombatStatus.EnemyTurn, endedTurn.Status, "EndPlayerTurn enters enemy turn");
AssertEqual(0, endedTurn.ActionPoints, "EndPlayerTurn clears unused action points");
AssertEqual(0, endedTurn.ColorEnergy.Count, "EndPlayerTurn clears color energy");
AssertEqual(0, endedTurn.DeckZones.HandCount, "EndPlayerTurn discards hand");
AssertEqual(run.CardsPerTurn, endedTurn.DeckZones.DiscardPileCount, "EndPlayerTurn moves hand into discard pile");
AssertEqual(7, endedTurn.PlayerBlock, "EndPlayerTurn keeps block until next turn start");
Assert(endedTurn.Log.Any(item => item.EventType == CombatLogEventType.CardsDiscarded), "EndPlayerTurn records discarded cards");

var afterEnemyTurn = turnService.ResolveEnemyTurn(endedTurn, enemiesById);
AssertEqual(1, afterEnemyTurn.Enemies[0].IntentIndex, "ResolveEnemyTurn advances fixed intent index");
AssertEqual(2, afterEnemyTurn.PlayerBlock, "ResolveEnemyTurn spends player block before HP");
AssertEqual(60, afterEnemyTurn.PlayerHp, "ResolveEnemyTurn prevents HP damage when block fully covers attack");
Assert(afterEnemyTurn.Log.Any(item => item.EventType == CombatLogEventType.EnemyIntentResolved), "ResolveEnemyTurn records enemy intent resolution");

var nextTurn = turnService.PrepareNextPlayerTurn(afterEnemyTurn);
AssertEqual(CombatStatus.PlayerTurn, nextTurn.Status, "PrepareNextPlayerTurn returns to player turn");
AssertEqual(2, nextTurn.TurnNumber, "PrepareNextPlayerTurn increments turn number");
AssertEqual(0, nextTurn.PlayerBlock, "PrepareNextPlayerTurn clears remaining block");
AssertEqual(run.BaseActionPoints, nextTurn.ActionPoints, "PrepareNextPlayerTurn restores base action points");
AssertEqual(run.CardsPerTurn, nextTurn.DeckZones.HandCount, "PrepareNextPlayerTurn draws a new hand");
Assert(nextTurn.Log.Any(item => item.EventType == CombatLogEventType.DeckReshuffled), "PrepareNextPlayerTurn reshuffles discard when draw pile is empty");

var drawCycleState = new CombatState
{
    CombatId = "combat_draw_cycle",
    EncounterId = encounter.Id,
    Status = CombatStatus.PlayerTurn,
    TurnNumber = 1,
    PlayerMaxHp = 60,
    PlayerHp = 60,
    PlayerBlock = 0,
    BaseActionPoints = 3,
    CardsPerTurn = 5,
    ActionPoints = 3,
    DeckZones = new DeckZones
    {
        DrawPile = ["draw_01"],
        Hand = [],
        DiscardPile = ["discard_01", "discard_02"]
    }
};

var afterDrawCycle = turnService.DrawCards(drawCycleState, 3);
AssertEqual(3, afterDrawCycle.DeckZones.HandCount, "DrawCards draws across draw pile and discard reshuffle");
AssertEqual(0, afterDrawCycle.DeckZones.DrawPileCount, "DrawCards consumes reshuffled draw pile when exact count is drawn");
AssertEqual(0, afterDrawCycle.DeckZones.DiscardPileCount, "DrawCards clears discard pile after reshuffle");
AssertEqual("draw_01", afterDrawCycle.DeckZones.Hand[0], "DrawCards draws existing draw pile before reshuffling discard");
Assert(afterDrawCycle.Log.Any(item => item.EventType == CombatLogEventType.DeckReshuffled), "DrawCards records reshuffle event");

var shuffleDeck = new[]
{
    "card_01",
    "card_02",
    "card_03",
    "card_04",
    "card_05",
    "card_06",
    "card_07",
    "card_08",
    "card_09",
    "card_10"
};
var seededRunA = runFactory.CreateNewRun("run_seed_a", 24680, 60, 3, 5, shuffleDeck, [encounter.Id]);
var seededRunB = runFactory.CreateNewRun("run_seed_b", 24680, 60, 3, 5, shuffleDeck, [encounter.Id]);
var randomStreamsA = RunRandomStreams.FromRunSeed(seededRunA.Seed);
var randomStreamsB = RunRandomStreams.FromRunSeed(seededRunB.Seed);
var shuffledCombatA = new CombatStateFactory(randomStreamsA.Deck.Shuffle)
    .CreateCombat("combat_seed_a", seededRunA, encounter, enemiesById);
var shuffledCombatB = new CombatStateFactory(randomStreamsB.Deck.Shuffle)
    .CreateCombat("combat_seed_b", seededRunB, encounter, enemiesById);
var openingHandA = new CombatTurnService(randomStreamsA.Deck.Shuffle)
    .StartCombat(shuffledCombatA).DeckZones.Hand;
var openingHandB = new CombatTurnService(randomStreamsB.Deck.Shuffle)
    .StartCombat(shuffledCombatB).DeckZones.Hand;
var openingHandDefinitionsA = openingHandA.Select(instanceId => seededRunA.MasterDeckInstances.Single(instance => instance.InstanceId == instanceId).DefinitionId).ToList();
var openingHandDefinitionsB = openingHandB.Select(instanceId => seededRunB.MasterDeckInstances.Single(instance => instance.InstanceId == instanceId).DefinitionId).ToList();
AssertSequenceEqual(openingHandDefinitionsA, openingHandDefinitionsB, "Same seed and deck reproduce opening hand definition order");
AssertEqual(seededRunA.CardsPerTurn, openingHandA.Count, "Seeded opening hand still draws cards per turn");

var differentSeedRun = runFactory.CreateNewRun("run_seed_c", 24681, 60, 3, 5, shuffleDeck, [encounter.Id]);
var differentSeedStreams = RunRandomStreams.FromRunSeed(differentSeedRun.Seed);
var differentSeedCombat = new CombatStateFactory(differentSeedStreams.Deck.Shuffle)
    .CreateCombat("combat_seed_c", differentSeedRun, encounter, enemiesById);
var differentSeedOpeningHand = new CombatTurnService(differentSeedStreams.Deck.Shuffle)
    .StartCombat(differentSeedCombat).DeckZones.Hand;
var differentSeedOpeningHandDefinitions = differentSeedOpeningHand
    .Select(instanceId => differentSeedRun.MasterDeckInstances.Single(instance => instance.InstanceId == instanceId).DefinitionId)
    .ToList();
Assert(!openingHandDefinitionsA.SequenceEqual(differentSeedOpeningHandDefinitions), "Different seeds produce a different opening hand for the smoke deck");

var reshuffleState = drawCycleState with
{
    DeckZones = new DeckZones
    {
        DrawPile = [],
        Hand = [],
        DiscardPile = ["discard_01", "discard_02", "discard_03", "discard_04", "discard_05"]
    }
};
var reshuffleA = new CombatTurnService(RunRandomStreams.FromRunSeed(112233).Deck.Shuffle)
    .DrawCards(reshuffleState, 5).DeckZones.Hand;
var reshuffleB = new CombatTurnService(RunRandomStreams.FromRunSeed(112233).Deck.Shuffle)
    .DrawCards(reshuffleState, 5).DeckZones.Hand;
AssertSequenceEqual(reshuffleA, reshuffleB, "Same seed reproduces discard reshuffle order");

var isolatedBaselineStreams = RunRandomStreams.FromRunSeed(998877);
var isolatedBaselineDeck = isolatedBaselineStreams.Deck.Shuffle(shuffleDeck);
var isolatedPerturbedStreams = RunRandomStreams.FromRunSeed(998877);
_ = isolatedPerturbedStreams.Reward.NextInt(100);
_ = isolatedPerturbedStreams.Map.NextInt(100);
_ = isolatedPerturbedStreams.Encounter.NextInt(100);
_ = isolatedPerturbedStreams.Reward.Shuffle(shuffleDeck);
var isolatedPerturbedDeck = isolatedPerturbedStreams.Deck.Shuffle(shuffleDeck);
AssertSequenceEqual(isolatedBaselineDeck, isolatedPerturbedDeck, "Other random streams do not affect deck stream results");

var generatedSeedA = RunSeedGenerator.CreateSeed();
var generatedSeedB = RunSeedGenerator.CreateSeed();
Assert(generatedSeedA != 12345 || generatedSeedB != 12345, "New Run seed generation is not fixed to 12345");

var cardPlayService = new CardPlayService(turnService);

var playableAction = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id], actionPoints: 1), strike, "enemy_01");
Assert(playableAction.Succeeded, "Action card can be played with enough action points and a valid target");
AssertEqual(0, playableAction.Combat.ActionPoints, "Action card consumes action points");
AssertEqual(18, playableAction.Combat.Enemies[0].CurrentHp, "Damage effect reduces target enemy HP");
AssertEqual(1, playableAction.Combat.ColorEnergy.Count, "Unenchanted action card generates colorless energy");
AssertEqual(ColorType.Colorless, playableAction.Combat.ColorEnergy.Slots[0].Color, "Unenchanted action card energy is colorless");
AssertEqual(ColorType.Colorless, playableAction.Preview?.GeneratedColorEnergyColor, "Action preview exposes generated colorless energy");
AssertEqual(0, playableAction.Combat.DeckZones.HandCount, "Played card leaves hand");
AssertEqual(strike.Id, playableAction.Combat.DeckZones.DiscardPile.Single(), "Played card enters discard pile");
Assert(playableAction.Events.Any(item => item.EventType == CombatLogEventType.CardPlayed), "Successful card play emits a card-play log event");
Assert(playableAction.Events.Any(item => item.Metadata.TryGetValue("effect_type", out var effectType) && effectType == "damage"), "Successful card play emits an effect log event");

var blueEnchantment = new CardEnchantment { CardInstanceId = "test_instance_blue", Color = ColorType.Blue };
var blueAction = cardPlayService.PlayCard(
    CreatePlayableCombat([strike.Id], actionPoints: 1),
    strike,
    "enemy_01",
    handIndex: null,
    enchantment: blueEnchantment);
Assert(blueAction.Succeeded, "Blue-enchanted action card can be played");
AssertEqual(ColorType.Blue, blueAction.Combat.ColorEnergy.Slots.Single().Color, "Blue-enchanted action generates blue color energy");
AssertEqual(3, blueAction.Combat.PlayerBlock, "Blue action enhancement grants block from final damage");
AssertEqual(ColorType.Blue, blueAction.Preview?.EnchantmentColor, "Action preview exposes enchantment color");
AssertEqual(ColorType.Blue, blueAction.Preview?.GeneratedColorEnergyColor, "Action preview exposes generated blue energy");
Assert(blueAction.Preview?.ColorEffects.Any(effect => effect.Color == ColorType.Blue && effect.EffectType == "gain_block") == true, "Action preview lists blue enhancement");

var cappedEnergyAction = cardPlayService.PlayCard(
    CreatePlayableCombat(
        [strike.Id],
        actionPoints: 1,
        colorEnergy: ColorEnergyPool.Empty().Add(ColorType.Blue, 5)),
    strike,
    "enemy_01",
    handIndex: null,
    enchantment: blueEnchantment);
Assert(cappedEnergyAction.Succeeded, "Action can be played while color energy pool has one empty slot");
AssertEqual(ColorEnergyPool.DefaultCapacity, cappedEnergyAction.Combat.ColorEnergy.Count, "Color energy pool caps at six slots");

var duplicateSlotResult = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id, guardAction.Id, strike.Id], actionPoints: 1), strike, "enemy_01", handIndex: 2);
Assert(duplicateSlotResult.Succeeded, "Duplicate card can be played from a specific hand slot");
AssertEqual(strike.Id, duplicateSlotResult.Combat.DeckZones.Hand[0], "Playing duplicate slot keeps earlier same-id card in hand");
AssertEqual(guardAction.Id, duplicateSlotResult.Combat.DeckZones.Hand[1], "Playing duplicate slot removes the clicked slot");
Assert(duplicateSlotResult.Events.Any(item =>
	item.EventType == CombatLogEventType.CardPlayed &&
	item.NumericChanges.TryGetValue("hand_index", out var handIndex) &&
	handIndex == 2), "Card play log records clicked hand slot for presentation animations");

var duplicateInstanceRun = runFactory.CreateNewRun("run_duplicate_instances", 13579, 60, 3, 2, [strike.Id, strike.Id], [encounter.Id]);
duplicateInstanceRun = runFactory.EnchantCard(duplicateInstanceRun, duplicateInstanceRun.MasterDeckInstances[0].InstanceId, ColorType.Blue);
var duplicateInstanceCombat = turnService.StartCombat(combatFactory.CreateCombat(
    combatId: "combat_duplicate_instances",
    runState: duplicateInstanceRun,
    encounter: encounter,
    enemiesById: enemiesById));
var unenchantedDuplicateInstance = duplicateInstanceRun.MasterDeckInstances[1];
var unenchantedDuplicateResult = cardPlayService.PlayCard(
    duplicateInstanceCombat,
    strike,
    "enemy_01",
    handIndex: 1,
    enchantment: unenchantedDuplicateInstance.Enchantment,
    cardInstanceId: unenchantedDuplicateInstance.InstanceId);
Assert(unenchantedDuplicateResult.Succeeded, "Unenchanted duplicate card instance can be played by instance id");
AssertEqual(ColorType.Colorless, unenchantedDuplicateResult.Combat.ColorEnergy.Slots.Single().Color, "Enchanting one duplicate does not color all same-definition instances");
AssertEqual(duplicateInstanceRun.MasterDeckInstances[0].InstanceId, unenchantedDuplicateResult.Combat.DeckZones.Hand.Single(), "Playing one duplicate instance leaves the other instance in hand");
AssertEqual(unenchantedDuplicateInstance.InstanceId, unenchantedDuplicateResult.Combat.DeckZones.DiscardPile.Single(), "Played duplicate instance enters discard by instance id");

var costFailure = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id], actionPoints: 0), strike, "enemy_01");
Assert(!costFailure.Succeeded, "Action card cannot be played without enough action points");
AssertEqual(PlayCardFailureReason.InsufficientActionPoints, costFailure.FailureReason, "Cost failure exposes UI-readable reason");
AssertEqual(1, costFailure.RequiredActionPoints, "Cost failure exposes required action points");
AssertEqual(0, costFailure.CurrentActionPoints, "Cost failure exposes current action points");
Assert(costFailure.Events.Any(item => item.EventType == CombatLogEventType.CardPlayRejected), "Cost failure emits a rejection log event");

var colorEnergyFailure = cardPlayService.PlayCard(
    CreatePlayableCombat([finisher.Id], actionPoints: 0, colorEnergy: ColorEnergyPool.Empty().Add(ColorType.Red, 2)),
    finisher,
    "enemy_01");
Assert(!colorEnergyFailure.Succeeded, "Finisher cannot be played without enough color energy");
AssertEqual(PlayCardFailureReason.InsufficientColorEnergy, colorEnergyFailure.FailureReason, "Color energy failure exposes UI-readable reason");
AssertEqual(3, colorEnergyFailure.RequiredColorEnergy, "Color energy failure exposes required energy");
AssertEqual(2, colorEnergyFailure.CurrentColorEnergy, "Color energy failure exposes current energy");

var missingTarget = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id], actionPoints: 1), strike);
Assert(!missingTarget.Succeeded, "Single-enemy card cannot be played without a target");
AssertEqual(PlayCardFailureReason.TargetMissing, missingTarget.FailureReason, "Target failure exposes UI-readable reason");
AssertEqual(TargetRule.SingleEnemy.ToString(), missingTarget.RequiredTargetRule, "Target failure exposes required target rule");

var supportActionResult = cardPlayService.PlayCard(CreatePlayableCombat([guardAction.Id], actionPoints: 0), guardAction);
Assert(supportActionResult.Succeeded, "Zero-cost support action can be played without action points");
AssertEqual(1, supportActionResult.Combat.ActionPoints, "Gain-action-points effect resolves during the current turn");
AssertEqual(5, supportActionResult.Combat.PlayerBlock, "Block effect increases player block");
AssertEqual(1, supportActionResult.Combat.ColorEnergy.Count, "Support action also follows action-card color energy generation");

var redBlueGreenEnergy = ColorEnergyPool.Empty()
    .Add(ColorType.Red, 1)
    .Add(ColorType.Blue, 1)
    .Add(ColorType.Green, 1);
var finisherResult = cardPlayService.PlayCard(
    CreatePlayableCombat([finisher.Id], actionPoints: 0, playerHp: 50, colorEnergy: redBlueGreenEnergy),
    finisher,
    "enemy_01");
Assert(finisherResult.Succeeded, "Finisher can be played when color energy cost is met");
AssertEqual(0, finisherResult.Combat.ActionPoints, "Finisher default does not consume action points");
AssertEqual(0, finisherResult.Combat.ColorEnergy.Count, "Finisher consumes spent color energy");
AssertEqual(3, finisherResult.Combat.Enemies[0].CurrentHp, "Red finisher enhancement increases damage");
AssertEqual(10, finisherResult.Combat.PlayerBlock, "Blue finisher enhancement grants block from final damage");
AssertEqual(60, finisherResult.Combat.PlayerHp, "Green finisher enhancement heals without exceeding max HP");
AssertSequenceEqual(new[] { ColorType.Red, ColorType.Blue, ColorType.Green }, finisherResult.Preview?.ConsumedColors ?? new List<ColorType>(), "Finisher preview exposes consumed colors in spend order");
Assert(finisherResult.Preview?.ColorEffects.Any(effect => effect.Color == ColorType.Red && effect.EffectType == "red_damage_bonus") == true, "Finisher preview lists red enhancement");

var allEnemiesResult = cardPlayService.PlayCard(
    CreatePlayableCombat(
        [arcSweepFinisher.Id],
        actionPoints: 0,
        colorEnergy: ColorEnergyPool.Empty().Add(ColorType.Colorless, 3),
        enemies:
        [
            CreateEnemyState("enemy_01", currentHp: 10, maxHp: 10),
            CreateEnemyState("enemy_02", currentHp: 12, maxHp: 12)
        ]),
    arcSweepFinisher);
Assert(allEnemiesResult.Succeeded, "All-enemies card can be played without a selected target");
AssertEqual(6, allEnemiesResult.Combat.Enemies[0].CurrentHp, "All-enemies damage hits the first enemy");
AssertEqual(8, allEnemiesResult.Combat.Enemies[1].CurrentHp, "All-enemies damage hits the second enemy");

var drawResult = cardPlayService.PlayCard(
    CreatePlayableCombat([drawAction.Id], actionPoints: 0, drawPile: [strike.Id]),
    drawAction);
Assert(drawResult.Succeeded, "Draw action can be played");
Assert(drawResult.Combat.DeckZones.Hand.Contains(strike.Id), "Draw effect adds drawn cards to hand");
Assert(drawResult.Events.Any(item => item.EventType == CombatLogEventType.CardsDrawn), "Draw action includes draw log events in the result");

var discountResult = cardPlayService.PlayCard(CreatePlayableCombat([discountAction.Id], actionPoints: 0), discountAction);
Assert(discountResult.Succeeded, "Temporary discount placeholder action can be played");
Assert(discountResult.Events.Any(item => item.Metadata.TryGetValue("effect_type", out var effectType) && effectType == "temporary_discount_placeholder"), "Temporary discount placeholder emits an effect log event");

var yellowEnchantment = new CardEnchantment { CardInstanceId = "test_instance_yellow", Color = ColorType.Yellow };
var yellowStrike = cardPlayService.PlayCard(
    CreatePlayableCombat([strike.Id], actionPoints: 1, enemies: [CreateEnemyState("enemy_01", currentHp: 24, maxHp: 24)]),
    strike,
    "enemy_01",
    handIndex: null,
    enchantment: yellowEnchantment);
Assert(yellowStrike.Succeeded, "Yellow-enchanted action card can be played");
AssertEqual(12, yellowStrike.Combat.Enemies[0].CurrentHp, "Yellow enhancement adds one extra damage release");
AssertEqual(1, yellowStrike.Preview?.EstimatedExtraCasts, "Yellow preview exposes capped extra cast count");

var yellowUtility = new CardDefinition
{
    Id = "card.yellow_utility_boundary",
    WeaponId = "weapon.mechanical_arm",
    Type = CardType.Action,
    Cost = 0,
    ColorEnergyGeneration = new ColorEnergyGeneration { Amount = 1, ColorSource = ColorEnergyColorSource.Enchantment },
    TargetRule = TargetRule.Self,
    Effects =
    [
        new EffectDefinition { Type = "draw_cards", Target = "self", Value = 1 },
        new EffectDefinition { Type = "gain_action_points", Target = "self", Value = 1 },
        new EffectDefinition { Type = "gain_resource", Target = "self", Value = 1 }
    ],
    Rarity = CardRarity.Common
};
var yellowUtilityResult = cardPlayService.PlayCard(
    CreatePlayableCombat([yellowUtility.Id], actionPoints: 0, drawPile: [strike.Id, finisher.Id]),
    yellowUtility,
    targetEnemyInstanceId: null,
    handIndex: null,
    enchantment: yellowEnchantment);
Assert(yellowUtilityResult.Succeeded, "Yellow utility action can be played");
AssertEqual(1, yellowUtilityResult.Combat.DeckZones.HandCount, "Yellow extra release does not repeat draw effects");
AssertEqual(1, yellowUtilityResult.Combat.ActionPoints, "Yellow extra release does not repeat action point gain");
AssertEqual(1, yellowUtilityResult.Combat.ColorEnergy.Count, "Yellow extra release does not repeat energy generation");
AssertEqual(1, yellowUtilityResult.Events.Count(item => item.Metadata.TryGetValue("effect_type", out var effectType) && effectType == "gain_action_points"), "Yellow only resolves AP gain once");

var greenCapResult = cardPlayService.PlayCard(
    CreatePlayableCombat(
        [finisher.Id],
        actionPoints: 0,
        playerHp: 59,
        enemies: [CreateEnemyState("enemy_01", currentHp: 100, maxHp: 100)],
        colorEnergy: ColorEnergyPool.Empty().Add(ColorType.Green, 3)),
    finisher,
    "enemy_01");
Assert(greenCapResult.Succeeded, "Green finisher can be played");
AssertEqual(60, greenCapResult.Combat.PlayerHp, "Green healing cannot exceed max HP");

var purpleResult = cardPlayService.PlayCard(
    CreatePlayableCombat(
        [finisher.Id],
        actionPoints: 0,
        enemies: [CreateEnemyState("enemy_01", currentHp: 100, maxHp: 100)],
        colorEnergy: ColorEnergyPool.Empty().Add(ColorType.Purple, 4)),
    finisher,
    "enemy_01");
Assert(purpleResult.Succeeded, "Purple finisher can be played");
AssertEqual(64, purpleResult.Combat.Enemies[0].CurrentHp, "Purple enhancement doubles once and does not loop infinitely");
AssertEqual(1, purpleResult.Combat.ColorEnergy.Count, "Fixed-cost finisher leaves unspent color energy in the pool");
Assert(purpleResult.Events.Count < 20, "Purple enhancement emits a bounded number of log events");

var sequenceCombat = CreateEnemyTurnCombat(
    enemies: [CreateEnemyState("enemy_01", currentHp: 20, maxHp: 20, enemyId: sequenceEnemy.Id)],
    playerHp: 30);
var sequenceViewBefore = turnService.GetEnemyIntentViews(sequenceCombat, enemiesById);
AssertEqual("intent.attack", sequenceViewBefore[0].IntentId, "Fixed intent sequence starts at the first intent");
var sequenceAfterAttack = turnService.ResolveEnemyTurn(sequenceCombat, enemiesById);
AssertEqual(24, sequenceAfterAttack.PlayerHp, "Enemy attack damages player HP when block is absent");
AssertEqual(1, sequenceAfterAttack.Enemies[0].IntentIndex, "Enemy attack advances to the next fixed intent");
var sequenceViewAfterAttack = turnService.GetEnemyIntentViews(sequenceAfterAttack, enemiesById);
AssertEqual("intent.guard", sequenceViewAfterAttack[0].IntentId, "Fixed intent sequence exposes the next intent after resolution");
var sequenceAfterGuard = turnService.ResolveEnemyTurn(sequenceAfterAttack, enemiesById);
AssertEqual(4, sequenceAfterGuard.Enemies[0].Block, "Enemy defend intent grants enemy block");
AssertEqual(2, sequenceAfterGuard.Enemies[0].IntentIndex, "Enemy defend intent advances sequence again");

var partialDeathResult = cardPlayService.PlayCard(
    CreatePlayableCombat(
        [strike.Id],
        actionPoints: 1,
        enemies:
        [
            CreateEnemyState("enemy_01", currentHp: 6, maxHp: 24),
            CreateEnemyState("enemy_02", currentHp: 10, maxHp: 24)
        ]),
    strike,
    "enemy_01");
Assert(partialDeathResult.Succeeded, "Damage card can kill one enemy");
AssertEqual(0, partialDeathResult.Combat.Enemies[0].CurrentHp, "Enemy HP reaches zero when killed");
AssertEqual(CombatStatus.PlayerTurn, partialDeathResult.Combat.Status, "Combat continues when enemies remain alive");
Assert(partialDeathResult.Events.Any(item => item.EventType == CombatLogEventType.EnemyDied), "Enemy death emits a structured log event");

var victoryResult = cardPlayService.PlayCard(
    CreatePlayableCombat([strike.Id], actionPoints: 1, enemies: [CreateEnemyState("enemy_01", currentHp: 6, maxHp: 24)]),
    strike,
    "enemy_01");
Assert(victoryResult.Succeeded, "Damage card can end combat");
AssertEqual(CombatStatus.Victory, victoryResult.Combat.Status, "Combat enters victory when all enemies die");
Assert(victoryResult.Events.Any(item => item.EventType == CombatLogEventType.CombatEnded && item.Metadata["status"] == CombatStatus.Victory.ToString()), "Victory emits combat-ended log event");

var lethalEnemyTurn = CreateEnemyTurnCombat(enemies: [CreateEnemyState("enemy_01")], playerHp: 5);
var defeatedCombat = turnService.ResolveEnemyTurn(lethalEnemyTurn, enemiesById);
AssertEqual(CombatStatus.Defeat, defeatedCombat.Status, "Combat enters defeat when player HP reaches zero");
AssertEqual(0, defeatedCombat.PlayerHp, "Enemy lethal attack clamps player HP to zero");
Assert(defeatedCombat.Log.Any(item => item.EventType == CombatLogEventType.CombatEnded && item.Metadata["status"] == CombatStatus.Defeat.ToString()), "Defeat emits combat-ended log event");

var runProgressService = new RunProgressService();
var failedRun = runProgressService.ApplyCombatResult(run, defeatedCombat);
AssertEqual(RunStatus.Failed, failedRun.Status, "Run fails when combat ends in defeat");
AssertEqual(0, failedRun.PlayerHp, "Run failure stores zero player HP");
AssertEqual(run.CurrentEncounterIndex, failedRun.CurrentEncounterIndex, "Run failure does not advance to a retry node");

var rewardService = new RewardService();
var generatedShard = rewardService.GenerateColorShard(_ => 1);
AssertEqual(ColorType.Yellow, generatedShard, "Reward service can generate a random yellow color shard");

var cardsById = new Dictionary<string, CardDefinition>
{
    [strike.Id] = strike,
    [finisher.Id] = finisher,
    [guardAction.Id] = guardAction,
    [drawAction.Id] = drawAction,
    [discountAction.Id] = discountAction,
    [arcSweepFinisher.Id] = arcSweepFinisher,
    [refundFinisher.Id] = refundFinisher
};

var shardRun = rewardService.AddPendingColorShard(run, ColorType.Yellow);
AssertEqual(1, shardRun.PendingColorShards.Count, "Generated color shard enters RunState as a pending shard");
var enchantableCards = rewardService.ListEnchantableActionCards(shardRun, cardsById);
AssertEqual(4, enchantableCards.Count, "Only unenchanted action card instances are listed for color shards");
Assert(enchantableCards.All(instance => cardsById[instance.DefinitionId].Type == CardType.Action), "Finisher instances are not listed as enchantable");

var yellowEnchantedRun = rewardService.ApplyColorShard(shardRun, ColorType.Yellow, enchantableCards[0].InstanceId, cardsById);
AssertEqual(0, yellowEnchantedRun.PendingColorShards.Count, "Applying a color shard consumes the pending shard");
AssertEqual(ColorType.Yellow, yellowEnchantedRun.MasterDeckInstances[0].Enchantment?.Color, "Color shard is recorded on the selected CardInstance");
AssertEqual(1, yellowEnchantedRun.CardEnchantments.Count, "RunState exposes the applied card enchantment");
var yellowGeneratedColor = strike.ColorEnergyGeneration?.ResolveColor(yellowEnchantedRun.MasterDeckInstances[0].Enchantment);
AssertEqual(ColorType.Yellow, yellowGeneratedColor, "Enchanted action card generates matching color energy");

var alreadyEnchantedRun = rewardService.AddPendingColorShard(yellowEnchantedRun, ColorType.Blue);
AssertThrows(
    () => rewardService.ApplyColorShard(alreadyEnchantedRun, ColorType.Blue, alreadyEnchantedRun.MasterDeckInstances[0].InstanceId, cardsById),
    "Already enchanted action cards cannot receive a normal color shard again");
AssertThrows(
    () => rewardService.ApplyColorShard(alreadyEnchantedRun, ColorType.Blue, alreadyEnchantedRun.MasterDeckInstances[^1].InstanceId, cardsById),
    "Finishers cannot be enchanted by color shards");

var weaponRewardPools = new[]
{
    new WeaponRewardPoolDefinition
    {
        WeaponId = "weapon.revolver_sword",
        CardIdsByRarity = new Dictionary<CardRarity, List<string>>
        {
            [CardRarity.Common] = [strike.Id],
            [CardRarity.Uncommon] = [drawAction.Id],
            [CardRarity.Rare] = [finisher.Id]
        }
    },
    new WeaponRewardPoolDefinition
    {
        WeaponId = "weapon.mechanical_arm",
        CardIdsByRarity = new Dictionary<CardRarity, List<string>>
        {
            [CardRarity.Common] = [guardAction.Id],
            [CardRarity.Uncommon] = [discountAction.Id],
            [CardRarity.Rare] = [arcSweepFinisher.Id]
        }
    }
};
var weaponCandidates = rewardService.GenerateWeaponCardCandidates(run, weaponRewardPools, cardsById, _ => 0);
AssertEqual(3, weaponCandidates.Count, "Weapon card reward generates three candidates");
AssertEqual(3, weaponCandidates.Distinct().Count(), "Weapon card candidates are distinct within one offer");
Assert(weaponCandidates.All(cardId => cardsById[cardId].Type == CardType.Action), "Weapon card reward temporarily excludes finisher candidates");
var rareRollWeaponCandidates = rewardService.GenerateWeaponCardCandidates(run, weaponRewardPools, cardsById, _ => 99);
Assert(rareRollWeaponCandidates.All(cardId => cardsById[cardId].Type == CardType.Action), "Weapon card reward filters finishers even when rarity rolls hit finisher-only buckets");
AssertThrows(
    () => rewardService.ClaimWeaponCardChoice(run, weaponCandidates, []),
    "Weapon card reward cannot be skipped");
AssertThrows(
    () => rewardService.ClaimWeaponCardChoice(run, weaponCandidates, [weaponCandidates[0], weaponCandidates[1]]),
    "Weapon card reward cannot pick multiple cards");
var oneCardRewardRun = rewardService.ClaimWeaponCardChoice(run, weaponCandidates, [weaponCandidates[0]]);
AssertEqual(run.MasterDeck.Count + 1, oneCardRewardRun.MasterDeck.Count, "Weapon card reward adds exactly one card");
AssertEqual(run.MasterDeckInstances.Count + 1, oneCardRewardRun.MasterDeckInstances.Count, "Reward card is added as a new CardInstance");
AssertEqual(weaponCandidates[0], oneCardRewardRun.MasterDeckInstances[^1].DefinitionId, "Reward CardInstance points to the selected card definition");
var duplicateRewardRun = rewardService.ClaimWeaponCardChoice(run, [strike.Id, guardAction.Id, finisher.Id], [strike.Id]);
AssertEqual(5, duplicateRewardRun.MasterDeck.Count(cardId => cardId == strike.Id), "Same-name weapon card rewards add a new deck copy");
AssertEqual(5, duplicateRewardRun.MasterDeckInstances.Count(instance => instance.DefinitionId == strike.Id), "Same-name reward cards coexist as separate instances");

var mvpRegressionCardsById = new Dictionary<string, CardDefinition>
{
    ["card.revolver_slash"] = strike with { Id = "card.revolver_slash" },
    ["card.revolver_hundred_blades"] = strike with { Id = "card.revolver_hundred_blades" },
    ["card.revolver_planetary_poem"] = finisher with { Id = "card.revolver_planetary_poem" },
    ["card.arm_guard"] = guardAction with { Id = "card.arm_guard" },
    ["card.arm_counter"] = guardAction with { Id = "card.arm_counter", WeaponId = "weapon.mechanical_arm" },
    ["card.arm_sweep"] = finisher with { Id = "card.arm_sweep", WeaponId = "weapon.mechanical_arm" }
};
var mvpSelectedRun = revolverMainRun with
{
    EncounterSequence = [encounter.Id, eliteEncounter.Id, bossEncounter.Id],
    CurrentEncounterIndex = 0
};
var mvpOpeningCombat = turnService.StartCombat(combatFactory.CreateCombat(
    "combat_mvp_regression_01",
    mvpSelectedRun,
    encounter,
    enemiesById));
AssertEqual(CombatStatus.PlayerTurn, mvpOpeningCombat.Status, "Full MVP regression can enter combat after weapon starting deck selection");
AssertEqual(6, mvpSelectedRun.MasterDeckInstances.Count, "Full MVP regression starts from 6 temporary action-only weapon-selected CardInstances");

var mvpPostCombatRun = runProgressService.ApplyCombatResult(
    mvpSelectedRun,
    CreateCombatResult(encounter.Id, CombatStatus.Victory, playerHp: 42),
    encounter);
string[] mvpRewardCandidates = ["card.revolver_slash", "card.arm_guard", "card.revolver_hundred_blades"];
var mvpRewardedRun = rewardService.ClaimWeaponCardChoice(mvpPostCombatRun, mvpRewardCandidates, ["card.arm_guard"]);
AssertEqual(0, mvpRewardedRun.PendingColorShards.Count, "Full MVP regression skips post-combat enchantment rewards");
AssertEqual(mvpSelectedRun.MasterDeckInstances.Count + 1, mvpRewardedRun.MasterDeckInstances.Count, "Full MVP regression adds one weapon reward card instance");
var mvpNextRun = runProgressService.AdvanceAfterRewards(mvpRewardedRun, encounter);
AssertEqual(1, mvpNextRun.CurrentEncounterIndex, "Full MVP regression advances to the next combat after rewards");
var mvpNextCombat = turnService.StartCombat(combatFactory.CreateCombat(
    "combat_mvp_regression_02",
    mvpNextRun,
    eliteEncounter,
    enemiesById));
AssertEqual(CombatStatus.PlayerTurn, mvpNextCombat.Status, "Full MVP regression can enter the next combat");

var eliteRelicRun = rewardService.GrantEncounterRelic(run, eliteEncounter, relicsById);
Assert(eliteRelicRun.RelicIds.Contains(relic.Id), "Elite encounter grants its fixed relic");
var duplicateRelicRun = rewardService.GrantEncounterRelic(eliteRelicRun, eliteEncounter, relicsById);
AssertEqual(1, duplicateRelicRun.RelicIds.Count(cardId => cardId == relic.Id), "Fixed unique relic is not duplicated if granted again");

var victoriousCombat = CreateCombatResult(encounter.Id, CombatStatus.Victory, playerHp: 27);
var postNormalCombatRun = runProgressService.ApplyCombatResult(run, victoriousCombat, encounter);
AssertEqual(RunStatus.InProgress, postNormalCombatRun.Status, "Normal combat victory keeps run in progress before advancement");
var advancedRun = runProgressService.AdvanceAfterRewards(postNormalCombatRun, encounter);
AssertEqual(1, advancedRun.CurrentEncounterIndex, "Run advances linearly after rewards are resolved");
AssertEqual(run.PlayerMaxHp, advancedRun.PlayerHp, "Run heals to full before the next encounter");

var preparedRun = runProgressService.PrepareForCombat(advancedRun with { PlayerHp = 3 });
AssertEqual(run.PlayerMaxHp, preparedRun.PlayerHp, "PrepareForCombat heals the player to full HP");

var bossVictoryCombat = CreateCombatResult(bossEncounter.Id, CombatStatus.Victory, playerHp: 14);
var clearedRun = runProgressService.ApplyCombatResult(advancedRun with { CurrentEncounterIndex = 2 }, bossVictoryCombat, bossEncounter);
AssertEqual(RunStatus.Cleared, clearedRun.Status, "Boss victory clears the MVP run");
AssertEqual(14, clearedRun.PlayerHp, "Boss clear keeps remaining combat HP on the run result");

var debugRunService = new DebugRunService();
var debugStart = debugRunService.StartDebugEncounter(
    new DebugRunDefaults
    {
        PlayerMaxHp = 60,
        BaseActionPoints = 3,
        CardsPerTurn = 5,
        StarterDeck = [strike.Id, finisher.Id],
        EncounterSequence = [encounter.Id, eliteEncounter.Id, bossEncounter.Id]
    },
    new DebugRunConfiguration
    {
        RunId = "debug_run_test",
        Seed = 98765,
        EncounterId = eliteEncounter.Id,
        StarterDeckOverride = [finisher.Id],
        AdditionalCardIds = [strike.Id],
        MainHandWeaponId = "weapon.revolver_sword",
        OffHandWeaponId = "weapon.mechanical_arm",
        ColorShardPreviewRoll = 2,
        WeaponCardPreviewIds = [strike.Id, guardAction.Id, finisher.Id]
    },
    new Dictionary<string, EncounterDefinition>
    {
        [encounter.Id] = encounter,
        [eliteEncounter.Id] = eliteEncounter,
        [bossEncounter.Id] = bossEncounter
    },
    enemiesById,
    cardsById,
    weaponRewardPools);
AssertEqual(98765, debugStart.RunState.Seed, "Debug run entry preserves requested seed");
AssertEqual(eliteEncounter.Id, debugStart.Encounter.Id, "Debug run entry can enter a selected fixed encounter directly");
AssertEqual(1, debugStart.RunState.CurrentEncounterIndex, "Debug run entry sets current encounter index for selected encounter");
AssertEqual(2, debugStart.RunState.MasterDeck.Count, "Debug run entry supports starter deck override plus added cards");
AssertEqual(strike.Id, debugStart.RunState.MasterDeck[^1], "Debug run entry appends requested card ids");
AssertEqual(ColorType.Blue, debugStart.ColorShardPreview, "Debug run entry previews a color shard");
AssertEqual(3, debugStart.WeaponCardPreviews.Count, "Debug run entry previews three weapon card candidates");

var metricsCombat = CreateCombatResult(encounter.Id, CombatStatus.Victory, playerHp: 51) with
{
    CombatId = "combat_metrics",
    TurnNumber = 2,
    Log =
    [
        new CombatLogEvent
        {
            EventId = "metrics_action_played",
            EventType = CombatLogEventType.CardPlayed,
            TurnNumber = 1,
            SourceId = strike.Id,
            NumericChanges = new Dictionary<string, int> { ["color_energy_before"] = 0, ["color_energy_after"] = 1 },
            Metadata = new Dictionary<string, string> { ["card_type"] = CardType.Action.ToString(), ["enchantment_color"] = ColorType.Blue.ToString() }
        },
        new CombatLogEvent
        {
            EventId = "metrics_color_energy_generated",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = 1,
            SourceId = strike.Id,
            NumericChanges = new Dictionary<string, int> { ["color_energy_generated"] = 1, ["color_energy_after"] = 1 },
            Metadata = new Dictionary<string, string> { ["effect_type"] = "gain_color_energy", ["color"] = ColorType.Blue.ToString() }
        },
        new CombatLogEvent
        {
            EventId = "metrics_finisher_played",
            EventType = CombatLogEventType.CardPlayed,
            TurnNumber = 2,
            SourceId = finisher.Id,
            NumericChanges = new Dictionary<string, int> { ["color_energy_before"] = 3, ["color_energy_spent"] = 3 },
            Metadata = new Dictionary<string, string> { ["card_type"] = CardType.Finisher.ToString(), ["spent_colors"] = "Red,Blue,Green" }
        },
        new CombatLogEvent
        {
            EventId = "metrics_enemy_damage",
            EventType = CombatLogEventType.EnemyIntentResolved,
            TurnNumber = 2,
            SourceId = "enemy_01",
            TargetIds = ["player"],
            NumericChanges = new Dictionary<string, int> { ["hp_damage"] = 4 }
        }
    ]
};
var metricsService = new PlaytestMetricsService();
var metricsReport = metricsService.BuildReport(
    clearedRun,
    [encounter.Id, eliteEncounter.Id, bossEncounter.Id],
    [metricsCombat],
    [
        new PlaytestRewardChoiceMetric
        {
            NodeOrder = 1,
            EncounterId = encounter.Id,
            ColorShard = ColorType.Blue.ToString(),
            EnchantedCardInstanceId = run.MasterDeckInstances[0].InstanceId,
            EnchantedCardDefinitionId = strike.Id,
            WeaponCardCandidateIds = [strike.Id, guardAction.Id, finisher.Id],
            SelectedWeaponCardId = guardAction.Id
        }
    ],
    [
        new PlaytestRelicGrantMetric
        {
            NodeOrder = 2,
            EncounterId = eliteEncounter.Id,
            RelicId = relic.Id
        }
    ],
    DateTimeOffset.UnixEpoch,
    DateTimeOffset.UnixEpoch.AddSeconds(90));
AssertEqual(run.Seed, metricsReport.RunSeed, "Metrics report stores run seed");
AssertEqual(3, metricsReport.NodeOrder.Count, "Metrics report stores node order");
AssertEqual(2, metricsReport.Combats[0].TurnCount, "Metrics report stores combat turn count");
AssertEqual(4, metricsReport.Combats[0].DamageTaken, "Metrics report sums damage taken");
AssertEqual(3, metricsReport.Combats[0].PeakColorEnergy, "Metrics report stores peak color energy");
AssertEqual(1, metricsReport.Combats[0].ColorEnergyGeneratedByColor[ColorType.Blue.ToString()], "Metrics report stores generated color composition");
AssertEqual(1, metricsReport.Combats[0].FinisherColorSpendByColor[ColorType.Red.ToString()], "Metrics report stores finisher red spend");
AssertEqual(1, metricsReport.Combats[0].EnchantedActionUseCount, "Metrics report counts enchanted action usage");
AssertEqual(1, metricsReport.Combats[0].FinisherUseCount, "Metrics report counts finisher use");
AssertEqual(1.0, metricsReport.EnchantmentUsageRate, "Metrics report stores enchantment use rate");
AssertEqual(2, metricsReport.BuildRoutes.BlueGreenHeavyCannonSignals, "Metrics report stores blue-green route signals");
AssertEqual(guardAction.Id, metricsReport.Rewards[0].SelectedWeaponCardId, "Metrics report stores selected weapon card");
AssertEqual(relic.Id, metricsReport.Relics[0].RelicId, "Metrics report stores relic grants");
AssertEqual(90, (int)metricsReport.TotalDurationSeconds, "Metrics report stores total duration");

var exportService = new DebugExportService();
var exportDirectory = Path.Combine(Path.GetTempPath(), "RoguelikeCardGameDebugTests", Guid.NewGuid().ToString("N"));
var combatLogPath = exportService.ExportCombatLog(metricsCombat, exportDirectory, "combat_log.json");
var metricsPath = exportService.ExportMetrics(metricsReport, exportDirectory, "metrics.json");
Assert(File.Exists(combatLogPath), "Debug export writes combat log JSON");
Assert(File.Exists(metricsPath), "Debug export writes metrics JSON");
Assert(File.ReadAllText(metricsPath).Contains("run_seed"), "Exported metrics use snake_case fields");

var serializedBundle = JsonSerializer.Serialize(new
{
    Cards = new[] { strike, finisher, guardAction, drawAction, discountAction, arcSweepFinisher, refundFinisher },
    Relics = new[] { relic },
    Enemies = new[] { enemy, sequenceEnemy },
    Encounters = new[] { encounter, eliteEncounter, bossEncounter },
    Run = run,
    Combat = combat
}, options);

if (string.IsNullOrWhiteSpace(serializedBundle))
{
    throw new InvalidOperationException("Serialized model bundle should not be empty.");
}

Console.WriteLine("Domain model smoke tests passed.");

static void AssertSequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException($"Assertion failed: {message}.");
    }
}

static void AssertThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch (InvalidOperationException)
    {
        return;
    }

    throw new InvalidOperationException($"Assertion failed: {message}.");
}

static CombatState CreatePlayableCombat(
    IEnumerable<string> hand,
    int actionPoints = 3,
    int playerHp = 60,
    int playerBlock = 0,
    ColorEnergyPool? colorEnergy = null,
    IEnumerable<string>? drawPile = null,
    IEnumerable<string>? discardPile = null,
    List<CombatEnemyState>? enemies = null)
{
    return new CombatState
    {
        CombatId = "combat_card_play",
        EncounterId = "encounter_card_play",
        Status = CombatStatus.PlayerTurn,
        TurnNumber = 1,
        PlayerMaxHp = 60,
        PlayerHp = playerHp,
        PlayerBlock = playerBlock,
        BaseActionPoints = 3,
        CardsPerTurn = 5,
        ActionPoints = actionPoints,
        ColorEnergy = colorEnergy ?? ColorEnergyPool.Empty(),
        DeckZones = new DeckZones
        {
            DrawPile = drawPile?.ToList() ?? [],
            Hand = hand.ToList(),
            DiscardPile = discardPile?.ToList() ?? []
        },
        Enemies = enemies ?? [CreateEnemyState("enemy_01")]
    };
}

static CombatState CreateEnemyTurnCombat(
    List<CombatEnemyState> enemies,
    int playerHp = 60,
    int playerBlock = 0,
    int turnNumber = 1)
{
    return new CombatState
    {
        CombatId = "combat_enemy_turn",
        EncounterId = "encounter_enemy_turn",
        Status = CombatStatus.EnemyTurn,
        TurnNumber = turnNumber,
        PlayerMaxHp = 60,
        PlayerHp = playerHp,
        PlayerBlock = playerBlock,
        BaseActionPoints = 3,
        CardsPerTurn = 5,
        ActionPoints = 0,
        DeckZones = new DeckZones(),
        Enemies = enemies
    };
}

static CombatState CreateCombatResult(string encounterId, CombatStatus status, int playerHp)
{
    return new CombatState
    {
        CombatId = $"combat_result_{encounterId}",
        EncounterId = encounterId,
        Status = status,
        TurnNumber = 1,
        PlayerMaxHp = 60,
        PlayerHp = playerHp,
        BaseActionPoints = 3,
        CardsPerTurn = 5,
        Enemies = [CreateEnemyState("enemy_01", currentHp: status == CombatStatus.Victory ? 0 : 24)]
    };
}

static CombatEnemyState CreateEnemyState(
    string instanceId,
    int currentHp = 24,
    int maxHp = 24,
    int block = 0,
    string enemyId = "enemy.training_dummy",
    int intentIndex = 0)
{
    return new CombatEnemyState
    {
        InstanceId = instanceId,
        EnemyId = enemyId,
        MaxHp = maxHp,
        CurrentHp = currentHp,
        Block = block,
        IntentIndex = intentIndex
    };
}

static BeatRoundState CreateBeatTargetRound(
    int beatCount,
    IEnumerable<int> enemyBeatIndexes,
    IEnumerable<PlayerBeatSlot> playerBeats)
{
    return new BeatRoundState
    {
        BeatCount = beatCount,
        PlayerBeats = playerBeats.ToList(),
        EnemyBeats = enemyBeatIndexes
            .Select(index => new EnemyBeatSlot
            {
                EnemyInstanceId = "enemy_01",
                BeatIndex = index,
                ActionCardId = $"enemy_card.beat_{index}"
            })
            .ToList()
    };
}

static PlayerBeatSlot CreatePlayerBeat(int beatIndex, string? cardId, BeatTarget? target)
{
    return new PlayerBeatSlot
    {
        BeatIndex = beatIndex,
        CardId = cardId,
        Target = target
    };
}

static BeatTarget CreateEnemyBeatTarget(int beatIndex)
{
    return new BeatTarget
    {
        Kind = BeatTargetKind.EnemyBeat,
        EnemyInstanceId = "enemy_01",
        EnemyBeatIndex = beatIndex
    };
}

static BeatTarget CreateEnemyBodyTarget()
{
    return new BeatTarget
    {
        Kind = BeatTargetKind.EnemyBody,
        EnemyInstanceId = "enemy_01"
    };
}

static string FindGameDataRoot()
{
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
         directory is not null;
         directory = directory.Parent)
    {
        var repoStyle = Path.Combine(directory.FullName, "game", "data");
        if (Directory.Exists(repoStyle))
        {
            return repoStyle;
        }

        var gameStyle = Path.Combine(directory.FullName, "data");
        if (Directory.Exists(gameStyle) && Directory.Exists(Path.Combine(gameStyle, "gameplay")))
        {
            return gameStyle;
        }
    }

    throw new DirectoryNotFoundException("Could not find game/data from the test working directory.");
}

static (int Width, int Height) PngSize(string path)
{
    var bytes = File.ReadAllBytes(path);
    if (bytes.Length < 24 ||
        bytes[0] != 0x89 ||
        bytes[1] != 0x50 ||
        bytes[2] != 0x4E ||
        bytes[3] != 0x47)
    {
        throw new InvalidOperationException($"Not a PNG file: {path}");
    }

    var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
    var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
    return (width, height);
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}. Expected: {expected}; Actual: {actual}");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class BeatRoundFactoryForTest
{
    public BeatRoundState CreateEmptyRound(string enemyInstanceId)
    {
        return new BeatRoundState
        {
            BeatCount = 3,
            PlayerBeats =
            [
                new PlayerBeatSlot { BeatIndex = 0 },
                new PlayerBeatSlot { BeatIndex = 1 },
                new PlayerBeatSlot { BeatIndex = 2 }
            ],
            EnemyBeats =
            [
                new EnemyBeatSlot
                {
                    EnemyInstanceId = enemyInstanceId,
                    BeatIndex = 0,
                    ActionCardId = "enemy_card.test_slash",
                    Actions =
                    [
                        new BeatActionDefinition { Kind = BeatActionKind.Attack, AttackType = BeatAttackType.Slash, Value = 4 }
                    ]
                },
                new EnemyBeatSlot
                {
                    EnemyInstanceId = enemyInstanceId,
                    BeatIndex = 1,
                    ActionCardId = "enemy_card.test_guard",
                    Actions =
                    [
                        new BeatActionDefinition { Kind = BeatActionKind.Block, Value = 3 }
                    ]
                }
            ]
        };
    }
}
