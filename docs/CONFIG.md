# Configuration (`GameConfig` & data)

This project keeps gameplay tuning in **`Resource` files** so designers can change behavior without editing GDScript. The main file is:

- **`res://config/game_settings.tres`** — `GameConfig` instance (load path also exposed as `GameConfig.DEFAULT_PATH`).

The autoload **`Game`** caches this via `Game.get_game_config()`. The run scene’s **`RunController`** uses an exported `game_config` if set; otherwise it falls back to `Game.get_game_config()`.

---

## `GameConfig` (`src/content/resources/game_config.gd`)

| Property      | Type               | Purpose                          |
| ------------- | ------------------ | -------------------------------- |
| `chest_rule`  | `ChestSpawnRule`   | Chest pacing and coin payout     |

---

## `ChestSpawnRule` (`src/content/resources/chest_spawn_rule.gd`)

Chests appear only on **safe** rows (e.g. grass) spawned from the procedural stream. They are skipped on roads.

| Property                       | Default | Meaning |
| ------------------------------ | ------- | ------- |
| `spawn_every_n_rows`         | `12`    | After this many **procedural** row spawns since the last chest (or run start), the spawner may place a chest on the next eligible safe row. |
| `min_score_for_chest`        | `50`    | Current run **score** must be at least this high before any chest can spawn. |
| `max_active_chests_per_run`  | `1`     | Cap on simultaneous chests in the world for that run. |
| `coins_per_chest`            | `10`    | Coins granted when the player **steps onto** the chest cell (via `ProfileService`). |
| `loot_table_id`              | `"default"` | Reserved for future loot tables; MVP uses `coins_per_chest` only. |

### Tuning workflow

1. Open **`config/game_settings.tres`** in the Godot inspector.
2. Expand **`chest_rule`** and edit exports.
3. Save, then start or restart a run so `GridWorld.reset_run()` picks up values.

No code changes are required for interval, score gate, cap, or coin amount.

---

## Row content (`RowDefinition`)

Obstacle and safe lanes are defined by **`RowDefinition`** resources under **`res://data/rows/`**.

### Naming conventions

- **`row_grass*.tres`** — safe rows (no cars).
- **`row_road*.tres`** — road rows with `kind = ROAD` and `car_lane_bits` / `ObstacleVariant` settings.

### Adding a new row type

1. Duplicate an existing `.tres` in `data/rows/` or create a new `Resource` with script `row_definition.gd`.
2. Set **`row_name`**, **`kind`**, colors, **`car_lane_bits`**, speeds, and optional **`obstacle`** (`ObstacleVariant`).
3. Add the resource to **`RunController.spawn_rows`** in the run scene (or keep the default code list in `run_controller.gd` → `_resolve_row_resources()`).

The **`RowSpawner`** reads that array in order (round-robin).

---

## Related code

| Area            | Location |
| --------------- | -------- |
| Chest spawn + collect | `src/gameplay/grid_world.gd` |
| Run wiring      | `src/gameplay/run_controller.gd` |
| Coins (MVP)     | `src/autoload/profile_service.gd` |
