class_name RowSpawner
extends RefCounted
## Picks the next RowDefinition in round-robin order (data-driven stream).

var _definitions: Array[RowDefinition] = []
var _index: int = 0


func configure(defs: Array[RowDefinition]) -> void:
	_definitions = defs.duplicate()


func reset() -> void:
	_index = 0


func pick_next() -> RowDefinition:
	if _definitions.is_empty():
		push_error("RowSpawner: no RowDefinition resources configured.")
		return null
	var def: RowDefinition = _definitions[_index % _definitions.size()]
	_index += 1
	return def
