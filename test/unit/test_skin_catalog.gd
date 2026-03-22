extends GutTest

const MockProfile := preload("res://test/support/mock_profile.gd")


func test_load_default_contains_expected_ids_and_colors() -> void:
	var cat := SkinCatalog.load_default()
	var ids: Array[String] = []
	for s in cat.get_all():
		ids.append(str(s.get("id", "")))
	assert_has(ids, "default")
	assert_has(ids, "frog_green")
	assert_has(ids, "ember")
	var frog := cat.get_skin("frog_green")
	assert_false(frog.is_empty())
	assert_eq(cat.parse_fill_color(frog), Color("#44cc66"))
	assert_eq(cat.parse_outline_color(frog), Color("#1a5c2e"))


func test_try_unlock_and_equip_with_mock_profile() -> void:
	var cat := SkinCatalog.load_default()
	var profile := MockProfile.new()
	profile.coins = 50
	assert_true(cat.try_unlock_and_equip("frog_green", profile))
	assert_eq(profile.coins, 0)
	assert_true(profile.is_skin_unlocked("frog_green"))
	assert_eq(profile.equipped_skin, "frog_green")
