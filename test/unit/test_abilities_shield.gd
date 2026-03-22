extends GutTest


func test_registry_shield_activate_grants_shield_hit() -> void:
	var reg := AbilityRegistry.new()
	reg.register(ShieldAbility.new())
	var bm := BuffManager.new()
	var rng := RandomNumberGenerator.new()
	var ctx := AbilityContext.new(null, null, bm, rng)
	var ab: Ability = reg.get_ability(&"shield")
	assert_not_null(ab)
	ab.activate(ctx)
	assert_eq(bm.get_shield_hits_remaining(), 1)
