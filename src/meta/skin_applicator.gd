class_name SkinApplicator
extends RefCounted
## Applies equipped skin colors from SkinCatalog + ProfileService to GridWorld drawing.


static var _catalog: SkinCatalog


static func _get_catalog() -> SkinCatalog:
	if _catalog == null:
		_catalog = SkinCatalog.load_default()
	return _catalog


static func apply_equipped_to_grid(grid: GridWorld) -> void:
	var cat := _get_catalog()
	var id: String = ProfileService.get_equipped_skin_id()
	var skin: Dictionary = cat.get_skin(id)
	if skin.is_empty():
		grid.player_fill_color = Color(0.95, 0.84, 0.2, 1)
		grid.player_outline_color = Color(0.22, 0.16, 0.04, 1)
		return
	grid.player_fill_color = cat.parse_fill_color(skin)
	grid.player_outline_color = cat.parse_outline_color(skin)


static func apply_skin_id_to_grid(grid: GridWorld, skin_id: String) -> void:
	var cat := _get_catalog()
	var skin: Dictionary = cat.get_skin(skin_id)
	if skin.is_empty():
		apply_equipped_to_grid(grid)
		return
	grid.player_fill_color = cat.parse_fill_color(skin)
	grid.player_outline_color = cat.parse_outline_color(skin)
