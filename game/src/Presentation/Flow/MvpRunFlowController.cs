using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Rewards;
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
    private const string StartMenuScenePath = "res://scenes/menus/StartMenuScreen.tscn";
    private const string BattleScenePath = "res://scenes/battle/BattleScreen.tscn";
    private const string RewardScenePath = "res://scenes/rewards/RewardScreen.tscn";
    private const string RunResultScenePath = "res://scenes/menus/RunResultScreen.tscn";

    private readonly GameContent content;
    private readonly SceneScreenHost screenHost;
    private readonly RunStateFactory runFactory = new();
    private readonly RunProgressService runProgressService = new();
    private readonly RewardService rewardService = new();
    private readonly HashSet<string> selectedRewardCards = new(StringComparer.Ordinal);

    private RunRandomStreams? randomStreams;
    private CombatStateFactory combatFactory = new();
    private CombatTurnService turnService = new();
    private CardPlayService cardPlayService = new();
    private RunState? run;
    private CombatState? combat;
    private EncounterDefinition? encounter;
    private int actionCardsPlayedThisTurn;
    private RewardPackDefinition? openedRewardPack;
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
        var sequence = content.MvpRun;
        var seed = RunSeedGenerator.CreateSeed();
        randomStreams = RunRandomStreams.FromRunSeed(seed);
        combatFactory = new CombatStateFactory(randomStreams.Deck.Shuffle);
        turnService = new CombatTurnService(randomStreams.Deck.Shuffle);
        cardPlayService = new CardPlayService(turnService);
        run = runFactory.CreateNewRun(
            runId: $"run_{DateTime.UtcNow:yyyyMMddHHmmss}",
            seed: seed,
            playerMaxHp: sequence.PlayerMaxHp,
            baseActionPoints: sequence.BaseActionPoints,
            cardsPerTurn: sequence.CardsPerTurn,
            starterDeck: sequence.StarterDeck,
            encounterSequence: sequence.EncounterSequence);
        StartCurrentEncounter();
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
        combat = turnService.StartCombat(combat);
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
        screen.EndTurnRequested += EndTurn;
        screen.RestartRequested += StartNewRun;
        screen.Render(combat, run, encounter, message);
    }

    private async void PlayCard(string cardId, int handIndex, string? targetEnemyInstanceId)
    {
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
            var result = cardPlayService.PlayCard(combat, card, targetEnemyInstanceId, handIndex);
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

        if (!run.RelicIds.Contains("relic.mvp_chain_spark"))
        {
            return;
        }

        var log = combat.Log.ToList();
        log.Add(new CombatLogEvent
        {
            EventId = $"{combat.CombatId}_turn_{combat.TurnNumber}_relic_chain_spark_{log.Count + 1}",
            EventType = CombatLogEventType.EffectResolved,
            TurnNumber = combat.TurnNumber,
            SourceId = "relic.mvp_chain_spark",
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
            ShowBattle("敌人行动已结算，新回合开始。");
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
        if (encounter is null)
        {
            ShowStartMenu();
            return;
        }

        selectedRewardCards.Clear();
        openedRewardPack = null;
        var screen = ShowRewardScreen();
        screen.RenderPackSelection(encounter);
    }

    private async void OpenRewardPack(string packId)
    {
        if (isAnimating)
        {
            return;
        }

        isAnimating = true;
        try
        {
            if (screenHost.ActiveScreen is RewardScreen rewardScreen)
            {
                await rewardScreen.PlayPackOpenAsync(packId);
            }

            openedRewardPack = rewardService.OpenRewardPack(packId, content.RewardPacksById);
            selectedRewardCards.Clear();
            RenderOpenedRewardPack();
            if (screenHost.ActiveScreen is RewardScreen openedScreen)
            {
                await openedScreen.PlayOpenedCardsEntranceAsync();
            }
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
        finally
        {
            isAnimating = false;
        }
    }

    private void RenderOpenedRewardPack()
    {
        if (openedRewardPack is null)
        {
            ShowRewardSelection();
            return;
        }

        var screen = ShowRewardScreen();
        screen.RenderOpenedPack(openedRewardPack, selectedRewardCards);
    }

    private async void ToggleRewardCard(string cardId)
    {
        if (isAnimating)
        {
            return;
        }

        isAnimating = true;
        try
        {
            var picked = selectedRewardCards.Add(cardId);
            if (!picked)
            {
                selectedRewardCards.Remove(cardId);
            }

            RenderOpenedRewardPack();
            if (screenHost.ActiveScreen is RewardScreen rewardScreen)
            {
                await rewardScreen.PlayRewardCardToggleAsync(cardId, picked);
            }
        }
        catch (Exception ex)
        {
            screenHost.ShowFatalError(ex);
        }
        finally
        {
            isAnimating = false;
        }
    }

    private void ConfirmReward()
    {
        if (run is null || encounter is null)
        {
            ShowStartMenu();
            return;
        }

        if (openedRewardPack is not null)
        {
            run = rewardService.ClaimCards(run, openedRewardPack, selectedRewardCards);
        }

        run = rewardService.GrantEncounterRelic(run, encounter, content.RelicsById);
        run = runProgressService.AdvanceAfterRewards(run, encounter);
        StartCurrentEncounter();
    }

    private RewardScreen ShowRewardScreen()
    {
        var screen = screenHost.ShowScreen<RewardScreen>(RewardScenePath);
        screen.RewardPackRequested += OpenRewardPack;
        screen.RewardCardToggled += ToggleRewardCard;
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
}
