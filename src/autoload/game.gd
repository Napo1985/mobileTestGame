extends Node
## Facade for config and scene flow. Expanded in later phases.

var _game_config: GameConfig


func get_game_config() -> GameConfig:
	if _game_config == null and ResourceLoader.exists(GameConfig.DEFAULT_PATH):
		_game_config = load(GameConfig.DEFAULT_PATH) as GameConfig
	return _game_config


func go_to_main_menu() -> void:
	# Defer scene switching so we don't mutate the scene tree while it's
	# still processing _ready() / child add/remove operations.
	get_tree().call_deferred("change_scene_to_file", "res://src/ui/main_menu.tscn")

func go_to_run() -> void:
	get_tree().call_deferred("change_scene_to_file", "res://scenes/gameplay/run_root.tscn")


func go_to_settings_skins() -> void:
	get_tree().call_deferred("change_scene_to_file", "res://src/ui/settings_skins.tscn")
