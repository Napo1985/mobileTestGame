class_name Ability
extends RefCounted
## Base for activatable abilities. Subclasses register with AbilityRegistry (one file per ability).


func get_id() -> StringName:
	push_error("Ability.get_id() not implemented")
	return &""


func activate(_ctx: AbilityContext) -> void:
	push_error("Ability.activate() not implemented for %s" % get_id())
