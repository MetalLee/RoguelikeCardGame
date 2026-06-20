using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;
using RoguelikeCardGame.Infrastructure.Randomness;
using RoguelikeCardGame.Presentation.Battle;
using RoguelikeCardGame.Presentation.Menus;
using RoguelikeCardGame.Presentation.Rewards;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Flow;

public sealed class MvpRunFlowController
{
    private const bool UseBeatCombatPrototype = true;
    private const string StartMenuScenePath = "res://scenes/menus/StartMenuScreen.tscn";
    private const string WeaponSelectionScenePath = "res://scenes/menus/WeaponSelectionScreen.tscn";
    private const string BattleScenePath = "res://scenes/battle/BattleScreen.tscn";
    private const string RewardScenePath = "res://scenes/rewards/RewardScreen.tscn";
    private const string RunResultScenePath = "res://scenes/menus/RunResultScreen.tscn";

    private readonly GameContent content;
    private readonly SceneScreenHost screenHost;
    private readonly RunStateFactory runFactory = new();
    private readonly RunProgressService runProgressService = new();
    private readonly StartingDeckSelectionService startingDeckSelectionService = new();
    private readonly RewardService rewardService = new();
    private readonly BeatRoundPlanningService beatPlanningService = new();

    private RunRandomStreams? randomStreams;
    private CombatStateFactory combatFactory = new();
    private CombatTurnService turnService = new();
    private BeatCombatRoundFactory beatRoundFactory = new();
    private CardPlayService cardPlayService = new();
    private RunState? run;
    private CombatState? combat;
    private EncounterDefinition? encounter;
    private ColorType? pendingRewardColorShard;
    private List<CardInstance> rewardEnchantableCards = new();
    private string? selectedRewardEnchantTargetId;
    private List<string> weaponRewardCandidateIds = new();
    private string? selectedWeaponRewardCardId;
    private int actionCardsPlayedThisTurn;
    private bool isAnimating;
    private int flowVersion;

    public MvpRunFlowController(GameContent content, SceneScreenHost screenHost)
    {
        this.content = content;
        this.screenHost = screenHost;
    }

    public void ShowStartMenu()
    {
        try
        {
            ResetFlowInteractionState();
            var screen = screenHost.ShowScreen<StartMenuScreen>(StartMenuScenePath);
            screen.StartRequested += StartNewRun;
            screen.Render();
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
    }

    private void StartNewRun()
    {
        ResetFlowInteractionState();
        ShowWeaponSelection();
    }

    private void ShowWeaponSelection()
    {
        try
        {
            var screen = screenHost.ShowScreen<WeaponSelectionScreen>(WeaponSelectionScenePath);
            screen.WeaponsConfirmed += ConfirmWeapons;
            screen.DebugEncounterRequested += StartDebugEncounterFromWeaponSelection;
            screen.BackRequested += ShowStartMenu;
            screen.Render();
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
    }

    private void StartDebugEncounterFromWeaponSelection(
        string mainHandWeaponId,
        string offHandWeaponId,
        WeaponSelectionDebugTarget target)
    {
        try
        {
            ResetFlowInteractionState();
            run = CreateRunFromWeaponSelection(mainHandWeaponId, offHandWeaponId) with
            {
                CurrentEncounterIndex = DebugEncounterIndexFor(target)
            };
            StartCurrentEncounter();
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
    }

    private int DebugEncounterIndexFor(WeaponSelectionDebugTarget target)
    {
        var nodeType = target switch
        {
            WeaponSelectionDebugTarget.Elite => EncounterNodeType.Elite,
            WeaponSelectionDebugTarget.Boss => EncounterNodeType.Boss,
            _ => throw new InvalidOperationException($"Unsupported debug target '{target}'.")
        };

        var index = content.MvpRun.EncounterSequence.FindIndex(encounterId =>
            content.EncountersById.TryGetValue(encounterId, out var definition) &&
            definition.NodeType == nodeType);
        if (index < 0)
        {
            throw new InvalidOperationException($"No {nodeType} encounter exists in the MVP run sequence.");
        }

        return index;
    }

    private void ConfirmWeapons(string mainHandWeaponId, string offHandWeaponId)
    {
        try
        {
            ConfirmStartingDeck(mainHandWeaponId, offHandWeaponId);
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
    }

    private List<WeaponStartingPoolDefinition> BuildWeaponStartingPools()
    {
        return content.WeaponsById.Values
            .Select(weapon => new WeaponStartingPoolDefinition
            {
                WeaponId = weapon.Id,
                CardIds = content.ExpandedStartingCardIdsForWeapon(weapon.Id).ToList()
            })
            .ToList();
    }

    private void ConfirmStartingDeck(string mainHandWeaponId, string offHandWeaponId)
    {
        ResetFlowInteractionState();
        run = CreateRunFromWeaponSelection(mainHandWeaponId, offHandWeaponId);
        StartCurrentEncounter();
    }

    private RunState CreateRunFromWeaponSelection(string mainHandWeaponId, string offHandWeaponId)
    {
        var validation = startingDeckSelectionService.BuildAutomaticStarterDeck(
            mainHandWeaponId,
            offHandWeaponId,
            BuildWeaponStartingPools(),
            content.CardsById);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Automatic starter deck could not be created:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, validation.Errors));
        }

        var sequence = content.MvpRun;
        var seed = RunSeedGenerator.CreateSeed();
        randomStreams = RunRandomStreams.FromRunSeed(seed);
        combatFactory = new CombatStateFactory(randomStreams.Deck.Shuffle);
        turnService = new CombatTurnService(randomStreams.Deck.Shuffle);
        cardPlayService = new CardPlayService(turnService);
        return runFactory.CreateNewRunFromWeaponSelection(
            runId: $"run_{DateTime.UtcNow:yyyyMMddHHmmss}",
            seed: seed,
            playerMaxHp: sequence.PlayerMaxHp,
            baseActionPoints: sequence.BaseActionPoints,
            cardsPerTurn: sequence.CardsPerTurn,
            mainHandWeaponId: mainHandWeaponId,
            offHandWeaponId: offHandWeaponId,
            selectedCardIds: validation.SelectedCardIds,
            encounterSequence: sequence.EncounterSequence);
    }

    private void StartCurrentEncounter()
    {
        if (run is null)
        {
            ShowStartMenu();
            return;
        }

        if (run.Status != RunStatus.InProgress)
        {
            ShowRunResult();
            return;
        }

        run = runProgressService.PrepareForCombat(run);
        var encounterId = run.EncounterSequence[run.CurrentEncounterIndex];
        encounter = content.EncountersById[encounterId];
        combat = combatFactory.CreateCombat(
            combatId: $"combat_{run.CurrentEncounterIndex + 1}",
            runState: run,
            encounter: encounter,
            enemiesById: content.EnemiesById);
        combat = IsBeatCombatPrototypeEnabled()
            ? turnService.StartBeatCombat(combat)
            : turnService.StartCombat(combat);
        if (IsBeatCombatPrototypeEnabled())
        {
            combat = combat with
            {
                BeatRound = beatRoundFactory.CreateRound(combat, content.EnemiesById)
            };
        }

        actionCardsPlayedThisTurn = 0;
        ShowBattle();
    }

    private void ShowBattle(string? message = null)
    {
        if (combat is null || encounter is null || run is null)
        {
            ShowStartMenu();
            return;
        }

        var screen = screenHost.ShowScreen<BattleScreen>(BattleScenePath);
        screen.CardRequested += PlayCard;
        screen.BeatCardDroppedOnSlot += PlaceBeatCard;
        screen.BeatTargetSelected += SelectBeatTarget;
        screen.EndTurnRequested += EndTurn;
        screen.RestartRequested += StartNewRun;
        screen.Render(combat, run, encounter, message);
    }

    private void PlaceBeatCard(string cardInstanceId, string cardId, int handIndex, int beatIndex)
    {
        if (combat is null || combat.BeatRound is null)
        {
            ShowBattle("无法放入该拍位");
            return;
        }

        if (!content.CardsById.TryGetValue(cardId, out var card))
        {
            screenHost.ShowFatalError(new InvalidOperationException($"Unknown card id '{cardId}' for beat placement."));
            return;
        }

        try
        {
            combat = beatPlanningService.PlaceActionCardInBeat(
                combat,
                cardInstanceId,
                cardId,
                handIndex,
                beatIndex,
                card);
        }
        catch (InvalidOperationException)
        {
            ShowBattle("无法放入该拍位");
            return;
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
            return;
        }

        ShowBattle();
    }

    private void SelectBeatTarget(int playerBeatIndex, BeatTarget target)
    {
        if (combat is null || combat.BeatRound is null)
        {
            ShowBattle("当前三拍回合尚未准备完成");
            return;
        }

        try
        {
            var beatCombatService = new BeatCombatService();
            combat = target.Kind == BeatTargetKind.EnemyBody
                ? beatPlanningService.SetEnemyBodyTarget(combat, playerBeatIndex, target.EnemyInstanceId, beatCombatService)
                : beatPlanningService.SetEnemyBeatTarget(
                    combat,
                    playerBeatIndex,
                    target.EnemyInstanceId,
                    target.EnemyBeatIndex ?? -1,
                    beatCombatService);
        }
        catch (InvalidOperationException)
        {
            ShowBattle(BeatTargetFailureText(target));
            return;
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
            return;
        }

        ShowBattle();
    }

    private static string BeatTargetFailureText(BeatTarget target)
    {
        return target.Kind == BeatTargetKind.EnemyBody
            ? "无法锁定魔物本体：请先锁定该魔物的全部拍位"
            : "无法锁定该敌方拍位：它可能已被其他玩家拍位锁定";
    }

    private async void PlayCard(string cardInstanceId, string cardId, int handIndex, string? targetEnemyInstanceId)
    {
        if (IsBeatCombatPrototypeEnabled())
        {
            ShowBattle("当前原型请使用三拍区/结束结算");
            return;
        }

        if (!TryBeginBattleAnimation(out var operationVersion))
        {
            return;
        }

        try
        {
            if (combat is null)
            {
                return;
            }

            var animationScreen = screenHost.ActiveScreen as BattleScreen;
            var card = content.CardsById[cardId];
            var result = cardPlayService.PlayCard(
                combat,
                card,
                targetEnemyInstanceId,
                handIndex,
                ResolveEnchantmentForInstance(cardInstanceId),
                cardInstanceId);
            combat = result.Combat;
            var eventsToAnimate = result.Events.ToList();

            if (!result.Succeeded)
            {
                ShowBattle(BattleScreen.FailureText(result));
                return;
            }

            if (card.Type == CardType.Action)
            {
                var beforeRelicLogCount = combat.Log.Count;
                ApplyFirstActionRelicIfNeeded(card.Id);
                eventsToAnimate.AddRange(combat.Log.Skip(beforeRelicLogCount));
                actionCardsPlayedThisTurn++;
            }

            animationScreen?.HidePlayedCard(handIndex);
            await PlayBattleAnimationsAsync(animationScreen, eventsToAnimate, card, handIndex, playConcurrently: true);
            if (!IsCurrentFlow(operationVersion))
            {
                return;
            }

            if (combat.Status == CombatStatus.Victory)
            {
                ResolveCombatVictory();
                return;
            }

            ShowBattle();
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
        finally
        {
            FinishBattleAnimation(operationVersion);
        }
    }

    private void ApplyFirstActionRelicIfNeeded(string cardId)
    {
        if (run is null || combat is null || actionCardsPlayedThisTurn != 0)
        {
            return;
        }

        if (!run.RelicIds.Contains("relic.mvp_color_spark"))
        {
            return;
        }

        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_relic_color_spark_{log.Count + 1}",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = "relic.mvp_color_spark",
            TargetIds = ["player"],
            NumericChanges = new Dictionary<string, int>
            {
                ["block_gained"] = 2,
                ["player_block_after"] = combat.PlayerBlock + 2
            },
            Metadata = new Dictionary<string, string>
            {
                ["trigger"] = "first_action_card_each_turn",
                ["card_id"] = cardId,
                ["effect_type"] = "gain_block"
            }
        });

        combat = combat with
        {
            PlayerBlock = combat.PlayerBlock + 2,
            Log = log
        };
    }

    private CardEnchantment? ResolveEnchantmentForInstance(string cardInstanceId)
    {
        return run?.MasterDeckInstances
            .FirstOrDefault(instance => string.Equals(instance.InstanceId, cardInstanceId, StringComparison.Ordinal))
            ?.Enchantment;
    }

    private async void EndTurn()
    {
        if (!TryBeginBattleAnimation(out var operationVersion))
        {
            return;
        }

        try
        {
            if (combat is null || combat.Status != CombatStatus.PlayerTurn)
            {
                return;
            }

            var animationScreen = screenHost.ActiveScreen as BattleScreen;
            var startLogCount = combat.Log.Count;

            if (IsBeatCombatPrototypeEnabled())
            {
                await EndBeatTurn(operationVersion, animationScreen);
                return;
            }

            combat = turnService.EndPlayerTurn(combat);
            combat = turnService.ResolveEnemyTurn(combat, content.EnemiesById);

            if (combat.Status == CombatStatus.Defeat)
            {
                var defeatEvents = combat.Log.Skip(startLogCount).ToList();
                await PlayBattleAnimationsAsync(animationScreen, defeatEvents, null, null);
                if (!IsCurrentFlow(operationVersion))
                {
                    return;
                }

                run = runProgressService.ApplyCombatResult(run!, combat, encounter);
                ShowRunResult();
                return;
            }

            combat = turnService.PrepareNextPlayerTurn(combat);
            var eventsToAnimate = combat.Log.Skip(startLogCount).ToList();
            await PlayBattleAnimationsAsync(animationScreen, eventsToAnimate, null, null);
            if (!IsCurrentFlow(operationVersion))
            {
                return;
            }

            actionCardsPlayedThisTurn = 0;
            ShowBattle();
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
        finally
        {
            FinishBattleAnimation(operationVersion);
        }
    }

    private async Task EndBeatTurn(int operationVersion, BattleScreen? animationScreen)
    {
        if (combat is null || combat.BeatRound is null)
        {
            ShowBattle("当前三拍回合尚未准备完成");
            return;
        }

        var result = new BeatCombatService().ResolveBeatRound(combat, content.CardsById, content.EnemiesById);
        combat = result.Combat;

        await PlayBattleAnimationsAsync(animationScreen, result.Events, null, null);
        if (!IsCurrentFlow(operationVersion))
        {
            return;
        }

        if (combat.Status == CombatStatus.Victory)
        {
            ResolveCombatVictory();
            return;
        }

        if (combat.Status == CombatStatus.Defeat)
        {
            run = runProgressService.ApplyCombatResult(run!, combat, encounter);
            ShowRunResult();
            return;
        }

        combat = turnService.PrepareNextBeatRound(combat);
        combat = combat with
        {
            BeatRound = beatRoundFactory.CreateRound(combat, content.EnemiesById)
        };
        actionCardsPlayedThisTurn = 0;
        ShowBattle();
    }

    private void ResolveCombatVictory()
    {
        run = runProgressService.ApplyCombatResult(run!, combat!, encounter);
        if (run.Status == RunStatus.Cleared || encounter!.NodeType == EncounterNodeType.Boss)
        {
            ShowRunResult();
            return;
        }

        ShowRewardSelection();
    }

    private void ShowRewardSelection()
    {
        if (encounter is null || run is null || randomStreams is null)
        {
            ShowStartMenu();
            return;
        }

        pendingRewardColorShard = rewardService.GenerateColorShard(randomStreams.Reward.NextInt);
        run = rewardService.AddPendingColorShard(run, pendingRewardColorShard.Value);
        rewardEnchantableCards = rewardService.ListEnchantableActionCards(run, content.CardsById).ToList();
        selectedRewardEnchantTargetId = null;
        weaponRewardCandidateIds = [];
        selectedWeaponRewardCardId = null;

        var screen = ShowRewardScreen();
        screen.RenderColorShardStep(
            encounter,
            pendingRewardColorShard.Value,
            rewardEnchantableCards,
            selectedRewardEnchantTargetId);
    }

    private void SelectRewardEnchantTarget(string cardInstanceId)
    {
        if (rewardEnchantableCards.All(instance => instance.InstanceId != cardInstanceId))
        {
            return;
        }

        selectedRewardEnchantTargetId = string.Equals(selectedRewardEnchantTargetId, cardInstanceId, StringComparison.Ordinal)
            ? null
            : cardInstanceId;
        RenderColorShardStep();
    }

    private void RenderColorShardStep()
    {
        if (encounter is null || pendingRewardColorShard is null)
        {
            ShowStartMenu();
            return;
        }

        var screen = ShowRewardScreen();
        screen.RenderColorShardStep(
            encounter,
            pendingRewardColorShard.Value,
            rewardEnchantableCards,
            selectedRewardEnchantTargetId);
    }

    private void ConfirmColorShardStep()
    {
        if (run is null || encounter is null || pendingRewardColorShard is null || randomStreams is null)
        {
            ShowStartMenu();
            return;
        }

        if (selectedRewardEnchantTargetId is null)
        {
            if (rewardEnchantableCards.Count > 0)
            {
                RenderColorShardStep();
                return;
            }
        }
        else
        {
            run = rewardService.ApplyColorShard(
                run,
                pendingRewardColorShard.Value,
                selectedRewardEnchantTargetId,
                content.CardsById);
        }

        weaponRewardCandidateIds = rewardService.GenerateWeaponCardCandidates(
            run,
            BuildWeaponRewardPools(),
            content.CardsById,
            randomStreams.Reward.NextInt).ToList();
        selectedWeaponRewardCardId = null;
        RenderWeaponCardRewardStep();
    }

    private void SelectWeaponRewardCard(string cardId)
    {
        if (!weaponRewardCandidateIds.Contains(cardId, StringComparer.Ordinal))
        {
            return;
        }

        selectedWeaponRewardCardId = string.Equals(selectedWeaponRewardCardId, cardId, StringComparison.Ordinal)
            ? null
            : cardId;
        RenderWeaponCardRewardStep();
    }

    private void RenderWeaponCardRewardStep()
    {
        if (encounter is null || pendingRewardColorShard is null)
        {
            ShowStartMenu();
            return;
        }

        var screen = ShowRewardScreen();
        screen.RenderWeaponCardChoiceStep(
            encounter,
            pendingRewardColorShard.Value,
            weaponRewardCandidateIds,
            selectedWeaponRewardCardId);
    }

    private void ConfirmReward()
    {
        if (run is null || encounter is null)
        {
            ShowStartMenu();
            return;
        }

        if (selectedWeaponRewardCardId is null)
        {
            RenderWeaponCardRewardStep();
            return;
        }

        run = rewardService.ClaimWeaponCard(run, weaponRewardCandidateIds, selectedWeaponRewardCardId);
        run = rewardService.GrantEncounterRelic(run, encounter, content.RelicsById);
        run = runProgressService.AdvanceAfterRewards(run, encounter);
        StartCurrentEncounter();
    }

    private void SkipWeaponCardReward()
    {
        if (run is null || encounter is null)
        {
            ShowStartMenu();
            return;
        }

        selectedWeaponRewardCardId = null;
        run = rewardService.GrantEncounterRelic(run, encounter, content.RelicsById);
        run = runProgressService.AdvanceAfterRewards(run, encounter);
        StartCurrentEncounter();
    }

    private List<WeaponRewardPoolDefinition> BuildWeaponRewardPools()
    {
        return content.WeaponsById.Values
            .Select(weapon =>
            {
                if (!content.CardPoolsById.TryGetValue(weapon.RewardPoolId, out var pool))
                {
                    throw new InvalidOperationException($"Unknown reward pool id '{weapon.RewardPoolId}'.");
                }

                return new WeaponRewardPoolDefinition
                {
                    WeaponId = weapon.Id,
                    CardIdsByRarity = pool.RewardByRarity.ToDictionary(
                        item => ParseRewardRarity(item.Key),
                        item => item.Value.ToList())
                };
            })
            .ToList();
    }

    private static CardRarity ParseRewardRarity(string rarity)
    {
        return rarity switch
        {
            "common" => CardRarity.Common,
            "uncommon" => CardRarity.Uncommon,
            "rare" => CardRarity.Rare,
            _ => throw new InvalidOperationException($"Unknown reward rarity '{rarity}'.")
        };
    }

    private RewardScreen ShowRewardScreen()
    {
        var screen = screenHost.ShowScreen<RewardScreen>(RewardScenePath);
        screen.ColorShardTargetSelected += SelectRewardEnchantTarget;
        screen.ColorShardConfirmed += ConfirmColorShardStep;
        screen.WeaponCardSelected += SelectWeaponRewardCard;
        screen.WeaponCardSkipped += SkipWeaponCardReward;
        screen.ConfirmRequested += ConfirmReward;
        return screen;
    }

    private void ShowRunResult()
    {
        ResetFlowInteractionState();
        var screen = screenHost.ShowScreen<RunResultScreen>(RunResultScenePath);
        screen.RestartRequested += StartNewRun;
        screen.RenderRunResult(run);
    }

    private static async Task PlayBattleAnimationsAsync(
        BattleScreen? screen,
        IReadOnlyList<CombatLogEvent> eventsToAnimate,
        CardDefinition? playedCard,
        int? playedHandIndex,
        bool playConcurrently = false)
    {
        if (screen is null || eventsToAnimate.Count == 0)
        {
            return;
        }

        await screen.PlayLogAnimationsAsync(eventsToAnimate, playedCard, playedHandIndex, playConcurrently);
    }

    private bool TryBeginBattleAnimation(out int operationVersion)
    {
        operationVersion = flowVersion;
        if (isAnimating)
        {
            SetActiveBattleInteractionsLocked(true);
            return false;
        }

        isAnimating = true;
        SetActiveBattleInteractionsLocked(true);
        return true;
    }

    private void FinishBattleAnimation(int operationVersion)
    {
        if (!IsCurrentFlow(operationVersion))
        {
            return;
        }

        isAnimating = false;
        SetActiveBattleInteractionsLocked(false);
    }

    private bool IsCurrentFlow(int operationVersion)
    {
        return operationVersion == flowVersion;
    }

    private void ResetFlowInteractionState()
    {
        flowVersion++;
        isAnimating = false;
        SetActiveBattleInteractionsLocked(false);
    }

    private void SetActiveBattleInteractionsLocked(bool locked)
    {
        if (screenHost.ActiveScreen is BattleScreen battleScreen)
        {
            battleScreen.SetInteractionsLocked(locked);
        }
    }

    private static bool IsBeatCombatPrototypeEnabled()
    {
        return UseBeatCombatPrototype;
    }
}
