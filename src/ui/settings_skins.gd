extends Control

@onready var _list: ItemList = $Center/VBox/Body/ItemList
@onready var _preview: Control = $Center/VBox/Body/PreviewPanel
@onready var _status: Label = $Center/VBox/StatusLabel
@onready var _equip: Button = $Center/VBox/Buttons/EquipButton
@onready var _back: Button = $Center/VBox/Buttons/BackButton

var _catalog: SkinCatalog
var _ids: Array[String] = []


func _ready() -> void:
	_catalog = SkinCatalog.load_default()
	_list.item_selected.connect(_on_item_selected)
	_equip.pressed.connect(_on_equip_pressed)
	_back.pressed.connect(_on_back_pressed)
	_populate_list()
	_refresh_status()


func _populate_list() -> void:
	_list.clear()
	_ids.clear()
	for skin: Dictionary in _catalog.get_all():
		var id: String = str(skin.get("id", ""))
		if id.is_empty():
			continue
		_ids.append(id)
		var name: String = str(skin.get("display_name", id))
		var suffix := ""
		if not _catalog.is_unlocked(id, ProfileService):
			var u: Dictionary = skin.get("unlock", {})
			if str(u.get("type", "")) == "coins":
				suffix = " (%d coins)" % int(u.get("cost", 0))
			else:
				suffix = " (locked)"
		_list.add_item("%s%s" % [name, suffix])
	if _list.item_count > 0:
		_list.select(0)
		_apply_selection(0)


func _selected_skin_id() -> String:
	var i := _list.get_selected_items()
	if i.is_empty():
		return ""
	var idx: int = int(i[0])
	if idx < 0 or idx >= _ids.size():
		return ""
	return _ids[idx]


func _on_item_selected(index: int) -> void:
	_apply_selection(index)
	_refresh_status()


func _apply_selection(index: int) -> void:
	if index < 0 or index >= _ids.size():
		return
	var id := _ids[index]
	var skin: Dictionary = _catalog.get_skin(id)
	if skin.is_empty():
		return
	var f := _catalog.parse_fill_color(skin)
	var o := _catalog.parse_outline_color(skin)
	_preview.set_skin_colors(f, o)


func _refresh_status() -> void:
	var id := _selected_skin_id()
	if id.is_empty():
		_status.text = ""
		return
	var skin: Dictionary = _catalog.get_skin(id)
	var unlocked := _catalog.is_unlocked(id, ProfileService)
	var line := "Coins: %d  |  Equipped: %s" % [ProfileService.get_coins(), ProfileService.get_equipped_skin_id()]
	if unlocked:
		_status.text = "%s\n%s" % [line, "This skin is unlocked. Tap Equip to wear it."]
	else:
		var u: Dictionary = skin.get("unlock", {})
		if str(u.get("type", "")) == "coins":
			_status.text = "%s\nUnlock for %d coins (tap Equip)." % [line, int(u.get("cost", 0))]
		else:
			_status.text = "%s\nLocked." % line


func _on_equip_pressed() -> void:
	var id := _selected_skin_id()
	if id.is_empty():
		return
	if _catalog.try_unlock_and_equip(id, ProfileService):
		_refresh_status()
		var keep := id
		_populate_list()
		_select_id(keep)
		return
	_status.text = "Not enough coins or skin unavailable."


func _select_id(skin_id: String) -> void:
	for i in _ids.size():
		if _ids[i] == skin_id:
			_list.select(i)
			_apply_selection(i)
			_refresh_status()
			return


func _on_back_pressed() -> void:
	Game.go_to_main_menu()
