extends GutTest


func test_grant_shield_hits_noop_for_non_positive() -> void:
	var bm := BuffManager.new()
	var emissions: Array = []
	bm.shield_changed.connect(func(active: bool): emissions.append(active))
	bm.grant_shield_hits(0)
	bm.grant_shield_hits(-3)
	assert_eq(emissions.size(), 0)


func test_first_grant_emits_shield_changed_true_once() -> void:
	var bm := BuffManager.new()
	var emissions: Array = []
	bm.shield_changed.connect(func(active: bool): emissions.append(active))
	bm.grant_shield_hits(1)
	assert_eq(emissions, [true])
	bm.grant_shield_hits(2)
	assert_eq(emissions, [true])


func test_try_absorb_fatal_hit_returns_false_when_empty() -> void:
	var bm := BuffManager.new()
	assert_false(bm.try_absorb_fatal_hit())


func test_try_absorb_decrements_and_emits_false_when_last_consumed() -> void:
	var bm := BuffManager.new()
	var emissions: Array = []
	bm.shield_changed.connect(func(active: bool): emissions.append(active))
	bm.grant_shield_hits(1)
	emissions.clear()
	assert_true(bm.try_absorb_fatal_hit())
	assert_eq(bm.get_shield_hits_remaining(), 0)
	assert_eq(emissions, [false])


func test_reset_for_new_run_clears_and_emits_false_if_shield_was_active() -> void:
	var bm := BuffManager.new()
	var emissions: Array = []
	bm.shield_changed.connect(func(active: bool): emissions.append(active))
	bm.grant_shield_hits(1)
	emissions.clear()
	bm.reset_for_new_run()
	assert_eq(emissions, [false])
	assert_eq(bm.get_shield_hits_remaining(), 0)


func test_reset_for_new_run_no_signal_when_already_inactive() -> void:
	var bm := BuffManager.new()
	var emissions: Array = []
	bm.shield_changed.connect(func(active: bool): emissions.append(active))
	bm.reset_for_new_run()
	assert_eq(emissions.size(), 0)
