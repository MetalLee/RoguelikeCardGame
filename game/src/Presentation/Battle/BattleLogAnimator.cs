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

        if (playConcurrently)
        {
            await Task.WhenAll(events.Select(item => PlayLogEventAsync(item, targets, playedCard, playedHandIndex)));
            return;
        }

        foreach (var item in events)
        {
            await PlayLogEventAsync(item, targets, playedCard, playedHandIndex);
        }
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
                await screen.PulseNodeAsync(targets.DrawPilePanel, 1.18f, 0.12f);
                await screen.PulseNodeAsync(targets.HandNode, 1.03f, 0.10f);
                break;
            case CombatLogEventType.CardsDiscarded:
                await screen.PulseNodeAsync(targets.DiscardPilePanel, 1.18f, 0.12f);
                break;
            case CombatLogEventType.DeckReshuffled:
                await screen.PulseNodeAsync(targets.DrawPilePanel, 1.22f, 0.12f);
                await screen.PulseNodeAsync(targets.DiscardPilePanel, 1.22f, 0.12f);
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

        if (effectType == "default_chain_change" && item.NumericChanges.TryGetValue("chain_before", out var before) && item.NumericChanges.TryGetValue("chain_after", out var after))
        {
            await PlayChainChangeAsync(before, after, targets);
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

    private async Task PlayChainChangeAsync(int before, int after, BattleAnimationTargets targets)
    {
        if (after > before)
        {
            var animations = new List<Task>
            {
                screen.SpawnVfxAsync(targets.FxLayer, "asset.vfx.chain_gain_spark", new Vector2(960, 118), new Vector2(220, 130), new Color(1f, 1f, 1f, 0.95f), 0.20f),
                screen.PulseNodeAsync(targets.ChainPanel, 1.18f, 0.11f)
            };
            foreach (var threshold in new[] { 3, 5, 8 })
            {
                if (before < threshold && after >= threshold)
                {
                    animations.Add(screen.SpawnVfxAsync(targets.FxLayer, $"asset.vfx.chain_threshold_{threshold}_burst", new Vector2(960, 118), new Vector2(300, 180), new Color(1f, 1f, 1f, 0.98f), 0.24f));
                }
            }
            await Task.WhenAll(animations);
            return;
        }

        if (after < before)
        {
            await screen.PulseNodeAsync(targets.ChainPanel, 0.9f, 0.12f);
        }
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
            await screen.ToSignal(tween, "finished");
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
            "asset.vfx.chain_gain_spark" => new Vector2(240, 150),
            _ => new Vector2(250, 150)
        };
    }
}

public sealed record BattleAnimationTargets(
    Control? PlayerNode,
    Control? ChainPanel,
    Control? BlockPanel,
    Control? ActionPointPanel,
    Control? HandNode,
    Control? DrawPilePanel,
    Control? DiscardPilePanel,
    Control? FxLayer,
    IReadOnlyDictionary<string, Control> EnemyNodes,
    Func<int, Control?> CardNodeByHandIndex,
    Func<string, Control?> FirstCardNodeByCardId);
