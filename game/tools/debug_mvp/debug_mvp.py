#!/usr/bin/env python3
"""Create MVP debug sessions and export log/metrics scaffolds."""

from __future__ import annotations

import argparse
import json
import random
from datetime import datetime
from pathlib import Path
from typing import Any


def load_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as file:
        return json.load(file)


def index_items(document: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {item["id"]: item for item in document.get("items", [])}


def expand_starter_deck(entries: list[dict[str, Any]]) -> list[str]:
    cards: list[str] = []
    for entry in entries:
        cards.extend([entry["card_id"]] * int(entry["count"]))
    return cards


def default_weapon_starter_deck(card_pools_by_id: dict[str, dict[str, Any]]) -> list[str]:
    """Compatibility helper for debug sessions before the weapon picker is simulated."""
    revolver_pool = card_pools_by_id["card_pool.starting.revolver_sword"]
    arm_pool = card_pools_by_id["card_pool.starting.mechanical_arm"]
    return expand_starter_deck(revolver_pool["starting_entries"])[:6] + expand_starter_deck(arm_pool["starting_entries"])[:4]


def flatten_reward_pool(pool: dict[str, Any]) -> list[str]:
    cards: list[str] = []
    for rarity in ("common", "uncommon", "rare"):
        cards.extend(pool.get("reward_by_rarity", {}).get(rarity, []))
    return cards


def parse_card_list(value: str | None) -> list[str] | None:
    if value is None:
        return None
    return [item.strip() for item in value.split(",") if item.strip()]


def write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as file:
        json.dump(value, file, ensure_ascii=False, indent=2)
        file.write("\n")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Create a RoguelikeCardGame MVP debug session.")
    parser.add_argument("--encounter-id", help="Fixed encounter id to enter directly.")
    parser.add_argument("--seed", type=int, help="Run seed. Defaults to a generated seed.")
    parser.add_argument("--starter-deck", help="Comma-separated card ids replacing the MVP starter deck.")
    parser.add_argument("--add-card", action="append", default=[], help="Card id to append to the starter deck.")
    parser.add_argument("--reward-pack-id", action="append", default=[], help="Deprecated compatibility option; card packs are no longer previewed.")
    parser.add_argument("--output-dir", type=Path, help="Output directory. Defaults to game/logs.")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    repo_root = Path(__file__).resolve().parents[3]
    data_root = repo_root / "game" / "data"
    output_dir = args.output_dir or repo_root / "game" / "logs"

    gameplay_root = data_root / "gameplay"
    cards_by_id = index_items(load_json(gameplay_root / "cards" / "cards.json"))
    card_pools_by_id = index_items(load_json(gameplay_root / "card_pools" / "card_pools.json"))
    encounters_by_id = index_items(load_json(gameplay_root / "encounters" / "encounters.json"))
    run_sequence = load_json(gameplay_root / "runs" / "mvp_run.json")

    node_order = [
        node["encounter_id"]
        for node in sorted(run_sequence["nodes"], key=lambda item: int(item["order"]))
    ]
    encounter_id = args.encounter_id or node_order[0]
    if encounter_id not in encounters_by_id:
        raise SystemExit(f"Unknown encounter id: {encounter_id}")

    starter_deck = parse_card_list(args.starter_deck)
    if starter_deck is None:
        starter_deck = expand_starter_deck(run_sequence.get("starter_deck", []))
    if not starter_deck:
        starter_deck = default_weapon_starter_deck(card_pools_by_id)
    starter_deck.extend(args.add_card)
    unknown_cards = [card_id for card_id in starter_deck if card_id not in cards_by_id]
    if unknown_cards:
        raise SystemExit(f"Unknown card id(s): {', '.join(unknown_cards)}")

    seed = args.seed if args.seed is not None else random.randint(0, 2_147_483_647)
    encounter = encounters_by_id[encounter_id]
    if args.reward_pack_id or encounter["reward_profile"].get("card_pack_ids"):
        print("Warning: card pack rewards are deprecated; previewing color shard plus weapon card rewards instead.")

    reward_previews = [
        {
            "reward_type": "color_shard",
            "candidate_colors": ["red", "yellow", "blue", "green", "purple"],
        },
        {
            "reward_type": "weapon_card_three_choice",
            "weapon_ids": ["weapon.revolver_sword", "weapon.mechanical_arm"],
            "candidate_pool_ids": ["card_pool.reward.revolver_sword", "card_pool.reward.mechanical_arm"],
            "candidate_ids": flatten_reward_pool(card_pools_by_id["card_pool.reward.revolver_sword"])
            + flatten_reward_pool(card_pools_by_id["card_pool.reward.mechanical_arm"]),
        },
    ]

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    stem = f"debug_{encounter_id}_seed_{seed}_{timestamp}"
    session_path = output_dir / f"{stem}_session.json"
    combat_log_path = output_dir / f"{stem}_combat_log.json"
    metrics_path = output_dir / f"{stem}_metrics.json"

    session = {
        "run_seed": seed,
        "direct_encounter_id": encounter_id,
        "node_order": node_order,
        "starter_deck": starter_deck,
        "added_cards": args.add_card,
        "reward_pack_previews": [],
        "reward_previews": reward_previews,
    }
    combat_log = {
        "combat_id": f"debug_{encounter_id}_seed_{seed}",
        "encounter_id": encounter_id,
        "status": "not_started",
        "events": [],
    }
    metrics = {
        "run_seed": seed,
        "node_order": node_order,
        "combats": [],
        "rewards": [],
        "relics": [],
        "final_state": "debug_session_created",
        "final_node_encounter_id": None,
        "total_duration_seconds": 0,
    }

    write_json(session_path, session)
    write_json(combat_log_path, combat_log)
    write_json(metrics_path, metrics)

    print(f"Debug session: {session_path}")
    print(f"Combat log: {combat_log_path}")
    print(f"Playtest metrics: {metrics_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
