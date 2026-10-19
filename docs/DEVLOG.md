# Dev Log

One entry per work session, newest first. Keep each entry short; details belong in code, commits
and `docs/DEVICE_FINDINGS.md`.

Entry template:

```
## YYYY-MM-DD - <author> - <branch>
**Done**
- ...
**Decisions** (what + why)
- ...
**Device findings** (also copy to DEVICE_FINDINGS.md)
- ...
**Open issues / blockers**
- ...
**Next**
- [ ] ...
```

## Team workflow
- One branch per feature (`<name>/<feature>`), merge to `main` through a pull request.
- Unity scenes do not merge well: only one person edits a given `.unity` file at a time.
  Build your own test scene for experiments.
- Read the latest entries here and `docs/DEVICE_FINDINGS.md` before starting work.

---

## 2026-10-19 - Yaxin - Yaxin-Mu-Anchor/Voice-Detect
**Done**
- Plan B feature launcher (`Assets/Scripts/Launcher/FeatureLauncher.cs`, scene `Assets/Scenes/Test-Launcher.unity`,
  now first in Build Settings): home screen with square tiles ANCHOR / TODO / QUIT, both features in one scene.
  Home screen + swipe focus tested on device.
- Inside a feature: double tap / Android back key = back to the home screen (blocked while the anchor menu is open).
- Layout of the home screen is on the FeatureLauncher Inspector ("Layout" header) and applies live in Play mode.
  Default: small tiles (90 px) at the bottom, no hint line.
- QUIT tile: single tap closes the app (`Application.Quit`, as in the ARDK HelloRayNeo sample).
- `ZoneDebugHUD`, `ZoneAnchorMarkers`, `VoiceTodoTestPanel` hide their head-locked canvas / markers in
  `OnDisable` and show them again in `OnEnable` (needed for switching features).
- New `Assets/Scripts/Common/SwipeMap.cs`: one horizontal swipe mapping for the whole project
  (forward = left / previous, back = right / next). Used by the launcher and the anchor menu.

**Decisions**
- Plan B in case our feature code cannot be merged into Devanshi's UI: a self-contained launcher scene.
  Our HUDs are also world-space Canvases (built in code), so the UI systems do not conflict; the real risks
  are both sides listening to `SimpleTouchForLite` at once and `.unity` merges. The launcher keeps only one
  feature's input + HUD enabled at a time, which is also how her buttons could call our features later.
- Leaving the anchor screen keeps SLAM + zone detection running (`Keep Anchor Running` ON): disabling SLAM
  may reset its origin and invalidate anchors (untested).
- Tile focus is clamped, not wrapped: with two tiles a wrap made any swipe a toggle (direction felt random),
  and wrapping onto QUIT by accident is risky.
- Layout values moved to the Inspector so Yaxin can tune them without code changes. Fonts are clamped to
  >= 16 px and lines to >= 2 px (RayNeo spec).

**Device findings** (also copy to DEVICE_FINDINGS.md)
- A physical forward swipe on the X3 Pro temple fires `OnSwipeRight`. The ARDK has no system setting
  for swipe direction, so the convention lives in `SwipeMap`.

**Open issues / blockers**
- Not tested on device: new bottom layout, QUIT tile, whether a double tap reaches the app (vs. system back).
- Does `startTodo` work while the app runs in SLAM mode, and does SLAM recover afterwards?
  `Test-Launcher` runs both features in SLAM mode, so it can answer this.
- `ProjectSettings.asset` Preloaded Assets question from 2026-10-18 is still open.

**Next**
- [ ] Device-test `Test-Launcher`: bottom layout, QUIT, double tap back, Todo voice flow in SLAM mode.
- [ ] Run AnchorZone device tests 1-4 (accuracy, drift, persistence, power) and log results.
- [ ] Agree with Devanshi how her Canvas buttons call our features (Plan A) before merging.

## 2026-10-18 - Yaxin - Yaxin-Mu-Anchor/Voice-Detect
**Done**
- Anchor zone prototype tested on device (`Assets/Scripts/AnchorZone/`, scene `Test-Anchor.unity`).
- Removed gaze-to-object anchor placement; anchors now mark where the user stands.
- Drift guard + "tracking lost" handling (markers hidden, zones paused, toast).
- Cleaned old scenes (`SampleScene`, `Test`) and the disabled launcher manifest.
- Set up `docs/DEVLOG.md` and `docs/DEVICE_FINDINGS.md`.
- Voice todo, step 1 (device validation build, compiles, not tested on device):
  `Assets/Scripts/Todo/` - `VoiceInput` (Android SpeechRecognizer via JNI), `SystemTodoClient`
  (RayNeo system To-do provider), `VoiceTodoTestPanel` + `VoiceTodoTestBootstrap`;
  scene `Assets/Scenes/Test-Voice.unity` (added first in Build Settings).
- `AndroidManifest.xml`: `RECORD_AUDIO` permission + `<queries>` for the speech service and the
  To-do provider (Android 11+ package visibility).

**Decisions**
- Use the RayNeo system To-do app as storage via its content provider. It has no create method,
  so new todos need another path (see DEVICE_FINDINGS).
- Placement by user position: X3 Pro has no plane detection / depth / hit test (see findings).
- Gameplay listens only to `ModeManager.OnModeChanged`, so detection can be swapped later
  (QR marker / Wi-Fi / BLE) if SLAM anchors do not persist across sessions.

**Device findings**
- Plane detection does nothing on X3 Pro; system screen recording breaks SLAM tracking.

**Open issues / blockers**
- Unknown whether the SLAM origin resets per session (decides if anchors survive app restart).
- Unknown whether a 3rd-party app may use the `com.rayneo.live.ai` speech recognizer.
- `ProjectSettings.asset`: two entries were removed from Preloaded Assets (guids `b31b95b3...`,
  `b932d0eb...`). Check this was intended before committing.

**Next**
- [x] Device-test `Test-Voice`: speech recognition result + reading/editing system todos (2026-10-19, see DEVICE_FINDINGS).
- [ ] Run AnchorZone device tests 1-4 (accuracy, drift, persistence, power) and log results.

## 2026-10-10 ~ 2026-10-14 - Yaxin, Devanshi
**Done**
- Unity project created and configured with RayNeo OpenXR ARDK (Yaxin).
- Cube rotator interaction test (Devanshi, merged to `main`); further changes on `Devanshi` branch.
- First anchor prototype (`Anchor_01`, `Anchor-Fixed-test1`).

**Device findings**
- `GetSlamStatus()` always 0; touchConfigs key must be `platformType`; Active Input Handling = Both.
