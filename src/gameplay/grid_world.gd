class_name GridWorld
extends Node2D
## Logical grid + visuals: rows keyed by world row id, discrete player cell, road collisions.

signal scored(new_total: int)
signal died
signal state_changed
signal chest_collected(coins: int)
signal shield_absorbed

const GRID_COLS := 5
const AHEAD_BUFFER := 10
const BEHIND_KEEP := 5

@export var cell_size: int = 72

var _spawner: RowSpawner
var _grass: RowDefinition
var _game_config: GameConfig
var _buff_manager: BuffManager

var player_fill_color: Color = Color(0.95, 0.84, 0.2, 1.0)
var player_outline_color: Color = Color(0.22, 0.16, 0.04, 1.0)

## int -> RowRuntime
var _rows: Dictionary = {}

var _rows_since_last_chest: int = 0
var _active_chests: int = 0

var _player_lane: int = 2
var _player_row_id: int = 0
var _score: int = 0

## Wall-clock seconds for car motion (stable across pause if we pause later).
var _time_sec: float = 0.0
var _dead: bool = false

var _rng := RandomNumberGenerator.new()


class RowRuntime:
	var def: RowDefinition
	## Lane index -> horizontal offset (pixels) for car center modulo period.
	var lane_offset: Dictionary = {}
	## Lane index with a collectible chest, or -1.
	var chest_lane: int = -1


func setup(
		spawner: RowSpawner,
		grass_row: RowDefinition,
		game_config: GameConfig = null,
		buff_manager: BuffManager = null,
) -> void:
	_spawner = spawner
	_grass = grass_row
	_game_config = game_config
	_buff_manager = buff_manager
	if _buff_manager:
		if not _buff_manager.shield_changed.is_connected(_on_buff_shield_changed):
			_buff_manager.shield_changed.connect(_on_buff_shield_changed)


func _on_buff_shield_changed(_active: bool) -> void:
	queue_redraw()


func reset_run() -> void:
	if _buff_manager:
		_buff_manager.reset_for_new_run()
	_rows.clear()
	_dead = false
	set_process(true)
	_player_lane = 2
	_player_row_id = 3
	_score = 0
	_time_sec = 0.0
	_rows_since_last_chest = 0
	_active_chests = 0
	_rng.randomize()
	_spawner.reset()

	var max_r := _player_row_id + AHEAD_BUFFER
	for r in max_r + 1:
		var def: RowDefinition
		if r < 3:
			def = _grass
			_rows[r] = _make_runtime(def, false)
		else:
			def = _spawner.pick_next()
			_rows[r] = _make_runtime(def, true)

	_ensure_rows_ahead()
	state_changed.emit()


func get_score() -> int:
	return _score


func get_player_cell() -> Vector2i:
	return Vector2i(_player_lane, _player_row_id)


func is_road_cell(row_id: int) -> bool:
	var rt: RowRuntime = _rows.get(row_id)
	if rt == null:
		return false
	return rt.def.kind == RowDefinition.Kind.ROAD


func try_move(dir: Vector2i) -> bool:
	# FORWARD: +row id (north / up-screen in our layout)
	var nl := _player_lane + dir.x
	var nr := _player_row_id + dir.y
	if nl < 0 or nl >= GRID_COLS:
		return false
	if nr < 0:
		return false
	_ensure_row_exists(nr)
	if not _rows.has(nr):
		return false

	_player_lane = nl
	_player_row_id = nr

	if dir.y > 0:
		_score += 1
		scored.emit(_score)

	_ensure_rows_ahead()
	_prune_behind()
	_try_collect_chest()

	if _is_car_collision():
		_handle_fatal_collision()
		state_changed.emit()
		queue_redraw()
		return true

	state_changed.emit()
	return true


func _process(delta: float) -> void:
	if _rows.is_empty() or _dead:
		return
	_time_sec += delta
	# Moving cars: refresh death if standing on road (stay off timing windows).
	if is_road_cell(_player_row_id) and _is_car_collision():
		_handle_fatal_collision()
	queue_redraw()


func _handle_fatal_collision() -> void:
	if _dead:
		return
	if _buff_manager and _buff_manager.try_absorb_fatal_hit():
		shield_absorbed.emit()
		return
	_emit_death_once()


func _emit_death_once() -> void:
	if _dead:
		return
	_dead = true
	set_process(false)
	died.emit()


func _draw() -> void:
	if _rows.is_empty():
		return
	var W := float(GRID_COLS * cell_size)
	var cs := float(cell_size)
	var lo := _player_row_id - BEHIND_KEEP
	var hi := _player_row_id + AHEAD_BUFFER
	for row_id in range(lo, hi + 1):
		if not _rows.has(row_id):
			continue
		var rt: RowRuntime = _rows[row_id]
		var def: RowDefinition = rt.def
		var y: float = float(_player_row_id - row_id) * cs
		var bg: Color = def.safe_color if def.kind == RowDefinition.Kind.SAFE else def.road_color
		draw_rect(Rect2(0.0, y, W, cs), bg)
		draw_rect(Rect2(0.0, y, W, cs), Color(0, 0, 0, 0.12), false, 2.0)

		if def.kind == RowDefinition.Kind.ROAD:
			_draw_cars(rt, y, cs, W)
		elif rt.chest_lane >= 0:
			_draw_chest(rt.chest_lane, y, cs)

	_draw_player(cs)


func _draw_chest(lane: int, y: float, cs: float) -> void:
	var cx := (float(lane) + 0.5) * cs
	var cy := y + cs * 0.5
	var w := cs * 0.38
	var h := cs * 0.3
	var rect := Rect2(cx - w * 0.5, cy - h * 0.5, w, h)
	draw_rect(rect, Color(0.72, 0.52, 0.2, 1))
	draw_rect(rect, Color(0.35, 0.24, 0.08, 1), false, 2.0)
	draw_arc(Vector2(cx, cy - h * 0.15), w * 0.22, PI * 0.15, PI * 0.85, 12, Color(0.9, 0.75, 0.25, 1), 2.5, true)


func _draw_player(cs: float) -> void:
	var px := (float(_player_lane) + 0.5) * cs
	var py := cs * 0.5
	var r := cs * 0.26
	draw_circle(Vector2(px, py), r, player_fill_color)
	draw_arc(Vector2(px, py), r, 0.0, TAU, 32, player_outline_color, 3.0, true)
	if _buff_manager and _buff_manager.has_active_shield():
		draw_arc(Vector2(px, py), r + cs * 0.06, 0.0, TAU, 48, Color(0.35, 0.75, 1.0, 0.95), 4.0, true)


func _draw_cars(rt: RowRuntime, y: float, cs: float, W: float) -> void:
	var def: RowDefinition = rt.def
	var smult := 1.0
	var wratio := 0.72
	var tint := def.car_color
	if def.obstacle:
		smult = def.obstacle.speed_multiplier
		wratio = def.obstacle.width_ratio
		tint = def.obstacle.tint
	var car_w := wratio * cs
	var speed := def.car_speed * smult * float(def.car_direction)
	for lane in GRID_COLS:
		if (def.car_lane_bits & (1 << lane)) == 0:
			continue
		var off: float = rt.lane_offset.get(lane, 0.0)
		var cx := fposmod(off + _time_sec * speed, W)
		var cy := y + cs * 0.5
		var rect := Rect2(cx - car_w * 0.5, cy - cs * 0.32, car_w, cs * 0.64)
		draw_rect(rect, tint)
		draw_rect(rect, Color(0, 0, 0, 0.35), false, 2.0)


func _make_runtime(def: RowDefinition, roll_chest: bool) -> RowRuntime:
	var rt := RowRuntime.new()
	rt.def = def
	if def.kind == RowDefinition.Kind.ROAD:
		for lane in GRID_COLS:
			if (def.car_lane_bits & (1 << lane)) != 0:
				rt.lane_offset[lane] = _rng.randf() * float(GRID_COLS * cell_size)
	if roll_chest:
		_try_attach_chest(rt)
	return rt


func _try_attach_chest(rt: RowRuntime) -> void:
	if _game_config == null or _game_config.chest_rule == null:
		return
	var rule: ChestSpawnRule = _game_config.chest_rule
	_rows_since_last_chest += 1
	if _rows_since_last_chest < rule.spawn_every_n_rows:
		return
	if _score < rule.min_score_for_chest:
		return
	if _active_chests >= rule.max_active_chests_per_run:
		return
	if rt.def.kind != RowDefinition.Kind.SAFE:
		return
	rt.chest_lane = _rng.randi_range(0, GRID_COLS - 1)
	_rows_since_last_chest = 0
	_active_chests += 1


func _try_collect_chest() -> void:
	var rt: RowRuntime = _rows.get(_player_row_id)
	if rt == null or rt.chest_lane < 0:
		return
	if rt.chest_lane != _player_lane:
		return
	var coins := 0
	if _game_config and _game_config.chest_rule:
		coins = _game_config.chest_rule.coins_per_chest
	rt.chest_lane = -1
	_active_chests = maxi(0, _active_chests - 1)
	chest_collected.emit(coins)


func _ensure_row_exists(row_id: int) -> void:
	if _rows.has(row_id):
		return
	while not _rows.has(row_id):
		var next_id := _max_row_id() + 1
		_rows[next_id] = _make_runtime(_spawner.pick_next(), true)


func _max_row_id() -> int:
	var m := -1
	for k in _rows.keys():
		if int(k) > m:
			m = int(k)
	return m


func _ensure_rows_ahead() -> void:
	var target := _player_row_id + AHEAD_BUFFER
	while _max_row_id() < target:
		var nid := _max_row_id() + 1
		_rows[nid] = _make_runtime(_spawner.pick_next(), true)


func _prune_behind() -> void:
	var cutoff := _player_row_id - BEHIND_KEEP - 1
	var to_erase: Array[int] = []
	for k in _rows.keys():
		if int(k) < cutoff:
			to_erase.append(int(k))
	for e in to_erase:
		_rows.erase(e)


func _is_car_collision() -> bool:
	var rt: RowRuntime = _rows.get(_player_row_id)
	if rt == null:
		return false
	var def: RowDefinition = rt.def
	if def.kind != RowDefinition.Kind.ROAD:
		return false
	if (def.car_lane_bits & (1 << _player_lane)) == 0:
		return false

	var W := float(GRID_COLS * cell_size)
	var smult := 1.0
	var wratio := 0.72
	if def.obstacle:
		smult = def.obstacle.speed_multiplier
		wratio = def.obstacle.width_ratio
	var car_half := wratio * float(cell_size) * 0.5
	var player_half := float(cell_size) * 0.28
	var speed := def.car_speed * smult * float(def.car_direction)
	var off: float = rt.lane_offset.get(_player_lane, 0.0)
	var cx := fposmod(off + _time_sec * speed, W)
	var px := (float(_player_lane) + 0.5) * float(cell_size)
	return _axis_dist_wrapped(cx, px, W) < (car_half + player_half)


func _axis_dist_wrapped(a: float, b: float, period: float) -> float:
	var d := absf(a - b)
	return minf(d, period - d)
