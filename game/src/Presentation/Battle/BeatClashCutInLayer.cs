using Godot;
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

    public BeatClashCutInLayer(
        ComicScreen screen,
        Control root,
        Control? playerNode,
        IReadOnlyDictionary<string, Control> enemyNodes)
    {
        this.screen = screen;
        this.root = root;
        this.playerNode = playerNode;
        this.enemyNodes = enemyNodes;
    }

    public async Task PlayAsync(IReadOnlyList<BeatClashAnimationStep> steps)
    {
        if (!GodotObject.IsInstanceValid(root) || steps.Count == 0)
        {
            return;
        }

        var overlay = CreateOverlay();
        root.AddChild(overlay);

        var playerClone = CreatePlayerClone();
        ComicScreen.AddAnimationNodeAt(overlay, playerClone, PlayerStartPosition, PlayerSize);

        TextureRect? targetClone = null;
        string? currentTargetId = null;

        try
        {
            foreach (var step in steps)
            {
                if (!GodotObject.IsInstanceValid(overlay) || !GodotObject.IsInstanceValid(playerClone))
                {
                    return;
                }

                var targetChanged = !string.Equals(currentTargetId, step.TargetId, StringComparison.Ordinal);
                if (targetClone is null || targetChanged)
                {
                    if (targetClone is not null && GodotObject.IsInstanceValid(targetClone))
                    {
                        targetClone.QueueFree();
                    }

                    currentTargetId = step.TargetId;
                    targetClone = CreateTargetClone(step.TargetId);
                    ComicScreen.AddAnimationNodeAt(overlay, targetClone, TargetPosition, targetClone.CustomMinimumSize);
                }

                if (step.ReturnToStartBeforeStep)
                {
                    await TweenPositionAsync(playerClone, PlayerStartPosition, 0.12, Tween.EaseType.Out);
                }

                await PlayStepAsync(overlay, playerClone, targetClone, step);
                await WaitAsync(0.08);
            }

            await FadeOutAndFreeAsync(overlay);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(overlay))
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

    private TextureRect CreatePlayerClone()
    {
        var clone = CreateTexture("asset.character.zu.revolver.battle", PlayerSize, TextureRect.StretchModeEnum.KeepAspectCentered);
        clone.Texture ??= FindTexture(playerNode);
        clone.ZIndex = 3;
        return clone;
    }

    private TextureRect CreateTargetClone(string? targetId)
    {
        var texture = FindTargetTexture(targetId) ?? screen.LoadAnimationTexture("asset.enemy.skeleton_smoke.sheet");
        var size = TargetCloneSize(targetId);
        var clone = CreateTexture(texture, size, TextureRect.StretchModeEnum.KeepAspectCentered);
        clone.ZIndex = 3;
        clone.PivotOffset = size * 0.5f;
        return clone;
    }

    private async Task PlayStepAsync(Control overlay, TextureRect playerClone, TextureRect targetClone, BeatClashAnimationStep step)
    {
        var dashPosition = targetClone.Position + new Vector2(-270, 70);
        await TweenPositionAsync(playerClone, dashPosition, 0.14, Tween.EaseType.Out);

        var impactCenter = playerClone.Position + new Vector2(PlayerSize.X * 0.88f, PlayerSize.Y * 0.44f);
        var targetCenter = targetClone.Position + targetClone.Size * 0.5f;
        impactCenter = (impactCenter + targetCenter) * 0.5f;

        var effects = new List<Task>
        {
            PlayImpactAsync(overlay, impactCenter),
            ShakeAsync(targetClone, 24f, 0.16),
            TweenPositionAsync(playerClone, dashPosition + new Vector2(-32, 0), 0.09, Tween.EaseType.Out)
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
}
