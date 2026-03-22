extends Control

@onready var _controller: RunController = $RunController
@onready var _player_input: PlayerActor = $RunController/PlayerActor
@onready var _score: Label = $CanvasLayer/HUD/VBox/ScoreLabel
@onready var _coins: Label = $CanvasLayer/HUD/VBox/CoinsLabel
@onready var _game_over: Control = $CanvasLayer/GameOver
@onready var _restart: Button = $CanvasLayer/GameOver/Center/Panel/VBox/RestartButton
@onready var _back: Button = $CanvasLayer/HUD/VBox/BackButton


func _ready() -> void:
	_controller.score_changed.connect(_on_score_changed)
	_controller.coins_changed.connect(_on_coins_changed)
	_controller.game_over_changed.connect(_on_game_over_changed)
	_restart.pressed.connect(_on_restart_pressed)
	_back.pressed.connect(_on_back_pressed)
	_game_over.visible = false
	_on_score_changed(_controller.grid.get_score())
	_on_coins_changed(ProfileService.get_coins())


func _on_score_changed(value: int) -> void:
	_score.text = "Score: %d" % value


func _on_coins_changed(total: int) -> void:
	_coins.text = "Coins: %d" % total


func _on_game_over_changed(active: bool) -> void:
	_game_over.visible = active


func _on_restart_pressed() -> void:
	_controller.restart_run()


func _on_back_pressed() -> void:
	Game.go_to_main_menu()


func _input(event: InputEvent) -> void:
	_player_input.forward_input(event)
