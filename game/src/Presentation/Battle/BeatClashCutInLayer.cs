using Godot;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Presentation.Shared;

namespace RoguelikeCardGame.Presentation.Battle;

internal sealed class BeatClashCutInLayer
{
    private static readonly Vector2 OverlaySize = new(1920, 1080);
    private static readonly Vector2 PlayerStartPosition = new(360, 260);
    private static readonly Vector2 PlayerSize = new(300, 630);
    private static readonly Vector2 TargetPosition = new(1120, 300);
    private static readonly Vector2 FallbackTargetSize = new(420, 420);
    private static readonly Vector2 MaxTargetSize = new(520, 420);
    private static readonly Vector2 ImpactSize = new(360, 240);

    private readonly ComicScreen screen;
    private readonly Control root;
    private readonly Control? playerNode;
    private readonly IReadOnlyDictionary<string, Control> enemyNodes;
    private readonly IReadOnlyDictionary<(string EnemyInstanceId, int BeatIndex), Control> enemyBeatSlotNodes;
    private readonly IReadOnlyDictionary<string, CardDefinition> cardsById;
    private readonly BeatClashActionAnimationCatalog actionAnimations = BeatClashActionAnimationCatalog.Default;
    private readonly Dictionary<BeatClashActionAnimationKind, int> actionAnimationIndexes = new();

    public BeatClashCutInLayer(
        ComicScreen screen,
        Control root,
        Control? playerNode,
        IReadOnlyDictionary<string, Control> enemyNodes,
        IReadOnlyDictionary<(string EnemyInstanceId, int BeatIndex), Control> enemyBeatSlotNodes,
        IReadOnlyDictionary<string, CardDefinition> cardsById)
    {
        this.screen = screen;
        this.root = root;
        this.playerNode = playerNode;
        this.enemyNodes = enemyNodes;
        this.enemyBeatSlotNodes = enemyBeatSlotNodes;
        this.cardsById = cardsById;
    }

    public async Task PlayAsync(IReadOnlyList<BeatClashAnimationStep> steps)
    {
        if (!GodotObject.IsInstanceValid(root) || steps.Count == 0)
        {
            return;
        }

        var index = 0;
        while (index < steps.Count)
        {
            var targetId = steps[index].TargetId;
            if (string.IsNullOrWhiteSpace(targetId) ||
                !enemyNodes.ContainsKey(targetId) ||
                !GodotObject.IsInstanceValid(root))
            {
                index++;
                continue;
            }

            var segmentSteps = steps
                .Skip(index)
                .TakeWhile(step => string.Equals(step.TargetId, targetId, StringComparison.Ordinal))
                .ToList();
            await PlayTargetSegmentAsync(targetId, segmentSteps);
            index += segmentSteps.Count;
        }
    }

    private async Task PlayTargetSegmentAsync(string targetId, IReadOnlyList<BeatClashAnimationStep> segmentSteps)
    {
        var hiddenNodes = HideNonTargetCombatants(targetId);
        Control? overlay = null;
        try
        {
            overlay = CreateOverlay();
            root.AddChild(overlay);

            var playerFrame = NodeFrameInRoot(playerNode, new BeatClashNodeFrame(PlayerStartPosition, PlayerSize));
            var targetFrame = NodeFrameInRoot(
                enemyNodes.TryGetValue(targetId, out var targetNode) ? targetNode : null,
                new BeatClashNodeFrame(TargetPosition, TargetCloneSize(targetId)));

            var playerClone = CreatePlayerClone(playerFrame);
            ComicScreen.AddAnimationNodeAt(overlay, playerClone, playerFrame.Position, playerFrame.Size);
            var targetClone = CreateTargetClone(targetId, targetFrame);
            ComicScreen.AddAnimationNodeAt(overlay, targetClone, targetFrame.Position, targetFrame.Size);

            foreach (var step in segmentSteps)
            {
                if (!GodotObject.IsInstanceValid(overlay) ||
                    !GodotObject.IsInstanceValid(playerClone) ||
                    !GodotObject.IsInstanceValid(targetClone))
                {
                    return;
                }

                await PlayStepAsync(overlay, playerClone, targetClone, playerFrame.Position, step);
            }

            if (GodotObject.IsInstanceValid(playerClone))
            {
                await PlayRunToPositionAsync(playerClone, playerFrame.Position, 0.24, Tween.EaseType.Out);
            }

            if (GodotObject.IsInstanceValid(overlay))
            {
                await FadeOutAndFreeAsync(overlay);
            }
        }
        finally
        {
            RestoreVisibility(hiddenNodes);
            if (overlay is not null && GodotObject.IsInstanceValid(overlay))
            {
                overlay.QueueFree();
            }
        }
    }

    private Control CreateOverlay()
    {
        var overlay = new Control
        {
            Name = "BeatClashCutInLayer",
            Size = OverlaySize,
            CustomMinimumSize = OverlaySize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 360,
            ZAsRelative = false
        };

        var shade = new ColorRect
        {
            Color = new Color(0.02f, 0.015f, 0.015f, 0.68f),
            Size = OverlaySize,
            CustomMinimumSize = OverlaySize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 0
        };
        overlay.AddChild(shade);

        var speedLines = CreateTexture("asset.vfx.slash_speed_lines", OverlaySize, TextureRect.StretchModeEnum.KeepAspectCovered);
        speedLines.Modulate = new Color(1f, 1f, 1f, 0.32f);
        speedLines.ZIndex = 1;
        ComicScreen.AddAnimationNodeAt(overlay, speedLines, Vector2.Zero, OverlaySize);

        return overlay;
    }

    private TextureRect CreatePlayerClone(BeatClashNodeFrame frame)
    {
        var clone = CreateTexture("asset.character.zu.revolver.battle", frame.Size, TextureRect.StretchModeEnum.KeepAspectCentered);
        clone.Texture ??= FindTexture(playerNode);
        clone.ZIndex = 3;
        return clone;
    }

    private TextureRect CreateTargetClone(string? targetId, BeatClashNodeFrame frame)
    {
        var texture = FindTargetTexture(targetId) ?? screen.LoadAnimationTexture("asset.enemy.skeleton_smoke.sheet");
        var size = frame.Size;
        var clone = CreateTexture(texture, size, TextureRect.StretchModeEnum.KeepAspectCentered);
        clone.ZIndex = 3;
        clone.PivotOffset = size * 0.5f;
        return clone;
    }

    private async Task PlayStepAsync(
        Control overlay,
        TextureRect playerClone,
        TextureRect targetClone,
        Vector2 playerStartPosition,
        BeatClashAnimationStep step)
    {
        var dashPosition = DashPositionBesideTarget(playerClone, targetClone);
        await PlayRunToPositionAsync(playerClone, dashPosition, 0.28, Tween.EaseType.Out);
        if (!GodotObject.IsInstanceValid(overlay) ||
            !GodotObject.IsInstanceValid(playerClone) ||
            !GodotObject.IsInstanceValid(targetClone))
        {
            return;
        }

        await WaitAsync(BeatClashPresentationTiming.PreActionPauseSeconds);

        var actionSequences = SelectActionSequences(step);
        for (var actionIndex = 0; actionIndex < actionSequences.Count; actionIndex++)
        {
            await PlaySequenceAsync(playerClone, actionSequences[actionIndex], 0);
            if (actionIndex < actionSequences.Count - 1)
            {
                await WaitAsync(BeatClashPresentationTiming.ActionIntervalSeconds);
            }
        }

        var impactCenter = playerClone.Position + new Vector2(playerClone.Size.X * 0.88f, playerClone.Size.Y * 0.44f);
        var targetCenter = targetClone.Position + targetClone.Size * 0.5f;
        impactCenter = (impactCenter + targetCenter) * 0.5f;

        var effects = new List<Task>
        {
            PlayImpactAsync(overlay, impactCenter),
            ShakeAsync(targetClone, 24f, 0.16)
        };

        if (step.EnemyDamage > 0)
        {
            effects.Add(PlayNumberAsync(overlay, $"-{step.EnemyDamage}", targetClone.Position + new Vector2(120, -36), new Color(1f, 0.32f, 0.18f, 1f)));
        }

        if (step.PlayerDamage > 0)
        {
            effects.Add(PlayNumberAsync(overlay, $"-{step.PlayerDamage}", playerClone.Position + new Vector2(60, 40), new Color(1f, 0.76f, 0.36f, 1f)));
        }

        if (step.EnergyGeneratedTotal > 0)
        {
            effects.Add(PlayNumberAsync(overlay, $"+{step.EnergyGeneratedTotal} 彩能", new Vector2(780, 760), new Color(0.95f, 0.88f, 0.36f, 1f), 42));
        }

        await Task.WhenAll(effects);
        await PlayRunToPositionAsync(playerClone, playerStartPosition, 0.24, Tween.EaseType.Out);
    }

    private async Task PlayRunToPositionAsync(TextureRect playerClone, Vector2 position, double duration, Tween.EaseType easeType)
    {
        await Task.WhenAll(
            TweenPositionAsync(playerClone, position, duration, easeType),
            PlaySequenceAsync(playerClone, actionAnimations.RunWithSword, duration));
    }

    private IReadOnlyList<BeatClashSpriteSequence> SelectActionSequences(BeatClashAnimationStep step)
    {
        var selected = new List<BeatClashSpriteSequence>();
        var kinds = CardAnimationKinds(step);
        foreach (var kind in kinds)
        {
            var sequences = actionAnimations.SequencesFor(kind);
            if (sequences.Count == 0)
            {
                continue;
            }

            var index = actionAnimationIndexes.GetValueOrDefault(kind);
            actionAnimationIndexes[kind] = index + 1;
            selected.Add(sequences[index % sequences.Count]);
        }

        return selected;
    }

    private IReadOnlyList<BeatClashActionAnimationKind> CardAnimationKinds(BeatClashAnimationStep step)
    {
        if (step.CardId is not null && cardsById.TryGetValue(step.CardId, out var card))
        {
            return actionAnimations.KindsForCard(card);
        }

        return step.EnemyDamage > 0 ? [BeatClashActionAnimationKind.Slash] : [];
    }

    private IReadOnlyList<BeatClashVisibilityState> HideNonTargetCombatants(string targetId)
    {
        var hidden = new List<BeatClashVisibilityState>();
        foreach (var (enemyId, node) in enemyNodes)
        {
            if (string.Equals(enemyId, targetId, StringComparison.Ordinal) ||
                !GodotObject.IsInstanceValid(node))
            {
                continue;
            }

            hidden.Add(new BeatClashVisibilityState(node, node.Visible));
            node.Visible = false;
        }

        foreach (var (key, node) in enemyBeatSlotNodes)
        {
            if (string.Equals(key.EnemyInstanceId, targetId, StringComparison.Ordinal) ||
                !GodotObject.IsInstanceValid(node))
            {
                continue;
            }

            hidden.Add(new BeatClashVisibilityState(node, node.Visible));
            node.Visible = false;
        }

        return hidden;
    }

    private static void RestoreVisibility(IReadOnlyList<BeatClashVisibilityState> states)
    {
        foreach (var state in states)
        {
            if (GodotObject.IsInstanceValid(state.Node))
            {
                state.Node.Visible = state.WasVisible;
            }
        }
    }

    private BeatClashNodeFrame NodeFrameInRoot(Control? node, BeatClashNodeFrame fallback)
    {
        if (node is null || !GodotObject.IsInstanceValid(node))
        {
            return fallback;
        }

        var size = node.Size;
        if (size.X <= 0 || size.Y <= 0)
        {
            size = node.CustomMinimumSize;
        }

        if (size.X <= 0 || size.Y <= 0)
        {
            return fallback;
        }

        var rootInverse = root.GetGlobalTransformWithCanvas().AffineInverse();
        var nodeTransform = node.GetGlobalTransformWithCanvas();
        var topLeft = rootInverse * (nodeTransform * Vector2.Zero);
        var bottomRight = rootInverse * (nodeTransform * size);
        return new BeatClashNodeFrame(
            topLeft,
            new Vector2(MathF.Abs(bottomRight.X - topLeft.X), MathF.Abs(bottomRight.Y - topLeft.Y)));
    }

    private static Vector2 DashPositionBesideTarget(TextureRect playerClone, TextureRect targetClone)
    {
        var x = targetClone.Position.X - playerClone.Size.X * 0.72f;
        var y = targetClone.Position.Y + targetClone.Size.Y * 0.5f - playerClone.Size.Y * 0.5f;
        return new Vector2(x, y);
    }

    private async Task PlayImpactAsync(Control overlay, Vector2 center)
    {
        var asset = screen.LoadAnimationTexture("asset.vfx.enemy_hit_comic_burst") is not null
            ? "asset.vfx.enemy_hit_comic_burst"
            : "asset.vfx.heavy_strike_impact_frame";
        var impact = CreateTexture(asset, ImpactSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        impact.Position = center - ImpactSize * 0.5f;
        impact.Size = ImpactSize;
        impact.CustomMinimumSize = ImpactSize;
        impact.PivotOffset = ImpactSize * 0.5f;
        impact.Scale = new Vector2(0.72f, 0.72f);
        impact.Modulate = new Color(1f, 1f, 1f, 0f);
        impact.ZIndex = 6;
        overlay.AddChild(impact);

        var tween = screen.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(impact, "modulate", new Color(1f, 1f, 1f, 0.96f), 0.04);
        tween.TweenProperty(impact, "scale", new Vector2(1.18f, 1.18f), 0.16);
        tween.Chain().TweenProperty(impact, "modulate", new Color(1f, 1f, 1f, 0f), 0.08);
        await screen.AwaitTweenFinishedAsync(tween, 0.35);

        if (GodotObject.IsInstanceValid(impact))
        {
            impact.QueueFree();
        }
    }

    private async Task PlayNumberAsync(Control overlay, string text, Vector2 position, Color color, int fontSize = 68)
    {
        var label = new Label
        {
            Text = text,
            Size = new Vector2(260, 92),
            CustomMinimumSize = new Vector2(260, 92),
            Position = position,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 7
        };

        var font = screen.LoadAnimationFont("asset.font.source_han_sans_sc.heavy");
        if (font is not null)
        {
            label.AddThemeFontOverride("font", font);
        }

        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0.02f, 0.015f, 0.015f, 0.95f));
        label.AddThemeConstantOverride("shadow_offset_x", 4);
        label.AddThemeConstantOverride("shadow_offset_y", 4);
        overlay.AddChild(label);

        var tween = screen.CreateTween();
        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "position", position + new Vector2(0, -54), 0.34);
        tween.TweenProperty(label, "modulate", new Color(1f, 1f, 1f, 0f), 0.34);
        await screen.AwaitTweenFinishedAsync(tween, 0.55);

        if (GodotObject.IsInstanceValid(label))
        {
            label.QueueFree();
        }
    }

    private async Task TweenPositionAsync(Control node, Vector2 position, double duration, Tween.EaseType easeType)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var tween = screen.CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(easeType);
        tween.TweenProperty(node, "position", position, duration);
        await screen.AwaitTweenFinishedAsync(tween, duration + 0.2);
    }

    private async Task PlaySequenceAsync(TextureRect node, BeatClashSpriteSequence sequence, double minimumDurationSeconds)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var frames = BuildFrames(sequence, node.Size.Y);
        if (frames.Count == 0)
        {
            return;
        }

        var originalTexture = node.Texture;
        var originalStretchMode = node.StretchMode;
        var originalSize = node.Size;
        var originalCustomMinimumSize = node.CustomMinimumSize;
        var originalPivotOffset = node.PivotOffset;

        try
        {
            node.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            var elapsed = 0.0;
            var frameIndex = 0;
            do
            {
                if (!GodotObject.IsInstanceValid(node))
                {
                    return;
                }

                var frame = frames[frameIndex % frames.Count];
                node.Texture = frame.Texture;
                node.Size = frame.DisplaySize;
                node.CustomMinimumSize = frame.DisplaySize;
                node.PivotOffset = frame.DisplaySize * 0.5f;
                await WaitAsync(sequence.FrameDurationSeconds);
                elapsed += sequence.FrameDurationSeconds;
                frameIndex++;
            }
            while (sequence.Loop ? elapsed < minimumDurationSeconds : frameIndex < frames.Count);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(node))
            {
                node.Texture = originalTexture;
                node.StretchMode = originalStretchMode;
                node.Size = originalSize;
                node.CustomMinimumSize = originalCustomMinimumSize;
                node.PivotOffset = originalPivotOffset;
            }
        }
    }

    private IReadOnlyList<BeatClashRenderedFrame> BuildFrames(BeatClashSpriteSequence sequence, float displayHeight)
    {
        var frames = new List<BeatClashRenderedFrame>();
        foreach (var sheet in sequence.Sheets)
        {
            var texture = screen.LoadAnimationTexture(sheet.AssetId);
            if (texture is null)
            {
                continue;
            }

            var columns = Math.Max(1, sheet.Columns);
            var rows = Math.Max(1, sheet.Rows);
            var frameWidth = texture.GetWidth() / (float)columns;
            var frameHeight = texture.GetHeight() / (float)rows;
            var height = displayHeight > 0 ? displayHeight : PlayerSize.Y;
            var displaySize = new Vector2(height * frameWidth / frameHeight, height);
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    frames.Add(new BeatClashRenderedFrame(
                        new AtlasTexture
                        {
                            Atlas = texture,
                            Region = new Rect2(column * frameWidth, row * frameHeight, frameWidth, frameHeight)
                        },
                        displaySize));
                }
            }
        }

        return frames;
    }

    private async Task ShakeAsync(Control node, float distance, double duration)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var originalPosition = node.Position;
        var step = duration / 4.0;
        var tween = screen.CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(distance, 0), step);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(-distance * 0.7f, 0), step);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(distance * 0.35f, 0), step);
        tween.TweenProperty(node, "position", originalPosition, step);
        await screen.AwaitTweenFinishedAsync(tween, duration + 0.2);
    }

    private async Task FadeOutAndFreeAsync(Control overlay)
    {
        if (!GodotObject.IsInstanceValid(overlay))
        {
            return;
        }

        var tween = screen.CreateTween();
        tween.TweenProperty(overlay, "modulate", new Color(1f, 1f, 1f, 0f), 0.12);
        await screen.AwaitTweenFinishedAsync(tween, 0.25);
        if (GodotObject.IsInstanceValid(overlay))
        {
            overlay.QueueFree();
        }
    }

    private Texture2D? FindTargetTexture(string? targetId)
    {
        if (targetId is null || !enemyNodes.TryGetValue(targetId, out var target) || !GodotObject.IsInstanceValid(target))
        {
            return null;
        }

        return FindTexture(target);
    }

    private static Texture2D? FindTexture(Control? node)
    {
        if (node is null || !GodotObject.IsInstanceValid(node))
        {
            return null;
        }

        if (node is TextureRect { Texture: not null } textureRect)
        {
            return textureRect.Texture;
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Control control)
            {
                var texture = FindTexture(control);
                if (texture is not null)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private Vector2 TargetCloneSize(string? targetId)
    {
        if (targetId is null || !enemyNodes.TryGetValue(targetId, out var target) || !GodotObject.IsInstanceValid(target))
        {
            return FallbackTargetSize;
        }

        var sourceSize = target.Size;
        if (sourceSize.X <= 0 || sourceSize.Y <= 0)
        {
            sourceSize = target.CustomMinimumSize;
        }

        if (sourceSize.X <= 0 || sourceSize.Y <= 0)
        {
            return FallbackTargetSize;
        }

        var scale = Math.Min(MaxTargetSize.X / sourceSize.X, MaxTargetSize.Y / sourceSize.Y);
        scale = Math.Min(Math.Max(scale, 0.75f), 1.85f);
        return new Vector2(
            Math.Clamp(sourceSize.X * scale, 260f, MaxTargetSize.X),
            Math.Clamp(sourceSize.Y * scale, 260f, MaxTargetSize.Y));
    }

    private TextureRect CreateTexture(string assetId, Vector2 size, TextureRect.StretchModeEnum stretchMode)
    {
        return CreateTexture(screen.LoadAnimationTexture(assetId), size, stretchMode);
    }

    private static TextureRect CreateTexture(Texture2D? texture, Vector2 size, TextureRect.StretchModeEnum stretchMode)
    {
        return new TextureRect
        {
            Texture = texture,
            Size = size,
            CustomMinimumSize = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    private async Task WaitAsync(double seconds)
    {
        var tree = screen.GetTree();
        if (tree is null)
        {
            return;
        }

        await screen.ToSignal(tree.CreateTimer(seconds), "timeout");
    }

    private sealed record BeatClashRenderedFrame(Texture2D Texture, Vector2 DisplaySize);

    private sealed record BeatClashNodeFrame(Vector2 Position, Vector2 Size);

    private sealed record BeatClashVisibilityState(Control Node, bool WasVisible);
}
