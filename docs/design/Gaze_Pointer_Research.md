# Gaze Pointer Research and Aim-to-Select Test Plan

Date: November 21, 2026. Branch: `Yaxin-Mu/Prototype-2`. Status: research done, decisions A–D made (section 4), test scene built (5.9), first device run passed (section 6).
Chinese copy: `Gaze_Pointer_Research.zh.md`.

---

## 1. Problem

While the game runs on the RayNeo X3 Pro, a small dot sits in the middle of the view the whole time. The main HUD is driven by swipe (move focus) and tap (confirm), so the dot has no job there and makes the view look busy.

Questions:
1. What is the dot, and can it be hidden?
2. Is "turn the head to aim, tap the temple to act" a usable way to pick objects on the treasure map?

## 2. Research process

| Step | Source | What was checked |
|---|---|---|
| 1 | Project code (`Assets/Scripts`) | Where the dot comes from; existing code that hides it |
| 2 | RayNeo ARDK 1.1.2 source (`Packages/com.unity.xr.rayneo.openxr`) | Which prefab the game uses, pointer components, touch timing |
| 3 | Qualcomm tutorial "Get Started with RayNeo X3 Pro AR Development" | What RayNeo calls the pointer parts and what they do |
| 4 | GitHub `shyam-barange/MultiSet-RayNeoSDK` README | Whether shipped RayNeo apps work without a pointer |
| 5 | No official RayNeo FAQ was found that explains how to turn the dot off. | — |

Links:
- https://www.qualcomm.com/developer/project/get-started-with-rayneo-x3-pro-ar-development (the page body is rendered by JavaScript; it was read in a browser)
- https://github.com/shyam-barange/MultiSet-RayNeoSDK (section "A note on input: there is no pointer")

## 3. Findings

### 3.1 What the dot is
- It is the SDK gaze pointer `LaserBeamDot`, part of the `XR Plugin` prefab that `Editor/GameSceneBuilder` puts in the device scene. It follows head direction, not the eyes: when the eyes look sideways, the dot does not move.
- The Qualcomm tutorial lists the prefab parts: `LaserBeam` = "ray control unit corresponding to the camera", `BeamGraphic` = ray renderer, marked "can be disabled", `LaserBeamDot` = "interaction point where the ray collides with objects". It gives no steps for hiding `LaserBeamDot`.
- The game uses `XR Plugin.prefab`, whose `LaserBeam` lives in the closed-source `UnityXRSDKCore.dll`. A second prefab, `XR Plugin Gaze.prefab`, uses `GazeLaserBeam.cs`, which fires a click automatically after the pointer rests on an object (dwell progress ring). The game does not use it; the test must not use it either (no dwell confirm).

### 3.2 Why it is always visible
- Nothing in the game hides it. The only hiding code is in the old AnchorZone debug HUD (`AnchorZone/ZoneDebugHUD.cs`), and only while the anchor menu is open.
- That code finds the renderers named `LaserBeamDot` and `BeamGraphic` and switches them off in `Camera.onPreCull`, right before each frame is drawn. The comment explains why: the closed-source `LaserBeam` may switch its renderers back on in its own update, so unticking the object once in the Inspector is not reliable.

### 3.3 The pointer already talks to our UI
- Every game canvas gets an `XRGraphicRaycaster` (`UI/UiFactory.cs`, `CreateCanvas`). The SDK ray therefore hits our HUD and map graphics, and the dot is drawn where it hits.
- The Qualcomm tutorial says that with `XRGraphicRaycaster` on a canvas, "clicking the temple should trigger button events". So the SDK sends its own click to whatever UI element the dot is on.
- Today this does nothing, because the game has no Unity `Button` and no `IPointerClickHandler`: all input goes through our `InputAdapter`. If the test objects were made as buttons, one temple tap would run twice: once through the SDK click and once through `InputAdapter`. This is the main input-conflict risk.

### 3.4 Pointer-free interaction is a normal pattern
- The MultiSet README says its app has no pointer: the UI canvas is parented under the head camera, so "a LaserBeam / EventSystem raycast cannot reliably hit its buttons". All input is touchpad: swipe moves the highlight, tap activates, double tap goes back. This matches our main HUD. It does not mention hiding the SDK dot.

### 3.5 Touchpad timing (ARDK source, `SimpleTouchForLite.cs`)
- Taps closer than `MulitTapSpacing` = 0.4 s merge: 2 taps = double tap (our global Back), 3 = triple tap.
- A single tap is reported only after the 0.4 s window closes, so every tap reaches the game about 0.4 s after the finger lifts.
- `OnTouchStart` fires when the finger touches down, with no delay.

### 3.6 Current map behaviour (`UI/TreasureMapView.cs`)
- The map sits 55° right of the body, about 8° below eye level, at 2.9 m. It moves only when the head yaw leaves the band from 35° left of the body to 25° past the map, held for 1.5 s. Looking at the map never moves it.
- "Map in view" today = flattened yaw angle to the map below 20° (yaw only, no pitch, no hysteresis). While in view, the map takes Prev / Next / Confirm for chest focus.
- Size estimate (not measured on device): the 600 × 464 px map covers about 15–16° × 12° of the view, so about 39 map px per degree, and every point on the map is within about 10° of its center.

### 3.7 Answer
- The dot is the SDK gaze pointer. It can be hidden by switching its renderers off before every frame (method already proven in `ZoneDebugHUD`); the input module behind it keeps running.
- Recommended default: hidden everywhere. Show a pointer only where aiming is the interaction, which the test below checks.

## 4. Review of the aim-to-select test plan

Verdict: **feasible as a separate test scene**, with the changes below. All numbers stay starting values for device tuning.

| # | Topic | Change or note |
|---|---|---|
| 1 | Which pointer (decided C) | Keep the SDK dot hidden all the time in the test scene and draw our own reticle on the map canvas (cyan ring, ≥ 2 px). Reason: the SDK dot is closed source, re-enables itself and also lands on the HUD; our own reticle obeys the show/hide table exactly. No SDK-dot comparison toggle. |
| 2 | Hit test | Do our own hit test: ray from the head camera → map plane → map coordinates → target hit areas. Do not use SDK pointer events. Test objects must not be `Button` or `IPointerClickHandler`, so a tap never runs twice (3.3). |
| 3 | Map-view angle | Use the full 3D angle between head forward and the map center (the current `InView` is yaw only). Enter: < 15° for 0.2 s. Exit: > 20°. Every target is within about 10° of the center, so all can be aimed at before the exit boundary. Between the map edge and 15°, the reticle is off the map and counts as empty space. |
| 4 | Target size | Hit area about 2.5° across (about 100 map px), gaps of at least 1.5° (about 60 px). Three targets fit the 600 px map with a clear empty area. Re-check on device. |
| 5 | Map lock | The current follow rule already never moves the map while the player looks at it. Still add an explicit lock while map mode is on, as the plan asks. |
| 6 | Tap target (decided A) | Remember the target under the reticle at `OnTouchStart` (finger down) and act on it when the tap arrives, if map mode is still on. Log the touch-down target and the target at tap time, so the device test shows how often they differ. |
| 7 | Tap delay (decided D) | Out of scope: this round tests aiming, not response time. Keep the SDK touch settings as they are (0.4 s double-tap window everywhere). Taps arrive about 0.4 s after the finger lifts; decision A makes the touch-down target count, so the delay cannot pick the wrong object. Tap at about one per second. |
| 8 | Input routing (decided B) | Main view = swipe to move focus, tap to confirm, no reticle, no dot. Map view = reticle only: no swipe selection (swipes are ignored), the reticle picks the object, a temple tap confirms. The aim handler pushes itself on the `InputAdapter` stack when map mode starts and pops when it ends; it consumes every tap and swipe while map mode is on, also on empty space, so the toolbar never gets them (T05). Panels push above it and own input (T11). |
| 9 | Old chest logic | The test map has no chests, and the chest focus input of `TreasureMapView` stays off in the test scene, so no "old map selection logic" can take the tap. |
| 10 | Head tracking lost | Use `SlamSessionController.OnTrackingChanged` (drift guard: head speed > 3 m/s counts as lost; `GetSlamStatus()` is not usable on this device). On loss: leave map mode, clear the highlight, ignore taps. |
| 11 | Pause / resume | On `OnApplicationPause`: leave map mode, clear the target and the touch-down snapshot (T13). |
| 12 | Isolation | New scene `AimTest.unity`, built by an editor menu like the device scene, not first in Build Settings; its own APK build trigger. Counters live in memory only (no save file), so the real save is never touched. Changes to shared code stay opt-in (map lock flag, chest input off flag) and are inactive in the main game. |
| 13 | Logging | One log line per event with tag `[AimTest]`: map mode enter/exit with angle, target change, touch-down target, tap target, result, ignored taps with reason. The device log then answers T01–T13. |
| 14 | RayNeo spec | Cyan highlight, green success, no red (nothing destructive). Text ≥ 16 px; result text in the top toast zone or next to the target; hint text short. Double tap stays Back, never gameplay. |

Decisions (Yaxin, November 21, 2026):
- **A. Tap target:** the target under the reticle when the finger touches down.
- **B. Two views, two ways to select:** main view (toolbar) = swipe selection only, no reticle, SDK dot hidden. Map view = no swipe selection, reticle only: the object under the reticle highlights, one temple tap confirms.
- **C. Reticle:** our own reticle only.
- **D. Tap delay:** out of scope for this test; touch settings stay unchanged.

### 4.1 Note on double tap and the 0.4 s delay (for later)
- Double tap = Back (RayNeo spec). It comes from the SDK touch setting `MulitTapSpacing: 0.4` in `Assets/XR/RayNeoGeneralSettings.asset` (SDK default, never changed) and from `InputAdapter`, which turns `OnDoubleTap` into `GameAction.Back`; panels go back or close, onboarding asks "Skip tutorial?".
- Because a single tap could be the first half of a double tap, the SDK waits 0.4 s before reporting it. That is the delay on every tap.
- Map view has nothing to go back from (the player leaves it by turning the head), so a later build could switch the double-tap window off there for instant taps. Not part of this test.

## 5. Test plan (as agreed, with the changes above)

### 5.1 Goal
Check on the glasses that this flow feels natural and is accurate:
turn to the map → reticle appears → aim at an object → object highlights → tap the temple → action runs → turn back to the main view → reticle hides.
This round tests selection only, with test objects and counters. No real gathering, building or economy.

### 5.2 Scene
Main-view HUD and the map on the right stay. Three test objects on the map, far enough apart:

| Object | Hint while aimed | Result of a tap |
|---|---|---|
| Tree | Tree · Tap to collect | Tree flashes, "Wood +1" |
| Rock | Rock · Tap to mine | Rock flashes, "Stone +1" |
| Building site | Site · Tap to build | Becomes a simple building, "Built" |

Tree and rock can be used again and again. After building, aiming shows "Building · Tap to inspect" and a tap shows "Inspected". The map keeps a clear empty area for miss tests. A test Reset entry restores the site and the counters.

### 5.3 View and pointer rules

| State | Reticle | Highlight | Swipe | Temple tap |
|---|---|---|---|---|
| Main view | Hidden (SDK dot hidden too) | None | Moves toolbar focus | Main HUD as today |
| Map view, aiming at empty space | Shown | None | Ignored | Nothing on the map; toolbar does not get it |
| Map view, aiming at an object | Shown | That object | Ignored | Runs that object's action |
| Panel, reward window or confirmation open | Hidden | Cleared | Panel | Only the panel |
| Leaving map view | Hidden | Cleared at once | Back to main view | No action on the old object |

Map view = head direction vs map center (not eye tracking). Enter at < 15° held 0.2 s, exit at > 20°. While map view is on, the map does not recenter; it resumes the normal follow rule after exit. The map never moves while the player is aiming.

### 5.4 Selection and feedback
- Aimed = reticle inside an object's hit area: cyan outline, name and action hint. Nothing runs yet.
- Confirmed = temple tap: runs the action of the object that was aimed at when the finger touched down (decision A).
- Moving to another object moves the highlight; moving to empty space clears it. At most one object is highlighted; on overlap, the first object the ray hits wins.
- A successful tap gives a short green flash and the result text. The text does not need a tap to close and does not take input focus.
- A target the reticle has left never reacts to a tap. Looking alone never runs anything (no dwell).
- While an action animation runs, the object shows "Working…" and ignores taps; taps are never queued.

### 5.5 One full flow (tree)
1. Look ahead: main HUD, no reticle.
2. Turn right to the map: after the enter rule, the reticle appears and the map stops recentering.
3. Aim at the tree: outline and "Tree · Tap to collect".
4. Tap: the tree flashes, Wood +1, "Wood +1".
5. Keep aiming: after the flash, the tree is ready again.
6. Aim at the rock: the tree loses the highlight, the rock gets it.
7. Turn back: reticle, highlight and hints disappear; the main HUD takes input again.

### 5.6 Input conflict rules
- In map view, one tap goes to the map only; never also to the toolbar or the old chest logic.
- In main view, the map ignores taps, even right after aiming at something.
- An open panel owns input.
- Double tap keeps the global Back meaning everywhere; on the map it never collects, builds or counts as two taps.
- A tap during "Working…" is ignored, never queued. No fast repeated tapping: about one tap per second.
- Head tracking lost: map actions stop and the highlight clears; after recovery the target is checked again.

### 5.7 Function tests
Record pass / fail, what happened and what triggered any problem.

| ID | Action | Expected |
|---|---|---|
| T01 | Start, stay in main view, small head turns left and right | No reticle; main HUD works |
| T02 | Main view → map → main view, 5 times | Reticle shows and hides by the rules; no highlight left over |
| T03 | Move the head slowly back and forth at the map-view edge | No flicker from small head moves; hides once past the exit angle |
| T04 | Aim at tree, rock, site in turn | Only the current object highlighted; name and action match |
| T05 | Move from the tree to empty space, tap | No collect, no count change, no toolbar action |
| T06 | Aim at the tree, tap 5 times about 1 s apart | Wood +5 exactly; feedback each time; no double runs |
| T07 | Aim at the tree, turn to the rock, tap once the rock is highlighted | Only "mine" runs |
| T08 | Tap the site, then tap the building | First builds, second inspects; never builds twice |
| T09 | Aim at an object, turn back to main view, tap | Old object not used; main-view rules apply |
| T10 | Double tap while aiming at an object | No resource, no build; Back rule applies |
| T11 | Open a panel, look at the map, tap | Only the panel reacts; no map highlight or action |
| T12 | Stay in map view 30 s and look at all three objects | Map never recenters or follows the reticle |
| T13 | Pause or close the app, resume | No old action runs; no stale highlight |

### 5.8 Player test
Instruction to the tester, no more detail: "Look at the map on your right. Collect one wood, one stone, then build on the site."

Watch for:
- Do they understand the reticle is for aiming?
- Can they tell "highlighted" from "done"?
- Do they know to tap the temple to confirm?
- Do they hit the neighbouring object by mistake?
- Do they need visible effort to keep the head still?
- Does the main view feel clean after turning back?
- Do they try to swipe on the map? (Swipes are ignored there.)

Then a timed round: pick the three objects in a given order, 10 taps in total. Record first-try correct picks, taps on empty space and wrong actions.

First-round pass targets:
- At least 9 of 10 picks correct on the first try.
- Zero double runs, zero actions from the main view, zero actions from a double tap.
- After 2–3 minutes of use, ask whether aiming is tiring and whether turning is uncomfortable.
- If it fails, first change target size, spacing and map position, before adding more complex interaction.

### 5.9 Test build (November 21, 2026, branch `Yaxin-Mu/Prototype-2`)
- Scene `Assets/Scenes/AimTest.unity`, built by Tools > RayneoIdle > Build Aim Test Scene (or `Temp/build-aimtest-scene.request`). Not in Build Settings; the game scene is unchanged.
- APK: Tools > RayneoIdle > Build Aim Test APK (or `Temp/build-aimtest-apk.request`) → `../Build/RayneoIdle_AimTest.apk`, built from this scene only. Same package name as the game: installing it replaces the game app until the game APK is installed again.
- Code: `AimTest/AimTestController.cs` (map view, reticle, hit test, taps, feedback, `[AimTest]` log), `UI/GazeDotHider.cs` (hides the SDK dot every frame). Opt-in hooks, off in the game: `TreasureMapView.ChestInputEnabled` / `FollowLocked`, `GameSession.startSlot` (test uses `SaveSystem.AimTestSlot` = 2).
- Layout on the map (texels, map = 140 × 100): Tree at (36, 62), Rock at (100, 60), Site at (70, 28); empty space between Tree and Rock. A small RESET sign in the bottom-right corner resets the counters and the site (it is a fourth target; do not count it in the 10-tap round). Counters are in memory; closing the app also resets them.
- Settings > Reset also works but clears the zone anchors (D10), so prefer the RESET sign.
- Editor: Q / E look forward / right, hold J / L / I / K to turn slowly, Space = tap, Escape = Back.

## 6. Results

First device run: November 21, 2026, 17:19–17:22, RayNeo X3 Pro, tester Yaxin, APK `RayneoIdle_AimTest.apk`. Log: `../Build/device_log_aimtest_1718.txt` (100 `[AimTest]` lines, no exceptions).

**Verdict (Yaxin): the aim-to-select interaction works and is usable.**

| ID | Result | What the log shows |
|---|---|---|
| T01 | Pass | 17 touch-downs in main view: none ran a map action; the toolbar kept its normal input. |
| T02 | Pass | 4 enter / exit cycles; every exit cleared the highlight. |
| T03 | Pass | Exits at 20.2–21.2°, enters at 3–12°; no enter / exit flicker. |
| T04 | Pass | Tree, Rock, Site and Reset all highlighted; one at a time. |
| T05 | Pass | One tap on empty space: nothing ran. |
| T06 | Pass | 8 taps on the tree about 0.5–1.4 s apart: Wood +8 exactly, one action per tap. |
| T07 | Pass (partly) | Two taps ran the touch-down target although the reticle had already left it when the tap arrived (Reset, Rock): decision A worked as designed. A turn from one object to another and then a tap was not done on purpose. |
| T08 | Not run | The site was never tapped (no Built / Inspected). |
| T09 | Pass | Taps after turning back never ran an old object. |
| T10–T13 | Not run | Double tap while aiming, panel open, 30 s hold, pause / resume: no log evidence. |

Observations:
- **Tap delay was not a problem** in this run. Taps arrived about 0.4–0.6 s after touch-down.
- **Decision A matters:** in 2 of 11 map taps, the reticle had left the object by the time the tap arrived. With "target at tap time" these would have hit empty space.
- **Highlight flickers at object edges:** the reticle often left an object within 0.1–0.5 s (25 "aim empty" events), once after 34 ms. Small head movement near an edge switches the highlight on and off. Possible fix for the next round: a little hysteresis (keep the highlight until the reticle is 1–2 texels outside the hit area, or for 0.15 s).
- One touch-down on the tree (17:22:32) produced no tap event; most likely a swipe or a double tap (neither is logged).
- The first launch did not render until the glasses were woken a second time (known dozing behaviour, DEVICE_FINDINGS).

Player test (10-tap round with other testers): not run yet.
