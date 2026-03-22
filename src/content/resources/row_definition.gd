class_name RowDefinition
extends Resource
## Data-driven row: safe grass or road with per-lane moving obstacles.

enum Kind { SAFE, ROAD }

@export var row_name: String = "unnamed"
@export var kind: Kind = Kind.SAFE
@export var safe_color: Color = Color(0.38, 0.64, 0.34)
@export var road_color: Color = Color(0.2, 0.2, 0.23)
@export var car_color: Color = Color(0.88, 0.22, 0.16)
## Bitmask: bit i = lane i has a car (lanes 0 .. GRID_COLS-1).
@export var car_lane_bits: int = 0
@export var car_speed: float = 140.0
@export var car_direction: int = 1
@export var obstacle: ObstacleVariant
