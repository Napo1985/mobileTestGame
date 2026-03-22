extends Node
class_name PlayerActor
## Tap/swipe and keyboard moves; forwards to RunController.

var _run: RunController
var _press_pos: Vector2
var _has_press: bool = false


func setup(run: RunController) -> void:
	_run = run


## Called from RunRoot._input so events are not swallowed by the full-screen Control.
func forward_input(event: InputEvent) -> void:
	if _run == null or _run.is_game_over():
		return
	if event is InputEventScreenTouch:
		if event.pressed:
			_has_press = true
			_press_pos = event.position
		else:
			if _has_press:
				_finish_gesture(event.position)
			_has_press = false
	elif event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed:
				_has_press = true
				_press_pos = event.position
			else:
				if _has_press:
					_finish_gesture(event.position)
				_has_press = false
	elif event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_UP, KEY_W:
				_run.request_move(Vector2i(0, 1))
			KEY_DOWN, KEY_S:
				_run.request_move(Vector2i(0, -1))
			KEY_LEFT, KEY_A:
				_run.request_move(Vector2i(-1, 0))
			KEY_RIGHT, KEY_D:
				_run.request_move(Vector2i(1, 0))


func _finish_gesture(end_pos: Vector2) -> void:
	var d := end_pos - _press_pos
	var t := 36.0
	if d.length() < t:
		_run.request_move(Vector2i(0, 1))
		return
	if absf(d.x) >= absf(d.y):
		if d.x > 0.0:
			_run.request_move(Vector2i(1, 0))
		else:
			_run.request_move(Vector2i(-1, 0))
	else:
		if d.y > 0.0:
			_run.request_move(Vector2i(0, -1))
		else:
			_run.request_move(Vector2i(0, 1))
