extends GutTest


func test_round_robin_order_and_reset() -> void:
	var a: RowDefinition = load("res://data/rows/row_grass.tres")
	var b: RowDefinition = load("res://data/rows/row_grass_gap.tres")
	var c: RowDefinition = load("res://data/rows/row_road_slow.tres")
	var sp := RowSpawner.new()
	sp.configure([a, b, c])
	assert_eq(sp.pick_next(), a)
	assert_eq(sp.pick_next(), b)
	assert_eq(sp.pick_next(), c)
	assert_eq(sp.pick_next(), a)
	sp.reset()
	assert_eq(sp.pick_next(), a)
