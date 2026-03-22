@tool
extends EditorPlugin
## Editor-only: wires the PlayGamesBridge AAR into Android exports when present under `addons/play_games_bridge/bin/`.


var _export: PlayGamesAndroidExport


func _enter_tree() -> void:
	_export = PlayGamesAndroidExport.new()
	add_export_plugin(_export)


func _exit_tree() -> void:
	if _export:
		remove_export_plugin(_export)
		_export = null


class PlayGamesAndroidExport extends EditorExportPlugin:
	func _get_name() -> String:
		return "PlayGamesBridge"


	func _supports_platform(platform: EditorExportPlatform) -> bool:
		return platform is EditorExportPlatformAndroid


	func _get_android_libraries(platform: EditorExportPlatform, debug: bool) -> PackedStringArray:
		var name := "PlayGamesBridge-debug.aar" if debug else "PlayGamesBridge-release.aar"
		var rel := "addons/play_games_bridge/bin/%s" % name
		if not FileAccess.file_exists("res://%s" % rel):
			return PackedStringArray()
		return PackedStringArray([rel])
