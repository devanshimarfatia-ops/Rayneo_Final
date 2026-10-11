# Anchor Zone Detection - Prototype

## Scene setup
1. The scene needs the RayNeo `XR Plugin` prefab (camera rig).
2. Add an empty GameObject and put **AnchorZoneBootstrap** on it. That is all.
   On Awake it creates `ZoneSystem` (SlamSessionController, ZoneAnchorStore, ZoneDetector,
   ModeManager, AnchorPlacementInput, ZoneDebugHUD) and the head-locked HUD canvas
   (ZoneHudBuilder: 640x480, XRGraphicRaycaster, Toast / Mode / Menu / Status texts), all wired.
   Tune radii, dwell time, swipe direction and the SLAM debug fallback on the bootstrap.
3. Optional: **Rayneo > Setup Anchor Zone Scene** creates a fresh test scene with just
   the XR Plugin prefab + AnchorZoneBootstrap.

Camera Attitude Type must be **SLAM** (XR Plug-in Management > OpenXR > RayNeo Support).

## Device controls (SimpleTouchForLite)
1. Tap -> "SET ANCHOR" menu opens above the center dot.
2. Swipe forward/back -> choose WORK / KITCHEN / BEDROOM.
3. Tap -> anchor is placed where you stand. The menu closes by itself after 10 s without input.
Triple tap (menu closed) = remove nearest anchor. Double tap is left to the system (back/exit).
If the swipe direction feels reversed, tick `Invert Swipe` on AnchorZoneBootstrap.

Zone triggers: ENTER = within 1.0 m for 2 s, EXIT = farther than 2.0 m (set on AnchorZoneBootstrap).

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
