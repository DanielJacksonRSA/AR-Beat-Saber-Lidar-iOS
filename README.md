# Spatial Pulse: AR Rhythm Game (LiDAR)

A Beat Saber-style rhythm game that runs in **augmented reality on an iPhone**. The game scans your room with the LiDAR sensor, finds a flat wall, and opens a grid of portals on it. Cubes fly out of the portals in time with a song you pick from your own phone, and you slice them by swinging the phone through them.

<p align="center">
  <img src="docs/media/03-grid-placed.jpg" width="23%" alt="Portal grid placed on a real wall">
  <img src="docs/media/04-gameplay.jpg" width="23%" alt="Cubes flying out of the portals">
  <img src="docs/media/05-bombs.jpg" width="23%" alt="Cubes and bombs">
  <img src="docs/media/02-scanning.jpg" width="23%" alt="Scanning for a wall, with a hologram preview">
</p>

**Demo video (with sound):** [`docs/media/demo.mp4`](docs/media/demo.mp4). It shows the song picker, wall scanning, placement and a full run, recorded on device.

| | |
|---|---|
| Engine | Unity 6000.4.2f1, URP 17.4 |
| AR | AR Foundation 6.4.2, ARKit XR Plugin 6.4.2, LiDAR scene meshing |
| Platform | iOS 16+ on a LiDAR iPhone (Pro models) |
| Language | C# (about 950 lines in `Assets/Scripts`) |
| Input | Device motion (the phone is the saber), touch to start and place |

## How it plays

1. **Pick a song.** Tap to open the iOS Files picker and choose an mp3 or wav from your phone. No music ships with the game.
2. **Scan.** Look around. A hologram of the portal grid appears on any wall that is flat enough.
3. **Place.** Tap to lock the grid in place. The portals turn solid and the song starts.
4. **Slice.** Cubes spawn from the portals on the beat and travel towards you. Swing the phone through them. Avoid the bombs.
5. **Finish.** When the song ends or your energy runs out you get a score, the maximum possible score and a rating from E to S.

Scoring and health:

- Each hit scores 10 points times a combo multiplier: 2x from a combo of 2, 4x from 6 and 8x from 14.
- A miss or a bomb resets the combo.
- Energy starts at 50 out of 100. A hit gives +5, a miss costs 15 and a bomb costs 30. At zero the level fails.

## How it works

### Finding a wall (`LiDARSpawner.cs`)

Each frame while scanning, the game casts a ray horizontally from the camera against the LiDAR mesh layer. A hit counts as a wall if the surface is near vertical (`|normal.y| < 0.3`) and at least 1.5 m away. Raw LiDAR meshes are noisy, so the hit is then checked for flatness:

1. Cast four probe rays at the corners of the area the grid would cover.
2. Fit a plane through the four hit points using the cross product of the two diagonals.
3. Reject the spot if any probe is more than 0.2 m off that plane.

If it passes, the grid snaps to the fitted plane, so the portals sit flush on the wall even when the raw mesh normal is slightly off.

### Beat detection and sync (`LiDARSpawner.cs`)

Notes are generated from the audio at runtime, so any song works without a hand-made beatmap.

- A near-silent `analyzerSource` plays the song and `GetSpectrumData` reads a 1024-bin spectrum (Blackman-Harris window). The energy in the lowest bins is summed, and a spike above `beatThreshold` (with a minimum gap between beats) schedules a note.
- The audible copy of the song plays from a separate 3D emitter using `PlayScheduled`, delayed by the note travel time (distance from player to wall divided by note speed). The music you hear is therefore late by exactly the time a cube takes to reach you, so the cube arrives on the beat you hear.
- Notes are spawned from a queue when `now + travel time >= hit time`. The travel time is recomputed from the player's current distance to the wall, so walking towards or away from the wall does not break the timing.
- The music emitter drifts towards the cube nearest the player, so the sound seems to come from the incoming note.
- Each note picks a random portal that differs from the previous one. Bombs make up about 5% of spawns.

### Other pieces

| Script | Role |
|---|---|
| `UIManager.cs` | Start, scanning, HUD and game-over screens. Opens the iOS file picker and loads the chosen file with `UnityWebRequestMultimedia`. Computes the maximum possible score and the letter rating. |
| `SaberBlade.cs` | Trigger on the camera. Handles hits and bombs: sound, particles, score, health and haptics (iOS system sound or the Android vibrator). |
| `ScoreManager.cs` | Score, combo and multiplier, plus on-screen feedback text. |
| `HealthManager.cs` | Energy bar and fail condition. |
| `MissZone.cs` | A wall that follows the player's depth and removes notes that were not hit, applying the miss penalty. |
| `NoteBehavior.cs` | Moves a note forward and removes it after 8 seconds. |

## Running it

You need a Mac with Xcode and a LiDAR-equipped iPhone. Notes and the scanner rely on the LiDAR mesh, so the iOS Simulator and non-LiDAR phones will not work.

1. Install Unity **6000.4.2f1** with the iOS build module.
2. Open this folder as a project in Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity`.
4. In *File > Build Profiles*, switch to iOS and build. Open the generated Xcode project, set your signing team and run it on the device. The bundle id is `com.DanielJackson.SpatialPulse`. Change it if you sign with your own team.
5. Copy an mp3 or wav onto the phone (the Files app) before you launch.

The scene needs the tags `Note` and `Bomb`, which are already set on the prefabs in `Assets/Prefabs`.

## Third-party assets and credits

These are included so the project opens and runs as built. Each remains under its own licence and belongs to its owner.

- **NativeFilePicker** by Yasir Kula (`Assets/Plugins/NativeFilePicker`), MIT licence. <https://github.com/yasirkula/UnityNativeFilePicker>
- **NamuFX AoeExplosions** (`Assets/NamuFX`): explosion effect used for bombs.
- **Procedural Portal Effect** (`Assets/Procedural Portal Effect`): portal shader and prefabs used for the grid.
- **TextMesh Pro**: Unity package resources.
- **Beat Saber logo** (`Assets/Images/Logo.png`): used on the start screen. Beat Saber is a trademark of Beat Games. This is an unofficial, non-commercial student project and is not affiliated with or endorsed by Beat Games.

The demo video contains background music that I do not own. No audio files are in the repository itself.

## Known limitations

- Beat detection uses only bass energy, so tracks without a strong low end produce sparse or uneven notes. Tuning `beatThreshold` per song helps.
- Notes are placed randomly, not designed. Left and right cube colours do not restrict the direction of a swing.
- The grid has to fit on one flat wall. Small or cluttered walls will not pass the flatness test.
- iOS only. The Android vibration path exists but AR Foundation for Android is not set up.

## Author

Daniel Jackson. [GitHub](https://github.com/DanielJacksonRSA)

Code in `Assets/Scripts` is licensed under the [All rights reserved](LICENSE). Third-party assets keep their own licences, as listed above.
