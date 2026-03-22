extends Node
## Local profile: coins, gems, equipped skin, unlocks; persisted under `user://profile.json`.


const SAVE_PATH := "user://profile.json"
const SAVE_VERSION := 1

var _coins: int = 0
var _gems: int = 0
var _equipped_skin_id: String = "default"
## Skin ids the player may equip (includes free `default`).
var _unlocked: Dictionary = {}
var _inventory: Array[String] = []
var _suppress_cloud_push: bool = false


func _ready() -> void:
	_unlocked["default"] = true
	_load()


func get_coins() -> int:
	return _coins


func get_gems() -> int:
	return _gems


func get_equipped_skin_id() -> String:
	return _equipped_skin_id


func get_inventory() -> Array[String]:
	return _inventory.duplicate()


func is_skin_unlocked(skin_id: String) -> bool:
	return _unlocked.get(skin_id, false)


func set_equipped_skin(skin_id: String) -> void:
	if skin_id.is_empty():
		return
	if not is_skin_unlocked(skin_id):
		return
	_equipped_skin_id = skin_id
	_save()


func unlock_skin(skin_id: String) -> void:
	if skin_id.is_empty():
		return
	_unlocked[skin_id] = true
	_save()


func add_coins(amount: int) -> void:
	if amount <= 0:
		return
	_coins += amount
	_save()


func spend_coins(amount: int) -> bool:
	if amount <= 0:
		return false
	if _coins < amount:
		return false
	_coins -= amount
	_save()
	return true


func add_gems(amount: int) -> void:
	if amount <= 0:
		return
	_gems += amount
	_save()


## Last-write-wins merge from Play Games Saved Games blob (JSON same shape as local save).
func apply_cloud_snapshot_json(json: String) -> void:
	if json.is_empty():
		return
	var data = JSON.parse_string(json)
	if typeof(data) != TYPE_DICTIONARY:
		return
	_suppress_cloud_push = true
	_apply_loaded_dictionary(data)
	_save()
	_suppress_cloud_push = false


func _apply_loaded_dictionary(d: Dictionary) -> void:
	if int(d.get("version", 0)) > SAVE_VERSION:
		return
	_coins = int(d.get("coins", _coins))
	_gems = int(d.get("gems", _gems))
	var eq := str(d.get("equipped_skin_id", _equipped_skin_id))
	if not eq.is_empty():
		_equipped_skin_id = eq
	_unlocked.clear()
	_unlocked["default"] = true
	var raw_unlocked = d.get("unlocked_skins", [])
	if raw_unlocked is Array:
		for x in raw_unlocked:
			_unlocked[str(x)] = true
	_inventory.clear()
	var raw_inv = d.get("inventory", [])
	if raw_inv is Array:
		for x in raw_inv:
			_inventory.append(str(x))


func _load() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		_save()
		return
	var f := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if f == null:
		return
	var txt := f.get_as_text()
	var data = JSON.parse_string(txt)
	if typeof(data) != TYPE_DICTIONARY:
		return
	var d: Dictionary = data
	if int(d.get("version", 0)) > SAVE_VERSION:
		push_warning("ProfileService: newer save version — ignoring")
		return
	_apply_loaded_dictionary(d)


func _save() -> void:
	var d := {
		"version": SAVE_VERSION,
		"coins": _coins,
		"gems": _gems,
		"equipped_skin_id": _equipped_skin_id,
		"unlocked_skins": _unlocked.keys(),
		"inventory": _inventory,
	}
	var json := JSON.stringify(d, "\t")
	var f := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if f:
		f.store_string(json)
	if not _suppress_cloud_push:
		PlayGamesService.notify_profile_json_saved(json)
