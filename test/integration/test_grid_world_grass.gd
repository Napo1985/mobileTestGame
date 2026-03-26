extends GutTest


func test_grass_only_forward_moves_keep_score_at_zero() -> void:
	var grass: RowDefinition = load("res://data/rows/row_grass.tres")
	var meadow: RowDefinition = load("res://data/rows/row_grass_gap.tres")
	var sp := RowSpawner.new()
	sp.configure([grass, meadow])
	var grid := GridWorld.new()
	grid.setup(sp, grass, null, BuffManager.new())
	grid.reset_run()
	assert_eq(grid.get_score(), 0)
	var steps := 12
	for i in steps:
		assert_true(grid.try_move(Vector2i(0, 1)), "move %d failed" % i)
	assert_eq(grid.get_score(), 0)


func test_road_highest_level_scoring_is_record_high_only() -> void:
	var grass: RowDefinition = load("res://data/rows/row_grass.tres")
	var gap: RowDefinition = load("res://data/rows/row_grass_gap.tres")
	var road: RowDefinition = load("res://data/rows/row_road_fast.tres")

	var sp := RowSpawner.new()
	# Pattern after `reset_run()`'s initial safe floors (rows < 3):
	# row3 = road, row4 = gap(safe), row5 = road, row6 = gap(safe), ...
	sp.configure([road, gap])

	var grid := GridWorld.new()
	grid.setup(sp, grass, null, BuffManager.new())
	grid.reset_run()

	assert_eq(grid.get_score(), 0)

	# Move on safe floors (rows 1..2): score should stay at 0.
	for i in range(2):
		assert_true(grid.try_move(Vector2i(0, 1)), "move %d failed" % i)
	assert_eq(grid.get_score(), 0)

	# Step into row3 (first ROAD): road_level = 1 => score = 1
	assert_true(grid.try_move(Vector2i(0, 1)))
	assert_eq(grid.get_score(), 1)

	# Step into row4 (SAFE gap): no new record-high ROAD level.
	assert_true(grid.try_move(Vector2i(0, 1)))
	assert_eq(grid.get_score(), 1)

	# Step into row5 (ROAD): road_level = 3 => +2 (delta) => score = 3
	assert_true(grid.try_move(Vector2i(0, 1)))
	assert_eq(grid.get_score(), 3)

	# Step into row6 (SAFE gap): no change
	assert_true(grid.try_move(Vector2i(0, 1)))
	assert_eq(grid.get_score(), 3)

	# Step into row7 (ROAD): road_level = 5 => +2 => score = 5
	assert_true(grid.try_move(Vector2i(0, 1)))
	assert_eq(grid.get_score(), 5)

	# Move back up within the already-reached maximum: score must not decrease.
	assert_true(grid.try_move(Vector2i(0, -1))) # row6 (SAFE)
	assert_eq(grid.get_score(), 5)
	assert_true(grid.try_move(Vector2i(0, -1))) # row5 (ROAD)
	assert_eq(grid.get_score(), 5)

	# Re-step onto the current maximum record-high ROAD level: score must not change.
	assert_true(grid.try_move(Vector2i(0, 1))) # row6 (SAFE)
	assert_eq(grid.get_score(), 5)
	assert_true(grid.try_move(Vector2i(0, 1))) # row7 (ROAD)
	assert_eq(grid.get_score(), 5)
