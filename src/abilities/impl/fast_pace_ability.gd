extends Ability
class_name FastPaceAbility
## Stub: temporary score multiplier or spawner cadence — needs RunController hooks.


func get_id() -> StringName:
	return &"fast_pace"


func activate(ctx: AbilityContext) -> void:
	if ctx.run:
		pass # Extension point: timed buff via BuffManager / RunController state.
