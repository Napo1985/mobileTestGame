extends Ability
class_name ShieldAbility


func get_id() -> StringName:
	return &"shield"


func activate(ctx: AbilityContext) -> void:
	if ctx.buff_manager:
		ctx.buff_manager.grant_shield_hits(1)
