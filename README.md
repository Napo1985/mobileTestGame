# mobileTestGame (Godot 4)

Endless grid runner prototype: **Godot 4.3**, 2D gameplay, data-driven rows, local profile + skins, optional **Google Play Games** on Android.

---

## Requirements

| Tool | Notes |
| ---- | ----- |
| **Godot 4.3** (or compatible 4.x) | Matches `config/features` in `project.godot`. |
| **Android SDK / platform tools** | For exporting and `adb`. Install via Android Studio or `sdkmanager`. |
| **JDK 17+** | Required for Gradle Android builds (Godot Android export). |

Optional:

- **Android Studio** — SDK setup, emulators, and building the Play Games plugin AAR (`android/plugins/`).
- **Physical device** with USB debugging for on-device testing.

---

## Open and run (desktop)

1. Install [Godot 4.3](https://godotengine.org/download) (or matching 4.x).
2. **Import** this folder as a project (`project.godot`).
3. Press **F5** (or **Project → Run**). The bootstrap scene runs `scenes/main.tscn`, which opens the main menu.

---

## Android emulator

1. Create an AVD in **Android Studio** (API level compatible with your `minSdk`; the Play Games plugin module uses **minSdk 24** in `android/plugins/PlayGamesBridge/build.gradle.kts`).
2. Start the emulator; verify **`adb devices`** lists it.
3. In Godot: **Project → Export → Android**, select the **Android** preset, **Export Project** (APK) or use **Remote Debug** / one-click deploy if configured.
4. Install: `adb install -r path/to/output.apk`.

**Tip:** Google Play Games and leaderboards need Play services; use an image **with Google Play** (not “AOSP” only) for realistic tests.

---

## Physical device (USB)

1. Enable **Developer options** → **USB debugging** on the device.
2. Connect USB; accept the debugging prompt on the device.
3. `adb devices` should show the device as `device`.
4. Export/install the APK/AAB build (debug or release) matching your signing setup.

---

## Export AAB (Play Console)

1. **Project → Export → Android**.
2. Use **Gradle build** (enabled in `export_presets.cfg` for this project).
3. Set **package name**, **version code/name**, and **signing** (release keystore for store uploads).
4. Choose **AAB** export format when using the preset targeting `export/android/mobileTestGame.aab`.
5. Upload the **AAB** to **Internal testing** (or another track) in Play Console.

See [Godot — Exporting for Android](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html) for details.

---

## Google Play Games

Build the plugin AAR, enable the editor addon, and configure Play Console — see **`docs/PLAY_GAMES.md`**.

---

## Documentation in this repo

| Doc | Content |
| --- | ------- |
| `docs/CONFIG.md` | `GameConfig`, chest rules, row resources. |
| `docs/SKINS.md` | Skin catalog JSON, profile, preview UI. |
| `docs/PLAY_GAMES.md` | Play Games plugin build, export checklist, testing. |

---

## Troubleshooting

| Symptom | Things to check |
| ------- | ----------------- |
| **Export fails: SDK / build-tools / licenses** | Open Android Studio → SDK Manager; install **Platform**, **Build-Tools**, **CMake/NDK** as prompted. Run `sdkmanager --licenses` and accept. Point Godot Editor Settings → Export → Android to the correct **SDK** and **JDK** paths. |
| **Gradle / JDK errors** | Use **JDK 17+**. In Godot **Editor Settings → Export → Android**, set **Gradle / JDK** explicitly if the default is wrong. |
| **APK installs but game crashes on launch** | Check `adb logcat` for missing `.so`, wrong **ABI**, or plugin AAR issues. Ensure **minSdk** ≤ device API. |
| **Play Games sign-in / leaderboards fail** | Release vs **debug keystore** SHA-1 must match the OAuth / Play Console app entry. Use **signed** builds. See `docs/PLAY_GAMES.md`. |
| **PlayGamesBridge singleton missing on device** | Build and copy **AARs** into `addons/play_games_bridge/bin/`, keep the **PlayGamesBridge** editor plugin enabled, re-export. |
| **Wrong or black screen / orientation** | **Project Settings → Display** (this project uses portrait-friendly defaults). |

---

## License

Project structure and code are provided as-is for development and testing.
