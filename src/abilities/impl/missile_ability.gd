extends Ability
class_name MissileAbility
## Stub: lane-clear or obstacle hit — wire RowRuntime targets later.


func get_id() -> StringName:
	return &"missile"


func activate(ctx: AbilityContext) -> void:
	if ctx.grid:
		pass # Extension point: pick lane, damage cars in row window.
