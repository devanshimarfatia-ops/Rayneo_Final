# RayNeo X3 Pro - Device Findings

Facts verified on real hardware. Read this before relying on any SDK feature.
Add a new entry whenever a device test confirms or disproves something (date + ARDK version).

Hardware / SDK: RayNeo X3 Pro, RayNeo OpenXR ARDK 1.1.2, Unity 2022.3.36f1.

## Project setup (required, or the app breaks)
| Setting | Value | Symptom if wrong |
|---|---|---|
| Launch activity | `com.rayneo.openxradapter.UnityOpenXrActivity` + meta-data `com.rayneo.mercury.app = true` in `Assets/Plugins/Android/AndroidManifest.xml` | App starts as a flat "phone app" |
| `Assets/XR/RayNeoGeneralSettings.asset` touchConfigs key | `platformType` (lowercase) | `SimpleTouchForLite` throws `KeyNotFoundException 'X2_Normal'`, touchpad dead |
| Player > Active Input Handling | **Both** | XR Plugin's `XRInputModule` uses legacy Input and fails |
| Camera Attitude Type (XR Plug-in Management > OpenXR > RayNeoSupport) | SLAM for anchor features | No 6DoF position |

## SLAM / tracking
- (2026-10-11) `Algorithm.GetSlamStatus()` always returns 0 (`FFVINS_INITIALIZING`), even while the
  6DoF pose clearly tracks walking. The runtime only logs `FFVINS state = 0` at startup.
  -> Do not gate features on SLAM status. Keep `Ignore Slam Status` ON in `AnchorZoneBootstrap`.
- (2026-10-18) RayNeo system screenshot / screen recording takes the camera away from SLAM:
  tracking is lost and world-locked content vanishes. To demo anchors, film the lenses with a phone.
- When visual tracking is lost the pose can run away on IMU only (fast jumps). The drift guard in
  `SlamSessionController` treats head speed > 3 m/s as "lost".

## Not available on X3 Pro (ARDK 1.1.2)
- (2026-10-18) Plane detection: `EnablePlaneDetection()` gets no runtime response, `GetPlaneInfo()` always 0.
- No depth, no hit test, no object recognition. Camera is unavailable while SLAM runs.
  -> Anchors are placed at the user's own position, not at a gazed object.
- No persistent spatial anchor / relocalization API.

## System To-do app provider (2026-10-18, undocumented, from the on-device RayNeoNotes.apk)
May change with any firmware update.
- Authority `com.rayneo.notes.TodoContentProvider`, exported, no permission needed.
  Needs a `<queries><provider android:authorities="..."/></queries>` entry on Android 11+.
- No query/insert/update/delete: everything goes through `ContentResolver.call(uri, method, arg, extras)`.
  Result Bundle: `callResult` (bool), `callErrorMessage` (string), `result` (JSON string or bool).
- Methods: `getTodoUnfinished`, `getTodoFinished` (-> `{"list":[...]}`), `todoSetFinished`,
  `todoSetUnfinished`, `deleteTodo` (arg = timestamp), `todoEdit` (arg = item JSON, edits text only),
  `startTodo` (asks the notes app to start its own voice-create flow; only works in some foreground states).
- **No add/create method** (`addTodo` -> `UnSupport Method`).
- (2026-10-19) **`startTodo` works from a 3rd-party app** while it is in the foreground: it opens
  `com.rayneo.notes/.ui.TodoCreateActivity` on top (our app loses focus / pauses), records, recognises
  in the RayNeo cloud (`vui-platform-p.rayneo.com`, language = system language, en-US here), saves the
  todo and returns to our app by itself. Detect the new item by re-reading `getTodoUnfinished` on focus
  and diffing timestamps. Needs network. The system app's own spoken prompt can end up in the
  transcript ("Please describe what to do in a single sentence. ...") when audio plays from the speaker.
- Item JSON: `{ "timestamp": long, "updateTimestamp": long, "text": string, "finished": bool }`; key = `timestamp`.
- adb check: `adb shell content call --uri content://com.rayneo.notes.TodoContentProvider --method getTodoUnfinished`

## Speech recognition
- (2026-10-19) **Android `SpeechRecognizer` does NOT work.** The only `RecognitionService`,
  `com.rayneo.live.ai/.wakeup.RayNeoRecognitionService`, is an empty stub (exists only so the
  VoiceInteractionService is valid): `onStartListening` / `onStopListening` / `onCancel` just log and
  never call back. `isRecognitionAvailable` = true and `createSpeechRecognizer` succeeds, but no
  ready / rms / result / error ever arrives.
  -> For voice todos use the system To-do `startTodo` flow (above). For free in-game speech a
  custom engine is needed (Unity Microphone + cloud STT, or offline Vosk).

## Debugging
- The glasses have no log viewer. Read logs over USB: `adb logcat -s Unity`.
- adb path: `D:/UnityHub/Editor/2022.3.36f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`
- Project log tags: `[ZoneDetector]`, `[SlamSession]`, `[ModeManager]`, `[SystemTodo]`, `[VoiceTest]`.
- Touchpad taps can be simulated for testing: `adb shell input tap 320 240` fires `OnSimpleTap`.
- Device logcat ring buffer is only 64 KiB and the touchpad SDK logs every swipe step, so app logs
  scroll away within seconds: read `adb logcat` right after the action, or filter on the PC side.

## Touchpad (SimpleTouchForLite)
- (2026-10-19) Vertical swipe on the X3 Pro temple: physical swipe up fires `OnSwipeUp`, swipe down
  fires `OnSwipeDown` (per-step events, no inversion needed). Used for list navigation in `Test-Voice`.

## Open questions (to test)
- Does the SLAM origin reset per app session? (AnchorZone test 3)
- How much does the pose drift over 10+ minutes? (test 2)
- Battery / heat cost of continuous SLAM over 20 min? (test 4)
- Does `startTodo` still work when the app runs in SLAM / 6DoF mode, and does SLAM recover after
  the system screen took over? (combine with AnchorZone)
