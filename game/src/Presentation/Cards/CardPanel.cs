using Godot;
using System.Text;
using System.Text.RegularExpressions;
using RoguelikeCardGame.Application.Battle;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Infrastructure.Content;

namespace RoguelikeCardGame.Presentation.Cards;

public static class CardPanel
{
	private static readonly Color InkText = new(0.13f, 0.075f, 0.04f);
	private static readonly Color PaperOutline = new(0.98f, 0.88f, 0.68f, 0.94f);
	private static readonly Color ShadowInk = new(0.03f, 0.018f, 0.012f, 0.65f);
	private static readonly Color RuleText = new(0.16f, 0.10f, 0.06f);

	public static Vector2 SizeForWidth(float width)
	{
		return SizeForWidth(width, CardType.Action);
	}

	public static Vector2 SizeForWidth(float width, CardType type)
	{
		var templateSize = CardPanelLayout.For(type).TemplateSize;
		return new Vector2(width, width * templateSize.Height / templateSize.Width);
	}

	public static Control Create(
		CardDefinition card,
		GameContent content,
		Func<string, Texture2D?> loadTexture,
		Func<string, Font?> loadFont,
		float width,
		bool dimmed = false,
		CardEnchantment? enchantment = null,
		CardPlayPreview? preview = null)
	{
		var layout = CardPanelLayout.For(card.Type);
		var templateSize = ToVector2(layout.TemplateSize);
		var displaySize = SizeForWidth(width, card.Type);
		var root = new Control
		{
			CustomMinimumSize = displaySize,
			Size = displaySize,
			ClipContents = true
		};

		var scale = width / templateSize.X;
		var canvas = new Control
		{
			CustomMinimumSize = templateSize,
			Size = templateSize,
			Scale = new Vector2(scale, scale),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		root.AddChild(canvas);

		var view = content.CardViewsById[card.Id];
		var heavyFont = loadFont("asset.font.source_han_sans_sc.heavy");
		var mediumFont = loadFont("asset.font.source_han_sans_sc.medium");
		var artRect = ToRect2(layout.ArtRect);

		var artWindow = new Control
		{
			Position = artRect.Position,
			Size = artRect.Size,
			CustomMinimumSize = artRect.Size,
			ClipContents = true,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		canvas.AddChild(artWindow);

		var artTexture = loadTexture(view.ArtAsset);
		var artSize = FitTextureSize(artTexture, artRect.Size);
		var art = new TextureRect
		{
			Texture = artTexture,
			Position = (artRect.Size - artSize) * 0.5f,
			Size = artSize,
			CustomMinimumSize = artSize,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		artWindow.AddChild(art);

		var template = new TextureRect
		{
			Texture = loadTexture(view.TemplateAsset),
			Position = Vector2.Zero,
			Size = templateSize,
			CustomMinimumSize = templateSize,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		canvas.AddChild(template);

		if (TryCostText(card, out var costText))
		{
			canvas.AddChild(CreateImpactLabel(
				costText,
				ToRect2(layout.CostRect),
				layout.CostFontSize,
				InkText,
				outlineSize: Math.Max(2, layout.CostFontSize / 12),
				heavyFont));
		}

		canvas.AddChild(CreateTitleLabel(
			content.CardName(card.Id),
			ToRect2(layout.NameRect),
			layout.NameFontSize,
			heavyFont));

		canvas.AddChild(CreateRulesLabel(
			RulesTextWithBeatActions(card, content),
			ToRect2(layout.RulesRect),
			layout.RulesFontSize,
			mediumFont,
			heavyFont));

		canvas.AddChild(CreateEnergyBadge(card, enchantment, preview, ToRect2(layout.MetaRect), layout.MetaFontSize, mediumFont, heavyFont));

		if (dimmed)
		{
			root.Modulate = new Color(0.55f, 0.55f, 0.55f, 1.0f);
		}

		return root;
	}

	private static Vector2 ToVector2(CardPanelSize size) => new(size.Width, size.Height);

	private static Rect2 ToRect2(CardPanelRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

	private static Vector2 FitTextureSize(Texture2D? texture, Vector2 bounds)
	{
		if (texture is null || texture.GetWidth() <= 0 || texture.GetHeight() <= 0)
		{
			return bounds;
		}

		var source = new Vector2(texture.GetWidth(), texture.GetHeight());
		var scale = Math.Min(bounds.X / source.X, bounds.Y / source.Y);
		return source * scale;
	}

	private static Label CreateImpactLabel(
		string text,
		Rect2 rect,
		int fontSize,
		Color color,
		int outlineSize,
		Font? font)
	{
		var label = new Label
		{
			Text = text,
			Position = rect.Position,
			Size = rect.Size,
			CustomMinimumSize = rect.Size,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off,
			ClipText = true,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_outline_color", PaperOutline);
		label.AddThemeColorOverride("font_shadow_color", ShadowInk);
		label.AddThemeConstantOverride("outline_size", outlineSize);
		label.AddThemeConstantOverride("shadow_offset_x", 4);
		label.AddThemeConstantOverride("shadow_offset_y", 5);
		if (font is not null)
		{
			label.AddThemeFontOverride("font", font);
		}

		return label;
	}

	private static Label CreateTitleLabel(string text, Rect2 rect, int fontSize, Font? font)
	{
		var label = new Label
		{
			Text = text,
			Position = rect.Position,
			Size = rect.Size,
			CustomMinimumSize = rect.Size,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off,
			ClipText = true,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", InkText);
		label.AddThemeColorOverride("font_outline_color", new Color(0.94f, 0.82f, 0.60f, 0.7f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0.04f, 0.025f, 0.015f, 0.45f));
		label.AddThemeConstantOverride("outline_size", 3);
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 3);
		if (font is not null)
		{
			label.AddThemeFontOverride("font", font);
		}

		return label;
	}

	private static RichTextLabel CreateRulesLabel(string text, Rect2 rect, int fontSize, Font? mediumFont, Font? heavyFont)
	{
		var label = new RichTextLabel
		{
			Text = HighlightRules(text),
			Position = rect.Position,
			Size = rect.Size,
			CustomMinimumSize = rect.Size,
			BbcodeEnabled = true,
			FitContent = false,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("normal_font_size", fontSize);
		label.AddThemeColorOverride("default_color", RuleText);
		label.AddThemeColorOverride("font_shadow_color", new Color(0.72f, 0.56f, 0.36f, 0.28f));
		label.AddThemeConstantOverride("shadow_offset_x", 1);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		if (mediumFont is not null)
		{
			label.AddThemeFontOverride("normal_font", mediumFont);
		}

		if (heavyFont is not null)
		{
			label.AddThemeFontOverride("bold_font", heavyFont);
		}

		return label;
	}

	private static string HighlightRules(string text)
	{
		var escaped = EscapeBbcode(text);
		escaped = escaped
			.Replace("行动牌", "[color=#8f1c16]行动牌[/color]", StringComparison.Ordinal)
			.Replace("终结牌", "[color=#6b35a8]终结牌[/color]", StringComparison.Ordinal)
			.Replace("行动点", "[color=#a06a12]行动点[/color]", StringComparison.Ordinal)
			.Replace("彩能", "[color=#6b35a8]彩能[/color]", StringComparison.Ordinal)
			.Replace("防御", "[color=#247a90]防御[/color]", StringComparison.Ordinal)
			.Replace("伤害", "[color=#a61f17]伤害[/color]", StringComparison.Ordinal)
			.Replace("抽", "[color=#247a90]抽[/color]", StringComparison.Ordinal);
		return escaped;
	}

	private static string RulesTextWithBeatActions(CardDefinition card, GameContent content)
	{
		var rules = content.CardRules(card.Id);
		if (card.BeatActions.Count == 0)
		{
			return rules;
		}

		return $"{rules}\n{BeatActionSummary(card.BeatActions)}";
	}

	private static string BeatActionSummary(IReadOnlyList<BeatActionDefinition> actions)
	{
		return string.Join(" -> ", actions.Select(BeatActionText));
	}

	private static string BeatActionText(BeatActionDefinition action)
	{
		var text = action.Kind switch
		{
			BeatActionKind.Attack => $"{BeatAttackText(action.AttackType)} {action.Value}",
			BeatActionKind.Block => $"格挡 {action.Value}",
			BeatActionKind.Dodge => $"闪避 {action.DodgeChancePercent}%",
			_ => action.Kind.ToString()
		};
		return action.Repeat > 1 ? $"{text} x{action.Repeat}" : text;
	}

	private static string BeatAttackText(BeatAttackType? attackType)
	{
		return attackType switch
		{
			BeatAttackType.Slash => "斩击",
			BeatAttackType.Strike => "钝击",
			BeatAttackType.Projectile => "弹射",
			_ => "斩击"
		};
	}

	private static string EscapeBbcode(string text)
	{
		var builder = new StringBuilder(text.Length);
		foreach (var c in text)
		{
			builder.Append(c switch
			{
				'[' => "[lb]",
				']' => "[rb]",
				_ => c
			});
		}

		return builder.ToString();
	}

	private static bool TryCostText(CardDefinition card, out string text)
	{
		text = "";
		if (card.Type == CardType.Action)
		{
			text = card.Cost.ToString();
			return true;
		}

		if (card.Type == CardType.Finisher)
		{
			text = card.ColorEnergyCost is null
				? "?"
				: card.ColorEnergyCost.Mode switch
				{
					ColorEnergySpendMode.Fixed => card.ColorEnergyCost.Amount.ToString(),
					ColorEnergySpendMode.X => "X",
					ColorEnergySpendMode.All => "全",
					_ => "?"
				};
			return true;
		}

		return false;
	}

	private static Control CreateEnergyBadge(CardDefinition card, CardEnchantment? enchantment, CardPlayPreview? preview, Rect2 rect, int fontSize, Font? mediumFont, Font? heavyFont)
	{
		var panel = new PanelContainer
		{
			Position = rect.Position,
			Size = rect.Size,
			CustomMinimumSize = rect.Size,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		var badgeColor = card.Type == CardType.Action
			? ColorForEnergy(enchantment?.Color ?? ColorType.Colorless)
			: new Color(0.56f, 0.34f, 0.76f, 0.82f);
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(badgeColor.R, badgeColor.G, badgeColor.B, 0.28f),
			BorderColor = new Color(badgeColor.R, badgeColor.G, badgeColor.B, 0.80f),
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
			ContentMarginTop = 4,
			ContentMarginBottom = 4
		});

		var text = card.Type == CardType.Action
			? $"附魔 {ColorName(enchantment?.Color ?? ColorType.Colorless)} / 生成 {preview?.GeneratedColorEnergyAmount ?? card.ColorEnergyGeneration?.Amount ?? 0} {ColorName(preview?.GeneratedColorEnergyColor ?? enchantment?.Color ?? ColorType.Colorless)}彩能"
			: $"消耗 {FinisherCostText(card)} 彩能 / 当前可消耗 {preview?.ColorEnergyCost ?? 0}";
		var label = CreateImpactLabel(text, new Rect2(Vector2.Zero, rect.Size), fontSize, InkText, 2, mediumFont ?? heavyFont);
		panel.AddChild(label);
		return panel;
	}

	private static string FinisherCostText(CardDefinition card)
	{
		if (card.ColorEnergyCost is null)
		{
			return "?";
		}

		return card.ColorEnergyCost.Mode switch
		{
			ColorEnergySpendMode.Fixed => card.ColorEnergyCost.Amount.ToString(),
			ColorEnergySpendMode.X => $"X 至少 {card.ColorEnergyCost.MinAmount}",
			ColorEnergySpendMode.All => $"全部 至少 {card.ColorEnergyCost.MinAmount}",
			_ => "?"
		};
	}

	private static Color ColorForEnergy(ColorType color)
	{
		return color switch
		{
			ColorType.Red => new Color(0.78f, 0.13f, 0.10f),
			ColorType.Yellow => new Color(0.94f, 0.70f, 0.12f),
			ColorType.Blue => new Color(0.15f, 0.42f, 0.86f),
			ColorType.Green => new Color(0.20f, 0.62f, 0.28f),
			ColorType.Purple => new Color(0.55f, 0.22f, 0.82f),
			_ => new Color(0.64f, 0.58f, 0.48f)
		};
	}

	private static string ColorName(ColorType color)
	{
		return color switch
		{
			ColorType.Red => "红色",
			ColorType.Yellow => "黄色",
			ColorType.Blue => "蓝色",
			ColorType.Green => "绿色",
			ColorType.Purple => "紫色",
			_ => "无色"
		};
	}

}
