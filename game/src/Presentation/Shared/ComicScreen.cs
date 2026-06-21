using Godot;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Shared;

public abstract partial class ComicScreen : Control
{
    private static readonly Vector2 BaseLayoutSize = new(1920, 1080);
    private static readonly Vector2 DesignSize = new(1920, 1080);
    private static readonly Vector2 DesignToBaseScale = DesignSize / BaseLayoutSize;
    private static readonly Color LetterboxBlack = new(0.0f, 0.0f, 0.0f, 1.0f);
    private static readonly Color PaperWhite = new(1.0f, 1.0f, 1.0f, 1.0f);

    private Control? activeCanvasRoot;

    protected GameContent? Content { get; private set; }

    protected Dictionary<string, Texture2D> TextureCache { get; private set; } = new(StringComparer.Ordinal);

    protected Dictionary<string, Font> FontCache { get; private set; } = new(StringComparer.Ordinal);

    protected static readonly Color InkPanel = new(0.045f, 0.035f, 0.035f, 0.9f);
    protected static readonly Color GoldLine = new(0.82f, 0.62f, 0.34f, 1.0f);
    protected static readonly Color BloodLine = new(0.78f, 0.12f, 0.09f, 1.0f);
    protected static readonly Color CyanLine = new(0.18f, 0.58f, 0.70f, 1.0f);
    protected static readonly Color FinisherLine = new(0.55f, 0.22f, 0.82f, 1.0f);

    public void Initialize(
        GameContent content,
        Dictionary<string, Texture2D> textureCache,
        Dictionary<string, Font> fontCache)
    {
        Content = content;
        TextureCache = textureCache;
        FontCache = fontCache;
    }

    protected Control CreateCanvas()
    {
        ClearChildren();

        var outerBackground = new ColorRect
        {
            Color = LetterboxBlack,
            MouseFilter = MouseFilterEnum.Ignore
        };
        outerBackground.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(outerBackground);

        var root = new Control();
        root.CustomMinimumSize = DesignSize;
        root.Size = DesignSize;
        root.SetAnchorsPreset(LayoutPreset.TopLeft);
        ApplyCanvasTransform(root);
        AddChild(root);
        activeCanvasRoot = root;
        AddBackground(root);

        var contentRoot = new Control
        {
            CustomMinimumSize = BaseLayoutSize,
            Size = BaseLayoutSize,
            Scale = DesignToBaseScale
        };
        contentRoot.SetAnchorsPreset(LayoutPreset.TopLeft);
        root.AddChild(contentRoot);
        return contentRoot;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && activeCanvasRoot is not null)
        {
            ApplyCanvasTransform(activeCanvasRoot);
        }
    }

    private void ApplyCanvasTransform(Control root)
    {
        var viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 0 || viewportSize.Y <= 0)
        {
            viewportSize = DesignSize;
        }

        var scale = Math.Min(viewportSize.X / DesignSize.X, viewportSize.Y / DesignSize.Y);
        root.Scale = new Vector2(scale, scale);
        root.Position = (viewportSize - DesignSize * scale) * 0.5f;
        root.Size = DesignSize;
    }

    protected static void AddAt(Control parent, Control child, Vector2 position, Vector2 size)
    {
        child.Position = position;
        child.Size = size;
        child.CustomMinimumSize = size;
        parent.AddChild(child);
    }

    internal Texture2D? LoadAnimationTexture(string assetId) => LoadTexture(assetId);

    internal Font? LoadAnimationFont(string assetId) => LoadFont(assetId);

    internal static void AddAnimationNodeAt(Control parent, Control child, Vector2 position, Vector2 size) =>
        AddAt(parent, child, position, size);

    protected void AddImageAt(Control parent, string assetId, Vector2 position, Vector2 size, TextureRect.StretchModeEnum stretchMode)
    {
        AddAt(parent, CreateImage(assetId, size, stretchMode), position, size);
    }

    protected static void AddLabelAt(
        Control parent,
        string text,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color,
        HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        AddAt(parent, label, position, size);
    }

    protected Button CreateArtButton(string text, string iconAsset, Vector2 minSize, Color borderColor)
    {
        var button = new Button
        {
            Text = text,
            Icon = LoadTexture(iconAsset),
            CustomMinimumSize = minSize,
            ExpandIcon = true,
            Alignment = HorizontalAlignment.Center
        };
        button.AddThemeFontSizeOverride("font_size", 17);
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.82f, 0.48f));
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(borderColor, 0.92f));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(1.0f, 0.78f, 0.34f), 0.98f));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.95f, 0.48f, 0.18f), 1.0f));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        return button;
    }

    protected StyleBoxFlat CreateButtonStyle(Color borderColor, float alpha)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.045f, 0.035f, alpha),
            BorderColor = borderColor,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
    }

    protected static void MakeTransparentButton(Button button)
    {
        var empty = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", empty);
        button.AddThemeStyleboxOverride("hover", empty);
        button.AddThemeStyleboxOverride("pressed", empty);
        button.AddThemeStyleboxOverride("disabled", empty);
        button.AddThemeStyleboxOverride("focus", empty);
    }

    protected PanelContainer CreateMessagePanel(string text)
    {
        var panel = CreateFramedPanel(Vector2.Zero, GoldLine);
        var label = CreateMessage(text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.AddChild(label);
        return panel;
    }

    protected Control CreateChip(string text, Color borderColor)
    {
        var chip = new PanelContainer
        {
            CustomMinimumSize = new Vector2(34, 28),
            Size = new Vector2(34, 28)
        };
        chip.AddThemeStyleboxOverride("panel", CreateButtonStyle(borderColor, 0.94f));
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.86f, 0.52f));
        chip.AddChild(label);
        return chip;
    }

    protected static Label CreateBodyLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
    }

    protected static Label CreateSmallLabel(string text)
    {
        var label = CreateBodyLabel(text);
        label.AddThemeFontSizeOverride("font_size", 13);
        return label;
    }

    protected static Label CreateMessage(string text)
    {
        var label = CreateBodyLabel(text);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.78f, 0.35f));
        return label;
    }

    protected PanelContainer CreateInfoPanel(string text, Color color, string? iconAsset = null)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(136, 44) };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(color.R * 0.18f, color.G * 0.18f, color.B * 0.18f, 0.92f),
            BorderColor = color,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        panel.AddThemeStyleboxOverride("panel", style);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        row.Alignment = BoxContainer.AlignmentMode.Center;
        panel.AddChild(row);
        if (iconAsset is not null)
        {
            row.AddChild(CreateImage(iconAsset, new Vector2(26, 26), TextureRect.StretchModeEnum.KeepAspectCentered));
        }

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.AddChild(label);
        return panel;
    }

    protected PanelContainer CreateFramedPanel(Vector2 minSize, Color borderColor)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = minSize,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        var style = new StyleBoxFlat
        {
            BgColor = InkPanel,
            BorderColor = borderColor,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    protected TextureRect CreateImage(string assetId, Vector2 minSize, TextureRect.StretchModeEnum stretchMode)
    {
        return new TextureRect
        {
            Texture = LoadTexture(assetId),
            CustomMinimumSize = minSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = stretchMode,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = MouseFilterEnum.Ignore
        };
    }

    protected Texture2D? LoadTexture(string assetId)
    {
        if (TextureCache.TryGetValue(assetId, out var cached))
        {
            return cached;
        }

        var assets = Content?.AssetsById;
        if (assets is null || !assets.TryGetValue(assetId, out var asset))
        {
            return null;
        }

        if (asset.Path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            var image = new Image();
            var bytes = System.IO.File.ReadAllBytes(ProjectSettings.GlobalizePath(asset.Path));
            if (image.LoadPngFromBuffer(bytes) != Error.Ok)
            {
                return null;
            }

            if (image.GetWidth() > 1 && image.GetHeight() > 1)
            {
                image.GenerateMipmaps();
            }

            var pngTexture = ImageTexture.CreateFromImage(image);
            TextureCache[assetId] = pngTexture;
            return pngTexture;
        }

        var texture = GD.Load<Texture2D>(asset.Path);
        if (texture is not null)
        {
            TextureCache[assetId] = texture;
        }

        return texture;
    }

    protected Font? LoadFont(string assetId)
    {
        if (FontCache.TryGetValue(assetId, out var cached))
        {
            return cached;
        }

        var assets = Content?.AssetsById;
        if (assets is null || !assets.TryGetValue(assetId, out var asset))
        {
            return null;
        }

        var font = GD.Load<Font>(asset.Path);
        if (font is not null)
        {
            FontCache[assetId] = font;
        }

        return font;
    }

    protected void AddBackground(Control parent)
    {
        var paperBase = new ColorRect
        {
            Color = PaperWhite,
            MouseFilter = MouseFilterEnum.Ignore
        };
        paperBase.SetAnchorsPreset(LayoutPreset.FullRect);
        parent.AddChild(paperBase);

        var backLayer = CreateFixedBackgroundImage("asset.background.mvp_battle.back");
        parent.AddChild(backLayer);

        var frontLayer = CreateFixedBackgroundImage("asset.background.mvp_battle.front");
        parent.AddChild(frontLayer);
    }

    private TextureRect CreateFixedBackgroundImage(string assetId)
    {
        var layer = new TextureRect
        {
            Texture = LoadTexture(assetId),
            Position = Vector2.Zero,
            Size = DesignSize,
            CustomMinimumSize = DesignSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = MouseFilterEnum.Ignore
        };
        return layer;
    }

    protected static Control CreateFxLayer(string name)
    {
        return new Control
        {
            Name = name,
            Size = BaseLayoutSize,
            CustomMinimumSize = BaseLayoutSize,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 100
        };
    }

    internal async Task PulseNodeAsync(Control? node, float peakScale, double duration)
    {
        if (node is null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var originalScale = node.Scale;
        node.PivotOffset = node.Size * 0.5f;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "scale", originalScale * peakScale, duration);
        tween.TweenProperty(node, "scale", originalScale, duration);
        await AwaitTweenFinishedAsync(tween, duration * 2.0 + 0.25);
    }

    internal async Task ShakeNodeAsync(Control? node, float distance, double duration)
    {
        if (node is null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var originalPosition = node.Position;
        var step = duration / 4.0;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(distance, 0), step);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(-distance * 0.7f, 0), step);
        tween.TweenProperty(node, "position", originalPosition + new Vector2(distance * 0.35f, 0), step);
        tween.TweenProperty(node, "position", originalPosition, step);
        await AwaitTweenFinishedAsync(tween, duration + 0.25);
    }

    internal async Task LungeNodeAsync(Control? node, Vector2 offset, double duration)
    {
        if (node is null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var originalPosition = node.Position;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node, "position", originalPosition + offset, duration);
        tween.TweenProperty(node, "position", originalPosition, duration);
        await AwaitTweenFinishedAsync(tween, duration * 2.0 + 0.25);
    }

    internal async Task SpawnVfxAsync(Control? fxLayer, string assetId, Vector2 center, Vector2 size, Color tint, double duration)
    {
        if (fxLayer is null || !GodotObject.IsInstanceValid(fxLayer))
        {
            return;
        }

        var vfx = CreateImage(assetId, size, TextureRect.StretchModeEnum.KeepAspectCentered);
        vfx.Position = center - size * 0.5f;
        vfx.Size = size;
        vfx.CustomMinimumSize = size;
        vfx.Modulate = new Color(tint.R, tint.G, tint.B, 0f);
        vfx.PivotOffset = size * 0.5f;
        vfx.Scale = new Vector2(0.78f, 0.78f);
        vfx.ZIndex = 110;
        fxLayer.AddChild(vfx);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(vfx, "modulate", tint, duration * 0.35);
        tween.TweenProperty(vfx, "scale", new Vector2(1.12f, 1.12f), duration);
        tween.Chain().TweenProperty(vfx, "modulate", new Color(tint.R, tint.G, tint.B, 0f), duration * 0.45);
        await AwaitTweenFinishedAsync(tween, duration * 2.0 + 0.35);
        if (GodotObject.IsInstanceValid(vfx))
        {
            vfx.QueueFree();
        }
    }

    internal async Task AwaitTweenFinishedAsync(Tween tween, double timeoutSeconds)
    {
        if (!GodotObject.IsInstanceValid(tween))
        {
            return;
        }

        var tree = GetTree();
        if (tree is null)
        {
            await ToSignal(tween, "finished");
            return;
        }

        var completed = false;
        var completion = new TaskCompletionSource();
        var timeout = tree.CreateTimer(Math.Max(0.05, timeoutSeconds));

        void Complete()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            completion.TrySetResult();
        }

        tween.Finished += Complete;
        timeout.Timeout += Complete;

        try
        {
            await completion.Task;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(tween))
            {
                tween.Finished -= Complete;
            }

            if (GodotObject.IsInstanceValid(timeout))
            {
                timeout.Timeout -= Complete;
            }
        }

        if (GodotObject.IsInstanceValid(tween) && tween.IsRunning())
        {
            tween.Kill();
        }
    }

    protected async Task WaitAsync(double seconds)
    {
        await ToSignal(GetTree().CreateTimer(seconds), "timeout");
    }

    internal static Vector2 CenterOf(Control? node)
    {
        return node is null
            ? new Vector2(960, 540)
            : node.Position + node.Size * 0.5f;
    }

    protected void ClearChildren()
    {
        activeCanvasRoot = null;
        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                RemoveChild(node);
                node.QueueFree();
            }
        }
    }

    protected GameContent RequireContent()
    {
        return Content ?? throw new InvalidOperationException("Game content is not loaded.");
    }

}
