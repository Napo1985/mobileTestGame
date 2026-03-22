extends Node
## Google Play Games bridge: sign-in, leaderboards, optional Saved Games snapshots.
## On desktop/editor, calls no-op. On Android, uses singleton `PlayGamesBridge` when the plugin AAR is exported.


signal sign_in_completed(success: bool, message: String)
signal leaderboard_submit_completed(success: bool, message: String)
signal snapshot_save_completed(success: bool, message: String)
signal snapshot_load_completed(success: bool, data: String)

var _plugin: Object
var _config: PlayGamesConfig


func _ready() -> void:
	if ResourceLoader.exists(PlayGamesConfig.DEFAULT_PATH):
		_config = load(PlayGamesConfig.DEFAULT_PATH) as PlayGamesConfig
	if OS.get_name() != "Android":
		set_process(false)
		return
	_plugin = Engine.get_singleton("PlayGamesBridge")
	if _plugin == null:
		push_warning("PlayGamesService: PlayGamesBridge singleton not found — export the Android plugin AAR (see docs/PLAY_GAMES.md).")
		return
	_connect_plugin_signals()
	call_deferred("sign_in")


func get_config() -> PlayGamesConfig:
	return _config


func is_plugin_available() -> bool:
	return _plugin != null


func sign_in() -> void:
	if _plugin == null:
		sign_in_completed.emit(false, "no_plugin")
		return
	_plugin.sign_in()


func sign_out() -> void:
	if _plugin == null:
		return
	_plugin.sign_out()


func submit_leaderboard_score(score: int) -> void:
	var cfg := _config
	if cfg == null or not cfg.enabled or cfg.leaderboard_high_score_id.is_empty():
		return
	if _plugin == null:
		return
	_plugin.submit_leaderboard_score(cfg.leaderboard_high_score_id, score)


func save_snapshot(data: String) -> void:
	var cfg := _config
	if cfg == null or not cfg.enabled or cfg.snapshot_name.is_empty():
		return
	if _plugin == null:
		return
	_plugin.save_snapshot(cfg.snapshot_name, data)


func load_snapshot() -> void:
	var cfg := _config
	if cfg == null or not cfg.enabled or cfg.snapshot_name.is_empty():
		return
	if _plugin == null:
		return
	_plugin.load_snapshot(cfg.snapshot_name)


func on_run_ended_with_score(score: int) -> void:
	submit_leaderboard_score(score)


func notify_profile_json_saved(json: String) -> void:
	var cfg := _config
	if cfg == null or not cfg.enabled:
		return
	save_snapshot(json)


func _connect_plugin_signals() -> void:
	if _plugin.has_signal("sign_in_result"):
		_plugin.sign_in_result.connect(_on_sign_in_result)
	if _plugin.has_signal("leaderboard_submit_result"):
		_plugin.leaderboard_submit_result.connect(_on_leaderboard_submit_result)
	if _plugin.has_signal("snapshot_save_result"):
		_plugin.snapshot_save_result.connect(_on_snapshot_save_result)
	if _plugin.has_signal("snapshot_load_result"):
		_plugin.snapshot_load_result.connect(_on_snapshot_load_result)


func _on_sign_in_result(success: bool, message: String) -> void:
	sign_in_completed.emit(success, message)
	if not success:
		return
	var cfg := _config
	if cfg and cfg.sync_profile_from_cloud_on_sign_in:
		load_snapshot()


func _on_leaderboard_submit_result(success: bool, message: String) -> void:
	leaderboard_submit_completed.emit(success, message)


func _on_snapshot_save_result(success: bool, message: String) -> void:
	snapshot_save_completed.emit(success, message)


func _on_snapshot_load_result(success: bool, data: String) -> void:
	snapshot_load_completed.emit(success, data)
	if not success or data.is_empty():
		return
	ProfileService.apply_cloud_snapshot_json(data)
