#!/usr/bin/env python3
"""Validate MVP JSON content data without external Python packages."""

from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ID_PATTERN = re.compile(r"^[a-z0-9_]+$")
CARD_EFFECT_TYPES = {
    "damage",
    "block",
    "gain_block",
    "draw_cards",
    "gain_action_points",
    "temporary_discount",
    "chain_threshold_bonus",
}
ENEMY_INTENT_EFFECT_TYPES = {"damage", "block", "gain_block"}


@dataclass(frozen=True)
class DataFile:
    name: str
    data_path: Path
    schema_path: Path


def load_json(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def json_type_name(value: Any) -> str:
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "boolean"
    if isinstance(value, int):
        return "integer"
    if isinstance(value, float):
        return "number"
    if isinstance(value, str):
        return "string"
    if isinstance(value, list):
        return "array"
    if isinstance(value, dict):
        return "object"
    return type(value).__name__


def matches_json_type(value: Any, expected_type: str) -> bool:
    if expected_type == "null":
        return value is None
    if expected_type == "boolean":
        return isinstance(value, bool)
    if expected_type == "integer":
        return isinstance(value, int) and not isinstance(value, bool)
    if expected_type == "number":
        return (isinstance(value, int) or isinstance(value, float)) and not isinstance(value, bool)
    if expected_type == "string":
        return isinstance(value, str)
    if expected_type == "array":
        return isinstance(value, list)
    if expected_type == "object":
        return isinstance(value, dict)
    return True


def validate_schema(value: Any, schema: dict[str, Any], path: str, errors: list[str]) -> None:
    if "type" in schema:
        expected = schema["type"]
        expected_types = expected if isinstance(expected, list) else [expected]
        if not any(matches_json_type(value, item) for item in expected_types):
            errors.append(f"{path}: expected {expected_types}, got {json_type_name(value)}")
            return

    if "enum" in schema and value not in schema["enum"]:
        errors.append(f"{path}: value {value!r} is not in enum {schema['enum']!r}")

    if isinstance(value, str):
        if "minLength" in schema and len(value) < schema["minLength"]:
            errors.append(f"{path}: string is shorter than minLength {schema['minLength']}")
        if "pattern" in schema and not re.match(schema["pattern"], value):
            errors.append(f"{path}: value {value!r} does not match pattern {schema['pattern']!r}")

    if isinstance(value, int) and not isinstance(value, bool):
        if "minimum" in schema and value < schema["minimum"]:
            errors.append(f"{path}: value {value} is less than minimum {schema['minimum']}")

    if isinstance(value, list):
        if "minItems" in schema and len(value) < schema["minItems"]:
            errors.append(f"{path}: array has {len(value)} items, expected at least {schema['minItems']}")
        if "maxItems" in schema and len(value) > schema["maxItems"]:
            errors.append(f"{path}: array has {len(value)} items, expected at most {schema['maxItems']}")
        item_schema = schema.get("items")
        if isinstance(item_schema, dict):
            for index, item in enumerate(value):
                validate_schema(item, item_schema, f"{path}[{index}]", errors)

    if isinstance(value, dict):
        required = schema.get("required", [])
        for key in required:
            if key not in value:
                errors.append(f"{path}: missing required field {key!r}")

        properties = schema.get("properties", {})
        additional = schema.get("additionalProperties", True)
        for key, item in value.items():
            if key in properties:
                validate_schema(item, properties[key], f"{path}.{key}", errors)
            elif isinstance(additional, dict):
                validate_schema(item, additional, f"{path}.{key}", errors)
            elif additional is False:
                errors.append(f"{path}: unexpected field {key!r}")


def collect_items(document: dict[str, Any], file_name: str, errors: list[str]) -> list[dict[str, Any]]:
    items = document.get("items")
    if not isinstance(items, list):
        errors.append(f"{file_name}: expected top-level items array")
        return []
    return [item for item in items if isinstance(item, dict)]


def add_text_key(errors: list[str], localization: dict[str, str], key: Any, location: str) -> None:
    if not isinstance(key, str) or not key:
        errors.append(f"{location}: text key must be a non-empty string")
        return
    if key not in localization:
        errors.append(f"{location}: missing localization key {key!r}")
        return
    if not localization[key].strip():
        errors.append(f"{location}: localization key {key!r} is empty")


def add_id(
    errors: list[str],
    seen: dict[str, str],
    content_id: Any,
    location: str,
) -> None:
    if not isinstance(content_id, str) or not ID_PATTERN.match(content_id):
        errors.append(f"{location}: id must use lowercase letters, numbers, and underscores")
        return
    if content_id in seen:
        errors.append(f"{location}: duplicate id {content_id!r}; first seen at {seen[content_id]}")
        return
    seen[content_id] = location


def index_by_id(items: list[dict[str, Any]], label: str, errors: list[str]) -> dict[str, dict[str, Any]]:
    indexed: dict[str, dict[str, Any]] = {}
    for item in items:
        content_id = item.get("id")
        if isinstance(content_id, str):
            if content_id in indexed:
                errors.append(f"{label}:{content_id}: duplicate id inside {label}")
            indexed[content_id] = item
    return indexed


def has_aoe_finisher(card: dict[str, Any]) -> bool:
    if card.get("type") != "finisher":
        return False
    if card.get("target_rule") == "all_enemies":
        return True
    if "aoe" in card.get("tags", []):
        return True
    for effect in card.get("effects", []):
        if isinstance(effect, dict) and effect.get("target") == "all_enemies":
            return True
        nested = effect.get("effect") if isinstance(effect, dict) else None
        if isinstance(nested, dict) and nested.get("target") == "all_enemies":
            return True
    return False


def validate_card_effect(effect: Any, location: str, errors: list[str]) -> None:
    if not isinstance(effect, dict):
        errors.append(f"{location}: effect must be an object")
        return

    effect_type = effect.get("type")
    if effect_type not in CARD_EFFECT_TYPES:
        errors.append(f"{location}.type: unsupported MVP card effect {effect_type!r}")
        return

    if effect_type == "chain_threshold_bonus":
        threshold = effect.get("threshold")
        if not isinstance(threshold, int) or threshold <= 0:
            errors.append(f"{location}.threshold: chain_threshold_bonus must define a positive threshold")
        nested = effect.get("effect")
        if not isinstance(nested, dict):
            errors.append(f"{location}.effect: chain_threshold_bonus must define a nested effect")
        else:
            validate_card_effect(nested, f"{location}.effect", errors)


def validate_enemy_intent_effect(effect: Any, location: str, errors: list[str]) -> None:
    if not isinstance(effect, dict):
        errors.append(f"{location}: effect must be an object")
        return

    effect_type = effect.get("type")
    target = effect.get("target")
    if effect_type not in ENEMY_INTENT_EFFECT_TYPES:
        errors.append(f"{location}.type: unsupported MVP enemy intent effect {effect_type!r}")
        return

    if effect_type == "damage" and target != "player":
        errors.append(f"{location}.target: MVP enemy damage effects must target 'player'")
    if effect_type in {"block", "gain_block"} and target != "self":
        errors.append(f"{location}.target: MVP enemy block effects must target 'self'")


def validate_cross_references(documents: dict[str, dict[str, Any]], errors: list[str]) -> None:
    localization_entries = documents["localization"].get("entries", {})
    localization = localization_entries if isinstance(localization_entries, dict) else {}

    cards = collect_items(documents["cards"], "cards", errors)
    relics = collect_items(documents["relics"], "relics", errors)
    enemies = collect_items(documents["enemies"], "enemies", errors)
    encounters = collect_items(documents["encounters"], "encounters", errors)
    rewards = collect_items(documents["rewards"], "rewards", errors)
    run_sequence = documents["run_sequence"]

    seen_ids: dict[str, str] = {}
    for label, items in (
        ("cards", cards),
        ("relics", relics),
        ("enemies", enemies),
        ("encounters", encounters),
        ("rewards", rewards),
    ):
        for item in items:
            add_id(errors, seen_ids, item.get("id"), f"{label}:{item.get('id', '<missing>')}")
    add_id(errors, seen_ids, run_sequence.get("id"), f"run_sequence:{run_sequence.get('id', '<missing>')}")

    cards_by_id = index_by_id(cards, "cards", errors)
    relics_by_id = index_by_id(relics, "relics", errors)
    enemies_by_id = index_by_id(enemies, "enemies", errors)
    encounters_by_id = index_by_id(encounters, "encounters", errors)
    rewards_by_id = index_by_id(rewards, "rewards", errors)

    for card in cards:
        card_id = card.get("id", "<missing>")
        add_text_key(errors, localization, card.get("text_key"), f"cards:{card_id}.text_key")
        card_type = card.get("type")
        default_chain_delta = card.get("default_chain_delta")
        min_chain = card.get("min_chain")
        cost = card.get("cost")

        if card_type == "action":
            if default_chain_delta != 1:
                errors.append(f"cards:{card_id}.default_chain_delta: action cards must default to +1 chain")
            if not isinstance(cost, int) or cost < 0:
                errors.append(f"cards:{card_id}.cost: action card cost must be a non-negative integer")
            if min_chain not in (None,):
                errors.append(f"cards:{card_id}.min_chain: non-finisher cards should not define min_chain")
        elif card_type == "skill":
            if default_chain_delta != 0:
                errors.append(f"cards:{card_id}.default_chain_delta: skill cards must default to 0 chain")
            if cost != 0:
                errors.append(f"cards:{card_id}.cost: MVP skill cards should default to 0 cost")
            if min_chain not in (None,):
                errors.append(f"cards:{card_id}.min_chain: non-finisher cards should not define min_chain")
        elif card_type == "finisher":
            if not isinstance(min_chain, int) or min_chain <= 0:
                errors.append(f"cards:{card_id}.min_chain: finisher cards must define positive min_chain")
            if default_chain_delta != "consume_all":
                errors.append(f"cards:{card_id}.default_chain_delta: finisher cards must consume all chain by default")
            if cost != 0:
                errors.append(f"cards:{card_id}.cost: MVP finisher cards should default to 0 cost")

        for index, effect in enumerate(card.get("effects", [])):
            validate_card_effect(effect, f"cards:{card_id}.effects[{index}]", errors)

    for relic in relics:
        relic_id = relic.get("id", "<missing>")
        add_text_key(errors, localization, relic.get("text_key"), f"relics:{relic_id}.text_key")

    for enemy in enemies:
        enemy_id = enemy.get("id", "<missing>")
        add_text_key(errors, localization, enemy.get("ui_name_key"), f"enemies:{enemy_id}.ui_name_key")
        for index, intent in enumerate(enemy.get("intent_sequence", [])):
            if isinstance(intent, dict):
                add_text_key(
                    errors,
                    localization,
                    intent.get("ui_text_key"),
                    f"enemies:{enemy_id}.intent_sequence[{index}].ui_text_key",
                )
                for effect_index, effect in enumerate(intent.get("effects", [])):
                    validate_enemy_intent_effect(
                        effect,
                        f"enemies:{enemy_id}.intent_sequence[{index}].effects[{effect_index}]",
                        errors,
                    )

    for pack in rewards:
        pack_id = pack.get("id", "<missing>")
        add_text_key(errors, localization, pack.get("text_key"), f"rewards:{pack_id}.text_key")
        candidates = pack.get("candidate_ids", [])
        if len(candidates) != 3:
            errors.append(f"rewards:{pack_id}.candidate_ids: reward packs must have exactly 3 candidates")
        if pack.get("min_pick") != 0 or pack.get("max_pick") != 3:
            errors.append(f"rewards:{pack_id}: MVP reward packs must allow picking 0-3 cards")
        for card_id in candidates:
            card = cards_by_id.get(card_id)
            if card is None:
                errors.append(f"rewards:{pack_id}.candidate_ids: unknown card id {card_id!r}")
                continue
            if card.get("type") != pack.get("pack_type"):
                errors.append(
                    f"rewards:{pack_id}.candidate_ids: card {card_id!r} type {card.get('type')!r} "
                    f"does not match pack type {pack.get('pack_type')!r}"
                )

    for encounter in encounters:
        encounter_id = encounter.get("id", "<missing>")
        add_text_key(
            errors,
            localization,
            encounter.get("teaching_goal_key"),
            f"encounters:{encounter_id}.teaching_goal_key",
        )
        for enemy_entry in encounter.get("enemies", []):
            enemy_id = enemy_entry.get("enemy_id") if isinstance(enemy_entry, dict) else None
            if enemy_id not in enemies_by_id:
                errors.append(f"encounters:{encounter_id}.enemies: unknown enemy id {enemy_id!r}")

        reward_profile = encounter.get("reward_profile", {})
        for pack_id in reward_profile.get("card_pack_ids", []):
            if pack_id not in rewards_by_id:
                errors.append(f"encounters:{encounter_id}.reward_profile.card_pack_ids: unknown pack id {pack_id!r}")
        relic_id = reward_profile.get("relic_id")
        if relic_id is not None and relic_id not in relics_by_id:
            errors.append(f"encounters:{encounter_id}.reward_profile.relic_id: unknown relic id {relic_id!r}")

    starter_deck_count = 0
    for entry in run_sequence.get("starter_deck", []):
        card_id = entry.get("card_id") if isinstance(entry, dict) else None
        count = entry.get("count", 0) if isinstance(entry, dict) else 0
        if card_id not in cards_by_id:
            errors.append(f"run_sequence.starter_deck: unknown card id {card_id!r}")
        if isinstance(count, int):
            starter_deck_count += count
    if starter_deck_count != 10:
        errors.append(f"run_sequence.starter_deck: MVP starter deck must contain exactly 10 cards, got {starter_deck_count}")

    ordered_nodes = sorted(run_sequence.get("nodes", []), key=lambda node: node.get("order", 0))
    normal_encounters: list[dict[str, Any]] = []
    for node in ordered_nodes:
        encounter_id = node.get("encounter_id") if isinstance(node, dict) else None
        encounter = encounters_by_id.get(encounter_id)
        if encounter is None:
            errors.append(f"run_sequence.nodes: unknown encounter id {encounter_id!r}")
            continue
        if encounter.get("node_type") == "normal":
            normal_encounters.append(encounter)

    boss_encounter_id = run_sequence.get("completion", {}).get("boss_encounter_id")
    boss_encounter = encounters_by_id.get(boss_encounter_id)
    if boss_encounter is None:
        errors.append(f"run_sequence.completion.boss_encounter_id: unknown encounter id {boss_encounter_id!r}")
    elif boss_encounter.get("node_type") != "boss":
        errors.append(f"run_sequence.completion.boss_encounter_id: encounter {boss_encounter_id!r} must be a boss")

    if len(ordered_nodes) != 6:
        errors.append(f"run_sequence.nodes: MVP run sequence must contain 6 encounters, got {len(ordered_nodes)}")

    for index, encounter in enumerate(normal_encounters[:2], start=1):
        encounter_id = encounter.get("id", "<missing>")
        pack_ids = encounter.get("reward_profile", {}).get("card_pack_ids", [])
        finisher_packs = [
            rewards_by_id[pack_id]
            for pack_id in pack_ids
            if pack_id in rewards_by_id and rewards_by_id[pack_id].get("pack_type") == "finisher"
        ]
        if not finisher_packs:
            errors.append(
                f"encounters:{encounter_id}: MVP normal combat {index} must offer a finisher pack after victory"
            )
            continue

        contains_aoe_finisher = any(
            has_aoe_finisher(cards_by_id.get(card_id, {}))
            for pack in finisher_packs
            for card_id in pack.get("candidate_ids", [])
        )
        if not contains_aoe_finisher:
            errors.append(
                f"encounters:{encounter_id}: finisher pack must contain an all-enemy finisher for MVP normal combat {index}"
            )


def run_validation(project_root: Path) -> int:
    data_root = project_root / "game" / "data"
    files = [
        DataFile("cards", data_root / "cards" / "cards.json", data_root / "schemas" / "cards.schema.json"),
        DataFile("relics", data_root / "relics" / "relics.json", data_root / "schemas" / "relics.schema.json"),
        DataFile("enemies", data_root / "enemies" / "enemies.json", data_root / "schemas" / "enemies.schema.json"),
        DataFile("encounters", data_root / "encounters" / "encounters.json", data_root / "schemas" / "encounters.schema.json"),
        DataFile("rewards", data_root / "rewards" / "reward_packs.json", data_root / "schemas" / "rewards.schema.json"),
        DataFile("run_sequence", data_root / "run_sequence" / "mvp_run.json", data_root / "schemas" / "run_sequence.schema.json"),
        DataFile("localization", data_root / "localization" / "zh_hans.json", data_root / "schemas" / "localization.schema.json"),
    ]

    errors: list[str] = []
    documents: dict[str, dict[str, Any]] = {}

    for data_file in files:
        if not data_file.data_path.exists():
            errors.append(f"{data_file.data_path}: data file does not exist")
            continue
        if not data_file.schema_path.exists():
            errors.append(f"{data_file.schema_path}: schema file does not exist")
            continue

        try:
            document = load_json(data_file.data_path)
            schema = load_json(data_file.schema_path)
        except json.JSONDecodeError as exc:
            errors.append(f"{data_file.data_path}: invalid JSON: {exc}")
            continue

        if not isinstance(document, dict):
            errors.append(f"{data_file.data_path}: top-level JSON value must be an object")
            continue
        if not isinstance(schema, dict):
            errors.append(f"{data_file.schema_path}: schema top-level JSON value must be an object")
            continue

        validate_schema(document, schema, str(data_file.data_path), errors)
        documents[data_file.name] = document

    if len(documents) == len(files):
        validate_cross_references(documents, errors)

    if errors:
        print("Data validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("Data validation passed.")
    print(f"Validated {len(files)} data files and {len(files)} schemas.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate RoguelikeCardGame MVP content data.")
    parser.add_argument(
        "--project-root",
        type=Path,
        default=Path(__file__).resolve().parents[3],
        help="Repository root. Defaults to the validator's repository root.",
    )
    args = parser.parse_args()
    return run_validation(args.project_root.resolve())


if __name__ == "__main__":
    sys.exit(main())
