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


CONTENT_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$")
CARD_EFFECT_OPS = {
    "conditional",
    "damage",
    "draw_cards",
    "gain_block",
    "gain_resource",
    "set_resource",
    "temporary_discount",
}
ENEMY_EFFECT_OPS = {"damage", "gain_block"}
RELIC_EFFECT_OPS = {"gain_block"}
RESOURCE_NAMES = {"action_point", "chain"}


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


def resolve_ref(schema: dict[str, Any], root_schema: dict[str, Any]) -> dict[str, Any]:
    ref = schema.get("$ref")
    if not isinstance(ref, str) or not ref.startswith("#/$defs/"):
        return schema
    key = ref.removeprefix("#/$defs/")
    defs = root_schema.get("$defs", {})
    return defs.get(key, schema) if isinstance(defs, dict) else schema


def validate_schema(value: Any, schema: dict[str, Any], path: str, errors: list[str], root_schema: dict[str, Any] | None = None) -> None:
    root = root_schema or schema
    schema = resolve_ref(schema, root)

    if "const" in schema and value != schema["const"]:
        errors.append(f"{path}: expected constant {schema['const']!r}, got {value!r}")

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
                validate_schema(item, item_schema, f"{path}[{index}]", errors, root)

    if isinstance(value, dict):
        required = schema.get("required", [])
        for key in required:
            if key not in value:
                errors.append(f"{path}: missing required field {key!r}")

        properties = schema.get("properties", {})
        additional = schema.get("additionalProperties", True)
        for key, item in value.items():
            if key in properties:
                validate_schema(item, properties[key], f"{path}.{key}", errors, root)
            elif isinstance(additional, dict):
                validate_schema(item, additional, f"{path}.{key}", errors, root)
            elif additional is False:
                errors.append(f"{path}: unexpected field {key!r}")


def collect_items(document: dict[str, Any], file_name: str, errors: list[str]) -> list[dict[str, Any]]:
    items = document.get("items")
    if not isinstance(items, list):
        errors.append(f"{file_name}: expected top-level items array")
        return []
    return [item for item in items if isinstance(item, dict)]


def add_id(errors: list[str], seen: dict[str, str], content_id: Any, location: str) -> None:
    if not isinstance(content_id, str) or not CONTENT_ID_PATTERN.match(content_id):
        errors.append(f"{location}: id must use namespaced lowercase dot syntax")
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


def add_text_key(errors: list[str], localization: dict[str, str], key: Any, location: str) -> None:
    if not isinstance(key, str) or not key:
        errors.append(f"{location}: text key must be a non-empty string")
        return
    if key not in localization:
        errors.append(f"{location}: missing localization key {key!r}")
        return
    if not localization[key].strip():
        errors.append(f"{location}: localization key {key!r} is empty")


def validate_target_ref(effect: dict[str, Any], allowed_refs: set[str], location: str, errors: list[str]) -> None:
    target = effect.get("target")
    if not isinstance(target, dict):
        errors.append(f"{location}.target: effect target must be an object")
        return
    target_ref = target.get("ref")
    if target_ref not in allowed_refs:
        errors.append(f"{location}.target.ref: unsupported target ref {target_ref!r}")


def validate_resource_amount(effect: dict[str, Any], location: str, errors: list[str]) -> None:
    resource = effect.get("resource")
    amount = effect.get("amount")
    if resource not in RESOURCE_NAMES:
        errors.append(f"{location}.resource: unsupported resource {resource!r}")
    if not isinstance(amount, int) or amount < 0:
        errors.append(f"{location}.amount: amount must be a non-negative integer")


def validate_card_effect(effect: Any, location: str, errors: list[str]) -> None:
    if not isinstance(effect, dict):
        errors.append(f"{location}: effect must be an object")
        return
    op = effect.get("op")
    if op not in CARD_EFFECT_OPS:
        errors.append(f"{location}.op: unsupported MVP card effect op {op!r}")
        return

    if op in {"damage", "gain_block", "draw_cards", "temporary_discount"}:
        validate_target_ref(effect, {"selected_target", "all_enemies", "self", "hand_action_cards"}, location, errors)
        amount = effect.get("amount")
        if not isinstance(amount, int) or amount < 0:
            errors.append(f"{location}.amount: amount must be a non-negative integer")
    elif op in {"gain_resource", "set_resource"}:
        validate_resource_amount(effect, location, errors)
    elif op == "conditional":
        condition = effect.get("if")
        if not isinstance(condition, dict) or condition.get("op") != "resource_at_least":
            errors.append(f"{location}.if: conditional must use resource_at_least for MVP")
        else:
            validate_resource_amount(condition, f"{location}.if", errors)
        nested = effect.get("then")
        if not isinstance(nested, list) or not nested:
            errors.append(f"{location}.then: conditional must define nested effects")
        else:
            for index, nested_effect in enumerate(nested):
                validate_card_effect(nested_effect, f"{location}.then[{index}]", errors)


def validate_requirement(requirement: Any, location: str, errors: list[str]) -> None:
    if not isinstance(requirement, dict):
        errors.append(f"{location}: requirement must be an object")
        return
    if requirement.get("op") != "resource_at_least":
        errors.append(f"{location}.op: unsupported requirement op {requirement.get('op')!r}")
        return
    validate_resource_amount(requirement, location, errors)


def validate_enemy_effect(effect: Any, location: str, errors: list[str]) -> None:
    if not isinstance(effect, dict):
        errors.append(f"{location}: effect must be an object")
        return
    op = effect.get("op")
    if op not in ENEMY_EFFECT_OPS:
        errors.append(f"{location}.op: unsupported MVP enemy effect op {op!r}")
        return
    validate_target_ref(effect, {"player", "self"}, location, errors)
    amount = effect.get("amount")
    if not isinstance(amount, int) or amount < 0:
        errors.append(f"{location}.amount: amount must be a non-negative integer")
    if op == "damage" and effect.get("target", {}).get("ref") != "player":
        errors.append(f"{location}.target.ref: enemy damage must target player")
    if op == "gain_block" and effect.get("target", {}).get("ref") != "self":
        errors.append(f"{location}.target.ref: enemy block must target self")


def validate_relic_effect(effect: Any, location: str, errors: list[str]) -> None:
    if not isinstance(effect, dict):
        errors.append(f"{location}: effect must be an object")
        return
    if effect.get("op") not in RELIC_EFFECT_OPS:
        errors.append(f"{location}.op: unsupported MVP relic effect op {effect.get('op')!r}")
        return
    validate_target_ref(effect, {"self"}, location, errors)


def card_has_aoe_finisher(card: dict[str, Any]) -> bool:
    if card.get("card_type") != "finisher":
        return False
    targeting = card.get("targeting", {})
    if isinstance(targeting, dict) and targeting.get("mode") == "all" and targeting.get("side") == "enemy":
        return True
    if "aoe" in card.get("tags", []):
        return True
    for effect in card.get("effects", []):
        if isinstance(effect, dict):
            target = effect.get("target", {})
            if isinstance(target, dict) and target.get("ref") == "all_enemies":
                return True
    return False


def collect_assets(items: list[dict[str, Any]]) -> set[str]:
    return {item["id"] for item in items if isinstance(item.get("id"), str)}


def require_asset(asset_ids: set[str], asset_id: Any, location: str, errors: list[str]) -> None:
    if not isinstance(asset_id, str) or not asset_id:
        errors.append(f"{location}: asset id must be a non-empty string")
        return
    if "placeholder" in asset_id:
        errors.append(f"{location}: asset id must not use placeholder")
    if asset_id not in asset_ids:
        errors.append(f"{location}: unknown asset id {asset_id!r}")


def validate_asset_paths(project_root: Path, assets: list[dict[str, Any]], errors: list[str]) -> None:
    for asset in assets:
        asset_id = asset.get("id", "<missing>")
        path = asset.get("path")
        if not isinstance(path, str) or not path.startswith("res://"):
            continue
        local_path = project_root / "game" / path.removeprefix("res://")
        if not local_path.exists():
            errors.append(f"assets:{asset_id}.path: file does not exist at {path!r}")


def validate_cross_references(project_root: Path, documents: dict[str, dict[str, Any]], errors: list[str]) -> None:
    localization_entries = documents["localization"].get("entries", {})
    localization = localization_entries if isinstance(localization_entries, dict) else {}

    cards = collect_items(documents["cards"], "cards", errors)
    enemies = collect_items(documents["enemies"], "enemies", errors)
    relics = collect_items(documents["relics"], "relics", errors)
    encounters = collect_items(documents["encounters"], "encounters", errors)
    rewards = collect_items(documents["rewards"], "rewards", errors)
    assets = collect_items(documents["assets"], "assets", errors)
    card_views = collect_items(documents["card_views"], "card_views", errors)
    enemy_views = collect_items(documents["enemy_views"], "enemy_views", errors)
    relic_views = collect_items(documents["relic_views"], "relic_views", errors)
    reward_pack_views = collect_items(documents["reward_pack_views"], "reward_pack_views", errors)
    run_sequence = documents["run_sequence"]

    seen_ids: dict[str, str] = {}
    for label, items in (
        ("cards", cards),
        ("enemies", enemies),
        ("relics", relics),
        ("encounters", encounters),
        ("rewards", rewards),
        ("assets", assets),
    ):
        for item in items:
            add_id(errors, seen_ids, item.get("id"), f"{label}:{item.get('id', '<missing>')}")
    add_id(errors, seen_ids, run_sequence.get("id"), f"run_sequence:{run_sequence.get('id', '<missing>')}")

    cards_by_id = index_by_id(cards, "cards", errors)
    enemies_by_id = index_by_id(enemies, "enemies", errors)
    relics_by_id = index_by_id(relics, "relics", errors)
    encounters_by_id = index_by_id(encounters, "encounters", errors)
    rewards_by_id = index_by_id(rewards, "rewards", errors)
    card_views_by_id = index_by_id(card_views, "card_views", errors)
    enemy_views_by_id = index_by_id(enemy_views, "enemy_views", errors)
    relic_views_by_id = index_by_id(relic_views, "relic_views", errors)
    reward_pack_views_by_id = index_by_id(reward_pack_views, "reward_pack_views", errors)
    asset_ids = collect_assets(assets)

    validate_asset_paths(project_root, assets, errors)

    for card in cards:
        card_id = card.get("id", "<missing>")
        forbidden = {"text_key", "art_key", "ui_name_key"}
        leaked = forbidden.intersection(card.keys())
        if leaked:
            errors.append(f"cards:{card_id}: gameplay card must not contain presentation fields {sorted(leaked)}")

        card_type = card.get("card_type")
        costs = card.get("costs", [])
        requirements = card.get("requirements", [])
        effects = card.get("effects", [])

        if card_id not in card_views_by_id:
            errors.append(f"cards:{card_id}: missing card view")

        for index, cost in enumerate(costs):
            validate_resource_amount(cost, f"cards:{card_id}.costs[{index}]", errors)
        for index, requirement in enumerate(requirements):
            validate_requirement(requirement, f"cards:{card_id}.requirements[{index}]", errors)
        for index, effect in enumerate(effects):
            validate_card_effect(effect, f"cards:{card_id}.effects[{index}]", errors)

        chain_gains = [
            effect for effect in effects
            if isinstance(effect, dict) and effect.get("op") == "gain_resource" and effect.get("resource") == "chain"
        ]
        chain_clears = [
            effect for effect in effects
            if isinstance(effect, dict) and effect.get("op") == "set_resource" and effect.get("resource") == "chain" and effect.get("amount") == 0
        ]
        chain_requirements = [
            requirement for requirement in requirements
            if isinstance(requirement, dict) and requirement.get("op") == "resource_at_least" and requirement.get("resource") == "chain"
        ]

        if card_type == "action":
            if not chain_gains or any(effect.get("amount") != 1 for effect in chain_gains):
                errors.append(f"cards:{card_id}: action cards must explicitly gain exactly 1 chain")
        elif card_type == "skill":
            if chain_gains:
                errors.append(f"cards:{card_id}: MVP skill cards should not gain chain by default")
        elif card_type == "finisher":
            if not chain_requirements:
                errors.append(f"cards:{card_id}: finisher cards must require chain")
            if not chain_clears:
                errors.append(f"cards:{card_id}: finisher cards must explicitly set chain to 0")

    for card_view in card_views:
        card_id = card_view.get("id", "<missing>")
        if card_id not in cards_by_id:
            errors.append(f"card_views:{card_id}: unknown card id")
        add_text_key(errors, localization, card_view.get("name_key"), f"card_views:{card_id}.name_key")
        add_text_key(errors, localization, card_view.get("rules_key"), f"card_views:{card_id}.rules_key")
        add_text_key(errors, localization, card_view.get("flavor_key"), f"card_views:{card_id}.flavor_key")
        require_asset(asset_ids, card_view.get("template_asset"), f"card_views:{card_id}.template_asset", errors)
        require_asset(asset_ids, card_view.get("art_asset"), f"card_views:{card_id}.art_asset", errors)

    for enemy in enemies:
        enemy_id = enemy.get("id", "<missing>")
        if enemy_id not in enemy_views_by_id:
            errors.append(f"enemies:{enemy_id}: missing enemy view")
        for intent_index, intent in enumerate(enemy.get("ai", {}).get("intents", [])):
            for effect_index, effect in enumerate(intent.get("effects", [])):
                validate_enemy_effect(effect, f"enemies:{enemy_id}.ai.intents[{intent_index}].effects[{effect_index}]", errors)

    for enemy_view in enemy_views:
        enemy_id = enemy_view.get("id", "<missing>")
        enemy = enemies_by_id.get(enemy_id)
        if enemy is None:
            errors.append(f"enemy_views:{enemy_id}: unknown enemy id")
            continue
        add_text_key(errors, localization, enemy_view.get("name_key"), f"enemy_views:{enemy_id}.name_key")
        require_asset(asset_ids, enemy_view.get("stand_asset"), f"enemy_views:{enemy_id}.stand_asset", errors)
        intent_text_keys = enemy_view.get("intent_text_keys", {})
        if not isinstance(intent_text_keys, dict):
            errors.append(f"enemy_views:{enemy_id}.intent_text_keys: must be an object")
            continue
        for intent in enemy.get("ai", {}).get("intents", []):
            intent_id = intent.get("id")
            if intent_id not in intent_text_keys:
                errors.append(f"enemy_views:{enemy_id}.intent_text_keys: missing text key for {intent_id!r}")
                continue
            add_text_key(errors, localization, intent_text_keys[intent_id], f"enemy_views:{enemy_id}.intent_text_keys.{intent_id}")

    for relic in relics:
        relic_id = relic.get("id", "<missing>")
        if relic_id not in relic_views_by_id:
            errors.append(f"relics:{relic_id}: missing relic view")
        for index, effect in enumerate(relic.get("effects", [])):
            validate_relic_effect(effect, f"relics:{relic_id}.effects[{index}]", errors)

    for relic_view in relic_views:
        relic_id = relic_view.get("id", "<missing>")
        if relic_id not in relics_by_id:
            errors.append(f"relic_views:{relic_id}: unknown relic id")
        add_text_key(errors, localization, relic_view.get("name_key"), f"relic_views:{relic_id}.name_key")
        add_text_key(errors, localization, relic_view.get("rules_key"), f"relic_views:{relic_id}.rules_key")
        require_asset(asset_ids, relic_view.get("icon_asset"), f"relic_views:{relic_id}.icon_asset", errors)

    for pack in rewards:
        pack_id = pack.get("id", "<missing>")
        if pack_id not in reward_pack_views_by_id:
            errors.append(f"rewards:{pack_id}: missing reward pack view")
        candidates = pack.get("candidate_ids", [])
        if len(candidates) != 3:
            errors.append(f"rewards:{pack_id}.candidate_ids: reward packs must have exactly 3 candidates")
        if pack.get("pick", {}).get("min") != 0 or pack.get("pick", {}).get("max") != 3:
            errors.append(f"rewards:{pack_id}: MVP reward packs must allow picking 0-3 cards")
        for card_id in candidates:
            card = cards_by_id.get(card_id)
            if card is None:
                errors.append(f"rewards:{pack_id}.candidate_ids: unknown card id {card_id!r}")
                continue
            if card.get("card_type") != pack.get("pack_type"):
                errors.append(f"rewards:{pack_id}.candidate_ids: card {card_id!r} type does not match pack type")

    for pack_view in reward_pack_views:
        pack_id = pack_view.get("id", "<missing>")
        if pack_id not in rewards_by_id:
            errors.append(f"reward_pack_views:{pack_id}: unknown reward pack id")
        add_text_key(errors, localization, pack_view.get("name_key"), f"reward_pack_views:{pack_id}.name_key")
        require_asset(asset_ids, pack_view.get("icon_asset"), f"reward_pack_views:{pack_id}.icon_asset", errors)

    for encounter in encounters:
        encounter_id = encounter.get("id", "<missing>")
        add_text_key(errors, localization, encounter.get("teaching_goal_key"), f"encounters:{encounter_id}.teaching_goal_key")
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
            errors.append(f"encounters:{encounter_id}: MVP normal combat {index} must offer a finisher pack after victory")
            continue
        contains_aoe_finisher = any(
            card_has_aoe_finisher(cards_by_id.get(card_id, {}))
            for pack in finisher_packs
            for card_id in pack.get("candidate_ids", [])
        )
        if not contains_aoe_finisher:
            errors.append(f"encounters:{encounter_id}: finisher pack must contain an all-enemy finisher")


def run_validation(project_root: Path) -> int:
    data_root = project_root / "game" / "data"
    files = [
        DataFile("cards", data_root / "gameplay" / "cards" / "cards.json", data_root / "schemas" / "gameplay" / "cards.schema.json"),
        DataFile("enemies", data_root / "gameplay" / "enemies" / "enemies.json", data_root / "schemas" / "gameplay" / "enemies.schema.json"),
        DataFile("relics", data_root / "gameplay" / "relics" / "relics.json", data_root / "schemas" / "gameplay" / "relics.schema.json"),
        DataFile("encounters", data_root / "gameplay" / "encounters" / "encounters.json", data_root / "schemas" / "gameplay" / "encounters.schema.json"),
        DataFile("rewards", data_root / "gameplay" / "rewards" / "reward_packs.json", data_root / "schemas" / "gameplay" / "rewards.schema.json"),
        DataFile("run_sequence", data_root / "gameplay" / "runs" / "mvp_run.json", data_root / "schemas" / "gameplay" / "run_sequence.schema.json"),
        DataFile("card_views", data_root / "presentation" / "card_views.json", data_root / "schemas" / "presentation" / "card_views.schema.json"),
        DataFile("enemy_views", data_root / "presentation" / "enemy_views.json", data_root / "schemas" / "presentation" / "enemy_views.schema.json"),
        DataFile("relic_views", data_root / "presentation" / "relic_views.json", data_root / "schemas" / "presentation" / "relic_views.schema.json"),
        DataFile("reward_pack_views", data_root / "presentation" / "reward_pack_views.json", data_root / "schemas" / "presentation" / "reward_pack_views.schema.json"),
        DataFile("assets", data_root / "presentation" / "assets.json", data_root / "schemas" / "presentation" / "assets.schema.json"),
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
        validate_cross_references(project_root, documents, errors)

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
