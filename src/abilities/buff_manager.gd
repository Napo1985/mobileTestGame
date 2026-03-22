class_name BuffManager
extends RefCounted
## Tracks combat shields and timed buff stacks. GridWorld consults this before fatal hits.


signal shield_changed(active: bool)


var _shield_hits_remaining: int = 0


func get_shield_hits_remaining() -> int:
	return _shield_hits_remaining


func has_active_shield() -> bool:
	return _shield_hits_remaining > 0


func grant_shield_hits(amount: int) -> void:
	if amount <= 0:
		return
	var was := has_active_shield()
	_shield_hits_remaining += amount
	if not was and has_active_shield():
		shield_changed.emit(true)


func reset_for_new_run() -> void:
	if has_active_shield():
		shield_changed.emit(false)
	_shield_hits_remaining = 0


## Returns true if a fatal hit was absorbed (caller should not apply death).
func try_absorb_fatal_hit() -> bool:
	if _shield_hits_remaining <= 0:
		return false
	_shield_hits_remaining -= 1
	if not has_active_shield():
		shield_changed.emit(false)
	return true
