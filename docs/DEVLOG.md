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
- [ ] Device-test `Test-Voice`: speech recognition result + reading/editing system todos.
- [ ] Run AnchorZone device tests 1-4 (accuracy, drift, persistence, power) and log results.

## 2026-10-10 ~ 2026-10-14 - Yaxin, Devanshi
**Done**
- Unity project created and configured with RayNeo OpenXR ARDK (Yaxin).
- Cube rotator interaction test (Devanshi, merged to `main`); further changes on `Devanshi` branch.
- First anchor prototype (`Anchor_01`, `Anchor-Fixed-test1`).

**Device findings**
- `GetSlamStatus()` always 0; touchConfigs key must be `platformType`; Active Input Handling = Both.
