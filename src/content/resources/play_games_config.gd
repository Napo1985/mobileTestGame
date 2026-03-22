class_name PlayGamesConfig
extends Resource
## Play Games IDs and feature flags. Edit `res://config/play_games_settings.tres` (or override path).


const DEFAULT_PATH := "res://config/play_games_settings.tres"

@export var enabled: bool = true
## Play Console → Leaderboards → API identifier for the high score board.
@export var leaderboard_high_score_id: String = ""
## Saved Games snapshot name for the profile JSON blob (optional).
@export var snapshot_name: String = "player_profile_v1"
## If true, attempts cloud load after sign-in (last-write-wins vs local).
@export var sync_profile_from_cloud_on_sign_in: bool = false
