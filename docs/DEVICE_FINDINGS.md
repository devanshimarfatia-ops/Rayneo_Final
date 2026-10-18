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
- Item JSON: `{ "timestamp": long, "updateTimestamp": long, "text": string, "finished": bool }`; key = `timestamp`.
- adb check: `adb shell content call --uri content://com.rayneo.notes.TodoContentProvider --method getTodoUnfinished`

## Speech recognition
- (2026-10-18) System default `RecognitionService` is `com.rayneo.live.ai/.wakeup.RayNeoRecognitionService`,
  so Android `SpeechRecognizer` should work from Unity. Needs `RECORD_AUDIO` and a `<queries>` entry for
  `android.speech.RecognitionService`, otherwise `isRecognitionAvailable` = false. Not yet device-tested.

## Debugging
- The glasses have no log viewer. Read logs over USB: `adb logcat -s Unity`.
- adb path: `D:/UnityHub/Editor/2022.3.36f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe`
- Project log tags: `[ZoneDetector]`, `[SlamSession]`, `[ModeManager]`.

## Open questions (to test)
- Does the SLAM origin reset per app session? (AnchorZone test 3)
- How much does the pose drift over 10+ minutes? (test 2)
- Battery / heat cost of continuous SLAM over 20 min? (test 4)
- Can a 3rd-party app use the `com.rayneo.live.ai` speech recognizer? (`Test-Voice` scene)
- Is there any way for a 3rd-party app to create a system todo (`startTodo` only)?
