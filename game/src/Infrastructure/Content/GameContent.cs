using System.Text.Json;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Colors;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Relics;
using RoguelikeCardGame.Domain.Weapons;

namespace RoguelikeCardGame.Infrastructure.Content;

public sealed record CardViewDefinition
{
    public required string Id { get; init; }

    public required string NameKey { get; init; }

    public required string RulesKey { get; init; }

    public required string FlavorKey { get; init; }

    public required string TemplateAsset { get; init; }

    public required string ArtAsset { get; init; }
}

public sealed record EnemyViewDefinition
{
    public required string Id { get; init; }

    public required string NameKey { get; init; }

    public required string StandAsset { get; init; }

    public Dictionary<string, string> IntentTextKeys { get; init; } = new(StringComparer.Ordinal);
}

public sealed record RelicViewDefinition
{
    public required string Id { get; init; }

    public required string NameKey { get; init; }

    public required string RulesKey { get; init; }

    public required string IconAsset { get; init; }
}

public sealed record AssetDefinition
{
    public required string Id { get; init; }

    public required string Type { get; init; }

    public required string Path { get; init; }
}

public sealed record StartingPoolEntryDefinition
{
    public required string CardId { get; init; }

    public int Count { get; init; }
}

public sealed record CardPoolDefinition
{
    public required string Id { get; init; }

    public required string PoolType { get; init; }

    public required string WeaponId { get; init; }

    public List<StartingPoolEntryDefinition> StartingEntries { get; init; } = new();

    public Dictionary<string, List<string>> RewardByRarity { get; init; } = new(StringComparer.Ordinal);

    public List<string> Tags { get; init; } = new();

    public List<string> ExpandedStartingCardIds()
    {
        return StartingEntries
            .SelectMany(entry => Enumerable.Repeat(entry.CardId, entry.Count))
            .ToList();
    }
}

public sealed class GameContent
{
    public IReadOnlyDictionary<string, CardDefinition> CardsById { get; private init; } =
        new Dictionary<string, CardDefinition>();

    public IReadOnlyDictionary<string, WeaponDefinition> WeaponsById { get; private init; } =
        new Dictionary<string, WeaponDefinition>();

    public IReadOnlyDictionary<string, ColorDefinition> ColorsById { get; private init; } =
        new Dictionary<string, ColorDefinition>();

    public IReadOnlyDictionary<string, CardPoolDefinition> CardPoolsById { get; private init; } =
        new Dictionary<string, CardPoolDefinition>();

    public IReadOnlyDictionary<string, EnemyDefinition> EnemiesById { get; private init; } =
        new Dictionary<string, EnemyDefinition>();

    public IReadOnlyDictionary<string, EncounterDefinition> EncountersById { get; private init; } =
        new Dictionary<string, EncounterDefinition>();

    public IReadOnlyDictionary<string, RelicDefinition> RelicsById { get; private init; } =
        new Dictionary<string, RelicDefinition>();

    public IReadOnlyDictionary<string, CardViewDefinition> CardViewsById { get; private init; } =
        new Dictionary<string, CardViewDefinition>();

    public IReadOnlyDictionary<string, EnemyViewDefinition> EnemyViewsById { get; private init; } =
        new Dictionary<string, EnemyViewDefinition>();

    public IReadOnlyDictionary<string, RelicViewDefinition> RelicViewsById { get; private init; } =
        new Dictionary<string, RelicViewDefinition>();

    public IReadOnlyDictionary<string, AssetDefinition> AssetsById { get; private init; } =
        new Dictionary<string, AssetDefinition>();

    public IReadOnlyDictionary<string, string> Text { get; private init; } =
        new Dictionary<string, string>();

    public required RunSequenceDefinition MvpRun { get; init; }

    public string T(string key) => Text.TryGetValue(key, out var value) ? value : key;

    public string CardName(string cardId) => T(CardViewsById[cardId].NameKey);

    public string CardRules(string cardId) => T(CardViewsById[cardId].RulesKey);

    public string WeaponName(string weaponId) => T($"{weaponId}.name");

    public string WeaponDescription(string weaponId) => T($"{weaponId}.description");

    public IReadOnlyList<string> ExpandedStartingCardIdsForWeapon(string weaponId)
    {
        if (!WeaponsById.TryGetValue(weaponId, out var weapon) ||
            !CardPoolsById.TryGetValue(weapon.StartingPoolId, out var pool))
        {
            return [];
        }

        return pool.ExpandedStartingCardIds();
    }

    public string CardLabel(string cardId) => $"{CardName(cardId)}\n{CardRules(cardId)}";

    public string EnemyName(string enemyId) => T(EnemyViewsById[enemyId].NameKey);

    public string EnemyIntentText(string enemyId, string intentId)
    {
        return EnemyViewsById.TryGetValue(enemyId, out var view) &&
               view.IntentTextKeys.TryGetValue(intentId, out var key)
            ? T(key)
            : intentId;
    }

    public string RelicName(string relicId) => T(RelicViewsById[relicId].NameKey);

    public string RelicRules(string relicId) => T(RelicViewsById[relicId].RulesKey);

    public static GameContent LoadFromProject()
    {
        return LoadFromDataRoot(ResolveProjectDataRoot());
    }

    public static GameContent LoadFromDataRoot(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        var root = Path.GetFullPath(dataRoot);
        var gameplay = Path.Combine(root, "gameplay");
        var presentation = Path.Combine(root, "presentation");

        var cards = ReadItems(Path.Combine(gameplay, "cards", "cards.json"), ParseCard);
        var weapons = ReadItems(Path.Combine(gameplay, "weapons", "weapons.json"), ParseWeapon);
        var colors = ReadItems(Path.Combine(gameplay, "colors", "colors.json"), ParseColor);
        var cardPools = ReadItems(Path.Combine(gameplay, "card_pools", "card_pools.json"), ParseCardPool);
        var enemies = ReadItems(Path.Combine(gameplay, "enemies", "enemies.json"), ParseEnemy);
        var encounters = ReadItems(Path.Combine(gameplay, "encounters", "encounters.json"), ParseEncounter);
        var relics = ReadItems(Path.Combine(gameplay, "relics", "relics.json"), ParseRelic);

        var cardViews = ReadItems(Path.Combine(presentation, "card_views.json"), ParseCardView);
        var enemyViews = ReadItems(Path.Combine(presentation, "enemy_views.json"), ParseEnemyView);
        var relicViews = ReadItems(Path.Combine(presentation, "relic_views.json"), ParseRelicView);
        var assets = ReadItems(Path.Combine(presentation, "assets.json"), ParseAsset);

        var text = ReadLocalization(Path.Combine(root, "localization", "zh_hans.json"));
        var run = ReadRunSequence(Path.Combine(gameplay, "runs", "mvp_run.json"));
        var allCardViews = cardViews.Concat(CreateFallbackCardViews(cards)).ToList();

        return new GameContent
        {
            CardsById = cards.ToDictionary(card => card.Id, StringComparer.Ordinal),
            WeaponsById = weapons.ToDictionary(weapon => weapon.Id, StringComparer.Ordinal),
            ColorsById = colors.ToDictionary(color => color.Id, StringComparer.Ordinal),
            CardPoolsById = cardPools.ToDictionary(pool => pool.Id, StringComparer.Ordinal),
            EnemiesById = enemies.ToDictionary(enemy => enemy.Id, StringComparer.Ordinal),
            EncountersById = encounters.ToDictionary(encounter => encounter.Id, StringComparer.Ordinal),
            RelicsById = relics.ToDictionary(relic => relic.Id, StringComparer.Ordinal),
            CardViewsById = allCardViews
                .GroupBy(view => view.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal),
            EnemyViewsById = enemyViews.ToDictionary(view => view.Id, StringComparer.Ordinal),
            RelicViewsById = relicViews.ToDictionary(view => view.Id, StringComparer.Ordinal),
            AssetsById = assets.ToDictionary(asset => asset.Id, StringComparer.Ordinal),
            Text = text,
            MvpRun = run
        };
    }

    private static string ResolveProjectDataRoot()
    {
        var godotProjectSettings = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("Godot.ProjectSettings"))
            .FirstOrDefault(type => type is not null);
        var globalizePath = godotProjectSettings?.GetMethod("GlobalizePath", [typeof(string)]);
        if (globalizePath?.Invoke(null, ["res://data"]) is string godotDataRoot &&
            !string.IsNullOrWhiteSpace(godotDataRoot))
        {
            return godotDataRoot;
        }

        var cwd = Directory.GetCurrentDirectory();
        var cwdData = Path.Combine(cwd, "data");
        if (Directory.Exists(cwdData))
        {
            return cwdData;
        }

        return Path.Combine(cwd, "game", "data");
    }

    private static List<T> ReadItems<T>(string path, Func<JsonElement, T> parse)
    {
        try
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var result = new List<T>();
            var index = 0;
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                try
                {
                    result.Add(parse(item));
                }
                catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException)
                {
                    var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                    var label = string.IsNullOrWhiteSpace(id) ? $"item[{index}]" : $"item[{index}] {id}";
                    throw new InvalidDataException($"Failed to parse {label} in '{path}': {ex.Message}", ex);
                }

                index++;
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to read JSON content file '{path}': {ex.Message}", ex);
        }
    }

    private static Dictionary<string, string> ReadLocalization(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            return document.RootElement.GetProperty("entries")
                .EnumerateObject()
                .ToDictionary(item => item.Name, item => item.Value.GetString() ?? item.Name, StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidDataException($"Failed to read localization file '{path}': {ex.Message}", ex);
        }
    }

    private static RunSequenceDefinition ReadRunSequence(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var root = document.RootElement;
            var player = root.GetProperty("player");
            var starterDeck = new List<string>();
            foreach (var entry in root.GetProperty("starter_deck").EnumerateArray())
            {
                var cardId = entry.GetProperty("card_id").GetStringRequired("card_id");
                var count = entry.GetProperty("count").GetInt32();
                for (var i = 0; i < count; i++)
                {
                    starterDeck.Add(cardId);
                }
            }

            return new RunSequenceDefinition
            {
                Id = root.GetProperty("id").GetStringRequired("id"),
                PlayerMaxHp = player.GetProperty("max_hp").GetInt32(),
                BaseActionPoints = player.GetProperty("base_action_points").GetInt32(),
                CardsPerTurn = player.GetProperty("cards_per_turn").GetInt32(),
                StarterDeck = starterDeck,
                EncounterSequence = root.GetProperty("nodes")
                    .EnumerateArray()
                    .OrderBy(node => node.GetProperty("order").GetInt32())
                    .Select(node => node.GetProperty("encounter_id").GetStringRequired("encounter_id"))
                    .ToList(),
                BossEncounterId = root.GetProperty("completion").GetProperty("boss_encounter_id")
                    .GetStringRequired("boss_encounter_id")
            };
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidDataException($"Failed to read run sequence file '{path}': {ex.Message}", ex);
        }
    }

    private static IEnumerable<CardViewDefinition> CreateFallbackCardViews(IEnumerable<CardDefinition> cards)
    {
        foreach (var card in cards)
        {
            yield return new CardViewDefinition
            {
                Id = card.Id,
                NameKey = $"{card.Id}.name",
                RulesKey = $"{card.Id}.rules",
                FlavorKey = $"{card.Id}.flavor",
                TemplateAsset = card.Type == CardType.Finisher ? "asset.card.template.finisher" : "asset.card.template.action",
                ArtAsset = card.Type == CardType.Finisher
                    ? card.WeaponId == "weapon.mechanical_arm" ? "asset.card.bulwark_finish.art" : "asset.card.burst_finish.art"
                    : card.WeaponId == "weapon.mechanical_arm" ? "asset.card.basic_guard.art" : "asset.card.basic_strike.art"
            };
        }
    }

    private static CardDefinition ParseCard(JsonElement item)
    {
        var energy = item.GetProperty("energy");
        return new CardDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            WeaponId = item.GetProperty("weapon_id").GetStringRequired("weapon_id"),
            Type = ParseCardType(item.GetProperty("card_type").GetStringRequired("card_type")),
            Cost = ParseActionPointCost(item),
            Costs = item.GetProperty("costs").EnumerateArray().Select(ParseResourceAmount).ToList(),
            Requirements = item.GetProperty("requirements").EnumerateArray().Select(ParseContentEffect).ToList(),
            Targeting = ParseCardTargeting(item.GetProperty("targeting")),
            ColorEnergyGeneration = TryParseColorEnergyGeneration(energy),
            ColorEnergyCost = TryParseColorEnergyCost(energy),
            TargetRule = ParseCardTargetRule(item.GetProperty("targeting")),
            Effects = item.GetProperty("effects").EnumerateArray().SelectMany(ParseCardEffect).ToList(),
            ColorInteractions = ParseCardColorInteractions(item.GetProperty("color_interactions")),
            AfterPlay = item.GetProperty("after_play").EnumerateArray().Select(ParseContentEffect).ToList(),
            Rarity = ParseCardRarity(item.GetProperty("rarity").GetStringRequired("rarity")),
            Balance = ParseCardBalance(item.GetProperty("balance")),
            Tags = ReadStringList(item, "tags")
        };
    }

    private static ResourceAmountDefinition ParseResourceAmount(JsonElement item)
    {
        return new ResourceAmountDefinition
        {
            Resource = item.GetProperty("resource").GetStringRequired("resource"),
            Amount = item.GetProperty("amount").GetInt32()
        };
    }

    private static ColorEnergyGeneration? TryParseColorEnergyGeneration(JsonElement energy)
    {
        if (!energy.TryGetProperty("generate", out var generate) || generate.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var source = generate.GetProperty("color_source").GetStringRequired("color_source") switch
        {
            "enchantment" => ColorEnergyColorSource.Enchantment,
            "fixed_color" => ColorEnergyColorSource.FixedColor,
            _ => ColorEnergyColorSource.Colorless
        };

        return new ColorEnergyGeneration
        {
            Amount = generate.GetProperty("amount").GetInt32(),
            ColorSource = source,
            FixedColorId = generate.TryGetProperty("fixed_color_id", out var fixedColorId)
                ? fixedColorId.GetString()
                : null,
            FixedColor = generate.TryGetProperty("fixed_color_id", out var fixedColor)
                ? ParseColorType(fixedColor.GetStringRequired("fixed_color_id"))
                : null
        };
    }

    private static ColorEnergyCost? TryParseColorEnergyCost(JsonElement energy)
    {
        if (!energy.TryGetProperty("consume", out var consume) || consume.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var mode = consume.GetProperty("mode").GetStringRequired("mode") switch
        {
            "fixed" => ColorEnergySpendMode.Fixed,
            "x" => ColorEnergySpendMode.X,
            "all" => ColorEnergySpendMode.All,
            var value => throw new InvalidOperationException($"Unknown color energy consume mode '{value}'.")
        };

        return new ColorEnergyCost
        {
            Mode = mode,
            Amount = consume.TryGetProperty("amount", out var amount) ? amount.GetInt32() : 0,
            MinAmount = consume.GetProperty("min_amount").GetInt32(),
            ColorFilter = consume.TryGetProperty("color_filter", out var colorFilter)
                ? colorFilter.GetStringRequired("color_filter")
                : "any"
        };
    }

    private static IEnumerable<EffectDefinition> ParseCardEffect(JsonElement item)
    {
        var op = item.GetProperty("op").GetStringRequired("op");
        var target = ParseTargetRef(item);
        return op switch
        {
            "damage" => [new EffectDefinition { Type = "damage", Target = target, Value = ReadOptionalInt(item, "amount") }],
            "multi_hit_damage" => [new EffectDefinition
            {
                Type = "damage",
                Target = target,
                Value = Math.Max(0, ReadOptionalInt(item, "amount") ?? 0) * Math.Max(1, ReadOptionalInt(item, "hits") ?? 1)
            }],
            "random_damage_per_energy" => [new EffectDefinition { Type = "damage", Target = "all_enemies", Value = ReadOptionalInt(item, "amount") }],
            "gain_block" or "gain_block_per_energy" => [new EffectDefinition { Type = "block", Target = "self", Value = ReadOptionalInt(item, "amount") }],
            "draw_cards" => [new EffectDefinition { Type = "draw_cards", Target = "self", Value = ReadOptionalInt(item, "amount") }],
            _ => [new EffectDefinition { Type = op, Target = target, Value = ReadOptionalInt(item, "amount") }]
        };
    }

    private static EffectDefinition ParseContentEffect(JsonElement item)
    {
        var type = item.TryGetProperty("op", out var op)
            ? op.GetStringRequired("op")
            : item.TryGetProperty("type", out var typeElement)
                ? typeElement.GetStringRequired("type")
                : "unspecified";
        return new EffectDefinition
        {
            Type = type,
            Target = ParseTargetRef(item),
            Value = ReadOptionalInt(item, "amount"),
            Threshold = ReadOptionalInt(item, "threshold"),
            Effect = item.TryGetProperty("effect", out var nested) && nested.ValueKind == JsonValueKind.Object
                ? ParseContentEffect(nested)
                : null,
            Extra = item.EnumerateObject()
                .Where(property => property.Name is not ("op" or "type" or "target" or "amount" or "threshold" or "effect"))
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal)
        };
    }

    private static int? ReadOptionalInt(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
    }

    private static TargetRule ParseCardTargetRule(JsonElement targeting)
    {
        var mode = targeting.GetProperty("mode").GetStringRequired("targeting.mode");
        var side = targeting.GetProperty("side").GetStringRequired("targeting.side");
        return (mode, side) switch
        {
            ("single", "enemy") => TargetRule.SingleEnemy,
            ("all", "enemy") => TargetRule.AllEnemies,
            ("random", "enemy") => TargetRule.AllEnemies,
            ("self", "player") => TargetRule.Self,
            ("none", _) => TargetRule.None,
            _ => throw new InvalidOperationException($"Unknown targeting rule '{mode}/{side}'.")
        };
    }

    private static CardTargetingDefinition ParseCardTargeting(JsonElement item)
    {
        return new CardTargetingDefinition
        {
            Mode = item.GetProperty("mode").GetStringRequired("targeting.mode"),
            Side = item.GetProperty("side").GetStringRequired("targeting.side"),
            Required = item.GetProperty("required").GetBoolean()
        };
    }

    private static CardColorInteractionsDefinition ParseCardColorInteractions(JsonElement item)
    {
        var enchantment = item.GetProperty("enchantment");
        return new CardColorInteractionsDefinition
        {
            Enchantment = new CardEnchantmentRulesDefinition
            {
                CanBeEnchanted = enchantment.GetProperty("can_be_enchanted").GetBoolean(),
                GeneratedEnergyColor = enchantment.GetProperty("generated_energy_color").GetStringRequired("generated_energy_color"),
                SelfEffects = enchantment.TryGetProperty("self_effects", out var selfEffects)
                    ? selfEffects.EnumerateArray().Select(ParseContentEffect).ToList()
                    : []
            },
            FinisherColorEffects = item.GetProperty("finisher_color_effects")
                .EnumerateArray()
                .Select(effect => new FinisherColorEffectsDefinition
                {
                    ColorId = effect.GetProperty("color_id").GetStringRequired("color_id"),
                    Effects = effect.GetProperty("effects").EnumerateArray().Select(ParseContentEffect).ToList(),
                    StackLimit = effect.GetProperty("stack_limit").GetInt32()
                })
                .ToList()
        };
    }

    private static CardBalanceDefinition ParseCardBalance(JsonElement item)
    {
        return new CardBalanceDefinition
        {
            Role = item.GetProperty("role").GetStringRequired("balance.role"),
            Tier = item.GetProperty("tier").GetInt32()
        };
    }

    private static WeaponDefinition ParseWeapon(JsonElement item)
    {
        return new WeaponDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            StartingPoolId = item.GetProperty("starting_pool_id").GetStringRequired("starting_pool_id"),
            RewardPoolId = item.GetProperty("reward_pool_id").GetStringRequired("reward_pool_id"),
            MainHandAllowed = item.GetProperty("main_hand_allowed").GetBoolean(),
            OffHandAllowed = item.GetProperty("off_hand_allowed").GetBoolean(),
            Tags = ReadStringList(item, "tags")
        };
    }

    private static ColorDefinition ParseColor(JsonElement item)
    {
        var stackRule = item.GetProperty("stack_rule");
        var template = item.GetProperty("base_effect_template");
        return new ColorDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            Color = ParseColorType(item.GetProperty("id").GetStringRequired("id")),
            Role = item.GetProperty("role").GetStringRequired("role"),
            BaseEffectTemplate = new ColorEffectTemplateDefinition
            {
                Op = template.GetProperty("op").GetStringRequired("base_effect_template.op"),
                Extra = template.EnumerateObject()
                    .Where(property => property.Name != "op")
                    .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal)
            },
            StackRule = new ColorStackRuleDefinition
            {
                Mode = stackRule.GetProperty("mode").GetStringRequired("stack_rule.mode"),
                MaxPerCard = stackRule.GetProperty("max_per_card").GetInt32()
            },
            Tags = ReadStringList(item, "tags")
        };
    }

    private static CardPoolDefinition ParseCardPool(JsonElement item)
    {
        var rewardByRarity = item.GetProperty("reward_by_rarity")
            .EnumerateObject()
            .ToDictionary(
                entry => entry.Name,
                entry => entry.Value.EnumerateArray().Select(value => value.GetStringRequired(entry.Name)).ToList(),
                StringComparer.Ordinal);

        return new CardPoolDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            PoolType = item.GetProperty("pool_type").GetStringRequired("pool_type"),
            WeaponId = item.GetProperty("weapon_id").GetStringRequired("weapon_id"),
            StartingEntries = item.GetProperty("starting_entries").EnumerateArray()
                .Select(entry => new StartingPoolEntryDefinition
                {
                    CardId = entry.GetProperty("card_id").GetStringRequired("card_id"),
                    Count = entry.GetProperty("count").GetInt32()
                })
                .ToList(),
            RewardByRarity = rewardByRarity,
            Tags = ReadStringList(item, "tags")
        };
    }

    private static EnemyDefinition ParseEnemy(JsonElement item)
    {
        var rank = item.GetProperty("rank").GetStringRequired("rank");
        var tags = new List<string> { rank };
        tags.AddRange(ReadStringList(item, "tags"));

        return new EnemyDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            MaxHp = item.GetProperty("stats").GetProperty("max_hp").GetInt32(),
            IntentSequence = item.GetProperty("ai").GetProperty("intents").EnumerateArray().Select(ParseEnemyIntent).ToList(),
            StatusImmunities = ReadStringList(item, "immunities"),
            Tags = tags.Distinct(StringComparer.Ordinal).ToList()
        };
    }

    private static EnemyIntentDefinition ParseEnemyIntent(JsonElement item)
    {
        return new EnemyIntentDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            IntentType = ParseEnemyIntentType(item.GetProperty("preview")),
            Effects = item.GetProperty("effects").EnumerateArray().Select(ParseEnemyEffect).ToList()
        };
    }

    private static EffectDefinition ParseEnemyEffect(JsonElement item)
    {
        var op = item.GetProperty("op").GetStringRequired("op");
        return new EffectDefinition
        {
            Type = op switch
            {
                "gain_block" => "gain_block",
                _ => op
            },
            Target = ParseTargetRef(item),
            Value = item.GetProperty("amount").GetInt32()
        };
    }

    private static EncounterDefinition ParseEncounter(JsonElement item)
    {
        var reward = item.GetProperty("reward_profile");
        return new EncounterDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            NodeType = ParseEncounterNodeType(item.GetProperty("node_type").GetStringRequired("node_type")),
            Enemies = item.GetProperty("enemies").EnumerateArray()
                .Select(enemy => new EncounterEnemyDefinition
                {
                    InstanceId = enemy.GetProperty("instance_id").GetStringRequired("instance_id"),
                    EnemyId = enemy.GetProperty("enemy_id").GetStringRequired("enemy_id")
                })
                .ToList(),
            RewardProfile = new EncounterRewardProfileDefinition
            {
                CardPackIds = ReadStringList(reward, "card_pack_ids"),
                RelicId = reward.TryGetProperty("relic_id", out var relicId) && relicId.ValueKind != JsonValueKind.Null
                    ? relicId.GetString()
                    : null
            },
            TeachingGoalKey = item.GetProperty("teaching_goal_key").GetStringRequired("teaching_goal_key"),
            DifficultyNote = item.GetProperty("difficulty_note").GetStringRequired("difficulty_note")
        };
    }

    private static RelicDefinition ParseRelic(JsonElement item)
    {
        return new RelicDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            Rarity = ParseRelicRarity(item.GetProperty("rarity").GetStringRequired("rarity")),
            Trigger = item.GetProperty("trigger").GetStringRequired("trigger"),
            Conditions = item.GetProperty("conditions").EnumerateArray()
                .Select(condition => new RelicConditionDefinition
                {
                    Type = condition.GetProperty("op").GetStringRequired("op"),
                    Value = condition.TryGetProperty("value", out var value) ? value.GetString() : null
                })
                .ToList(),
            Effects = item.GetProperty("effects").EnumerateArray().Select(ParseRelicEffect).ToList(),
            StackRule = ParseRelicStackRule(item.GetProperty("stack_rule").GetStringRequired("stack_rule"))
        };
    }

    private static EffectDefinition ParseRelicEffect(JsonElement item)
    {
        var op = item.GetProperty("op").GetStringRequired("op");
        return new EffectDefinition
        {
            Type = op,
            Target = ParseTargetRef(item),
            Value = item.GetProperty("amount").GetInt32()
        };
    }

    private static CardViewDefinition ParseCardView(JsonElement item)
    {
        return new CardViewDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            NameKey = item.GetProperty("name_key").GetStringRequired("name_key"),
            RulesKey = item.GetProperty("rules_key").GetStringRequired("rules_key"),
            FlavorKey = item.GetProperty("flavor_key").GetStringRequired("flavor_key"),
            TemplateAsset = item.GetProperty("template_asset").GetStringRequired("template_asset"),
            ArtAsset = item.GetProperty("art_asset").GetStringRequired("art_asset")
        };
    }

    private static EnemyViewDefinition ParseEnemyView(JsonElement item)
    {
        return new EnemyViewDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            NameKey = item.GetProperty("name_key").GetStringRequired("name_key"),
            StandAsset = item.GetProperty("stand_asset").GetStringRequired("stand_asset"),
            IntentTextKeys = item.GetProperty("intent_text_keys")
                .EnumerateObject()
                .ToDictionary(entry => entry.Name, entry => entry.Value.GetStringRequired(entry.Name), StringComparer.Ordinal)
        };
    }

    private static RelicViewDefinition ParseRelicView(JsonElement item)
    {
        return new RelicViewDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            NameKey = item.GetProperty("name_key").GetStringRequired("name_key"),
            RulesKey = item.GetProperty("rules_key").GetStringRequired("rules_key"),
            IconAsset = item.GetProperty("icon_asset").GetStringRequired("icon_asset")
        };
    }

    private static AssetDefinition ParseAsset(JsonElement item)
    {
        return new AssetDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            Type = item.GetProperty("type").GetStringRequired("type"),
            Path = item.GetProperty("path").GetStringRequired("path")
        };
    }

    private static int ParseActionPointCost(JsonElement item)
    {
        foreach (var cost in item.GetProperty("costs").EnumerateArray())
        {
            if (cost.GetProperty("resource").GetString() == "action_point")
            {
                return cost.GetProperty("amount").GetInt32();
            }
        }

        return 0;
    }

    private static string? ParseTargetRef(JsonElement item)
    {
        if (!item.TryGetProperty("target", out var target) || target.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var targetRef = target.GetProperty("ref").GetString();
        return targetRef switch
        {
            "all_enemies" => "all_enemies",
            "selected_target" => "selected_enemy",
            "random_enemy" => "all_enemies",
            "self" => "self",
            "player" => "player",
            "hand_action_cards" => "hand_action_cards",
            _ => targetRef
        };
    }

    private static List<string> ReadStringList(JsonElement item, string propertyName)
    {
        return item.TryGetProperty(propertyName, out var values)
            ? values.EnumerateArray().Select(value => value.GetStringRequired(propertyName)).ToList()
            : [];
    }

    private static CardType ParseCardType(string value) => value switch
    {
        "action" => CardType.Action,
        "finisher" => CardType.Finisher,
        _ => throw new InvalidOperationException($"Unknown card type '{value}'.")
    };

    private static ColorType ParseColorType(string value) => value switch
    {
        "color.red" => ColorType.Red,
        "color.yellow" => ColorType.Yellow,
        "color.blue" => ColorType.Blue,
        "color.green" => ColorType.Green,
        "color.purple" => ColorType.Purple,
        _ => ColorType.Colorless
    };

    private static CardRarity ParseCardRarity(string value) => value switch
    {
        "starter" => CardRarity.Starter,
        "common" => CardRarity.Common,
        "uncommon" => CardRarity.Uncommon,
        "rare" => CardRarity.Rare,
        _ => throw new InvalidOperationException($"Unknown card rarity '{value}'.")
    };

    private static TargetRule ParseTargetRule(JsonElement targeting)
    {
        var mode = targeting.GetProperty("mode").GetStringRequired("targeting.mode");
        var side = targeting.GetProperty("side").GetStringRequired("targeting.side");
        return (mode, side) switch
        {
            ("single", "enemy") => TargetRule.SingleEnemy,
            ("all", "enemy") => TargetRule.AllEnemies,
            ("self", "player") => TargetRule.Self,
            ("none", _) => TargetRule.None,
            _ => throw new InvalidOperationException($"Unknown targeting rule '{mode}/{side}'.")
        };
    }

    private static EnemyIntentType ParseEnemyIntentType(JsonElement previews)
    {
        var kinds = previews.EnumerateArray()
            .Select(preview => preview.GetProperty("kind").GetString())
            .Where(kind => !string.IsNullOrWhiteSpace(kind))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (kinds.Count > 1)
        {
            return EnemyIntentType.Mixed;
        }

        return kinds.FirstOrDefault() switch
        {
            "attack" => EnemyIntentType.Attack,
            "block" => EnemyIntentType.Defend,
            "buff" => EnemyIntentType.Buff,
            "debuff" => EnemyIntentType.Debuff,
            _ => EnemyIntentType.Mixed
        };
    }

    private static EncounterNodeType ParseEncounterNodeType(string value) => value switch
    {
        "normal" => EncounterNodeType.Normal,
        "elite" => EncounterNodeType.Elite,
        "boss" => EncounterNodeType.Boss,
        _ => throw new InvalidOperationException($"Unknown encounter node type '{value}'.")
    };

    private static RelicRarity ParseRelicRarity(string value) => value switch
    {
        "common" => RelicRarity.Common,
        "uncommon" => RelicRarity.Uncommon,
        "rare" => RelicRarity.Rare,
        "boss" => RelicRarity.Boss,
        _ => throw new InvalidOperationException($"Unknown relic rarity '{value}'.")
    };

    private static RelicStackRule ParseRelicStackRule(string value) => value switch
    {
        "unique" => RelicStackRule.Unique,
        "stackable" => RelicStackRule.Stackable,
        _ => throw new InvalidOperationException($"Unknown relic stack rule '{value}'.")
    };
}

internal static class JsonElementExtensions
{
    public static string GetStringRequired(this JsonElement element, string propertyName)
    {
        return element.GetString() ?? throw new InvalidOperationException($"Property '{propertyName}' must be a string.");
    }
}
