extends Node
## Local profile / persistence (stub for Phase 0+).

var _coins: int = 0


func get_coins() -> int:
	return _coins


func add_coins(amount: int) -> void:
	if amount <= 0:
		return
	_coins += amount
