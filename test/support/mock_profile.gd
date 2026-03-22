extends Node
## Minimal profile double for SkinCatalog unit tests (no autoloads).


var coins: int = 0
var equipped_skin: String = ""
var unlocked_skins: Dictionary = {}


func get_coins() -> int:
	return coins


func is_skin_unlocked(skin_id: String) -> bool:
	return unlocked_skins.get(skin_id, false)


func spend_coins(amount: int) -> bool:
	if coins < amount:
		return false
	coins -= amount
	return true


func unlock_skin(skin_id: String) -> void:
	unlocked_skins[skin_id] = true


func set_equipped_skin(skin_id: String) -> void:
	equipped_skin = skin_id
