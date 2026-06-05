using System.Text.Json;
using Godot;
using RoguelikeCardGame.Domain.Cards;
using RoguelikeCardGame.Domain.Combat;
using RoguelikeCardGame.Domain.Effects;
using RoguelikeCardGame.Domain.Enemies;
using RoguelikeCardGame.Domain.Relics;
using RoguelikeCardGame.Domain.Rewards;

namespace RoguelikeCardGame.Infrastructure.Content;

public sealed class GameContent
{
    public IReadOnlyDictionary<string, CardDefinition> CardsById { get; private init; } =
        new Dictionary<string, CardDefinition>();

    public IReadOnlyDictionary<string, EnemyDefinition> EnemiesById { get; private init; } =
        new Dictionary<string, EnemyDefinition>();

    public IReadOnlyDictionary<string, EncounterDefinition> EncountersById { get; private init; } =
        new Dictionary<string, EncounterDefinition>();

    public IReadOnlyDictionary<string, RewardPackDefinition> RewardPacksById { get; private init; } =
        new Dictionary<string, RewardPackDefinition>();

    public IReadOnlyDictionary<string, RelicDefinition> RelicsById { get; private init; } =
        new Dictionary<string, RelicDefinition>();

    public IReadOnlyDictionary<string, string> Text { get; private init; } =
        new Dictionary<string, string>();

    public required RunSequenceDefinition MvpRun { get; init; }

    public string T(string key) => Text.TryGetValue(key, out var value) ? value : key;

    public static GameContent LoadFromProject()
    {
        var root = ProjectSettings.GlobalizePath("res://data");
        var cards = ReadItems(Path.Combine(root, "cards", "cards.json"), ParseCard);
        var enemies = ReadItems(Path.Combine(root, "enemies", "enemies.json"), ParseEnemy);
        var encounters = ReadItems(Path.Combine(root, "encounters", "encounters.json"), ParseEncounter);
        var rewardPacks = ReadItems(Path.Combine(root, "rewards", "reward_packs.json"), ParseRewardPack);
        var relics = ReadItems(Path.Combine(root, "relics", "relics.json"), ParseRelic);
        var text = ReadLocalization(Path.Combine(root, "localization", "zh_hans.json"));
        var run = ReadRunSequence(Path.Combine(root, "run_sequence", "mvp_run.json"));

        return new GameContent
        {
            CardsById = cards.ToDictionary(card => card.Id, StringComparer.Ordinal),
            EnemiesById = enemies.ToDictionary(enemy => enemy.Id, StringComparer.Ordinal),
            EncountersById = encounters.ToDictionary(encounter => encounter.Id, StringComparer.Ordinal),
            RewardPacksById = rewardPacks.ToDictionary(pack => pack.Id, StringComparer.Ordinal),
            RelicsById = relics.ToDictionary(relic => relic.Id, StringComparer.Ordinal),
            Text = text,
            MvpRun = run
        };
    }

    private static List<T> ReadItems<T>(string path, Func<JsonElement, T> parse)
    {
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        return document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(parse)
            .ToList();
    }

    private static Dictionary<string, string> ReadLocalization(string path)
    {
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path));
        return document.RootElement.GetProperty("entries")
            .EnumerateObject()
            .ToDictionary(item => item.Name, item => item.Value.GetString() ?? item.Name, StringComparer.Ordinal);
    }

    private static RunSequenceDefinition ReadRunSequence(string path)
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

    private static CardDefinition ParseCard(JsonElement item)
    {
        return new CardDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            Type = ParseCardType(item.GetProperty("type").GetStringRequired("type")),
            Cost = item.GetProperty("cost").GetInt32(),
            MinChain = item.TryGetProperty("min_chain", out var minChain) ? minChain.GetInt32() : null,
            DefaultChainChange = ParseChainChange(item.GetProperty("default_chain_delta")),
            TargetRule = ParseTargetRule(item.GetProperty("target_rule").GetStringRequired("target_rule")),
            Effects = item.GetProperty("effects").EnumerateArray().Select(ParseEffect).ToList(),
            Rarity = ParseCardRarity(item.GetProperty("rarity").GetStringRequired("rarity")),
            Tags = ReadStringList(item, "tags"),
            TextKey = item.GetProperty("text_key").GetStringRequired("text_key"),
            ArtKey = item.GetProperty("art_key").GetStringRequired("art_key")
        };
    }

    private static EffectDefinition ParseEffect(JsonElement item)
    {
        return new EffectDefinition
        {
            Type = item.GetProperty("type").GetStringRequired("type"),
            Target = item.TryGetProperty("target", out var target) ? target.GetString() : null,
            Value = item.TryGetProperty("value", out var value) ? value.GetInt32() : null,
            Threshold = item.TryGetProperty("threshold", out var threshold) ? threshold.GetInt32() : null,
            Effect = item.TryGetProperty("effect", out var nested) ? ParseEffect(nested) : null
        };
    }

    private static EnemyDefinition ParseEnemy(JsonElement item)
    {
        return new EnemyDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            MaxHp = item.GetProperty("max_hp").GetInt32(),
            IntentSequence = item.GetProperty("intent_sequence").EnumerateArray().Select(ParseEnemyIntent).ToList(),
            StatusImmunities = ReadStringList(item, "status_immunities"),
            Tags = ReadStringList(item, "tags"),
            ArtKey = item.GetProperty("art_key").GetStringRequired("art_key"),
            UiNameKey = item.GetProperty("ui_name_key").GetStringRequired("ui_name_key")
        };
    }

    private static EnemyIntentDefinition ParseEnemyIntent(JsonElement item)
    {
        return new EnemyIntentDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            IntentType = ParseEnemyIntentType(item.GetProperty("intent_type").GetStringRequired("intent_type")),
            UiTextKey = item.GetProperty("ui_text_key").GetStringRequired("ui_text_key"),
            Effects = item.GetProperty("effects").EnumerateArray().Select(ParseEffect).ToList()
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

    private static RewardPackDefinition ParseRewardPack(JsonElement item)
    {
        return new RewardPackDefinition
        {
            Id = item.GetProperty("id").GetStringRequired("id"),
            PackType = ParseCardType(item.GetProperty("pack_type").GetStringRequired("pack_type")),
            CandidateIds = ReadStringList(item, "candidate_ids"),
            MinPick = item.GetProperty("min_pick").GetInt32(),
            MaxPick = item.GetProperty("max_pick").GetInt32(),
            GuaranteeRule = item.GetProperty("guarantee_rule").GetStringRequired("guarantee_rule"),
            RepeatRule = ParseRepeatRule(item.GetProperty("repeat_rule").GetStringRequired("repeat_rule")),
            TextKey = item.GetProperty("text_key").GetStringRequired("text_key")
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
                    Type = condition.GetProperty("type").GetStringRequired("type"),
                    Value = condition.TryGetProperty("value", out var value) ? value.GetString() : null
                })
                .ToList(),
            Effects = item.GetProperty("effects").EnumerateArray().Select(ParseEffect).ToList(),
            StackRule = ParseRelicStackRule(item.GetProperty("stack_rule").GetStringRequired("stack_rule")),
            TextKey = item.GetProperty("text_key").GetStringRequired("text_key"),
            IconKey = item.GetProperty("icon_key").GetStringRequired("icon_key")
        };
    }

    private static ChainChange ParseChainChange(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String && value.GetString() == "consume_all"
            ? ChainChange.ConsumeAll
            : ChainChange.Gain(value.GetInt32());
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
        "skill" => CardType.Skill,
        "finisher" => CardType.Finisher,
        _ => throw new InvalidOperationException($"Unknown card type '{value}'.")
    };

    private static CardRarity ParseCardRarity(string value) => value switch
    {
        "starter" => CardRarity.Starter,
        "common" => CardRarity.Common,
        "uncommon" => CardRarity.Uncommon,
        "rare" => CardRarity.Rare,
        _ => throw new InvalidOperationException($"Unknown card rarity '{value}'.")
    };

    private static TargetRule ParseTargetRule(string value) => value switch
    {
        "single_enemy" => TargetRule.SingleEnemy,
        "all_enemies" => TargetRule.AllEnemies,
        "self" => TargetRule.Self,
        "none" => TargetRule.None,
        _ => throw new InvalidOperationException($"Unknown target rule '{value}'.")
    };

    private static EnemyIntentType ParseEnemyIntentType(string value) => value switch
    {
        "attack" => EnemyIntentType.Attack,
        "defend" => EnemyIntentType.Defend,
        "mixed" => EnemyIntentType.Mixed,
        _ => throw new InvalidOperationException($"Unknown enemy intent type '{value}'.")
    };

    private static EncounterNodeType ParseEncounterNodeType(string value) => value switch
    {
        "normal" => EncounterNodeType.Normal,
        "elite" => EncounterNodeType.Elite,
        "boss" => EncounterNodeType.Boss,
        _ => throw new InvalidOperationException($"Unknown encounter node type '{value}'.")
    };

    private static RewardRepeatRule ParseRepeatRule(string value) => value switch
    {
        "repeatable" => RewardRepeatRule.Repeatable,
        "unique_until_seen" => RewardRepeatRule.UniqueUntilSeen,
        _ => throw new InvalidOperationException($"Unknown reward repeat rule '{value}'.")
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
