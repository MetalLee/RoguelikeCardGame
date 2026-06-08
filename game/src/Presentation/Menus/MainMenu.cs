using Godot;
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
using RoguelikeCardGame.Presentation.Rewards;

namespace RoguelikeCardGame.Presentation.Menus;

public partial class MainMenu : Control
{
    private const string StartMenuScenePath = "res://scenes/menus/StartMenuScreen.tscn";
    private const string BattleScenePath = "res://scenes/battle/BattleScreen.tscn";
    private const string RewardScenePath = "res://scenes/rewards/RewardScreen.tscn";
    private const string RunResultScenePath = "res://scenes/menus/RunResultScreen.tscn";

    private readonly RunStateFactory runFactory = new();
    private readonly RunProgressService runProgressService = new();
    private readonly CombatStateFactory combatFactory = new();
    private readonly CombatTurnService turnService = new();
    private readonly CardPlayService cardPlayService = new();
    private readonly RewardService rewardService = new();

    private readonly Dictionary<string, Texture2D> textureCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Font> fontCache = new(StringComparer.Ordinal);
    private readonly HashSet<string> selectedRewardCards = new(StringComparer.Ordinal);

    private GameContent? content;
    private RunState? run;
    private CombatState? combat;
    private EncounterDefinition? encounter;
    private string? selectedEnemyInstanceId;
    private int actionCardsPlayedThisTurn;
    private RewardPackDefinition? openedRewardPack;
    private Control? activeScreen;

    public override void _Ready()
    {
        try
        {
            content = GameContent.LoadFromProject();
            ShowStartMenu();
        }
        catch (Exception ex)
        {
            ShowFatalError(ex);
        }
    }

    private void ShowStartMenu()
    {
        var screen = ShowScreen<StartMenuScreen>(StartMenuScenePath);
        screen.StartRequested += StartNewRun;
        screen.Render();
    }

    private void StartNewRun()
    {
        RequireContent();
        var sequence = content!.MvpRun;
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
        RequireContent();
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
        encounter = content!.EncountersById[encounterId];
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

        var screen = ShowScreen<BattleScreen>(BattleScenePath);
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

    private void PlayCard(string cardId)
    {
        RequireContent();
        if (combat is null)
        {
            return;
        }

        var card = content!.CardsById[cardId];
        var result = cardPlayService.PlayCard(combat, card, ResolveTargetFor(card));
        combat = result.Combat;

        if (!result.Succeeded)
        {
            ShowBattle(BattleScreen.FailureText(result));
            return;
        }

        if (card.Type == CardType.Action)
        {
            ApplyFirstActionRelicIfNeeded(card.Id);
            actionCardsPlayedThisTurn++;
        }

        selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
        if (combat.Status == CombatStatus.Victory)
        {
            ResolveCombatVictory();
            return;
        }

        ShowBattle();
    }

    private void ApplyFirstActionRelicIfNeeded(string cardId)
    {
        if (run is null || combat is null || content is null || actionCardsPlayedThisTurn != 0)
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

    private void EndTurn()
    {
        RequireContent();
        if (combat is null || combat.Status != CombatStatus.PlayerTurn)
        {
            return;
        }

        combat = turnService.EndPlayerTurn(combat);
        combat = turnService.ResolveEnemyTurn(combat, content!.EnemiesById);

        if (combat.Status == CombatStatus.Defeat)
        {
            run = runProgressService.ApplyCombatResult(run!, combat, encounter);
            ShowRunResult();
            return;
        }

        combat = turnService.PrepareNextPlayerTurn(combat);
        selectedEnemyInstanceId = combat.Enemies.FirstOrDefault(enemy => enemy.CurrentHp > 0)?.InstanceId;
        actionCardsPlayedThisTurn = 0;
        ShowBattle("敌人行动已结算，新回合开始。");
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

    private void OpenRewardPack(string packId)
    {
        RequireContent();
        openedRewardPack = rewardService.OpenRewardPack(packId, content!.RewardPacksById);
        selectedRewardCards.Clear();
        RenderOpenedRewardPack();
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

    private void ToggleRewardCard(string cardId)
    {
        if (!selectedRewardCards.Add(cardId))
        {
            selectedRewardCards.Remove(cardId);
        }

        RenderOpenedRewardPack();
    }

    private void ConfirmReward()
    {
        if (run is null || encounter is null || content is null)
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
        var screen = ShowScreen<RewardScreen>(RewardScenePath);
        screen.RewardPackRequested += OpenRewardPack;
        screen.RewardCardToggled += ToggleRewardCard;
        screen.ConfirmRequested += ConfirmReward;
        return screen;
    }

    private void ShowRunResult()
    {
        var screen = ShowScreen<RunResultScreen>(RunResultScenePath);
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

    private T ShowScreen<T>(string scenePath)
        where T : Control
    {
        RequireContent();
        ClearActiveScreen();
        var packedScene = GD.Load<PackedScene>(scenePath)
            ?? throw new InvalidOperationException($"Scene '{scenePath}' could not be loaded.");
        var screen = packedScene.Instantiate<T>();
        if (screen is Shared.ComicScreen comicScreen)
        {
            comicScreen.Initialize(content!, textureCache, fontCache);
        }

        screen.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(screen);
        activeScreen = screen;
        return screen;
    }

    private void ShowFatalError(Exception ex)
    {
        ClearActiveScreen();
        var label = new Label
        {
            Text = ex.ToString(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        label.AddThemeColorOverride("font_color", Colors.Red);
        AddChild(label);
        activeScreen = label;
    }

    private void ClearActiveScreen()
    {
        if (activeScreen is null)
        {
            foreach (var child in GetChildren())
            {
                if (child is Node node)
                {
                    RemoveChild(node);
                    node.QueueFree();
                }
            }

            return;
        }

        RemoveChild(activeScreen);
        activeScreen.QueueFree();
        activeScreen = null;
    }

    private void RequireContent()
    {
        if (content is null)
        {
            throw new InvalidOperationException("Game content is not loaded.");
        }
    }
}
