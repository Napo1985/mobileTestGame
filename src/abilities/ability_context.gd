class_name AbilityContext
extends RefCounted
## Passed into Ability.activate; holds run-scoped services and RNG.


var run: RunController
var grid: GridWorld
var buff_manager: BuffManager
var rng: RandomNumberGenerator


func _init(
		p_run: RunController,
		p_grid: GridWorld,
		p_buffs: BuffManager,
		p_rng: RandomNumberGenerator,
) -> void:
	run = p_run
	grid = p_grid
	buff_manager = p_buffs
	rng = p_rng
