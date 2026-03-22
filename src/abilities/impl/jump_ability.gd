extends Ability
class_name JumpAbility
## Stub: discrete hop game; jump-over-car would need grid rules extension.


func get_id() -> StringName:
	return &"jump"


func activate(ctx: AbilityContext) -> void:
	if ctx.run:
		pass # Extension point: request vertical hop without advancing score row, etc.
