class_name SkinCatalog
extends RefCounted
## Loads `res://data/skins/skin_catalog.json` (data-driven skins).


const CATALOG_PATH := "res://data/skins/skin_catalog.json"

var _skins: Array[Dictionary] = []
var _by_id: Dictionary = {}


static func load_default() -> SkinCatalog:
	var c := SkinCatalog.new()
	c._load_from_path(CATALOG_PATH)
	return c


func _load_from_path(path: String) -> void:
	_skins.clear()
	_by_id.clear()
	if not FileAccess.file_exists(path):
		push_warning("SkinCatalog: missing %s" % path)
		return
	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		push_warning("SkinCatalog: could not open %s" % path)
		return
	var txt := f.get_as_text()
	var data = JSON.parse_string(txt)
	if typeof(data) != TYPE_DICTIONARY:
		push_warning("SkinCatalog: invalid JSON root")
		return
	var arr: Array = data.get("skins", [])
	for item in arr:
		if typeof(item) != TYPE_DICTIONARY:
			continue
		var d: Dictionary = item
		var id: String = str(d.get("id", ""))
		if id.is_empty():
			continue
		_skins.append(d)
		_by_id[id] = d


func get_all() -> Array[Dictionary]:
	var out: Array[Dictionary] = []
	for s in _skins:
		out.append(s)
	return out


func get_skin(skin_id: String) -> Dictionary:
	return _by_id.get(skin_id, {}) as Dictionary


func parse_fill_color(skin: Dictionary) -> Color:
	return _parse_color(skin.get("player_fill", "#ffffff"))


func parse_outline_color(skin: Dictionary) -> Color:
	return _parse_color(skin.get("player_outline", "#000000"))


func _parse_color(s: Variant) -> Color:
	if s is Color:
		return s
	var str_val := str(s).strip_edges()
	if str_val.begins_with("#"):
		return Color(str_val)
	return Color(str_val)


func can_unlock(skin: Dictionary, profile: Node) -> bool:
	var u: Dictionary = skin.get("unlock", {})
	var t := str(u.get("type", "default"))
	if t == "default":
		return true
	if t == "coins":
		var cost: int = int(u.get("cost", 0))
		return profile.get_coins() >= cost
	return false


func is_unlocked(skin_id: String, profile: Node) -> bool:
	if skin_id == "default" or skin_id.is_empty():
		return true
	return profile.is_skin_unlocked(skin_id)


func try_unlock_and_equip(skin_id: String, profile: Node) -> bool:
	var skin: Dictionary = get_skin(skin_id)
	if skin.is_empty():
		return false
	if is_unlocked(skin_id, profile):
		profile.set_equipped_skin(skin_id)
		return true
	var u: Dictionary = skin.get("unlock", {})
	if str(u.get("type", "")) != "coins":
		return false
	var cost: int = int(u.get("cost", 0))
	if profile.get_coins() < cost:
		return false
	if not profile.spend_coins(cost):
		return false
	profile.unlock_skin(skin_id)
	profile.set_equipped_skin(skin_id)
	return true
