# Google Play Games (Android)

This project integrates **Play Games Services** through a small **Godot Android plugin v2** (`PlayGamesBridge`) and the **`PlayGamesService`** autoload. The Kotlin sources live under `android/plugins/PlayGamesBridge/`; the editor addon under `addons/play_games_bridge/` packages the built **AAR** into Gradle exports.

---

## What is implemented

| Area | Behavior |
| ---- | -------- |
| **GDScript** | `PlayGamesService` calls into singleton `PlayGamesBridge` on Android (`Engine.get_singleton("PlayGamesBridge")`). Desktop/editor: no-op with warnings if the singleton is missing. |
| **Kotlin plugin** | Stub methods: `sign_in`, `sign_out`, `submit_leaderboard_score`, `save_snapshot`, `load_snapshot`. They log and emit success signals so you can wire UI and flows before real OAuth / Games APIs are connected. |
| **Leaderboards** | On run over, `RunController` calls `PlayGamesService.on_run_ended_with_score(score)` which submits when `leaderboard_high_score_id` is set in `config/play_games_settings.tres`. |
| **Saved Games** | Optional profile JSON sync: `ProfileService` pushes local JSON after each save when Play Games is enabled; optional pull after sign-in when `sync_profile_from_cloud_on_sign_in` is true (see `PlayGamesConfig`). |

Replace the Kotlin stubs with **Play Games Services v2** calls (sign-in, leaderboards, snapshots) following Google’s current Android documentation.

---

## Configuration (`PlayGamesConfig`)

Resource: **`res://config/play_games_settings.tres`**

| Property | Purpose |
| -------- | ------- |
| `enabled` | Master switch for cloud calls from GDScript. |
| `leaderboard_high_score_id` | Play Console leaderboard API identifier (string). |
| `snapshot_name` | Saved Games snapshot id for the profile blob. |
| `sync_profile_from_cloud_on_sign_in` | If true, requests `load_snapshot` after a successful `sign_in_result`. |

---

## Build the plugin AAR

1. Install **Android Studio** and a recent **JDK** (17+).
2. Open the Gradle project at **`android/plugins/`** (the `settings.gradle.kts` includes `:PlayGamesBridge`).
3. Run **`PlayGamesBridge` → `assembleDebug`** / **`assembleRelease`** (or `./gradlew :PlayGamesBridge:assembleRelease` from that folder).
4. Copy the outputs into **`addons/play_games_bridge/bin/`**:
   - `PlayGamesBridge-debug.aar`
   - `PlayGamesBridge-release.aar`  
   (See `addons/play_games_bridge/bin/PLACE_AAR_HERE.txt`.)

The **Play Games bridge editor plugin** is enabled in `project.godot` so Android exports pick up these AARs when present.

---

## Godot editor checklist

1. **Project → Project Settings → Plugins**: `PlayGamesBridge` should be enabled (already listed in `[editor_plugins]`).
2. **Project → Export → Android**: use a **Gradle** build (`gradle_build/use_gradle_build` is enabled in this repo’s export preset).
3. Export a **debug APK** or **release AAB** and install on a device or emulator with Google Play services.

---

## Play Console checklist (high level)

1. Create the game in [Google Play Console](https://play.google.com/console) and link the **Play Games Services** configuration.
2. **App signing**: use the same keystore you expect for release; SHA-1 / SHA-256 for **OAuth** and **Play Games** must match the signing key used on the build you install (debug vs release keystore differ).
3. **OAuth client** (Google Cloud Console): Android client with package name `com.example.mobiletestgame` (or your final `applicationId`) and SHA-1 of the signing cert.
4. **Linked apps**: link the Android app entry to your OAuth client.
5. Create a **leaderboard** and paste its **API identifier** into `leaderboard_high_score_id` in `play_games_settings.tres`.
6. Enable **Saved Games** in Play Games configuration if you use snapshots.

---

## Testing notes

- **Unsigned / mismatched SHA**: sign-in and leaderboards often fail silently or with API errors; always test **signed** builds that match Play Console credentials.
- **Stub plugin**: until you replace Kotlin bodies, `sign_in` / leaderboard / snapshot calls still emit **stub** success signals — verify real behavior only after wiring Play Services.
- **Snapshot size**: Saved Games blobs are limited (on the order of a few MB); keep profile JSON small.
- **Conflicts**: MVP uses **last-write-wins** when applying cloud JSON to `ProfileService`; add versioning/timestamps if you need merge UI later.

---

## Related files

| File | Role |
| ---- | ---- |
| `src/autoload/play_games_service.gd` | Singleton API, signals, Android wiring. |
| `src/content/resources/play_games_config.gd` | `PlayGamesConfig` script. |
| `config/play_games_settings.tres` | Exported IDs and flags. |
| `android/plugins/PlayGamesBridge/.../PlayGamesBridgePlugin.kt` | JNI-facing plugin implementation. |
| `addons/play_games_bridge/play_games_bridge_editor_plugin.gd` | Editor export hook for the AAR. |
