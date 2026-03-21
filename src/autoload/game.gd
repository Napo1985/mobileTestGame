extends Node
## Facade for config and scene flow. Expanded in later phases.

func go_to_main_menu() -> void:
	get_tree().change_scene_to_file("res://src/ui/main_menu.tscn")

func go_to_run() -> void:
	get_tree().change_scene_to_file("res://scenes/gameplay/run_root.tscn")
