using System.Text.Json;
using System.Text.Json.Serialization;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Debug;
using RoguelikeCardGame.Application.Rewards;
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
    Id = "card.basic_strike",
    Type = CardType.Action,
    Cost = 1,
    DefaultChainChange = ChainChange.Gain(1),
    TargetRule = TargetRule.SingleEnemy,
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 6 }],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "attack"]
};

var finisher = new CardDefinition
{
    Id = "card.burst_finish",
    Type = CardType.Finisher,
    Cost = 0,
    MinChain = 3,
    DefaultChainChange = ChainChange.ConsumeAll,
    TargetRule = TargetRule.SingleEnemy,
    Effects = [new EffectDefinition { Type = "damage", Target = "selected_enemy", Value = 18 }],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "finisher"]
};

var guardSkill = new CardDefinition
{
    Id = "card.basic_guard",
    Type = CardType.Skill,
    Cost = 0,
    DefaultChainChange = ChainChange.None,
    TargetRule = TargetRule.Self,
    Effects =
    [
        new EffectDefinition { Type = "block", Target = "self", Value = 5 },
        new EffectDefinition { Type = "gain_action_points", Target = "self", Value = 1 }
    ],
    Rarity = CardRarity.Starter,
    Tags = ["starter", "defense"]
};

var drawSkill = new CardDefinition
{
    Id = "card.focus_draw",
    Type = CardType.Skill,
    Cost = 0,
    DefaultChainChange = ChainChange.None,
    TargetRule = TargetRule.Self,
    Effects = [new EffectDefinition { Type = "draw_cards", Target = "self", Value = 1 }],
    Rarity = CardRarity.Common,
    Tags = ["skill", "draw"]
};

var discountSkill = new CardDefinition
{
    Id = "card.setup_discount",
    Type = CardType.Skill,
    Cost = 0,
    DefaultChainChange = ChainChange.None,
    TargetRule = TargetRule.Self,
    Effects = [new EffectDefinition { Type = "temporary_discount", Target = "hand", Value = 1 }],
    Rarity = CardRarity.Common,
    Tags = ["skill", "discount"]
};

var arcSweepFinisher = new CardDefinition
{
    Id = "card.arc_sweep_finish",
    Type = CardType.Finisher,
    Cost = 0,
    MinChain = 3,
    DefaultChainChange = ChainChange.ConsumeAll,
    TargetRule = TargetRule.AllEnemies,
    Effects = [new EffectDefinition { Type = "damage", Target = "all_enemies", Value = 4 }],
    Rarity = CardRarity.Common,
    Tags = ["finisher", "attack", "aoe"]
};

var refundFinisher = new CardDefinition
{
    Id = "card.refund_finish",
    Type = CardType.Finisher,
    Cost = 0,
    MinChain = 5,
    DefaultChainChange = ChainChange.ConsumeAll,
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
    Id = "relic.mvp_chain_spark",
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
        CardPackIds = ["reward_pack.mvp.action", "reward_pack.mvp.skill", "reward_pack.mvp.finisher"],
        RelicId = null
    },
    TeachingGoalKey = "encounter.mvp.normal_01.goal",
    DifficultyNote = "Smoke test encounter."
};

var rewardPack = new RewardPackDefinition
{
    Id = "reward_pack.mvp.action",
    PackType = CardType.Action,
    CandidateIds = [strike.Id, "card.quick_jab", "card.heavy_strike"],
    MinPick = 0,
    MaxPick = 3,
    GuaranteeRule = "fixed_three_candidates",
    RepeatRule = RewardRepeatRule.Repeatable
};

var skillRewardPack = new RewardPackDefinition
{
    Id = "reward_pack.mvp.skill",
    PackType = CardType.Skill,
    CandidateIds = [guardSkill.Id, drawSkill.Id, discountSkill.Id],
    MinPick = 0,
    MaxPick = 3,
    GuaranteeRule = "fixed_three_candidates",
    RepeatRule = RewardRepeatRule.Repeatable
};

var finisherRewardPack = new RewardPackDefinition
{
    Id = "reward_pack.mvp.finisher",
    PackType = CardType.Finisher,
    CandidateIds = [arcSweepFinisher.Id, refundFinisher.Id, finisher.Id],
    MinPick = 0,
    MaxPick = 3,
    GuaranteeRule = "fixed_three_candidates_with_aoe_finisher",
    RepeatRule = RewardRepeatRule.Repeatable
};

var eliteEncounter = new EncounterDefinition
{
    Id = "encounter.mvp.elite_01",
    NodeType = EncounterNodeType.Elite,
    Enemies = [new EncounterEnemyDefinition { InstanceId = "enemy_01", EnemyId = sequenceEnemy.Id }],
    RewardProfile = new EncounterRewardProfileDefinition
    {
        CardPackIds = ["reward_pack.mvp.action", "reward_pack.mvp.skill", "reward_pack.mvp.finisher"],
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

var rewardPacksById = new Dictionary<string, RewardPackDefinition>
{
    [rewardPack.Id] = rewardPack,
    [skillRewardPack.Id] = skillRewardPack,
    [finisherRewardPack.Id] = finisherRewardPack
};

var relicsById = new Dictionary<string, RelicDefinition>
{
    [relic.Id] = relic
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
    encounterSequence: [encounter.Id, eliteEncounter.Id, bossEncounter.Id]);

AssertEqual(RunStatus.InProgress, run.Status, "RunStateFactory starts run in progress");
AssertEqual(60, run.PlayerHp, "RunStateFactory starts at full HP");
AssertEqual(5, run.MasterDeck.Count, "RunStateFactory copies starter deck");

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

var endedTurn = turnService.EndPlayerTurn(combat with { ActionPoints = 2, Chain = 3, PlayerBlock = 7 });
AssertEqual(CombatStatus.EnemyTurn, endedTurn.Status, "EndPlayerTurn enters enemy turn");
AssertEqual(0, endedTurn.ActionPoints, "EndPlayerTurn clears unused action points");
AssertEqual(0, endedTurn.Chain, "EndPlayerTurn clears chain");
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
    Chain = 0,
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

var cardPlayService = new CardPlayService(turnService);

var playableAction = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id], actionPoints: 1), strike, "enemy_01");
Assert(playableAction.Succeeded, "Action card can be played with enough action points and a valid target");
AssertEqual(0, playableAction.Combat.ActionPoints, "Action card consumes action points");
AssertEqual(1, playableAction.Combat.Chain, "Action card default adds one chain");
AssertEqual(18, playableAction.Combat.Enemies[0].CurrentHp, "Damage effect reduces target enemy HP");
AssertEqual(0, playableAction.Combat.DeckZones.HandCount, "Played card leaves hand");
AssertEqual(strike.Id, playableAction.Combat.DeckZones.DiscardPile.Single(), "Played card enters discard pile");
Assert(playableAction.Events.Any(item => item.EventType == CombatLogEventType.CardPlayed), "Successful card play emits a card-play log event");
Assert(playableAction.Events.Any(item => item.Metadata.TryGetValue("effect_type", out var effectType) && effectType == "damage"), "Successful card play emits an effect log event");

var duplicateSlotResult = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id, guardSkill.Id, strike.Id], actionPoints: 1), strike, "enemy_01", handIndex: 2);
Assert(duplicateSlotResult.Succeeded, "Duplicate card can be played from a specific hand slot");
AssertEqual(strike.Id, duplicateSlotResult.Combat.DeckZones.Hand[0], "Playing duplicate slot keeps earlier same-id card in hand");
AssertEqual(guardSkill.Id, duplicateSlotResult.Combat.DeckZones.Hand[1], "Playing duplicate slot removes the clicked slot");
Assert(duplicateSlotResult.Events.Any(item =>
	item.EventType == CombatLogEventType.CardPlayed &&
	item.NumericChanges.TryGetValue("hand_index", out var handIndex) &&
	handIndex == 2), "Card play log records clicked hand slot for presentation animations");

var costFailure = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id], actionPoints: 0), strike, "enemy_01");
Assert(!costFailure.Succeeded, "Action card cannot be played without enough action points");
AssertEqual(PlayCardFailureReason.InsufficientActionPoints, costFailure.FailureReason, "Cost failure exposes UI-readable reason");
AssertEqual(1, costFailure.RequiredActionPoints, "Cost failure exposes required action points");
AssertEqual(0, costFailure.CurrentActionPoints, "Cost failure exposes current action points");
Assert(costFailure.Events.Any(item => item.EventType == CombatLogEventType.CardPlayRejected), "Cost failure emits a rejection log event");

var chainFailure = cardPlayService.PlayCard(CreatePlayableCombat([finisher.Id], actionPoints: 0, chain: 2), finisher, "enemy_01");
Assert(!chainFailure.Succeeded, "Finisher cannot be played below min_chain");
AssertEqual(PlayCardFailureReason.InsufficientChain, chainFailure.FailureReason, "Chain failure exposes UI-readable reason");
AssertEqual(3, chainFailure.RequiredChain, "Chain failure exposes required chain");
AssertEqual(2, chainFailure.CurrentChain, "Chain failure exposes current chain");

var missingTarget = cardPlayService.PlayCard(CreatePlayableCombat([strike.Id], actionPoints: 1), strike);
Assert(!missingTarget.Succeeded, "Single-enemy card cannot be played without a target");
AssertEqual(PlayCardFailureReason.TargetMissing, missingTarget.FailureReason, "Target failure exposes UI-readable reason");
AssertEqual(TargetRule.SingleEnemy.ToString(), missingTarget.RequiredTargetRule, "Target failure exposes required target rule");

var skillResult = cardPlayService.PlayCard(CreatePlayableCombat([guardSkill.Id], actionPoints: 0, chain: 2), guardSkill);
Assert(skillResult.Succeeded, "Skill card can be played without action points");
AssertEqual(1, skillResult.Combat.ActionPoints, "Gain-action-points effect resolves during the current turn");
AssertEqual(2, skillResult.Combat.Chain, "Skill card default does not add chain");
AssertEqual(5, skillResult.Combat.PlayerBlock, "Block effect increases player block");

var finisherResult = cardPlayService.PlayCard(CreatePlayableCombat([finisher.Id], actionPoints: 0, chain: 3), finisher, "enemy_01");
Assert(finisherResult.Succeeded, "Finisher can be played when min_chain is met");
AssertEqual(0, finisherResult.Combat.ActionPoints, "Finisher default does not consume action points");
AssertEqual(0, finisherResult.Combat.Chain, "Finisher default clears chain");
AssertEqual(6, finisherResult.Combat.Enemies[0].CurrentHp, "Finisher damage resolves");

var allEnemiesResult = cardPlayService.PlayCard(
    CreatePlayableCombat(
        [arcSweepFinisher.Id],
        actionPoints: 0,
        chain: 3,
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
    CreatePlayableCombat([drawSkill.Id], actionPoints: 0, chain: 1, drawPile: [strike.Id]),
    drawSkill);
Assert(drawResult.Succeeded, "Draw skill can be played");
Assert(drawResult.Combat.DeckZones.Hand.Contains(strike.Id), "Draw effect adds drawn cards to hand");
AssertEqual(1, drawResult.Combat.Chain, "Draw skill default does not add chain");
Assert(drawResult.Events.Any(item => item.EventType == CombatLogEventType.CardsDrawn), "Draw skill includes draw log events in the result");

var discountResult = cardPlayService.PlayCard(CreatePlayableCombat([discountSkill.Id], actionPoints: 0, chain: 1), discountSkill);
Assert(discountResult.Succeeded, "Temporary discount placeholder skill can be played");
Assert(discountResult.Events.Any(item => item.Metadata.TryGetValue("effect_type", out var effectType) && effectType == "temporary_discount_placeholder"), "Temporary discount placeholder emits an effect log event");

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
var availablePacks = rewardService.GetAvailableCardPacks(encounter, rewardPacksById);
AssertEqual(3, availablePacks.Count, "Normal combat victory offers three reward pack types");
Assert(availablePacks.Any(pack => pack.PackType == CardType.Action), "Reward choice includes action pack");
Assert(availablePacks.Any(pack => pack.PackType == CardType.Skill), "Reward choice includes skill pack");
Assert(availablePacks.Any(pack => pack.PackType == CardType.Finisher), "Reward choice includes finisher pack");

var openedActionPack = rewardService.OpenRewardPack(rewardPack.Id, rewardPacksById);
AssertEqual(3, openedActionPack.CandidateIds.Count, "Opening a reward pack exposes exactly three candidates");
Assert(openedActionPack.CandidateIds.All(cardId => rewardPack.CandidateIds.Contains(cardId)), "Opened pack exposes the selected pack candidates");

var skippedRewardRun = rewardService.ClaimCards(run, openedActionPack, []);
AssertEqual(run.MasterDeck.Count, skippedRewardRun.MasterDeck.Count, "Skipping reward cards leaves deck unchanged");

var multiPickRun = rewardService.ClaimCards(run, openedActionPack, [strike.Id, "card.quick_jab"]);
AssertEqual(run.MasterDeck.Count + 2, multiPickRun.MasterDeck.Count, "Reward claim can add multiple cards");
AssertEqual("card.quick_jab", multiPickRun.MasterDeck[^1], "Reward claim appends selected cards to the master deck");

var duplicatePickRun = rewardService.ClaimCards(run, openedActionPack, [strike.Id, strike.Id]);
AssertEqual(run.MasterDeck.Count + 2, duplicatePickRun.MasterDeck.Count, "Reward claim allows duplicate card ids when pack is repeatable");
AssertEqual(6, duplicatePickRun.MasterDeck.Count(cardId => cardId == strike.Id), "Duplicate reward cards can coexist with existing deck copies");

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
        RewardPackPreviewIds = [finisherRewardPack.Id]
    },
    new Dictionary<string, EncounterDefinition>
    {
        [encounter.Id] = encounter,
        [eliteEncounter.Id] = eliteEncounter,
        [bossEncounter.Id] = bossEncounter
    },
    enemiesById,
    rewardPacksById);
AssertEqual(98765, debugStart.RunState.Seed, "Debug run entry preserves requested seed");
AssertEqual(eliteEncounter.Id, debugStart.Encounter.Id, "Debug run entry can enter a selected fixed encounter directly");
AssertEqual(1, debugStart.RunState.CurrentEncounterIndex, "Debug run entry sets current encounter index for selected encounter");
AssertEqual(2, debugStart.RunState.MasterDeck.Count, "Debug run entry supports starter deck override plus added cards");
AssertEqual(strike.Id, debugStart.RunState.MasterDeck[^1], "Debug run entry appends requested card ids");
AssertEqual(1, debugStart.RewardPackPreviews.Count, "Debug run entry supports selected reward pack preview");
AssertEqual(3, debugStart.RewardPackPreviews[0].CandidateIds.Count, "Debug reward pack preview exposes three candidates");

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
            NumericChanges = new Dictionary<string, int> { ["chain_before"] = 2 },
            Metadata = new Dictionary<string, string> { ["card_type"] = CardType.Action.ToString() }
        },
        new CombatLogEvent
        {
            EventId = "metrics_chain_3",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = 1,
            SourceId = strike.Id,
            NumericChanges = new Dictionary<string, int> { ["chain_before"] = 2, ["chain_after"] = 3 },
            Metadata = new Dictionary<string, string> { ["effect_type"] = "default_chain_change" }
        },
        new CombatLogEvent
        {
            EventId = "metrics_finisher_played",
            EventType = CombatLogEventType.CardPlayed,
            TurnNumber = 2,
            SourceId = finisher.Id,
            NumericChanges = new Dictionary<string, int> { ["chain_before"] = 5 },
            Metadata = new Dictionary<string, string> { ["card_type"] = CardType.Finisher.ToString() }
        },
        new CombatLogEvent
        {
            EventId = "metrics_finisher_bonus",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = 2,
            SourceId = finisher.Id,
            NumericChanges = new Dictionary<string, int> { ["threshold"] = 5, ["chain_before_play"] = 5, ["triggered"] = 1 },
            Metadata = new Dictionary<string, string> { ["effect_type"] = "chain_threshold_bonus" }
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
            RewardPackId = rewardPack.Id,
            RewardPackType = rewardPack.PackType.ToString(),
            PickedCount = 2,
            CardIds = [strike.Id, "card.quick_jab"]
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
AssertEqual(5, metricsReport.Combats[0].HighestChain, "Metrics report stores highest chain");
AssertEqual(1, metricsReport.Combats[0].ChainThreshold3ReachedCount, "Metrics report counts chain threshold 3 crossings");
AssertEqual(1, metricsReport.Combats[0].FinisherUseCount, "Metrics report counts finisher use");
AssertEqual(1, metricsReport.Combats[0].FinisherBonusTriggerCount, "Metrics report counts triggered finisher bonus effects");
AssertEqual(2, metricsReport.Rewards[0].PickedCount, "Metrics report stores reward pick count");
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
    Cards = new[] { strike, finisher, guardSkill, drawSkill, discountSkill, arcSweepFinisher, refundFinisher },
    Relics = new[] { relic },
    Enemies = new[] { enemy, sequenceEnemy },
    Encounters = new[] { encounter, eliteEncounter, bossEncounter },
    Rewards = new[] { rewardPack, skillRewardPack, finisherRewardPack },
    Run = run,
    Combat = combat
}, options);

if (string.IsNullOrWhiteSpace(serializedBundle))
{
    throw new InvalidOperationException("Serialized model bundle should not be empty.");
}

Console.WriteLine("Domain model smoke tests passed.");

static CombatState CreatePlayableCombat(
    IEnumerable<string> hand,
    int actionPoints = 3,
    int chain = 0,
    int playerBlock = 0,
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
        PlayerHp = 60,
        PlayerBlock = playerBlock,
        BaseActionPoints = 3,
        CardsPerTurn = 5,
        ActionPoints = actionPoints,
        Chain = chain,
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
        Chain = 0,
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
