using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Application.Rewards;
using RoguelikeCardGame.Application.Runs;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Rewards;
using RoguelikeCardGame.Domain.Runs;
using RoguelikeCardGame.Infrastructure.Content;
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
    private readonly CombatStateFactory combatFactory = new();
    private readonly CombatTurnService turnService = new();
    private readonly CardPlayService cardPlayService = new();
    private readonly RewardService rewardService = new();
    private readonly HashSet<string> selectedRewardCards = new(StringComparer.Ordinal);

    private RunState? run;
    private CombatState? combat;
    private EncounterDefinition? encounter;
    private string? selectedEnemyInstanceId;
    private int actionCardsPlayedThisTurn;
    private RewardPackDefinition? openedRewardPack;
    private bool isAnimating;

    public MvpRunFlowController(GameContent content, SceneScreenHost screenHost)
    {
        this.content = content;
        this.screenHost = screenHost;
    }

    public void ShowStartMenu()
    {
        try
        {
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
        var sequence = content.MvpRun;
        run = runFactory.CreateNewRun(
            runId: $"run_{DateTime.UtcNow:yyyyMMddHHmmss}",
            seed: 12345,
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
        selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
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
        screen.EnemySelected += SelectEnemy;
        screen.CardRequested += PlayCard;
        screen.EndTurnRequested += EndTurn;
        screen.RestartRequested += StartNewRun;
        screen.Render(combat, run, encounter, selectedEnemyInstanceId, message);
    }

    private void SelectEnemy(string enemyInstanceId)
    {
        selectedEnemyInstanceId = enemyInstanceId;
        ShowBattle();
    }

    private async void PlayCard(string cardId, int handIndex)
    {
        if (isAnimating)
        {
            return;
        }

        isAnimating = true;
        try
        {
            if (combat is null)
            {
                return;
            }

            var animationScreen = screenHost.ActiveScreen as BattleScreen;
            var card = content.CardsById[cardId];
            var result = cardPlayService.PlayCard(combat, card, ResolveTargetFor(card), handIndex);
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

            await PlayBattleAnimationsAsync(animationScreen, eventsToAnimate, card, handIndex);

            selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
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
            isAnimating = false;
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
        if (isAnimating)
        {
            return;
        }

        isAnimating = true;
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
                run = runProgressService.ApplyCombatResult(run!, combat, encounter);
                ShowRunResult();
                return;
            }

            combat = turnService.PrepareNextPlayerTurn(combat);
            var eventsToAnimate = combat.Log.Skip(startLogCount).ToList();
            await PlayBattleAnimationsAsync(animationScreen, eventsToAnimate, null, null);

            selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
            actionCardsPlayedThisTurn = 0;
            ShowBattle("敌人行动已结算，新回合开始。");
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
        var screen = screenHost.ShowScreen<RunResultScreen>(RunResultScenePath);
        screen.RestartRequested += StartNewRun;
        screen.RenderRunResult(run);
    }

    private string? ResolveTargetFor(CardDefinition card)
    {
        if (card.TargetRule != TargetRule.SingleEnemy || combat is null)
        {
            return null;
        }

        var selected = combat.Enemies.FirstOrDefault(enemy =>
            enemy.InstanceId == selectedEnemyInstanceId && enemy.CurrentHp > 0);
        return selected?.InstanceId ?? combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
    }

    private static async Task PlayBattleAnimationsAsync(
        BattleScreen? screen,
        IReadOnlyList<CombatLogEvent> eventsToAnimate,
        CardDefinition? playedCard,
        int? playedHandIndex)
    {
        if (screen is null || eventsToAnimate.Count == 0)
        {
            return;
        }

        await screen.PlayLogAnimationsAsync(eventsToAnimate, playedCard, playedHandIndex);
    }
}
