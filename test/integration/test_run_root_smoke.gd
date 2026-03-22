extends GutTest


func test_run_root_has_run_controller_and_grid_world() -> void:
	var packed: PackedScene = load("res://scenes/gameplay/run_root.tscn")
	assert_not_null(packed)
	var root := packed.instantiate()
	autofree(root)
	var rc: Node = root.get_node_or_null("RunController")
	assert_not_null(rc)
	var gw: Node = rc.get_node_or_null("GridWorld")
	assert_not_null(gw)
	assert_true(gw is GridWorld)
