class_name AbilityRegistry
extends RefCounted
## Maps ability id -> instance. New abilities: add a .gd file and register in RunController._register_abilities.


var _by_id: Dictionary = {}


func register(ability: Ability) -> void:
	var id := ability.get_id()
	if id.is_empty():
		push_warning("AbilityRegistry: skipped ability with empty id")
		return
	_by_id[id] = ability


func get_ability(id: StringName) -> Ability:
	return _by_id.get(id) as Ability


func has_ability(id: StringName) -> bool:
	return _by_id.has(id)
