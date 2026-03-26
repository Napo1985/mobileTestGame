# mobileTestGame (Unity)

Unity **2022.3 LTS** project with a minimal **2D lane runner**: tap the **left** or **right** half of the screen (or click in the editor) to switch lanes while the player moves forward.

## Open the project

1. Install [Unity Hub](https://unity.com/download) and editor **2022.3.48f1** (or any **2022.3.x** — allow the upgrade dialog if your patch version differs).
2. **Add** → folder `Unity` (this repo’s `Unity` directory, which contains `Assets` and `ProjectSettings`).
3. Open the project, then open scene **`Assets/Scenes/Main.unity`** and press **Play**.

## Android

1. Install **Android Build Support** (SDK & NDK tools) for your Unity version via Hub.
2. **File → Build Settings → Android → Switch Platform**, then configure package name in **Player Settings** if needed (`com.example.mobiletestgame`).

## Controls

- **Touch** (device): tap left/right side of the screen to move one lane (three lanes).
- **Mouse** (editor): click left/right half of the Game view.

Core gameplay code lives in `Unity/Assets/Scripts/GameBootstrap.cs`.

## Documentation

- Curated links: [`docs/unity-links.md`](docs/unity-links.md)
