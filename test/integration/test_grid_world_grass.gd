extends GutTest


func test_grass_only_forward_moves_match_score() -> void:
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
	assert_eq(grid.get_score(), steps)
