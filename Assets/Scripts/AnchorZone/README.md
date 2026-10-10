# Anchor Zone Detection - Prototype

## Scene setup
Run **Rayneo > Setup Anchor Zone Scene**. It creates `Assets/Scenes/AnchorZoneTest.unity` with:
- `XR Plugin` prefab from the ARDK (replaces Main Camera), clear = Solid Color #000000, no skybox
- `ZoneSystem`: SlamSessionController, ZoneAnchorStore, ZoneDetector, ModeManager,
  AnchorPlacementInput, ZoneDebugHUD, all references wired
- `HUD Canvas`: World Space, child of the head camera at 2 m (0DoF HUD), 640x480 reference,
  XRGraphicRaycaster, Bold legacy Text: ToastText (top), ModeText (top-left), MenuText (center), StatusText (bottom)
- EditorHeadMover on the camera (Editor only)
- the scene is put first in Build Settings

Camera Attitude Type must be **SLAM** (XR Plug-in Management > OpenXR > RayNeo Support).

## Device controls (SimpleTouchForLite)
1. Tap -> "SET ANCHOR" menu opens above the center dot.
2. Swipe forward/back -> choose WORK / KITCHEN / BEDROOM.
3. Tap -> anchor is placed where you stand. The menu closes by itself after 10 s without input.
Triple tap (menu closed) = remove nearest anchor. Double tap is left to the system (back/exit).
If the swipe direction feels reversed, tick `Invert Swipe` on AnchorPlacementInput.

Zone triggers: ENTER = within 1.0 m for 2 s, EXIT = farther than 2.0 m (ZoneDetector).

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
