using Godot;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Battle;

public sealed class BattleLogAnimator
{
    private const string DefaultCardVfxAsset = "asset.vfx.enemy_hit_comic_burst";

    private readonly ComicScreen screen;

    public BattleLogAnimator(ComicScreen screen)
    {
        this.screen = screen;
    }

    public async Task PlayAsync(
        IReadOnlyList<CombatLogEvent> events,
        BattleAnimationTargets targets,
        CardDefinition? playedCard = null,
        int? playedHandIndex = null,
        bool playConcurrently = false)
    {
        if (events.Count == 0)
        {
            return;
        }

        await PlayBeatClashCutInAsync(events, targets);
        var genericEvents = events
            .Where(item => !IsBeatClashAnimationEvent(item.EventType))
            .ToList();

        if (playConcurrently)
        {
            await Task.WhenAll(genericEvents.Select(item => PlayLogEventAsync(item, targets, playedCard, playedHandIndex)));
            return;
        }

        foreach (var item in genericEvents)
        {
            await PlayLogEventAsync(item, targets, playedCard, playedHandIndex);
        }
    }

    private static async Task PlayBeatClashCutInAsync(
        IReadOnlyList<CombatLogEvent> events,
        BattleAnimationTargets targets)
    {
        if (!events.Any(item => item.EventType == CombatLogEventType.BeatActionResolved) ||
            targets.PlayBeatClashCutInAsync is null)
        {
            return;
        }

        var steps = new BeatClashAnimationPlanner().Plan(events);
        if (steps.Count == 0)
        {
            return;
        }

        var originalHandVisible = targets.HandNode?.Visible;
        if (targets.HandNode is not null && GodotObject.IsInstanceValid(targets.HandNode))
        {
            targets.HandNode.Visible = false;
        }

        try
        {
            await targets.PlayBeatClashCutInAsync(steps);
        }
        finally
        {
            if (targets.HandNode is not null &&
                GodotObject.IsInstanceValid(targets.HandNode) &&
                originalHandVisible is not null)
            {
                targets.HandNode.Visible = originalHandVisible.Value;
            }
        }
    }

    private static bool IsBeatClashAnimationEvent(CombatLogEventType eventType)
    {
        return eventType is CombatLogEventType.BeatActionResolved or CombatLogEventType.BeatEnergyGenerated;
    }

    private async Task PlayLogEventAsync(
        CombatLogEvent item,
        BattleAnimationTargets targets,
        CardDefinition? playedCard,
        int? playedHandIndex)
    {
        switch (item.EventType)
        {
            case CombatLogEventType.CardPlayed:
                await PlayCardPlayedAsync(item, targets, playedHandIndex);
                await PlayColorEnergySpentAsync(item, targets);
                break;
            case CombatLogEventType.EffectResolved:
                await PlayEffectResolvedAsync(item, targets, playedCard);
                break;
            case CombatLogEventType.EnemyIntentResolved:
                await PlayEnemyIntentAsync(item, targets);
                break;
            case CombatLogEventType.EnemyDied:
                await PlayEnemyDiedAsync(item, targets);
                break;
            case CombatLogEventType.PlayerTurnEnded:
                await screen.PulseNodeAsync(targets.HandNode, 0.94f, 0.10f);
                break;
            case CombatLogEventType.CardsDrawn:
                await screen.PulseNodeAsync(targets.HandNode, 1.03f, 0.10f);
                break;
            case CombatLogEventType.CardsDiscarded:
                break;
            case CombatLogEventType.DeckReshuffled:
                break;
        }
    }

    private async Task PlayCardPlayedAsync(
        CombatLogEvent item,
        BattleAnimationTargets targets,
        int? playedHandIndex)
    {
        var sourceCard = playedHandIndex is not null
            ? targets.CardNodeByHandIndex(playedHandIndex.Value)
            : item.SourceId is not null
                ? targets.FirstCardNodeByCardId(item.SourceId)
                : null;

        if (sourceCard is not null && sourceCard.Visible)
        {
            await screen.PulseNodeAsync(sourceCard, 1.08f, 0.09f);
        }
    }

    private async Task PlayEffectResolvedAsync(
        CombatLogEvent item,
        BattleAnimationTargets targets,
        CardDefinition? playedCard)
    {
        var effectType = item.Metadata.TryGetValue("effect_type", out var value) ? value : "";
        if (effectType == "damage" && item.NumericChanges.TryGetValue("hp_damage", out var hpDamage) && hpDamage > 0)
        {
            await PlayDamageAsync(item, targets, playedCard);
            return;
        }

        if ((effectType == "block" || effectType == "gain_block") && item.NumericChanges.TryGetValue("block_gained", out var block) && block > 0)
        {
            await PlayBlockAsync(item, targets, playedCard);
            return;
        }

        if (effectType == "gain_color_energy")
        {
            return;
        }

        if (effectType == "yellow_extra_casts")
        {
            await PlayYellowExtraCastAsync(targets);
            return;
        }

        if (item.NumericChanges.TryGetValue("purple_multiplier", out var purpleMultiplier) && purpleMultiplier > 1)
        {
            await PlayPurpleAmplifyAsync(targets);
            return;
        }

        if (effectType == "draw_cards")
        {
            var asset = CardVfxAsset(playedCard);
            await Task.WhenAll(
                screen.SpawnVfxAsync(targets.FxLayer, asset, ComicScreen.CenterOf(targets.PlayerNode), CardVfxSize(asset), new Color(1f, 1f, 1f, 0.92f), 0.18f),
                screen.PulseNodeAsync(targets.HandNode, 1.04f, 0.10f));
            return;
        }

        if (effectType == "gain_action_points")
        {
            var asset = CardVfxAsset(playedCard);
            await Task.WhenAll(
                screen.SpawnVfxAsync(targets.FxLayer, asset, ComicScreen.CenterOf(targets.ActionPointPanel), CardVfxSize(asset), new Color(1f, 1f, 1f, 0.92f), 0.18f),
                screen.PulseNodeAsync(targets.ActionPointPanel, 1.2f, 0.12f));
            return;
        }

        if (effectType == "temporary_discount_placeholder")
        {
            var asset = CardVfxAsset(playedCard);
            await Task.WhenAll(
                screen.SpawnVfxAsync(targets.FxLayer, asset, ComicScreen.CenterOf(targets.HandNode), CardVfxSize(asset), new Color(1f, 1f, 1f, 0.92f), 0.18f),
                screen.PulseNodeAsync(targets.HandNode, 1.04f, 0.10f));
        }
    }

    private async Task PlayDamageAsync(
        CombatLogEvent item,
        BattleAnimationTargets targets,
        CardDefinition? playedCard)
    {
        var asset = CardVfxAsset(playedCard);
        var size = CardVfxSize(asset);

        foreach (var targetId in item.TargetIds)
        {
            if (!targets.EnemyNodes.TryGetValue(targetId, out var enemyNode))
            {
                continue;
            }

            var center = ComicScreen.CenterOf(enemyNode);
            await Task.WhenAll(
                screen.SpawnVfxAsync(targets.FxLayer, asset, center, size, new Color(1f, 1f, 1f, 0.94f), 0.20f),
                screen.ShakeNodeAsync(enemyNode, 18f, 0.16f));
        }
    }

    private async Task PlayBlockAsync(
        CombatLogEvent item,
        BattleAnimationTargets targets,
        CardDefinition? playedCard)
    {
        var target = item.TargetIds.Contains("player", StringComparer.Ordinal)
            ? targets.PlayerNode
            : item.TargetIds.Select(id => targets.EnemyNodes.TryGetValue(id, out var enemyNode) ? enemyNode : null).FirstOrDefault(node => node is not null);
        var center = target is null ? new Vector2(300, 590) : ComicScreen.CenterOf(target);
        var asset = CardVfxAsset(playedCard);
        await Task.WhenAll(
            screen.SpawnVfxAsync(targets.FxLayer, asset, center, CardVfxSize(asset), new Color(0.75f, 0.95f, 1f, 0.9f), 0.22f),
            screen.PulseNodeAsync(target ?? targets.BlockPanel, 1.05f, 0.10f),
            screen.PulseNodeAsync(targets.BlockPanel, 1.2f, 0.10f));
    }

    private async Task PlayColorEnergySpentAsync(CombatLogEvent item, BattleAnimationTargets targets)
    {
        if (!item.NumericChanges.TryGetValue("color_energy_spent", out var spent) || spent <= 0)
        {
            return;
        }

        await Task.WhenAll(
            screen.SpawnVfxAsync(targets.FxLayer, "asset.vfx.finisher_release_shockwave", new Vector2(960, 118), new Vector2(270, 160), new Color(1f, 0.94f, 0.62f, 0.92f), 0.20f),
            screen.PulseNodeAsync(targets.ColorEnergyPanel, 0.90f, 0.12f));
    }

    private async Task PlayYellowExtraCastAsync(BattleAnimationTargets targets)
    {
        await Task.WhenAll(
            screen.SpawnVfxAsync(targets.FxLayer, "asset.vfx.color_energy_spark", new Vector2(960, 118), new Vector2(260, 150), new Color(1f, 0.82f, 0.20f, 0.95f), 0.20f),
            screen.PulseNodeAsync(targets.HandNode, 1.05f, 0.10f));
    }

    private async Task PlayPurpleAmplifyAsync(BattleAnimationTargets targets)
    {
        await screen.SpawnVfxAsync(targets.FxLayer, "asset.vfx.finisher_release_shockwave", new Vector2(960, 118), new Vector2(300, 180), new Color(0.72f, 0.38f, 1f, 0.92f), 0.22f);
    }

    private async Task PlayEnemyIntentAsync(CombatLogEvent item, BattleAnimationTargets targets)
    {
        var sourceEnemy = item.SourceId is not null && targets.EnemyNodes.TryGetValue(item.SourceId, out var enemyNode)
            ? enemyNode
            : null;
        var effectType = item.Metadata.TryGetValue("effect_type", out var value) ? value : "";

        if (effectType == "damage")
        {
            await Task.WhenAll(
                screen.LungeNodeAsync(sourceEnemy, new Vector2(-38, 0), 0.12f),
                screen.SpawnVfxAsync(targets.FxLayer, "asset.vfx.enemy_hit_comic_burst", ComicScreen.CenterOf(targets.PlayerNode), new Vector2(210, 150), new Color(1f, 0.74f, 0.62f, 0.9f), 0.18f),
                screen.ShakeNodeAsync(targets.PlayerNode, 14f, 0.15f));
            return;
        }

        if (effectType == "block" || effectType == "gain_block")
        {
            var center = sourceEnemy is null ? new Vector2(1250, 520) : ComicScreen.CenterOf(sourceEnemy);
            await Task.WhenAll(
                screen.SpawnVfxAsync(targets.FxLayer, "asset.vfx.defense_shield_flash", center, new Vector2(220, 160), new Color(0.72f, 0.95f, 1f, 0.88f), 0.22f),
                screen.PulseNodeAsync(sourceEnemy, 1.05f, 0.10f));
        }
    }

    private async Task PlayEnemyDiedAsync(CombatLogEvent item, BattleAnimationTargets targets)
    {
        foreach (var targetId in item.TargetIds)
        {
            if (!targets.EnemyNodes.TryGetValue(targetId, out var enemyNode))
            {
                continue;
            }

            enemyNode.PivotOffset = enemyNode.Size * 0.5f;
            var tween = screen.CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(enemyNode, "scale", enemyNode.Scale * 0.9f, 0.16);
            tween.TweenProperty(enemyNode, "modulate", new Color(0.25f, 0.25f, 0.25f, 0.55f), 0.16);
            await screen.AwaitTweenFinishedAsync(tween, 0.45);
        }
    }

    private static string CardVfxAsset(CardDefinition? card)
    {
        return string.IsNullOrWhiteSpace(card?.VfxAsset)
            ? DefaultCardVfxAsset
            : card.VfxAsset;
    }

    private static Vector2 CardVfxSize(string asset)
    {
        return asset switch
        {
            "asset.vfx.group_sweep_arc_light" => new Vector2(420, 220),
            "asset.vfx.finisher_release_shockwave" => new Vector2(360, 220),
            "asset.vfx.heavy_strike_impact_frame" => new Vector2(280, 180),
            "asset.vfx.defense_shield_flash" => new Vector2(260, 180),
            "asset.vfx.color_energy_spark" => new Vector2(240, 150),
            _ => new Vector2(250, 150)
        };
    }
}

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
    Func<IReadOnlyList<BeatClashAnimationStep>, Task>? PlayBeatClashCutInAsync);
