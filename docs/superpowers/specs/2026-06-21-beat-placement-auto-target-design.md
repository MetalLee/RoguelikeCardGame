# Beat Placement Auto Target Design

## Goal

Improve beat combat card placement so the player does not need to click a filled player beat slot a second time before selecting an enemy beat target.

## Interaction

1. The player drags an action card from hand to an empty player beat slot.
2. Releasing the mouse on that empty slot places the card into the slot.
3. The placed slot immediately becomes a temporary targeting anchor.
4. An arrow starts from that player beat slot and follows the mouse.
5. The player clicks an unlocked enemy beat to set the collision target.
6. If all beats of that enemy are locked, the player may click the enemy body instead.
7. If the player right-clicks or left-clicks empty space while targeting, the newly placed card returns to hand and the player beat slot is cleared.

## Scope

- This replaces the previous "place card, then click the player beat slot again" targeting start.
- Only the most recently placed, untargeted action card can be auto-targeted.
- Cancellation restores the card instance to the hand and clears the player beat slot.
- Existing target validation remains authoritative in `BeatRoundPlanningService` and `BeatCombatService`.

## Error Handling

- Expected cancellation is not an error and shows no fatal screen.
- Invalid target selection keeps the same stable battle feedback.
- Unexpected state mismatches still route through fatal error handling.

## Verification

- Unit tests cover rollback from a slotted beat back to hand.
- Godot build and headless project startup must pass.
