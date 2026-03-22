class_name ChestSpawnRule
extends Resource
## Tunable chest pacing for a run. Read from GameConfig / game_settings.tres.

@export var spawn_every_n_rows: int = 12
@export var min_score_for_chest: int = 50
@export var max_active_chests_per_run: int = 1
@export var coins_per_chest: int = 10
## Reserved for future loot tables; not used by MVP open flow.
@export var loot_table_id: String = "default"
