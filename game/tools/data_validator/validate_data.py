#!/usr/bin/env python3
"""Validate Sword Black Tower MVP JSON content data without external packages."""

from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any


CONTENT_ID_PATTERN = re.compile(r"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$")
REQUIRED_WEAPON_IDS = {"weapon.revolver_sword", "weapon.mechanical_arm"}
REQUIRED_COLOR_IDS = {"color.red", "color.yellow", "color.blue", "color.green", "color.purple"}
CARD_TYPES = {"action", "finisher"}
FINISHER_ENERGY_MODES = {"fixed", "x", "all"}
LEGACY_MARKERS = {"chain", "min_chain", "default_chain_delta", "chain_threshold_bonus", "skill"}
PRESENTATION_LEGACY_TOKENS = ("chain_", "chain.", "min_chain", "chain_threshold", "skill_card", "reward_pack")
YELLOW_FORBIDDEN_OPS = {
    "draw_card",
    "draw_cards",
    "gain_action_point",
    "gain_action_points",
    "refund_action_point",
    "refund_action_points",
    "gain_resource",
    "gain_color_energy",
    "generate_color_energy",
}
YELLOW_FORBIDDEN_RESOURCES = {"action_point", "action_points", "color_energy", "energy", "chain"}


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
        for key in schema.get("required", []):
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


def index_by_id(items: list[dict[str, Any]], label: str, errors: list[str]) -> dict[str, dict[str, Any]]:
    indexed: dict[str, dict[str, Any]] = {}
    for item in items:
        content_id = item.get("id")
        if not isinstance(content_id, str) or not CONTENT_ID_PATTERN.match(content_id):
            errors.append(f"{label}:{content_id or '<missing>'}: id must use namespaced lowercase dot syntax")
            continue
        if content_id in indexed:
            errors.append(f"{label}:{content_id}: duplicate id")
        indexed[content_id] = item
    return indexed


def iter_json_nodes(value: Any, path: str = "$"):
    yield path, value
    if isinstance(value, dict):
        for key, item in value.items():
            yield from iter_json_nodes(item, f"{path}.{key}")
    elif isinstance(value, list):
        for index, item in enumerate(value):
            yield from iter_json_nodes(item, f"{path}[{index}]")


def contains_legacy_marker(value: Any) -> tuple[str, str] | None:
    for path, node in iter_json_nodes(value):
        if isinstance(node, dict):
            for key, item in node.items():
                key_lower = key.lower()
                if key_lower in LEGACY_MARKERS or "chain" in key_lower:
                    return path, f"field {key!r}"
                if key_lower in {"threshold", "thresholds"} and item == [3, 5, 8]:
                    return path, "legacy 3/5/8 threshold list"
        elif isinstance(node, str):
            node_lower = node.lower()
            if any(marker in node_lower for marker in ("min_chain", "3/5/8", "chain_threshold")):
                return path, f"legacy string {node!r}"
            if node_lower == "skill":
                return path, "legacy skill card type"
    return None


def contains_forbidden_yellow_semantics(value: Any) -> tuple[str, str] | None:
    for path, node in iter_json_nodes(value):
        if not isinstance(node, dict):
            continue
        op = node.get("op")
        if isinstance(op, str) and op in YELLOW_FORBIDDEN_OPS:
            return path, f"forbidden yellow op {op!r}"
        resource = node.get("resource")
        if isinstance(resource, str) and resource in YELLOW_FORBIDDEN_RESOURCES:
            return path, f"forbidden yellow resource {resource!r}"
    return None


def contains_legacy_presentation_token(value: Any) -> tuple[str, str] | None:
    for path, node in iter_json_nodes(value):
        if not isinstance(node, str):
            continue
        node_lower = node.lower()
        for token in PRESENTATION_LEGACY_TOKENS:
            if token in node_lower:
                return path, f"legacy presentation token {token!r} in {node!r}"
    return None


def require_text(localization: dict[str, str], key: Any, location: str, errors: list[str]) -> None:
    if not isinstance(key, str) or not key:
        errors.append(f"{location}: text key must be a non-empty string")
        return
    if key not in localization:
        errors.append(f"{location}: missing localization key {key!r}")
    elif not localization[key].strip():
        errors.append(f"{location}: localization key {key!r} is empty")


def collect_assets(items: list[dict[str, Any]]) -> set[str]:
    return {item["id"] for item in items if isinstance(item.get("id"), str)}


def require_asset(asset_ids: set[str], asset_id: Any, location: str, errors: list[str]) -> None:
    if not isinstance(asset_id, str) or not asset_id:
        errors.append(f"{location}: asset id must be a non-empty string")
        return
    if asset_id not in asset_ids:
        errors.append(f"{location}: unknown asset id {asset_id!r}")


def validate_asset_paths(project_root: Path, assets: list[dict[str, Any]], errors: list[str]) -> None:
    for asset in assets:
        asset_id = asset.get("id", "<missing>")
        path = asset.get("path")
        if not isinstance(path, str) or not path.startswith("res://"):
            errors.append(f"assets:{asset_id}.path: must use res://")
            continue
        local_path = project_root / "game" / path.removeprefix("res://")
        if not local_path.exists():
            errors.append(f"assets:{asset_id}.path: file does not exist at {path!r}")


def validate_unified_gameplay(project_root: Path, documents: dict[str, dict[str, Any]], errors: list[str]) -> None:
    cards = collect_items(documents["cards"], "cards", errors)
    weapons = collect_items(documents["weapons"], "weapons", errors)
    colors = collect_items(documents["colors"], "colors", errors)
    card_pools = collect_items(documents["card_pools"], "card_pools", errors)
    enemies = collect_items(documents["enemies"], "enemies", errors)
    relics = collect_items(documents["relics"], "relics", errors)
    encounters = collect_items(documents["encounters"], "encounters", errors)
    assets = collect_items(documents["assets"], "assets", errors)
    card_views = collect_items(documents["card_views"], "card_views", errors)
    enemy_views = collect_items(documents["enemy_views"], "enemy_views", errors)
    relic_views = collect_items(documents["relic_views"], "relic_views", errors)
    run_sequence = documents["run_sequence"]
    localization_entries = documents["localization"].get("entries", {})
    localization = localization_entries if isinstance(localization_entries, dict) else {}

    cards_by_id = index_by_id(cards, "cards", errors)
    weapons_by_id = index_by_id(weapons, "weapons", errors)
    colors_by_id = index_by_id(colors, "colors", errors)
    pools_by_id = index_by_id(card_pools, "card_pools", errors)
    enemies_by_id = index_by_id(enemies, "enemies", errors)
    relics_by_id = index_by_id(relics, "relics", errors)
    encounters_by_id = index_by_id(encounters, "encounters", errors)
    card_views_by_id = index_by_id(card_views, "card_views", errors)
    enemy_views_by_id = index_by_id(enemy_views, "enemy_views", errors)
    relic_views_by_id = index_by_id(relic_views, "relic_views", errors)
    asset_ids = collect_assets(assets)

    validate_asset_paths(project_root, assets, errors)

    missing_weapons = sorted(REQUIRED_WEAPON_IDS.difference(weapons_by_id))
    if missing_weapons:
        errors.append(f"gameplay.weapons: missing required MVP weapons {missing_weapons}")
    missing_colors = sorted(REQUIRED_COLOR_IDS.difference(colors_by_id))
    if missing_colors:
        errors.append(f"gameplay.colors: missing required colors {missing_colors}")

    for label, document in (
        ("gameplay.weapons", documents["weapons"]),
        ("gameplay.colors", documents["colors"]),
        ("gameplay.cards", documents["cards"]),
        ("gameplay.card_pools", documents["card_pools"]),
    ):
        legacy_marker = contains_legacy_marker(document)
        if legacy_marker is not None:
            path, reason = legacy_marker
            errors.append(f"{label}{path.removeprefix('$')}: uses deprecated skill/chain/3-5-8 semantics via {reason}")

    for label, document in (
        ("presentation.assets", documents["assets"]),
        ("presentation.card_views", documents["card_views"]),
        ("presentation.enemy_views", documents["enemy_views"]),
        ("presentation.relic_views", documents["relic_views"]),
        ("localization.zh_hans", documents["localization"]),
    ):
        legacy_presentation = contains_legacy_presentation_token(document)
        if legacy_presentation is not None:
            path, reason = legacy_presentation
            errors.append(f"{label}{path.removeprefix('$')}: active data must not reference legacy chain/skill/reward-pack assets: {reason}")

    yellow = colors_by_id.get("color.yellow")
    if yellow is not None:
        forbidden = contains_forbidden_yellow_semantics(yellow)
        if forbidden is not None:
            path, reason = forbidden
            errors.append(f"gameplay.colors:color.yellow{path.removeprefix('$')}: {reason}; yellow may only increase card cast count")

    for card in cards:
        card_id = card.get("id", "<missing>")
        card_type = card.get("card_type")
        weapon_id = card.get("weapon_id")
        if card_type not in CARD_TYPES:
            errors.append(f"gameplay.cards:{card_id}.card_type: only action and finisher are allowed")
        if weapon_id not in weapons_by_id:
            errors.append(f"gameplay.cards:{card_id}.weapon_id: unknown weapon id {weapon_id!r}")

        energy = card.get("energy", {})
        color_interactions = card.get("color_interactions", {})
        enchantment = color_interactions.get("enchantment", {}) if isinstance(color_interactions, dict) else {}
        finisher_color_effects = color_interactions.get("finisher_color_effects", []) if isinstance(color_interactions, dict) else []

        if card_type == "action":
            generate = energy.get("generate") if isinstance(energy, dict) else None
            if not isinstance(generate, dict):
                errors.append(f"gameplay.cards:{card_id}.energy.generate: action cards must declare color energy generation")
            else:
                amount = generate.get("amount")
                if not isinstance(amount, int) or amount <= 0:
                    errors.append(f"gameplay.cards:{card_id}.energy.generate.amount: action cards must generate at least 1 color energy")
                if generate.get("color_source") != "enchantment":
                    errors.append(f"gameplay.cards:{card_id}.energy.generate.color_source: action cards should inherit enchantment color")
            if enchantment.get("can_be_enchanted") is not True:
                errors.append(f"gameplay.cards:{card_id}: action cards must be enchantable")
            if "consume" in energy:
                errors.append(f"gameplay.cards:{card_id}.energy.consume: action cards should not use finisher consume rules")

        if card_type == "finisher":
            consume = energy.get("consume") if isinstance(energy, dict) else None
            if not isinstance(consume, dict):
                errors.append(f"gameplay.cards:{card_id}.energy.consume: finishers must declare color energy consume rules")
            else:
                mode = consume.get("mode")
                if mode not in FINISHER_ENERGY_MODES:
                    errors.append(f"gameplay.cards:{card_id}.energy.consume.mode: must be fixed, x, or all")
                if mode == "fixed":
                    amount = consume.get("amount")
                    if not isinstance(amount, int) or amount <= 0:
                        errors.append(f"gameplay.cards:{card_id}.energy.consume.amount: fixed consume finishers need a positive amount")
                    if consume.get("min_amount") != amount:
                        errors.append(f"gameplay.cards:{card_id}.energy.consume.min_amount: fixed consume finishers should match amount")
                else:
                    min_amount = consume.get("min_amount")
                    if not isinstance(min_amount, int) or min_amount <= 0:
                        errors.append(f"gameplay.cards:{card_id}.energy.consume.min_amount: x/all finishers need a positive minimum")
            if enchantment.get("can_be_enchanted") is not False:
                errors.append(f"gameplay.cards:{card_id}: finishers are not enchanted by color shards in MVP")
            effect_color_ids = {effect.get("color_id") for effect in finisher_color_effects if isinstance(effect, dict)}
            missing_effect_colors = sorted(REQUIRED_COLOR_IDS.difference(effect_color_ids))
            if missing_effect_colors:
                errors.append(f"gameplay.cards:{card_id}.finisher_color_effects: missing MVP color effects {missing_effect_colors}")
            unknown_color_ids = sorted(color_id for color_id in effect_color_ids if color_id not in colors_by_id)
            if unknown_color_ids:
                errors.append(f"gameplay.cards:{card_id}.finisher_color_effects: unknown color ids {unknown_color_ids}")

        for color_effect in finisher_color_effects:
            if isinstance(color_effect, dict) and color_effect.get("color_id") == "color.yellow":
                forbidden = contains_forbidden_yellow_semantics(color_effect)
                if forbidden is not None:
                    path, reason = forbidden
                    errors.append(f"gameplay.cards:{card_id}.yellow_effect{path.removeprefix('$')}: {reason}; yellow may only increase card cast count")
        if isinstance(enchantment, dict):
            for self_effect in enchantment.get("self_effects", []):
                if isinstance(self_effect, dict) and self_effect.get("color_id") == "color.yellow":
                    forbidden = contains_forbidden_yellow_semantics(self_effect)
                    if forbidden is not None:
                        path, reason = forbidden
                        errors.append(f"gameplay.cards:{card_id}.yellow_enchantment{path.removeprefix('$')}: {reason}; yellow may only increase card cast count")

        if card_id in card_views_by_id:
            view = card_views_by_id[card_id]
            require_text(localization, view.get("name_key"), f"card_views:{card_id}.name_key", errors)
            require_text(localization, view.get("rules_key"), f"card_views:{card_id}.rules_key", errors)
            require_text(localization, view.get("flavor_key"), f"card_views:{card_id}.flavor_key", errors)
            require_asset(asset_ids, view.get("template_asset"), f"card_views:{card_id}.template_asset", errors)
            require_asset(asset_ids, view.get("art_asset"), f"card_views:{card_id}.art_asset", errors)

    starting_pool_by_weapon: dict[str, dict[str, Any]] = {}
    reward_pool_by_weapon: dict[str, dict[str, Any]] = {}
    for pool in card_pools:
        pool_id = pool.get("id", "<missing>")
        pool_type = pool.get("pool_type")
        weapon_id = pool.get("weapon_id")
        if weapon_id not in weapons_by_id:
            errors.append(f"gameplay.card_pools:{pool_id}.weapon_id: unknown weapon id {weapon_id!r}")
            continue

        if pool_type == "starting":
            starting_pool_by_weapon[weapon_id] = pool
            total = 0
            action_count = 0
            finisher_count = 0
            for index, entry in enumerate(pool.get("starting_entries", [])):
                card_id = entry.get("card_id") if isinstance(entry, dict) else None
                count = entry.get("count") if isinstance(entry, dict) else None
                card = cards_by_id.get(card_id)
                if card is None:
                    errors.append(f"gameplay.card_pools:{pool_id}.starting_entries[{index}].card_id: unknown card id {card_id!r}")
                elif card.get("weapon_id") != weapon_id:
                    errors.append(f"gameplay.card_pools:{pool_id}.starting_entries[{index}].card_id: card {card_id!r} belongs to {card.get('weapon_id')!r}")
                elif isinstance(count, int):
                    if card.get("card_type") == "action":
                        action_count += count
                    elif card.get("card_type") == "finisher":
                        finisher_count += count
                if isinstance(count, int):
                    total += count
            if total != 6:
                errors.append(f"gameplay.card_pools:{pool_id}.starting_entries: each MVP starting weapon needs exactly 6 cards, got {total}")
            if action_count != 4 or finisher_count != 2:
                errors.append(f"gameplay.card_pools:{pool_id}.starting_entries: each MVP starting weapon needs 4 action cards and 2 finishers, got {action_count} action and {finisher_count} finisher")
        elif pool_type == "reward":
            reward_pool_by_weapon[weapon_id] = pool
            reward_by_rarity = pool.get("reward_by_rarity", {})
            if not isinstance(reward_by_rarity, dict):
                errors.append(f"gameplay.card_pools:{pool_id}.reward_by_rarity: must be an object")
                continue
            for rarity, card_ids in reward_by_rarity.items():
                if rarity not in {"common", "uncommon", "rare"}:
                    errors.append(f"gameplay.card_pools:{pool_id}.reward_by_rarity: unsupported rarity {rarity!r}")
                    continue
                if not isinstance(card_ids, list):
                    errors.append(f"gameplay.card_pools:{pool_id}.reward_by_rarity.{rarity}: must be a list")
                    continue
                for card_id in card_ids:
                    card = cards_by_id.get(card_id)
                    if card is None:
                        errors.append(f"gameplay.card_pools:{pool_id}.reward_by_rarity.{rarity}: unknown card id {card_id!r}")
                    elif card.get("weapon_id") != weapon_id:
                        errors.append(f"gameplay.card_pools:{pool_id}.reward_by_rarity.{rarity}: card {card_id!r} belongs to {card.get('weapon_id')!r}")
        else:
            errors.append(f"gameplay.card_pools:{pool_id}.pool_type: unsupported pool type {pool_type!r}")

    for weapon in weapons:
        weapon_id = weapon.get("id", "<missing>")
        if weapon.get("starting_pool_id") not in pools_by_id:
            errors.append(f"gameplay.weapons:{weapon_id}.starting_pool_id: unknown pool id {weapon.get('starting_pool_id')!r}")
        if weapon.get("reward_pool_id") not in pools_by_id:
            errors.append(f"gameplay.weapons:{weapon_id}.reward_pool_id: unknown pool id {weapon.get('reward_pool_id')!r}")
        if weapon_id not in starting_pool_by_weapon:
            errors.append(f"gameplay.card_pools: missing starting pool for {weapon_id!r}")
        if weapon_id not in reward_pool_by_weapon:
            errors.append(f"gameplay.card_pools: missing reward pool for {weapon_id!r}")

    for enemy in enemies:
        enemy_id = enemy.get("id", "<missing>")
        view = enemy_views_by_id.get(enemy_id)
        if view is None:
            errors.append(f"enemies:{enemy_id}: missing enemy view")
        else:
            require_text(localization, view.get("name_key"), f"enemy_views:{enemy_id}.name_key", errors)
            require_asset(asset_ids, view.get("stand_asset"), f"enemy_views:{enemy_id}.stand_asset", errors)
            for intent_id, key in view.get("intent_text_keys", {}).items():
                require_text(localization, key, f"enemy_views:{enemy_id}.intent_text_keys.{intent_id}", errors)

    for relic in relics:
        relic_id = relic.get("id", "<missing>")
        view = relic_views_by_id.get(relic_id)
        if view is None:
            errors.append(f"relics:{relic_id}: missing relic view")
        else:
            require_text(localization, view.get("name_key"), f"relic_views:{relic_id}.name_key", errors)
            require_text(localization, view.get("rules_key"), f"relic_views:{relic_id}.rules_key", errors)
            require_asset(asset_ids, view.get("icon_asset"), f"relic_views:{relic_id}.icon_asset", errors)

    for encounter in encounters:
        encounter_id = encounter.get("id", "<missing>")
        for enemy_entry in encounter.get("enemies", []):
            enemy_id = enemy_entry.get("enemy_id") if isinstance(enemy_entry, dict) else None
            if enemy_id not in enemies_by_id:
                errors.append(f"encounters:{encounter_id}.enemies: unknown enemy id {enemy_id!r}")
        reward_profile = encounter.get("reward_profile", {})
        if reward_profile.get("card_pack_ids"):
            errors.append(f"encounters:{encounter_id}.reward_profile.card_pack_ids: card pack rewards are deprecated; use color shard plus weapon card rewards")
        relic_id = reward_profile.get("relic_id")
        if relic_id is not None and relic_id not in relics_by_id:
            errors.append(f"encounters:{encounter_id}.reward_profile.relic_id: unknown relic id {relic_id!r}")

    ordered_nodes = sorted(run_sequence.get("nodes", []), key=lambda node: node.get("order", 0))
    if len(ordered_nodes) != 6:
        errors.append(f"run_sequence.nodes: MVP run sequence must contain 6 encounters, got {len(ordered_nodes)}")
    for node in ordered_nodes:
        encounter_id = node.get("encounter_id") if isinstance(node, dict) else None
        if encounter_id not in encounters_by_id:
            errors.append(f"run_sequence.nodes: unknown encounter id {encounter_id!r}")

    for entry in run_sequence.get("starter_deck", []):
        card_id = entry.get("card_id") if isinstance(entry, dict) else None
        if card_id not in cards_by_id:
            errors.append(f"run_sequence.starter_deck: unknown card id {card_id!r}")

    boss_encounter_id = run_sequence.get("completion", {}).get("boss_encounter_id")
    boss_encounter = encounters_by_id.get(boss_encounter_id)
    if boss_encounter is None:
        errors.append(f"run_sequence.completion.boss_encounter_id: unknown encounter id {boss_encounter_id!r}")
    elif boss_encounter.get("node_type") != "boss":
        errors.append(f"run_sequence.completion.boss_encounter_id: encounter {boss_encounter_id!r} must be a boss")


def run_validation(project_root: Path) -> int:
    data_root = project_root / "game" / "data"
    files = [
        DataFile("cards", data_root / "gameplay" / "cards" / "cards.json", data_root / "schemas" / "gameplay" / "cards.schema.json"),
        DataFile("weapons", data_root / "gameplay" / "weapons" / "weapons.json", data_root / "schemas" / "gameplay" / "weapons.schema.json"),
        DataFile("colors", data_root / "gameplay" / "colors" / "colors.json", data_root / "schemas" / "gameplay" / "colors.schema.json"),
        DataFile("card_pools", data_root / "gameplay" / "card_pools" / "card_pools.json", data_root / "schemas" / "gameplay" / "card_pools.schema.json"),
        DataFile("enemies", data_root / "gameplay" / "enemies" / "enemies.json", data_root / "schemas" / "gameplay" / "enemies.schema.json"),
        DataFile("relics", data_root / "gameplay" / "relics" / "relics.json", data_root / "schemas" / "gameplay" / "relics.schema.json"),
        DataFile("encounters", data_root / "gameplay" / "encounters" / "encounters.json", data_root / "schemas" / "gameplay" / "encounters.schema.json"),
        DataFile("run_sequence", data_root / "gameplay" / "runs" / "mvp_run.json", data_root / "schemas" / "gameplay" / "run_sequence.schema.json"),
        DataFile("card_views", data_root / "presentation" / "card_views.json", data_root / "schemas" / "presentation" / "card_views.schema.json"),
        DataFile("enemy_views", data_root / "presentation" / "enemy_views.json", data_root / "schemas" / "presentation" / "enemy_views.schema.json"),
        DataFile("relic_views", data_root / "presentation" / "relic_views.json", data_root / "schemas" / "presentation" / "relic_views.schema.json"),
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

    if all(data_file.name in documents for data_file in files):
        validate_unified_gameplay(project_root, documents, errors)

    if errors:
        print("Data validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("Data validation passed.")
    print(f"Validated {len(files)} data files and {len(files)} schemas.")
    print("Unified gameplay data is validated as the Sword Black Tower color energy schema.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate Sword Black Tower MVP content data.")
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
