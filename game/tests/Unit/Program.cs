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
    [finisher.Id] = finisher
};
var beatEnemies = new Dictionary<string, EnemyDefinition>
{
    [beatEnemy.Id] = beatEnemy
};
var resolvedBeatRound = beatService.ResolveBeatRound(beatCombat, beatCards, beatEnemies);
AssertEqual(1, resolvedBeatRound.Combat.ColorEnergy.Count, "Successful unopposed attack generates one colorless energy");
AssertEqual(21, resolvedBeatRound.Combat.Enemies[0].CurrentHp, "Weakness-adjusted beat damage is applied to enemy HP");
Assert(resolvedBeatRound.Events.Any(item => item.EventType == CombatLogEventType.BeatEnergyGenerated), "Beat energy generation is logged");

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
        "card.revolver_slash",
        "card.revolver_double_slash",
        "card.revolver_heavy_cannon",
        "card.revolver_bullet_storm"
    ]
};
var armStartingPool = new WeaponStartingPoolDefinition
{
    WeaponId = "weapon.mechanical_arm",
    CardIds =
    [
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_crush",
        "card.arm_counter",
        "card.arm_army_sweep"
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
        "card.revolver_slash",
        "card.revolver_double_slash",
        "card.revolver_heavy_cannon",
        "card.revolver_bullet_storm"
    ],
    OffHandCardIds =
    [
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_crush"
    ]
};
var revolverMainValidation = startingDeckSelectionService.Validate(revolverMainSelection, startingPools);
Assert(revolverMainValidation.IsValid, "Revolver sword main-hand 6 plus mechanical arm off-hand 4 is a valid start");
AssertEqual(10, revolverMainValidation.SelectedCardIds.Count, "Starting deck selection creates a 10-card starter deck");
var automaticRevolverMain = startingDeckSelectionService.BuildAutomaticStarterDeck(
    revolverStartingPool.WeaponId,
    armStartingPool.WeaponId,
    startingPools,
    new Dictionary<string, CardDefinition>
    {
        ["card.revolver_slash"] = strike with { Id = "card.revolver_slash" },
        ["card.revolver_double_slash"] = strike with { Id = "card.revolver_double_slash" },
        ["card.revolver_heavy_cannon"] = finisher with { Id = "card.revolver_heavy_cannon" },
        ["card.revolver_bullet_storm"] = finisher with { Id = "card.revolver_bullet_storm" },
        ["card.arm_guard"] = guardAction with { Id = "card.arm_guard" },
        ["card.arm_crush"] = strike with { Id = "card.arm_crush", WeaponId = "weapon.mechanical_arm" },
        ["card.arm_counter"] = finisher with { Id = "card.arm_counter", WeaponId = "weapon.mechanical_arm" },
        ["card.arm_army_sweep"] = finisher with { Id = "card.arm_army_sweep", WeaponId = "weapon.mechanical_arm" }
    });
Assert(automaticRevolverMain.IsValid, "Automatic starter deck accepts main-hand all cards and off-hand actions");
AssertSequenceEqual(revolverMainValidation.SelectedCardIds, automaticRevolverMain.SelectedCardIds, "Automatic starter deck matches the fixed 6 plus 4 action rule");
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
AssertEqual(10, revolverMainRun.MasterDeckInstances.Count, "Weapon-selected run creates card instances for all starter cards");

var armMainSelection = new StartingDeckSelection
{
    MainHandWeaponId = armStartingPool.WeaponId,
    OffHandWeaponId = revolverStartingPool.WeaponId,
    MainHandCardIds =
    [
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_guard",
        "card.arm_crush",
        "card.arm_counter",
        "card.arm_army_sweep"
    ],
    OffHandCardIds =
    [
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_double_slash",
    ]
};
var armMainValidation = startingDeckSelectionService.Validate(armMainSelection, startingPools);
Assert(armMainValidation.IsValid, "Mechanical arm main-hand 6 plus revolver sword off-hand 4 is a valid start");
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
AssertEqual(10, armMainRun.MasterDeckInstances.Count, "Automatic-compatible arm main run also creates 10 starter card instances");

var loadedContent = GameContent.LoadFromDataRoot(FindGameDataRoot());
AssertEqual(5, loadedContent.ColorsById.Count, "GameContent loads all MVP colors");
AssertEqual("tempo_conversion", loadedContent.ColorsById["color.yellow"].Role, "GameContent maps color roles");
AssertEqual("extra_casts", loadedContent.ColorsById["color.yellow"].BaseEffectTemplate.Op, "GameContent maps color effect templates");
AssertEqual(1, loadedContent.ColorsById["color.yellow"].StackRule.MaxPerCard, "GameContent maps color stack rules");
AssertEqual(2, loadedContent.WeaponsById.Count, "GameContent loads MVP weapons");
AssertEqual("card_pool.starting.revolver_sword", loadedContent.WeaponsById["weapon.revolver_sword"].StartingPoolId, "GameContent maps weapon starting pools");
AssertEqual(20, loadedContent.CardsById.Count, "GameContent loads the 20-card MVP weapon card set");
var loadedActionCard = loadedContent.CardsById["card.revolver_slash"];
AssertEqual(CardType.Action, loadedActionCard.Type, "GameContent maps card type");
AssertEqual("action_point", loadedActionCard.Costs[0].Resource, "GameContent maps structured card costs");
Assert(loadedActionCard.Targeting.Required, "GameContent maps targeting.required");
AssertEqual(5, loadedActionCard.ColorInteractions.Enchantment.SelfEffects.Count, "GameContent maps action enchantment color effects");
AssertEqual("move_card", loadedActionCard.AfterPlay[0].Type, "GameContent maps after_play operations");
AssertEqual("basic_single_target_generator", loadedActionCard.Balance.Role, "GameContent maps card balance metadata");
var loadedFinisher = loadedContent.CardsById["card.revolver_heavy_cannon"];
AssertEqual("any", loadedFinisher.ColorEnergyCost?.ColorFilter, "GameContent maps finisher color filter");
AssertEqual(5, loadedFinisher.ColorInteractions.FinisherColorEffects.Count, "GameContent maps finisher color effects");
AssertEqual(6, loadedContent.ExpandedStartingCardIdsForWeapon("weapon.revolver_sword").Count, "GameContent expands fixed 6-card weapon starting pools");
AssertEqual(CardRarity.Common, loadedContent.CardsById["card.revolver_quick_thrust"].Rarity, "Quick thrust is a common reward-pool card");
Assert(!loadedContent.CardsById["card.revolver_quick_thrust"].Tags.Contains("starting_pool"), "Quick thrust is no longer tagged as a starting-pool card");
AssertEqual(CardRarity.Common, loadedContent.CardsById["card.arm_bind"].Rarity, "Bind is a common reward-pool card");
Assert(!loadedContent.CardsById["card.arm_bind"].Tags.Contains("starting_pool"), "Bind is no longer tagged as a starting-pool card");
Assert(loadedContent.CardPoolsById["card_pool.reward.mechanical_arm"].RewardByRarity["rare"].Contains("card.arm_shield_overload"), "GameContent maps weapon reward pools");
AssertEqual(6, loadedContent.EncountersById.Count, "GameContent loads the MVP encounter sequence");
AssertEqual(0, loadedContent.EncountersById["encounter.mvp.normal_01"].RewardProfile.CardPackIds.Count, "GameContent preserves empty card_pack_ids compatibility field");
AssertEqual("左轮剑", loadedContent.WeaponName("weapon.revolver_sword"), "GameContent maps localization for weapons");
Assert(loadedContent.AssetsById.ContainsKey("asset.card.template.action"), "GameContent loads presentation asset manifest");

var underPicked = startingDeckSelectionService.Validate(revolverMainSelection with
{
    MainHandCardIds = revolverMainSelection.MainHandCardIds.Take(5).ToList()
}, startingPools);
Assert(!underPicked.IsValid, "Under-picking main-hand starting cards cannot start a run");

var overPicked = startingDeckSelectionService.Validate(revolverMainSelection with
{
    OffHandCardIds = revolverMainSelection.OffHandCardIds.Concat(["card.arm_counter"]).ToList()
}, startingPools);
Assert(!overPicked.IsValid, "Over-picking off-hand starting cards cannot start a run");

var outsidePoolPick = startingDeckSelectionService.Validate(revolverMainSelection with
{
    MainHandCardIds =
    [
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_slash",
        "card.revolver_double_slash",
        "card.revolver_heavy_cannon",
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
    ["card.revolver_double_slash"] = strike with { Id = "card.revolver_double_slash" },
    ["card.revolver_quick_thrust"] = strike with { Id = "card.revolver_quick_thrust", Cost = 0 },
    ["card.revolver_reload_slash"] = strike with { Id = "card.revolver_reload_slash" },
    ["card.revolver_heavy_cannon"] = finisher with { Id = "card.revolver_heavy_cannon" },
    ["card.revolver_bullet_storm"] = finisher with { Id = "card.revolver_bullet_storm" },
    ["card.arm_guard"] = guardAction with { Id = "card.arm_guard" },
    ["card.arm_bind"] = strike with { Id = "card.arm_bind", WeaponId = "weapon.mechanical_arm" },
    ["card.arm_crush"] = strike with { Id = "card.arm_crush", WeaponId = "weapon.mechanical_arm" },
    ["card.arm_counter"] = finisher with { Id = "card.arm_counter", WeaponId = "weapon.mechanical_arm" },
    ["card.arm_army_sweep"] = finisher with { Id = "card.arm_army_sweep", WeaponId = "weapon.mechanical_arm" }
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
AssertEqual(10, mvpSelectedRun.MasterDeckInstances.Count, "Full MVP regression starts from 10 weapon-selected CardInstances");

var mvpPostCombatRun = runProgressService.ApplyCombatResult(
    mvpSelectedRun,
    CreateCombatResult(encounter.Id, CombatStatus.Victory, playerHp: 42),
    encounter);
var mvpShardRun = rewardService.AddPendingColorShard(mvpPostCombatRun, ColorType.Blue);
var mvpEnchantTarget = rewardService.ListEnchantableActionCards(mvpShardRun, mvpRegressionCardsById).First();
var mvpEnchantedRun = rewardService.ApplyColorShard(mvpShardRun, ColorType.Blue, mvpEnchantTarget.InstanceId, mvpRegressionCardsById);
string[] mvpRewardCandidates = ["card.revolver_slash", "card.arm_guard", "card.revolver_heavy_cannon"];
var mvpRewardedRun = rewardService.ClaimWeaponCardChoice(mvpEnchantedRun, mvpRewardCandidates, ["card.arm_guard"]);
AssertEqual(0, mvpRewardedRun.PendingColorShards.Count, "Full MVP regression consumes the post-combat color shard");
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
