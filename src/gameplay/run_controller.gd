extends Node
class_name RunController
## Score, run lifecycle, and wiring between input, grid, and UI signals.

signal score_changed(value: int)
signal game_over_changed(active: bool)
signal coins_changed(total: int)

@export var grass_row: RowDefinition
@export var spawn_rows: Array[RowDefinition] = []
@export var game_config: GameConfig

@onready var grid: GridWorld = $GridWorld
@onready var player_input: PlayerActor = $PlayerActor

var _spawner := RowSpawner.new()
var _game_over: bool = false
var _buff_manager := BuffManager.new()
var _ability_registry := AbilityRegistry.new()
var _ability_rng := RandomNumberGenerator.new()
var _ability_context: AbilityContext


func _ready() -> void:
	_resolve_row_resources()
	_spawner.configure(spawn_rows)
	var cfg: GameConfig = game_config
	if cfg == null:
		cfg = Game.get_game_config()
	_register_abilities()
	_ability_rng.randomize()
	_ability_context = AbilityContext.new(self, grid, _buff_manager, _ability_rng)
	grid.setup(_spawner, grass_row, cfg, _buff_manager)
	grid.scored.connect(_on_scored)
	grid.died.connect(_on_died)
	grid.chest_collected.connect(_on_chest_collected)
	grid.reset_run()
	player_input.setup(self)
	score_changed.emit(grid.get_score())
	coins_changed.emit(ProfileService.get_coins())
	SkinApplicator.apply_equipped_to_grid(grid)


func get_buff_manager() -> BuffManager:
	return _buff_manager


func request_ability(ability_id: StringName) -> void:
	if _game_over:
		return
	var ab: Ability = _ability_registry.get_ability(ability_id)
	if ab and _ability_context:
		ab.activate(_ability_context)


func _register_abilities() -> void:
	_ability_registry.register(ShieldAbility.new())
	_ability_registry.register(JumpAbility.new())
	_ability_registry.register(MissileAbility.new())
	_ability_registry.register(FastPaceAbility.new())


func _apply_equipped_skin_to_grid() -> void:
	SkinApplicator.apply_equipped_to_grid(grid)


func is_game_over() -> bool:
	return _game_over


func request_move(dir: Vector2i) -> void:
	if _game_over:
		return
	grid.try_move(dir)


func restart_run() -> void:
	_game_over = false
	game_over_changed.emit(false)
	grid.reset_run()
	SkinApplicator.apply_equipped_to_grid(grid)
	score_changed.emit(grid.get_score())


func _on_scored(value: int) -> void:
	score_changed.emit(value)


func _on_died() -> void:
	if _game_over:
		return
	_game_over = true
	PlayGamesService.on_run_ended_with_score(grid.get_score())
	game_over_changed.emit(true)


func _on_chest_collected(amount: int) -> void:
	ProfileService.add_coins(amount)
	coins_changed.emit(ProfileService.get_coins())


func _resolve_row_resources() -> void:
	if grass_row == null:
		grass_row = load("res://data/rows/row_grass.tres") as RowDefinition
	if spawn_rows.is_empty():
		spawn_rows = [
			load("res://data/rows/row_road_slow.tres") as RowDefinition,
			load("res://data/rows/row_road_fast.tres") as RowDefinition,
			load("res://data/rows/row_road_left.tres") as RowDefinition,
			load("res://data/rows/row_grass_gap.tres") as RowDefinition,
		]
