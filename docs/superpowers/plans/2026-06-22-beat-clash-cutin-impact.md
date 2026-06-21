# Beat Clash Cut-In Impact Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add MVP comic cut-in impact for beat clash resolution: play I/II/III beat clashes in order, keep same-enemy actions continuous, return to the player start before switching enemies, and show damage/energy feedback.

**Architecture:** Keep combat rules authoritative and presentation-only animation separate. Add a pure C# `BeatClashAnimationPlanner` that converts `CombatLogEvent` lists into ordered animation steps, then let `BattleLogAnimator` delegate beat-round events to a `BattleScreen` cut-in callback. Build `BeatClashCutInLayer` as a temporary Godot overlay that uses existing textures, labels, tweens, and VFX assets.

**Tech Stack:** Godot 4 C#, existing `ComicScreen` helpers, existing unit smoke test project, existing `game/data/presentation/assets.json` resources.

---

## File Structure

- Modify: `game/tests/Unit/Program.cs`
  Add unit coverage for beat log ordering, animation step extraction, same-enemy continuity, target switching, and energy-event binding.
- Create: `game/src/Presentation/Battle/BeatClashAnimationPlanner.cs`
  Pure C# translation from combat logs to `BeatClashAnimationStep` records.
- Create: `game/src/Presentation/Battle/BeatClashCutInLayer.cs`
  Temporary Godot overlay responsible for close-up visuals, movement, damage numbers, and continuity.
- Modify: `game/src/Presentation/Battle/BattleLogAnimator.cs`
  Detect `BeatActionResolved` events and delegate them to the cut-in callback instead of using the older generic damage animation.
- Modify: `game/src/Presentation/Battle/BattleScreen.cs`
  Provide the cut-in callback and create/remove the overlay during battle log playback.
- Modify: `game/src/Presentation/Shared/ComicScreen.cs`
  Expose small internal animation helpers needed by the cut-in layer, without moving rule logic into presentation nodes.
- Modify: `design/01_core_gameplay/02_combat_system.md`
  Record that beat clash presentation follows I/II/III and same-target continuity.
- Modify: `design/03_experience/01_visual_direction.md`
  Record the MVP cut-in overlay visual rule.
- Modify: `design/08_governance/01_change_log.md`
  Record the implemented impact enhancement.

---

### Task 1: Beat Clash Planner Tests

**Files:**
- Modify: `game/tests/Unit/Program.cs`
- Test: `game/tests/Unit/Program.cs`

- [ ] **Step 1: Write failing planner tests**

Add this block after the existing beat connector and targeting assertions, near the current beat UI tests around the `BeatSlotPresentationGeometry.RomanBeatNumber` assertions:

```csharp
var beatClashPlanner = new RoguelikeCardGame.Presentation.Battle.BeatClashAnimationPlanner();
var beatClashSteps = beatClashPlanner.BuildSteps(
[
    CreateBeatActionResolvedEvent(
        eventId: "beat_2",
        beatIndex: 2,
        cardId: "card.beat_slash",
        cardInstanceId: "instance_2",
        enemyInstanceId: "enemy_b",
        targetKind: BeatTargetKind.EnemyBeat,
        enemyBeatIndex: 0,
        enemyDamage: 5,
        playerDamage: 0,
        successfulPlayerActions: 1),
    CreateBeatActionResolvedEvent(
        eventId: "beat_0",
        beatIndex: 0,
        cardId: "card.beat_slash",
        cardInstanceId: "instance_0",
        enemyInstanceId: "enemy_a",
        targetKind: BeatTargetKind.EnemyBeat,
        enemyBeatIndex: 1,
        enemyDamage: 8,
        playerDamage: 1,
        successfulPlayerActions: 1),
    CreateBeatEnergyGeneratedEvent(
        eventId: "energy_0",
        cardId: "card.beat_slash",
        cardInstanceId: "instance_0",
        generated: 1),
    CreateBeatActionResolvedEvent(
        eventId: "beat_1",
        beatIndex: 1,
        cardId: "card.arm_counter",
        cardInstanceId: "instance_1",
        enemyInstanceId: "enemy_a",
        targetKind: BeatTargetKind.EnemyBody,
        enemyBeatIndex: null,
        enemyDamage: 6,
        playerDamage: 0,
        successfulPlayerActions: 1)
]);
AssertSequenceEqual(new[] { 0, 1, 2 }, beatClashSteps.Select(step => step.BeatIndex), "Beat clash animation steps are sorted by left-side beat order");
AssertEqual("enemy_a", beatClashSteps[0].EnemyInstanceId, "First beat targets enemy A");
AssertEqual(false, beatClashSteps[0].ContinuesPreviousTarget, "First beat starts a cut-in segment");
AssertEqual(true, beatClashSteps[1].ContinuesPreviousTarget, "Consecutive beats against the same enemy continue the cut-in");
AssertEqual(false, beatClashSteps[2].ContinuesPreviousTarget, "Switching enemies starts a new cut-in segment");
AssertEqual(1, beatClashSteps[0].ColorEnergyGenerated, "Beat energy event binds to the matching card instance");
AssertEqual(0, beatClashSteps[1].ColorEnergyGenerated, "No matching energy event leaves generated energy at zero");
AssertEqual(BeatTargetKind.EnemyBody, beatClashSteps[1].TargetKind, "Enemy body target is preserved in animation step");
```

Add these helper functions near the existing test helper functions at the bottom of `Program.cs`:

```csharp
static CombatLogEvent CreateBeatActionResolvedEvent(
    string eventId,
    int beatIndex,
    string cardId,
    string cardInstanceId,
    string enemyInstanceId,
    BeatTargetKind targetKind,
    int? enemyBeatIndex,
    int enemyDamage,
    int playerDamage,
    int successfulPlayerActions)
{
    return new CombatLogEvent
    {
        EventId = eventId,
        EventType = CombatLogEventType.BeatActionResolved,
        TurnNumber = 1,
        SourceId = cardId,
        TargetIds = [enemyInstanceId],
        NumericChanges = new Dictionary<string, int>
        {
            ["beat_index"] = beatIndex,
            ["enemy_damage"] = enemyDamage,
            ["player_damage"] = playerDamage,
            ["successful_player_actions"] = successfulPlayerActions
        },
        Metadata = new Dictionary<string, string>
        {
            ["card_instance_id"] = cardInstanceId,
            ["target_kind"] = targetKind.ToString(),
            ["enemy_beat_index"] = enemyBeatIndex?.ToString() ?? string.Empty
        }
    };
}

static CombatLogEvent CreateBeatEnergyGeneratedEvent(
    string eventId,
    string cardId,
    string cardInstanceId,
    int generated)
{
    return new CombatLogEvent
    {
        EventId = eventId,
        EventType = CombatLogEventType.BeatEnergyGenerated,
        TurnNumber = 1,
        SourceId = cardId,
        TargetIds = ["color_energy_pool"],
        NumericChanges = new Dictionary<string, int>
        {
            ["color_energy_generated"] = generated,
            ["color_energy_after"] = generated
        },
        Metadata = new Dictionary<string, string>
        {
            ["color"] = ColorType.Colorless.ToString(),
            ["card_instance_id"] = cardInstanceId
        }
    };
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj
```

Expected: build fails with a message that `RoguelikeCardGame.Presentation.Battle.BeatClashAnimationPlanner` does not exist.

- [ ] **Step 3: Commit red test**

Do not commit the failing test separately unless working branch policy requires checkpoint commits. Keep the failing test in the working tree for Task 2.

---

### Task 2: Pure Beat Clash Animation Planner

**Files:**
- Create: `game/src/Presentation/Battle/BeatClashAnimationPlanner.cs`
- Test: `game/tests/Unit/Program.cs`

- [ ] **Step 1: Add the planner implementation**

Create `game/src/Presentation/Battle/BeatClashAnimationPlanner.cs`:

```csharp
using RoguelikeCardGame.Domain.Combat;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed record BeatClashAnimationStep
{
    public required string EventId { get; init; }
    public int BeatIndex { get; init; }
    public required string SourceCardId { get; init; }
    public required string CardInstanceId { get; init; }
    public required string EnemyInstanceId { get; init; }
    public BeatTargetKind TargetKind { get; init; }
    public int? EnemyBeatIndex { get; init; }
    public int EnemyDamage { get; init; }
    public int PlayerDamage { get; init; }
    public int SuccessfulPlayerActions { get; init; }
    public int ColorEnergyGenerated { get; init; }
    public bool ContinuesPreviousTarget { get; init; }
}

public sealed class BeatClashAnimationPlanner
{
    public IReadOnlyList<BeatClashAnimationStep> BuildSteps(IReadOnlyList<CombatLogEvent> events)
    {
        var energyByCardInstanceId = events
            .Where(item => item.EventType == CombatLogEventType.BeatEnergyGenerated)
            .Select(item => new
            {
                CardInstanceId = item.Metadata.GetValueOrDefault("card_instance_id", string.Empty),
                Generated = item.NumericChanges.GetValueOrDefault("color_energy_generated")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.CardInstanceId))
            .GroupBy(item => item.CardInstanceId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Generated),
                StringComparer.Ordinal);

        var ordered = events
            .Where(IsPlayerBeatActionResolved)
            .Select(item => BuildStep(item, energyByCardInstanceId))
            .OrderBy(step => step.BeatIndex)
            .ToList();

        var result = new List<BeatClashAnimationStep>();
        string? previousEnemyInstanceId = null;
        foreach (var step in ordered)
        {
            var continuesPrevious = previousEnemyInstanceId is not null &&
                string.Equals(previousEnemyInstanceId, step.EnemyInstanceId, StringComparison.Ordinal);
            result.Add(step with { ContinuesPreviousTarget = continuesPrevious });
            previousEnemyInstanceId = step.EnemyInstanceId;
        }

        return result;
    }

    private static bool IsPlayerBeatActionResolved(CombatLogEvent item)
    {
        if (item.EventType != CombatLogEventType.BeatActionResolved ||
            item.TargetIds.Count == 0 ||
            item.TargetIds.Contains("player", StringComparer.Ordinal))
        {
            return false;
        }

        var targetKind = item.Metadata.GetValueOrDefault("target_kind", string.Empty);
        return string.Equals(targetKind, BeatTargetKind.EnemyBeat.ToString(), StringComparison.Ordinal) ||
            string.Equals(targetKind, BeatTargetKind.EnemyBody.ToString(), StringComparison.Ordinal);
    }

    private static BeatClashAnimationStep BuildStep(
        CombatLogEvent item,
        IReadOnlyDictionary<string, int> energyByCardInstanceId)
    {
        var cardInstanceId = item.Metadata.GetValueOrDefault("card_instance_id", string.Empty);
        var targetKindText = item.Metadata.GetValueOrDefault("target_kind", BeatTargetKind.EnemyBeat.ToString());
        var enemyBeatIndexText = item.Metadata.GetValueOrDefault("enemy_beat_index", string.Empty);
        return new BeatClashAnimationStep
        {
            EventId = item.EventId,
            BeatIndex = item.NumericChanges.GetValueOrDefault("beat_index"),
            SourceCardId = item.SourceId ?? string.Empty,
            CardInstanceId = cardInstanceId,
            EnemyInstanceId = item.TargetIds[0],
            TargetKind = Enum.TryParse<BeatTargetKind>(targetKindText, out var targetKind)
                ? targetKind
                : BeatTargetKind.EnemyBeat,
            EnemyBeatIndex = int.TryParse(enemyBeatIndexText, out var enemyBeatIndex)
                ? enemyBeatIndex
                : null,
            EnemyDamage = item.NumericChanges.GetValueOrDefault("enemy_damage"),
            PlayerDamage = item.NumericChanges.GetValueOrDefault("player_damage"),
            SuccessfulPlayerActions = item.NumericChanges.GetValueOrDefault("successful_player_actions"),
            ColorEnergyGenerated = energyByCardInstanceId.GetValueOrDefault(cardInstanceId)
        };
    }
}
```

- [ ] **Step 2: Run planner tests to verify green**

Run:

```powershell
dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj
```

Expected: test output ends with `Domain model smoke tests passed.`

- [ ] **Step 3: Commit planner**

Run:

```powershell
git add game\src\Presentation\Battle\BeatClashAnimationPlanner.cs game\tests\Unit\Program.cs
git commit -m "test: cover beat clash animation planning"
```

---

### Task 3: Rule Log Ordering Regression

**Files:**
- Modify: `game/tests/Unit/Program.cs`
- Test: `game/tests/Unit/Program.cs`

- [ ] **Step 1: Write failing-or-confirming regression for I/II/III order**

Add this block after existing `resolvedBeatRound` assertions around the beat combat resolution tests:

```csharp
var reversedSelectionRound = CreateBeatTargetRound(
    beatCount: 3,
    enemyBeatIndexes: [0, 1, 2],
    playerBeats:
    [
        CreatePlayerBeat(2, beatSlashCard.Id, CreateEnemyBeatTarget(2)) with { CardInstanceId = "instance_2" },
        CreatePlayerBeat(0, beatSlashCard.Id, CreateEnemyBeatTarget(0)) with { CardInstanceId = "instance_0" },
        CreatePlayerBeat(1, beatSlashCard.Id, CreateEnemyBeatTarget(1)) with { CardInstanceId = "instance_1" }
    ]);
var reversedSelectionCombat = CreatePlayableCombat(
    [],
    enemies: [CreateEnemyState("enemy_01", currentHp: 60, maxHp: 60, enemyId: beatEnemy.Id)]) with
{
    BeatRound = reversedSelectionRound
};
var reversedSelectionResult = beatService.ResolveBeatRound(reversedSelectionCombat, beatCards, beatEnemies);
var resolvedBeatIndexes = reversedSelectionResult.Events
    .Where(item => item.EventType == CombatLogEventType.BeatActionResolved && !item.TargetIds.Contains("player"))
    .Select(item => item.NumericChanges.GetValueOrDefault("beat_index"))
    .ToList();
AssertSequenceEqual(new[] { 0, 1, 2 }, resolvedBeatIndexes, "Beat round resolution follows left-side I/II/III order regardless of selection order");
```

- [ ] **Step 2: Run test**

Run:

```powershell
dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj
```

Expected: test passes because the current `BeatCombatService.ResolveBeatRound()` already orders by `BeatIndex`.

- [ ] **Step 3: Commit regression**

Run:

```powershell
git add game\tests\Unit\Program.cs
git commit -m "test: lock beat resolution order to player beat index"
```

---

### Task 4: Cut-In Callback Wiring

**Files:**
- Modify: `game/src/Presentation/Battle/BattleLogAnimator.cs`
- Modify: `game/src/Presentation/Battle/BattleScreen.cs`
- Test: `game/tests/Unit/Program.cs`

- [ ] **Step 1: Add target callback field**

Modify the `BattleAnimationTargets` record in `BattleLogAnimator.cs` to include a cut-in callback:

```csharp
public sealed record BattleAnimationTargets(
    Control? PlayerNode,
    Control? ColorEnergyPanel,
    Control? BlockPanel,
    Control? ActionPointPanel,
    Control? HandNode,
    Control? FxLayer,
    IReadOnlyDictionary<string, Control> EnemyNodes,
    Func<int, Control?> CardNodeByHandIndex,
    Func<string, Control?> FirstCardNodeByCardId,
    Func<IReadOnlyList<BeatClashAnimationStep>, Task> PlayBeatClashCutInAsync);
```

- [ ] **Step 2: Update `BattleScreen.CreateAnimationTargets()`**

Update the return value in `BattleScreen.cs`:

```csharp
return new BattleAnimationTargets(
    playerNode,
    colorEnergyPanel,
    blockPanel,
    actionPointPanel,
    handNode,
    fxLayer,
    battleEnemyView?.EnemyNodes ?? new Dictionary<string, Control>(),
    handIndex => battleHandView?.GetCardNodeByHandIndex(handIndex),
    cardId => battleHandView?.GetFirstCardNode(cardId),
    PlayBeatClashCutInAsync);
```

Add a temporary stub method in `BattleScreen.cs`:

```csharp
private Task PlayBeatClashCutInAsync(IReadOnlyList<BeatClashAnimationStep> steps)
{
    return Task.CompletedTask;
}
```

- [ ] **Step 3: Route beat events in `BattleLogAnimator`**

Add a field:

```csharp
private readonly BeatClashAnimationPlanner beatClashPlanner = new();
```

At the start of `PlayAsync`, after the empty-events guard and before concurrent playback:

```csharp
var beatClashSteps = beatClashPlanner.BuildSteps(events);
if (beatClashSteps.Count > 0)
{
    await targets.PlayBeatClashCutInAsync(beatClashSteps);
    var remainingEvents = events
        .Where(item =>
            item.EventType != CombatLogEventType.BeatActionResolved &&
            item.EventType != CombatLogEventType.BeatEnergyGenerated)
        .ToList();
    foreach (var item in remainingEvents)
    {
        await PlayLogEventAsync(item, targets, playedCard, playedHandIndex);
    }

    return;
}
```

- [ ] **Step 4: Build to verify wiring**

Run:

```powershell
dotnet build game\RoguelikeCardGame.csproj
```

Expected: build succeeds with 0 errors.

- [ ] **Step 5: Commit wiring**

Run:

```powershell
git add game\src\Presentation\Battle\BattleLogAnimator.cs game\src\Presentation\Battle\BattleScreen.cs
git commit -m "feat: route beat logs to cut-in animator"
```

---

### Task 5: ComicScreen Animation Helpers

**Files:**
- Modify: `game/src/Presentation/Shared/ComicScreen.cs`
- Test: `dotnet build game\RoguelikeCardGame.csproj`

- [ ] **Step 1: Expose internal helpers for temporary overlay nodes**

Add these methods near existing animation helpers in `ComicScreen.cs`:

```csharp
internal Texture2D? LoadAnimationTexture(string assetId)
{
    return LoadTexture(assetId);
}

internal Font? LoadAnimationFont(string assetId)
{
    return LoadFont(assetId);
}

internal static void AddAnimationNodeAt(Control parent, Control child, Vector2 position, Vector2 size)
{
    AddAt(parent, child, position, size);
}
```

- [ ] **Step 2: Build to verify helper visibility**

Run:

```powershell
dotnet build game\RoguelikeCardGame.csproj
```

Expected: build succeeds with 0 errors.

- [ ] **Step 3: Commit helpers**

Run:

```powershell
git add game\src\Presentation\Shared\ComicScreen.cs
git commit -m "feat: expose comic animation helpers"
```

---

### Task 6: Beat Clash Cut-In Layer

**Files:**
- Create: `game/src/Presentation/Battle/BeatClashCutInLayer.cs`
- Modify: `game/src/Presentation/Battle/BattleScreen.cs`
- Test: `dotnet build game\RoguelikeCardGame.csproj`

- [ ] **Step 1: Create `BeatClashCutInLayer`**

Create `game/src/Presentation/Battle/BeatClashCutInLayer.cs`:

```csharp
using Godot;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed partial class BeatClashCutInLayer : Control
{
    private static readonly Vector2 LayerSize = new(1920, 1080);
    private static readonly Vector2 PlayerStartPosition = new(360, 514);
    private static readonly Vector2 PlayerClashPosition = new(650, 514);
    private static readonly Vector2 PlayerSize = new(260, 520);
    private static readonly Vector2 EnemyPosition = new(1080, 480);
    private static readonly Vector2 EnemySize = new(420, 420);

    private readonly ComicScreen screen;
    private readonly Func<string, Texture2D?> loadTexture;
    private readonly Font? labelFont;
    private readonly Texture2D? playerTexture;

    private TextureRect? playerSprite;
    private TextureRect? enemySprite;
    private Label? beatLabel;
    private Label? damageLabel;
    private string? activeEnemyInstanceId;

    public BeatClashCutInLayer(
        ComicScreen screen,
        Func<string, Texture2D?> loadTexture,
        Font? labelFont,
        Texture2D? playerTexture)
    {
        this.screen = screen;
        this.loadTexture = loadTexture;
        this.labelFont = labelFont;
        this.playerTexture = playerTexture;
        Size = LayerSize;
        CustomMinimumSize = LayerSize;
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 220;
        ZAsRelative = false;
        BuildBase();
    }

    public async Task PlayStepAsync(
        BeatClashAnimationStep step,
        Control? sourceEnemyNode)
    {
        var continuing = string.Equals(activeEnemyInstanceId, step.EnemyInstanceId, StringComparison.Ordinal);
        if (!continuing)
        {
            activeEnemyInstanceId = step.EnemyInstanceId;
            RebuildEnemySprite(sourceEnemyNode);
            await EnterTargetAsync();
        }

        UpdateBeatLabel(step);
        await PlayImpactAsync(step);
    }

    public async Task ExitAsync()
    {
        if (playerSprite is not null)
        {
            await MoveNodeAsync(playerSprite, PlayerStartPosition, 0.14);
        }
    }

    private void BuildBase()
    {
        var shade = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.68f),
            Size = LayerSize,
            CustomMinimumSize = LayerSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(shade);

        playerSprite = CreateSprite(playerTexture, PlayerSize);
        ComicScreen.AddAnimationNodeAt(this, playerSprite, PlayerStartPosition, PlayerSize);

        beatLabel = CreateLabel("", 46, new Color(1f, 0.92f, 0.58f));
        ComicScreen.AddAnimationNodeAt(this, beatLabel, new Vector2(842, 126), new Vector2(240, 72));

        damageLabel = CreateLabel("", 66, new Color(0.92f, 0.08f, 0.06f));
        damageLabel.Visible = false;
        ComicScreen.AddAnimationNodeAt(this, damageLabel, new Vector2(1048, 292), new Vector2(320, 110));
    }

    private void RebuildEnemySprite(Control? sourceEnemyNode)
    {
        if (enemySprite is not null)
        {
            RemoveChild(enemySprite);
            enemySprite.QueueFree();
        }

        enemySprite = CreateSprite(FindTexture(sourceEnemyNode) ?? loadTexture("asset.vfx.enemy_hit_comic_burst"), EnemySize);
        ComicScreen.AddAnimationNodeAt(this, enemySprite, EnemyPosition, EnemySize);
    }

    private async Task EnterTargetAsync()
    {
        if (playerSprite is null)
        {
            return;
        }

        playerSprite.Position = PlayerStartPosition;
        await MoveNodeAsync(playerSprite, PlayerClashPosition, 0.16);
    }

    private async Task PlayImpactAsync(BeatClashAnimationStep step)
    {
        UpdateDamageLabel(step);
        var center = EnemyPosition + EnemySize * 0.48f;
        await Task.WhenAll(
            screen.SpawnVfxAsync(this, "asset.vfx.enemy_hit_comic_burst", center, new Vector2(300, 210), Colors.White, 0.18),
            screen.ShakeNodeAsync(enemySprite, 18f, 0.16),
            screen.PulseNodeAsync(beatLabel, 1.08f, 0.08));

        if (damageLabel is not null)
        {
            damageLabel.Visible = false;
        }

        if (step.ColorEnergyGenerated > 0)
        {
            await screen.SpawnVfxAsync(this, "asset.vfx.color_energy_spark", new Vector2(960, 206), new Vector2(260, 150), new Color(1f, 0.95f, 0.65f), 0.16);
        }
    }

    private void UpdateBeatLabel(BeatClashAnimationStep step)
    {
        if (beatLabel is null)
        {
            return;
        }

        beatLabel.Text = BeatSlotPresentationGeometry.RomanBeatNumber(step.BeatIndex);
    }

    private void UpdateDamageLabel(BeatClashAnimationStep step)
    {
        if (damageLabel is null)
        {
            return;
        }

        var value = Math.Max(step.EnemyDamage, step.PlayerDamage);
        damageLabel.Text = value > 0 ? value.ToString() : step.SuccessfulPlayerActions > 0 ? "HIT" : "MISS";
        damageLabel.Visible = true;
    }

    private async Task MoveNodeAsync(Control node, Vector2 targetPosition, double duration)
    {
        var tween = screen.CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "position", targetPosition, duration);
        await screen.AwaitTweenFinishedAsync(tween, duration + 0.20);
    }

    private TextureRect CreateSprite(Texture2D? texture, Vector2 size)
    {
        return new TextureRect
        {
            Texture = texture,
            Size = size,
            CustomMinimumSize = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = MouseFilterEnum.Ignore
        };
    }

    private Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (labelFont is not null)
        {
            label.AddThemeFontOverride("font", labelFont);
        }

        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        label.AddThemeConstantOverride("shadow_offset_x", 4);
        label.AddThemeConstantOverride("shadow_offset_y", 4);
        return label;
    }

    private static Texture2D? FindTexture(Node? node)
    {
        if (node is TextureRect textureRect && textureRect.Texture is not null)
        {
            return textureRect.Texture;
        }

        if (node is null)
        {
            return null;
        }

        foreach (var child in node.GetChildren())
        {
            var texture = FindTexture(child);
            if (texture is not null)
            {
                return texture;
            }
        }

        return null;
    }
}
```

- [ ] **Step 2: Replace the BattleScreen stub with real playback**

Replace the temporary `PlayBeatClashCutInAsync` stub in `BattleScreen.cs`:

```csharp
private async Task PlayBeatClashCutInAsync(IReadOnlyList<BeatClashAnimationStep> steps)
{
    if (steps.Count == 0 || fxLayer is null)
    {
        return;
    }

    var layer = new BeatClashCutInLayer(
        this,
        LoadTexture,
        LoadAnimationFont("asset.font.source_han_sans_sc.heavy"),
        LoadAnimationTexture("asset.character.zu.revolver.battle"));
    fxLayer.AddChild(layer);

    try
    {
        foreach (var step in steps)
        {
            var enemyNode = battleEnemyView?.EnemyNodes.TryGetValue(step.EnemyInstanceId, out var node) == true
                ? node
                : null;
            await layer.PlayStepAsync(step, enemyNode);
        }

        await layer.ExitAsync();
    }
    finally
    {
        if (GodotObject.IsInstanceValid(layer))
        {
            layer.GetParent()?.RemoveChild(layer);
            layer.QueueFree();
        }
    }
}
```

- [ ] **Step 3: Build to verify the overlay compiles**

Run:

```powershell
dotnet build game\RoguelikeCardGame.csproj
```

Expected: build succeeds with 0 errors.

- [ ] **Step 4: Commit overlay**

Run:

```powershell
git add game\src\Presentation\Battle\BeatClashCutInLayer.cs game\src\Presentation\Battle\BattleScreen.cs
git commit -m "feat: add beat clash cut-in overlay"
```

---

### Task 7: Presentation Polish Pass

**Files:**
- Modify: `game/src/Presentation/Battle/BeatClashCutInLayer.cs`
- Test: `dotnet build game\RoguelikeCardGame.csproj`

- [ ] **Step 1: Add target switching return motion**

In `BeatClashCutInLayer.PlayStepAsync`, replace the non-continuing block with:

```csharp
if (!continuing)
{
    if (activeEnemyInstanceId is not null)
    {
        await ExitAsync();
    }

    activeEnemyInstanceId = step.EnemyInstanceId;
    RebuildEnemySprite(sourceEnemyNode);
    await EnterTargetAsync();
}
```

This gives A/B/A the required `return -> dash -> clash` rhythm while A/A/A stays continuous.

- [ ] **Step 2: Add player damage positioning**

In `UpdateDamageLabel`, replace the position logic with:

```csharp
var value = Math.Max(step.EnemyDamage, step.PlayerDamage);
damageLabel.Position = step.PlayerDamage > step.EnemyDamage
    ? PlayerClashPosition + new Vector2(34, -84)
    : EnemyPosition + new Vector2(48, -82);
damageLabel.Text = value > 0 ? value.ToString() : step.SuccessfulPlayerActions > 0 ? "HIT" : "MISS";
damageLabel.Visible = true;
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build game\RoguelikeCardGame.csproj
```

Expected: build succeeds with 0 errors.

- [ ] **Step 4: Commit polish**

Run:

```powershell
git add game\src\Presentation\Battle\BeatClashCutInLayer.cs
git commit -m "feat: polish beat clash target transitions"
```

---

### Task 8: Design Docs and Change Log

**Files:**
- Modify: `design/01_core_gameplay/02_combat_system.md`
- Modify: `design/03_experience/01_visual_direction.md`
- Modify: `design/08_governance/01_change_log.md`

- [ ] **Step 1: Update combat design**

Add a short paragraph under the combat animation or beat resolution section in `design/01_core_gameplay/02_combat_system.md`:

```markdown
### 拍位对撞特写

拍位结算表现按左侧己方拍位 I、II、III 顺序播放。每个已锁定目标的己方拍位会临时进入漫画特写覆盖层，只保留主角与当前目标魔物；主角先从初始站位冲向目标再执行该拍动作。连续拍位锁定同一个魔物的拍位或本体时，特写层保持不断开，主角不回位，以保证连贯动作动画。跨魔物目标、空拍或无目标拍会中断连续特写，主角回到初始站位后再冲向下一个目标。该演出只读取规则日志，不重新计算伤害、格挡、闪避或彩能。
```

- [ ] **Step 2: Update visual direction**

Add a bullet under the combat scene or animation rules in `design/03_experience/01_visual_direction.md`:

```markdown
- 三拍拆招的 MVP 打击演出使用临时漫画特写覆盖层：普通战斗画面暗化，近景只保留主角和目标魔物，伤害数字、冲击帧、短暂停顿和产彩反馈由规则日志驱动；同一魔物连续拍位保持镜头不断开，跨魔物时回位后再切入。
```

- [ ] **Step 3: Update change log**

Add an entry at the top of `design/08_governance/01_change_log.md`:

```markdown
## 2026-06-22

- 设计并实现三拍拆招的拍位对撞特写覆盖层：拍位演出按 I、II、III 顺序播放，同目标连续拍位不断镜，跨魔物目标回位后重新冲刺切入；表现层由规则日志驱动，不改变战斗数值结算。
```

- [ ] **Step 4: Commit docs**

Run:

```powershell
git add design\01_core_gameplay\02_combat_system.md design\03_experience\01_visual_direction.md design\08_governance\01_change_log.md
git commit -m "docs: record beat clash cut-in presentation"
```

---

### Task 9: Full Verification

**Files:**
- Verify the entire touched surface.

- [ ] **Step 1: Run data validator**

Run:

```powershell
.\game\tools\data_validator\validate_data.ps1
```

Expected: validator exits 0.

- [ ] **Step 2: Run unit tests**

Run:

```powershell
dotnet run --project game\tests\Unit\RoguelikeCardGame.Tests.csproj
```

Expected: output ends with `Domain model smoke tests passed.`

- [ ] **Step 3: Build main project**

Run:

```powershell
dotnet build game\RoguelikeCardGame.csproj
```

Expected: build exits 0 with 0 errors.

- [ ] **Step 4: Run Godot headless project check**

Run:

```powershell
.\game\tools\check_project.ps1
```

Expected: script exits 0 and reports the Godot project check completed.

- [ ] **Step 5: Manual smoke in Godot**

Run the project, start a debug encounter from weapon selection, place three action cards, and verify:

- I/II/III play in left-side beat order.
- A/A/A stays in one continuous cut-in.
- A/B/A exits and re-enters for each enemy switch.
- Damage numbers appear near the damaged side.
- Interactions unlock after the animation finishes.

- [ ] **Step 6: Final status**

Run:

```powershell
git status --short
```

Expected: only intentional uncommitted files remain. If all tasks committed, output is empty.
