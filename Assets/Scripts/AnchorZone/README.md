# Anchor Zone Detection - Prototype

## Scene setup
1. The scene needs the RayNeo `XR Plugin` prefab (camera rig).
2. Add an empty GameObject and put **AnchorZoneBootstrap** on it. That is all.
   All tuning (radii, dwell time, swipe direction, SLAM debug fallback) is on this component.

## Files
| File | Role |
|---|---|
| AnchorZoneBootstrap | Entry point. Creates and wires everything below at runtime. |
| SlamSessionController | Starts/stops SLAM, reports tracking, drift guard. |
| ZoneAnchorStore | Anchor list, saved as JSON in persistentDataPath. |
| ZoneDetector | ENTER / EXIT logic (radius + dwell, hysteresis). |
| ModeManager | Zone events -> AppMode. Gameplay listens to `OnModeChanged` only. |
| AnchorPlacementInput | Touchpad menu flow + Editor keyboard / WASD head simulation. |
| ZoneDebugHUD | Builds the head-locked HUD canvas and updates its texts. |
| ZoneAnchorMarkers | World-locked diamond + zone name at each anchor (shows drift by eye). |
| ZoneTypes | ZoneType, AppMode, ZoneAnchorData. |

Camera Attitude Type must be **SLAM** (XR Plug-in Management > OpenXR > RayNeo Support).

## Device controls (SimpleTouchForLite)
Anchors mark WHERE YOU STAND, not what you look at. Stand / sit at the spot first.
1. Tap -> "MARK WHERE YOU STAND" menu opens, with "zone = 1.0 m around you" under it.
   The XR Plugin gaze dot is hidden while the menu is open (`Hide Gaze Dot In Menu`),
   so it is not mistaken for an object pointer.
2. Swipe forward/back -> choose WORK / KITCHEN / BEDROOM.
3. Tap -> your current spot becomes the anchor. Double tap -> cancel without placing.
   The menu closes by itself after 10 s without input.
Triple tap (menu closed) = remove nearest anchor. Double tap with the menu closed is left to the system (back/exit).
A diamond marker with the zone name is drawn at each anchor (about waist height); you see it once you step away.

Why not "point at an object": ARDK 1.1.2 has no depth, hit test or object recognition, and plane
detection returned no planes on X3 Pro (tested 2026-10-18: EnablePlaneDetection gets no runtime
response, GetPlaneInfo always 0). The camera is also unavailable while SLAM runs.
If the swipe direction feels reversed, tick `Invert Swipe` on AnchorZoneBootstrap.

Zone triggers: ENTER = within 1.0 m for 2 s, EXIT = farther than 2.0 m (set on AnchorZoneBootstrap).

Drift guard: if the head pose moves faster than 3 m/s (`Max Head Speed`), SLAM has most likely lost
visual tracking and the pose is running away on IMU only. HUD shows `DRIFT`, zone detection and anchor
placement pause, and resume after 2 s of normal speed (`Drift Recover Seconds`).
Slow drift below 3 m/s is NOT caught; the anchor markers make it visible.

Tracking lost (drift, or SLAM losing the camera): anchor markers are hidden, `near` shows `-`,
a toast says "Tracking lost - zones paused" / "Position jumped - zones paused", then "Tracking back".
System screenshot / screen recording (RayNeo AR capture) takes the camera from SLAM, so anchors usually
vanish while recording. Film the lenses with another device to demo anchors.

## Editor controls
WASD walk | Q/E turn | Space open menu / confirm | Left/Right choose | Esc cancel | Backspace remove nearest

## Device test plan
- Test 1 (accuracy): place a Work anchor at the desk, walk 3 m away, come back.
  Note at what distance ENTER fires. Repeat 5x.
- Test 2 (drift): stay in the app 10+ min walking around the room; check if ENTER still
  fires at the desk or the anchor has drifted.
- Test 3 (persistence): place anchor, quit app, restart from a DIFFERENT spot.
  If the anchor now appears in the wrong place, SLAM origin resets per session and we
  need a re-alignment strategy (e.g. a QR marker or a "re-place anchors" step).
- Test 4 (power/heat): run 20 min, note battery drop and temperature.

Logs: `adb logcat -s Unity` and filter by [ZoneDetector] / [SlamSession] / [ModeManager].
