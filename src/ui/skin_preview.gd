extends Control
## Simple two-circle preview matching `GridWorld` player drawing style.


var fill_color: Color = Color.WHITE
var outline_color: Color = Color.BLACK


func set_skin_colors(fill: Color, outline: Color) -> void:
	fill_color = fill
	outline_color = outline
	queue_redraw()


func _draw() -> void:
	var c := size * 0.5
	var r := minf(size.x, size.y) * 0.35
	draw_circle(c, r, fill_color)
	draw_arc(c, r, 0.0, TAU, 32, outline_color, 3.0, true)
