# Beat Slot Targeting UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the beat combat prototype playable by allowing action cards to be dragged into player beat slots, then targeted at visible enemy beats or enemy bodies.

**Revision note (2026-06-21):** The original plan assumed the player would place a card, then click the filled player beat slot to start targeting. Current confirmed interaction supersedes that detail: releasing an action card on an empty player beat slot immediately places the card and starts a temporary targeting arrow from that slot. Right-clicking or clicking empty space cancels this temporary targeting and returns the newly placed card to hand. See `docs/superpowers/specs/2026-06-21-beat-placement-auto-target-design.md`.

**Architecture:** Add a small application service for immutable beat-round planning changes so drag/drop UI does not own rules. `BattleScreen` owns hit-testing for player beat slots, enemy beat slots, enemy body targets, and the targeting arrow. `MvpRunFlowController` receives UI intents, updates `CombatState`, and keeps existing `ResolveBeatRound` as the final rules authority.

**Tech Stack:** Godot 4.6 .NET / C#, existing `game/tests/Unit/Program.cs` smoke tests, generated Godot UI controls, existing `BeatCombatService`.

---

## File Structure

- Create `game/src/Application/Battle/BeatRoundPlanningService.cs`
  - Places action cards into player beat slots.
  - Removes slotted card instances from hand.
  - Assigns enemy beat or enemy body targets.
  - Discards slotted action card instances after a beat round resolves.

- Modify `game/tests/Unit/Program.cs`
  - Adds TDD coverage for placing a card into a beat slot, target assignment, body-target prerequisite, and discarding slotted cards after resolution.

- Modify `game/src/Presentation/Battle/BattleHandView.cs`
  - In beat prototype mode, restores dragging for action cards.
  - Emits a beat-drop request instead of old AP card play.

- Modify `game/src/Presentation/Battle/BattleScreen.cs`
  - Maintains player beat slot hit boxes.
  - Renders enemy beat slots and locked states.
  - Shows target arrow from pending player beat.
  - Emits beat card drop and beat target requests to the flow controller.

- Modify `game/src/Presentation/Flow/MvpRunFlowController.cs`
  - Handles beat card drop and target requests.
  - Uses `BeatRoundPlanningService`.
  - Discards slotted action cards after beat round resolution before preparing the next beat round.

## Task 1: Beat Round Planning Service

**Files:**
- Create: `game/src/Application/Battle/BeatRoundPlanningService.cs`
- Modify: `game/tests/Unit/Program.cs`

- [ ] **Step 1: Write failing tests for placing action cards and assigning targets**

Add this test block near the existing beat-round tests in `game/tests/Unit/Program.cs`:

```csharp
var beatPlanning = new BeatRoundPlanningService();
var planningCombat = CreatePlayableCombat(
    [beatSlashCard.Id, guardAction.Id, beatSlashCard.Id],
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

var discardAfterBeat = beatPlanning.DiscardSlottedActionCards(bodyTargetedBeat);
Assert(discardAfterBeat.DeckZones.DiscardPile.Contains(planningCombat.DeckZones.Hand[0]), "Beat planning discards slotted action card instances after beat resolution");
```

Add this small helper near other test helpers:

```csharp
private sealed class BeatRoundFactoryForTest
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
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: compile failure because `BeatRoundPlanningService` does not exist.

- [ ] **Step 3: Implement `BeatRoundPlanningService`**

Create `game/src/Application/Battle/BeatRoundPlanningService.cs`:

```csharp
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Application.Battle;

public sealed class BeatRoundPlanningService
{
    public CombatState PlaceActionCardInBeat(
        CombatState combat,
        string cardInstanceId,
        string cardId,
        int handIndex,
        int beatIndex,
        CardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(card);
        if (combat.Status != CombatStatus.PlayerTurn)
        {
            throw new InvalidOperationException("Beat cards can only be placed during the player turn.");
        }

        if (combat.BeatRound is null)
        {
            throw new InvalidOperationException("Combat has no beat round.");
        }

        if (card.Type != CardType.Action)
        {
            throw new InvalidOperationException($"Only action cards can be placed in beat slots. Card '{card.Id}' is '{card.Type}'.");
        }

        if (handIndex < 0 || handIndex >= combat.DeckZones.Hand.Count)
        {
            throw new InvalidOperationException($"Hand index {handIndex} is outside the current hand.");
        }

        if (!string.Equals(combat.DeckZones.Hand[handIndex], cardInstanceId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Card instance '{cardInstanceId}' is not at hand index {handIndex}.");
        }

        var playerBeats = combat.BeatRound.PlayerBeats.ToList();
        var slotIndex = playerBeats.FindIndex(beat => beat.BeatIndex == beatIndex);
        if (slotIndex < 0)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} does not exist.");
        }

        var existing = playerBeats[slotIndex];
        if (!string.IsNullOrWhiteSpace(existing.CardInstanceId) || !string.IsNullOrWhiteSpace(existing.CardId))
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} already has a card.");
        }

        playerBeats[slotIndex] = existing with
        {
            CardInstanceId = cardInstanceId,
            CardId = cardId,
            Target = null
        };

        var hand = combat.DeckZones.Hand.ToList();
        hand.RemoveAt(handIndex);

        return combat with
        {
            BeatRound = combat.BeatRound with { PlayerBeats = playerBeats },
            DeckZones = combat.DeckZones with { Hand = hand }
        };
    }

    public CombatState SetEnemyBeatTarget(
        CombatState combat,
        int beatIndex,
        string enemyInstanceId,
        int enemyBeatIndex,
        BeatCombatService validator)
    {
        return SetTarget(
            combat,
            beatIndex,
            new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBeat,
                EnemyInstanceId = enemyInstanceId,
                EnemyBeatIndex = enemyBeatIndex
            },
            validator);
    }

    public CombatState SetEnemyBodyTarget(
        CombatState combat,
        int beatIndex,
        string enemyInstanceId,
        BeatCombatService validator)
    {
        return SetTarget(
            combat,
            beatIndex,
            new BeatTarget
            {
                Kind = BeatTargetKind.EnemyBody,
                EnemyInstanceId = enemyInstanceId
            },
            validator);
    }

    public CombatState DiscardSlottedActionCards(CombatState combat)
    {
        if (combat.BeatRound is null)
        {
            return combat;
        }

        var slotted = combat.BeatRound.PlayerBeats
            .Select(beat => beat.CardInstanceId)
            .Where(cardInstanceId => !string.IsNullOrWhiteSpace(cardInstanceId))
            .Cast<string>()
            .ToList();
        if (slotted.Count == 0)
        {
            return combat;
        }

        return combat with
        {
            DeckZones = combat.DeckZones with
            {
                DiscardPile = combat.DeckZones.DiscardPile.Concat(slotted).ToList()
            }
        };
    }

    private static CombatState SetTarget(
        CombatState combat,
        int beatIndex,
        BeatTarget target,
        BeatCombatService validator)
    {
        ArgumentNullException.ThrowIfNull(combat);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(validator);
        if (combat.BeatRound is null)
        {
            throw new InvalidOperationException("Combat has no beat round.");
        }

        var playerBeats = combat.BeatRound.PlayerBeats.ToList();
        var slotIndex = playerBeats.FindIndex(beat => beat.BeatIndex == beatIndex);
        if (slotIndex < 0)
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} does not exist.");
        }

        var slot = playerBeats[slotIndex];
        if (string.IsNullOrWhiteSpace(slot.CardId) || string.IsNullOrWhiteSpace(slot.CardInstanceId))
        {
            throw new InvalidOperationException($"Player beat index {beatIndex} has no action card.");
        }

        playerBeats[slotIndex] = slot with { Target = target };
        var updated = combat with
        {
            BeatRound = combat.BeatRound with { PlayerBeats = playerBeats }
        };
        var validation = validator.ValidatePlayerBeatTargets(updated.BeatRound, updated);
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException(validation.Message ?? $"Invalid beat target: {validation.FailureReason}");
        }

        return updated;
    }
}
```

- [ ] **Step 4: Run tests to verify GREEN**

Run:

```bash
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
dotnet build game/RoguelikeCardGame.csproj
```

Expected: tests pass and build succeeds.

- [ ] **Step 5: Commit**

```bash
git add game/src/Application/Battle/BeatRoundPlanningService.cs game/tests/Unit/Program.cs
git commit -m "feat: add beat round planning service"
```

## Task 2: Drag Action Cards Into Player Beat Slots

**Files:**
- Modify: `game/src/Presentation/Battle/BattleHandView.cs`
- Modify: `game/src/Presentation/Battle/BattleScreen.cs`
- Modify: `game/src/Presentation/Flow/MvpRunFlowController.cs`

- [ ] **Step 1: Add beat drop events to `BattleHandView` and `BattleScreen`**

Modify `BattleHandView`:

```csharp
public event Action<string, string, int, Vector2>? BeatCardDropRequested;
```

In `OnCardGuiInput`, replace the beat-prototype short-circuit with:

```csharp
if (beatPrototypeMode && card.Card.Type != CardType.Action)
{
    FeedbackRequested?.Invoke("终结牌暂时只能显示在终结槽，不能放入三拍槽");
    GetViewport().SetInputAsHandled();
    return;
}
```

In `CompleteCardDrag`, add this as the first branch after `var viewportMouse = GetViewport().GetMousePosition();`:

```csharp
if (beatPrototypeMode)
{
    var isAction = card.Card.Type == CardType.Action;
    CleanupDragVisuals(restoreCard: true);
    if (!isAction)
    {
        FeedbackRequested?.Invoke("终结牌暂时只能显示在终结槽，不能放入三拍槽");
        return;
    }

    BeatCardDropRequested?.Invoke(card.CardInstanceId, card.CardId, card.HandIndex, viewportMouse);
    return;
}
```

Modify `BattleScreen` events:

```csharp
public event Action<string, string, int, int>? BeatCardDroppedOnSlot;
```

Subscribe in `Render` after `battleHandView.FeedbackRequested += ShowDragFeedback;`:

```csharp
battleHandView.BeatCardDropRequested += DropBeatCardFromHand;
```

Add method:

```csharp
private void DropBeatCardFromHand(string cardInstanceId, string cardId, int handIndex, Vector2 viewportMouse)
{
    var beatIndex = PlayerBeatIndexUnderMouse(viewportMouse);
    if (beatIndex is null)
    {
        ShowDragFeedback("需要把行动牌拖到一个空的玩家拍位");
        return;
    }

    BeatCardDroppedOnSlot?.Invoke(cardInstanceId, cardId, handIndex, beatIndex.Value);
}
```

- [ ] **Step 2: Add player beat slot hit testing in `BattleScreen`**

Add field:

```csharp
private readonly Dictionary<int, Control> playerBeatSlotNodes = new();
```

Clear it at the start of `Render`:

```csharp
playerBeatSlotNodes.Clear();
```

In `CreateBeatSlot`, register the node before returning:

```csharp
playerBeatSlotNodes[beat.BeatIndex] = slot;
```

Add hit-test helpers:

```csharp
private int? PlayerBeatIndexUnderMouse(Vector2 viewportPoint)
{
    foreach (var (beatIndex, node) in playerBeatSlotNodes.OrderByDescending(pair => pair.Key))
    {
        if (IsPointInsideControl(node, viewportPoint))
        {
            return beatIndex;
        }
    }

    return null;
}

private static bool IsPointInsideControl(Control control, Vector2 viewportPoint)
{
    if (!GodotObject.IsInstanceValid(control) || !control.Visible)
    {
        return false;
    }

    var local = control.GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
    return new Rect2(Vector2.Zero, control.Size).HasPoint(local);
}
```

- [ ] **Step 3: Wire beat card placement in `MvpRunFlowController`**

Add field:

```csharp
private readonly BeatRoundPlanningService beatPlanningService = new();
```

In `ShowBattle`, subscribe:

```csharp
screen.BeatCardDroppedOnSlot += PlaceBeatCard;
```

Add method:

```csharp
private void PlaceBeatCard(string cardInstanceId, string cardId, int handIndex, int beatIndex)
{
    try
    {
        if (combat is null)
        {
            return;
        }

        combat = beatPlanningService.PlaceActionCardInBeat(
            combat,
            cardInstanceId,
            cardId,
            handIndex,
            beatIndex,
            content.CardsById[cardId]);
        ShowBattle();
    }
    catch (Exception ex)
    {
        ShowBattle(ex.Message);
    }
}
```

- [ ] **Step 4: Run build and smoke tests**

Run:

```bash
dotnet build game/RoguelikeCardGame.csproj
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
```

Expected: build and tests pass.

- [ ] **Step 5: Commit**

```bash
git add game/src/Presentation/Battle/BattleHandView.cs game/src/Presentation/Battle/BattleScreen.cs game/src/Presentation/Flow/MvpRunFlowController.cs
git commit -m "feat: drag action cards into beat slots"
```

## Task 3: Render Enemy Beat Slots and Assign Targets

**Files:**
- Modify: `game/src/Presentation/Battle/BattleScreen.cs`
- Modify: `game/src/Presentation/Flow/MvpRunFlowController.cs`

- [ ] **Step 1: Add enemy beat target events and hit boxes**

In `BattleScreen`, add event and fields:

```csharp
public event Action<int, BeatTarget>? BeatTargetSelected;

private readonly Dictionary<(string EnemyInstanceId, int BeatIndex), Control> enemyBeatSlotNodes = new();
private readonly Dictionary<string, Control> enemyBodyTargetNodes = new(StringComparer.Ordinal);
```

Clear them at render start:

```csharp
enemyBeatSlotNodes.Clear();
enemyBodyTargetNodes.Clear();
```

- [ ] **Step 2: Render enemy beat slots**

After `battleEnemyView.Render(...)`, call:

```csharp
CreateEnemyBeatTargets(root, combat, battleEnemyView);
```

Add:

```csharp
private void CreateEnemyBeatTargets(Control root, CombatState combatState, BattleEnemyView enemyView)
{
    if (combatState.BeatRound is null)
    {
        return;
    }

    foreach (var group in combatState.BeatRound.EnemyBeats.GroupBy(beat => beat.EnemyInstanceId))
    {
        if (!enemyView.EnemyNodes.TryGetValue(group.Key, out var enemyNode))
        {
            continue;
        }

        var beats = group.OrderBy(beat => beat.BeatIndex).ToList();
        var row = new HBoxContainer
        {
            ZIndex = 95,
            MouseFilter = MouseFilterEnum.Pass
        };
        row.AddThemeConstantOverride("separation", 8);
        foreach (var beat in beats)
        {
            var locked = IsEnemyBeatLocked(combatState.BeatRound, beat.EnemyInstanceId, beat.BeatIndex);
            var slot = CreateEnemyBeatSlot(beat, locked);
            enemyBeatSlotNodes[(beat.EnemyInstanceId, beat.BeatIndex)] = slot;
            row.AddChild(slot);
        }

        var width = beats.Count * 118 + Math.Max(0, beats.Count - 1) * 8;
        var enemyTop = enemyNode.Position.Y;
        var position = new Vector2(enemyNode.Position.X + (enemyNode.Size.X - width) * 0.5f, Math.Max(258, enemyTop - 104));
        AddAt(root, row, position, new Vector2(width, 70));

        enemyBodyTargetNodes[group.Key] = enemyNode;
    }
}
```

Add slot helpers:

```csharp
private Control CreateEnemyBeatSlot(EnemyBeatSlot beat, bool locked)
{
    var slot = new PanelContainer
    {
        CustomMinimumSize = new Vector2(118, 64),
        MouseFilter = MouseFilterEnum.Pass,
        TooltipText = locked ? "已接招" : $"魔物第 {beat.BeatIndex + 1} 拍"
    };
    slot.AddThemeStyleboxOverride("panel", CreateButtonStyle(locked ? new Color(0.45f, 0.45f, 0.45f) : FinisherLine, 0.78f));

    var label = CreateSmallLabel(locked ? "已接招" : EnemyBeatText(beat));
    label.HorizontalAlignment = HorizontalAlignment.Center;
    label.VerticalAlignment = VerticalAlignment.Center;
    label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
    label.ClipText = true;
    label.AddThemeFontSizeOverride("font_size", 16);
    slot.AddChild(label);
    return slot;
}

private static string EnemyBeatText(EnemyBeatSlot beat)
{
    if (beat.Hidden)
    {
        return "隐藏";
    }

    if (beat.Actions.Count == 0)
    {
        return "空拍";
    }

    return string.Join(" -> ", beat.Actions.Select(BeatActionText));
}

private static string BeatActionText(BeatActionDefinition action)
{
    var text = action.Kind switch
    {
        BeatActionKind.Attack => $"{AttackTypeText(action.AttackType)} {action.Value}",
        BeatActionKind.Block => $"格挡 {action.Value}",
        BeatActionKind.Dodge => $"闪避 {action.DodgeChancePercent}%",
        _ => action.Kind.ToString()
    };
    return action.Repeat > 1 ? $"{text} x{action.Repeat}" : text;
}

private static string AttackTypeText(BeatAttackType? attackType)
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

- [ ] **Step 3: Show target arrow from pending player beat**

In `BattleScreen._Process`, add:

```csharp
public override void _Process(double delta)
{
    UpdateBeatTargetingArrow();
}
```

Add:

```csharp
private void UpdateBeatTargetingArrow()
{
    if (combat?.BeatRound is null || targetingOverlay is null)
    {
        return;
    }

    var pending = combat.BeatRound.PlayerBeats
        .OrderBy(beat => beat.BeatIndex)
        .FirstOrDefault(beat => !string.IsNullOrWhiteSpace(beat.CardId) && beat.Target is null);
    if (pending is null || !playerBeatSlotNodes.TryGetValue(pending.BeatIndex, out var source))
    {
        targetingOverlay.HideArrow();
        return;
    }

    var mouse = GetViewport().GetMousePosition();
    var isValid = TargetUnderMouse(mouse, pending.BeatIndex) is not null;
    var start = source.GetGlobalTransformWithCanvas() * new Vector2(source.Size.X * 0.5f, source.Size.Y * 0.1f);
    targetingOverlay.ShowArrowFromViewport(start, mouse, isValid);
}
```

Add click handling in `_UnhandledInput` before the F12 return:

```csharp
if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton &&
    TrySelectBeatTarget(mouseButton.Position))
{
    GetViewport().SetInputAsHandled();
    return;
}
```

Add target helpers:

```csharp
private bool TrySelectBeatTarget(Vector2 viewportPoint)
{
    if (combat?.BeatRound is null)
    {
        return false;
    }

    var pending = combat.BeatRound.PlayerBeats
        .OrderBy(beat => beat.BeatIndex)
        .FirstOrDefault(beat => !string.IsNullOrWhiteSpace(beat.CardId) && beat.Target is null);
    if (pending is null)
    {
        return false;
    }

    var target = TargetUnderMouse(viewportPoint, pending.BeatIndex);
    if (target is null)
    {
        return false;
    }

    BeatTargetSelected?.Invoke(pending.BeatIndex, target);
    return true;
}

private BeatTarget? TargetUnderMouse(Vector2 viewportPoint, int playerBeatIndex)
{
    if (combat?.BeatRound is null)
    {
        return null;
    }

    foreach (var (key, node) in enemyBeatSlotNodes)
    {
        if (!IsPointInsideControl(node, viewportPoint) || IsEnemyBeatLocked(combat.BeatRound, key.EnemyInstanceId, key.BeatIndex))
        {
            continue;
        }

        return new BeatTarget
        {
            Kind = BeatTargetKind.EnemyBeat,
            EnemyInstanceId = key.EnemyInstanceId,
            EnemyBeatIndex = key.BeatIndex
        };
    }

    foreach (var (enemyInstanceId, node) in enemyBodyTargetNodes)
    {
        if (!IsPointInsideControl(node, viewportPoint) || !AllEnemyBeatsLocked(combat.BeatRound, enemyInstanceId))
        {
            continue;
        }

        return new BeatTarget
        {
            Kind = BeatTargetKind.EnemyBody,
            EnemyInstanceId = enemyInstanceId
        };
    }

    return null;
}

private static bool IsEnemyBeatLocked(BeatRoundState round, string enemyInstanceId, int enemyBeatIndex)
{
    return round.PlayerBeats.Any(beat =>
        beat.Target?.Kind == BeatTargetKind.EnemyBeat &&
        string.Equals(beat.Target.EnemyInstanceId, enemyInstanceId, StringComparison.Ordinal) &&
        beat.Target.EnemyBeatIndex == enemyBeatIndex);
}

private static bool AllEnemyBeatsLocked(BeatRoundState round, string enemyInstanceId)
{
    var enemyBeats = round.EnemyBeats
        .Where(beat => string.Equals(beat.EnemyInstanceId, enemyInstanceId, StringComparison.Ordinal))
        .ToList();
    return enemyBeats.Count == 0 || enemyBeats.All(beat => IsEnemyBeatLocked(round, enemyInstanceId, beat.BeatIndex));
}
```

- [ ] **Step 4: Wire target assignment in flow controller**

In `ShowBattle`, subscribe:

```csharp
screen.BeatTargetSelected += SetBeatTarget;
```

Add method:

```csharp
private void SetBeatTarget(int beatIndex, BeatTarget target)
{
    try
    {
        if (combat is null)
        {
            return;
        }

        var validator = new BeatCombatService();
        combat = target.Kind == BeatTargetKind.EnemyBody
            ? beatPlanningService.SetEnemyBodyTarget(combat, beatIndex, target.EnemyInstanceId, validator)
            : beatPlanningService.SetEnemyBeatTarget(combat, beatIndex, target.EnemyInstanceId, target.EnemyBeatIndex ?? 0, validator);
        ShowBattle();
    }
    catch (Exception ex)
    {
        ShowBattle(ex.Message);
    }
}
```

- [ ] **Step 5: Run build and smoke tests**

Run:

```bash
dotnet build game/RoguelikeCardGame.csproj
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
godot-mono --headless --path game --quit
```

Expected: build and tests pass. Godot exits 0; existing font import errors may still appear.

- [ ] **Step 6: Commit**

```bash
git add game/src/Presentation/Battle/BattleScreen.cs game/src/Presentation/Flow/MvpRunFlowController.cs
git commit -m "feat: target enemy beats from player beats"
```

## Task 4: Finish Beat Turn Cleanup and Documentation

**Files:**
- Modify: `game/src/Presentation/Flow/MvpRunFlowController.cs`
- Modify: `design/03_experience/00_ui_ux.md`

- [ ] **Step 1: Use cleanup after `ResolveBeatRound`**

In `MvpRunFlowController.EndBeatTurn`, after:

```csharp
combat = result.Combat;
```

add:

```csharp
combat = beatPlanningService.DiscardSlottedActionCards(combat);
```

Keep this before victory/defeat handling and before `PrepareNextBeatRound`.

- [ ] **Step 2: Update UI documentation**

Append to `design/03_experience/00_ui_ux.md` under the three-beat UI section. The original text below has been superseded by the 2026-06-21 auto-targeting revision:

```markdown
### 三拍槽位瞄准交互

第一版可玩原型采用“两步式连续操作”：玩家先将行动牌拖入空的玩家拍位并松开鼠标，系统立即以该拍位作为临时锚点拉出箭头，等待玩家选择目标。若魔物仍有未锁定拍位，只能选择未锁定魔物拍作为对撞目标；当该魔物所有拍位都已锁定后，才允许从已填入行动牌的玩家拍位选择魔物本体。手牌不允许直接拖到魔物拍或魔物本体。若玩家在临时瞄准期间右键取消或点击空白区域，本次刚放入的行动牌退回手牌，该玩家拍位清空。
```

- [ ] **Step 3: Run final verification**

Run:

```bash
python3 game/tools/data_validator/validate_data.py
dotnet run --project game/tests/Unit/RoguelikeCardGame.Tests.csproj
dotnet build game/RoguelikeCardGame.csproj
godot-mono --headless --path game --quit
git diff --check HEAD
```

Expected:
- Data validator passes.
- Tests pass.
- Build passes with 0 warnings/errors.
- Godot exits 0; existing font import errors may still appear.
- `git diff --check HEAD` has no output.

- [ ] **Step 4: Commit**

```bash
git add game/src/Presentation/Flow/MvpRunFlowController.cs game/tests/Unit/Program.cs design/03_experience/00_ui_ux.md
git commit -m "docs: record beat slot targeting interaction"
```
