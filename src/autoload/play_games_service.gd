extends Node
## Google Play Games bridge; no-op on desktop/editor (stub for Phase 0).

func _ready() -> void:
	if OS.get_name() != "Android":
		set_process(false)
