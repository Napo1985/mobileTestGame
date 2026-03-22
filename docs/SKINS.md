# Skins (`data/skins/`)

Skins change how the **player token** is drawn in the run scene (`GridWorld` fill + outline colors). The game ships a **JSON catalog** so you can add or tune skins without new code.

---

## Layout

| Path | Purpose |
| ---- | ------- |
| `res://data/skins/skin_catalog.json` | List of skin entries (id, display name, colors, unlock rule). |
| `res://src/meta/skin_catalog.gd` | Loads and parses the JSON; unlock/equip helpers. |
| `res://src/meta/skin_applicator.gd` | Applies the **equipped** skin from `ProfileService` to a `GridWorld`. |

Optional: add per-skin art under `res://data/skins/sprites/<skin_id>/` for future `Sprite2D`-based players; the MVP uses **colors only**.

---

## Catalog schema (`skin_catalog.json`)

Top level:

- **`skins`**: array of skin objects.

Each skin object:

| Field | Type | Purpose |
| ----- | ---- | ------- |
| `id` | string | Stable id (e.g. `frog_green`). Used in saves. |
| `display_name` | string | Shown in the Skins UI. |
| `player_fill` | string | Hex color for the player fill (`#rrggbb`). |
| `player_outline` | string | Hex color for the outline ring. |
| `unlock` | object | How the skin is obtained (see below). |

### `unlock` types

| `unlock.type` | Extra fields | Behavior |
| ------------- | ------------ | -------- |
| `default` | — | Unlocked for everyone (e.g. `default` skin). |
| `coins` | `cost` (int) | Player spends coins once to **unlock**; then they can equip anytime. |

---

## Profile and persistence

- **`ProfileService`** (autoload) stores `equipped_skin_id`, `unlocked_skins`, coins, gems, and inventory in **`user://profile.json`**.
- Equipping a skin requires that it is **unlocked**. The Skins screen calls `SkinCatalog.try_unlock_and_equip()` which deducts coins when needed and then sets the equipped id.

---

## Add a skin (checklist)

1. Add a new object to the `skins` array in **`data/skins/skin_catalog.json`** with a unique `id`, `display_name`, `player_fill`, `player_outline`, and `unlock`.
2. Restart the game (or re-open the Skins scene) so the catalog is re-read.
3. In the **Skins** UI (main menu → Skins), select the skin and use **Equip / Unlock**.

No code changes are required for simple color-only skins.

---

## Preview and in-run application

- **Preview**: `res://src/ui/settings_skins.tscn` shows a list + small preview control (`skin_preview.gd`).
- **In-run**: `RunController` calls `SkinApplicator.apply_equipped_to_grid(grid)` after each `reset_run()` so restarts pick up the equipped skin.

---

## Optional `.tres` catalog

The **authoritative** catalog for this MVP is **`skin_catalog.json`**. You can mirror metadata in a `Resource` later for inspector editing, but keep a **single source of truth** to avoid drift between JSON and `.tres`.
