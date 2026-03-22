extends Control

func _on_play_pressed() -> void:
	Game.go_to_run()


func _on_skins_pressed() -> void:
	Game.go_to_settings_skins()
