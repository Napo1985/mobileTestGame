# Testing (GUT)

This project uses **[GUT (Godot Unit Test)](https://github.com/bitwes/Gut)** under `addons/gut/`. Tests live in `test/unit/` and `test/integration/`; shared doubles are in `test/support/`.

## Editor

1. Enable **Project → Project Settings → Plugins** and ensure **Gut** is enabled (already listed in `project.godot` with `play_games_bridge`).
2. Open the **GUT** panel (bottom dock) and run **Run All** or pick directories/scripts.

The `.gutconfig.json` at the project root sets `dirs` to `res://test/unit` and `res://test/integration`, `include_subdirs` to `true`, and `should_exit` so CLI runs terminate after the suite.

## Headless / CI

From the project root (Godot 4.x on `PATH`):

```bash
godot --path . --headless -s res://addons/gut/gut_cmdln.gd
```

Override options when needed (CLI wins over `.gutconfig.json`), for example:

```bash
godot --path . --headless -s res://addons/gut/gut_cmdln.gd -- -gexit -gdir=res://test/unit
```

Use `-gh` for CLI help. Exit code is non-zero if tests fail (when using `-gexit` / `should_exit` so the process quits after the run).

## GitHub Actions

See `.github/workflows/godot-gut.yml` for an optional job that downloads Godot **4.3-stable** on Ubuntu and runs the same headless command. Adjust the version or install step to match your team’s Godot build.

## ProfileService and Play Games

`ProfileService` mixes **disk I/O** (`user://profile.json`) and **PlayGamesService** notifications inside save paths. Automated tests do not cover it end-to-end; validating serialization, merge rules, or cloud hooks would need **dependency injection**, a **save path override**, or **extracted** `to_dictionary` / `from_dictionary` helpers (see plan notes). Until then, treat profile and cloud behavior as **manual** or follow-up refactors.

## What is covered

- **Unit:** `BuffManager`, `RowSpawner`, `SkinCatalog` (with `test/support/mock_profile.gd`), `AbilityRegistry` + `ShieldAbility`.
- **Integration:** grass-only `GridWorld` forward moves / score; smoke check that `run_root.tscn` contains `RunController` and `GridWorld`.

Road collisions and RNG-heavy paths stay out of CI unless you add a test-only RNG API (see project planning docs).
