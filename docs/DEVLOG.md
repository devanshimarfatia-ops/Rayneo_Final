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

## 2026-11-07 - Yaxin - Yaxin-Mu/Prototype (prototype phase: full MVP loop, 4 device test rounds)
**Done**
- Core in plain C# (no SDK): `Idle/` IdleCore + `IGameClock` + IdleConfig (MVP economy values), `Cards/`,
  `Island/IslandMap`, `Save/` (JSON per slot, temp file + .bak), `Game/GameSession` (launch settle, silent
  resume settle, 30 s autosave), DemoMode (1x/60x/1800x, own slot), `Todo/ZoneTag` + `ZoneTodoService`
  (zone prefix, one-time +320 reward), `Input/InputAdapter` (touchpad + keyboard, handler stack). Covers W1-W9.
- UI built in code (`UI/`): head-locked HUD (coins, zone todos, toasts, 6-tool toolbar, progress row),
  body-following line-art treasure map (`TreasureMapView`), panels Anchors / To-do / Collection / Profile /
  Settings, chest `RewardWindow`, `StartScreen` (offline earnings + Start), pixel art in `PixelSprites`,
  `CardArt`, `CharacterArt`.
- W11 onboarding (`Onboarding/OnboardingController`): intro card, gesture steps, anchor 1 (place + name),
  To-do, map, tutorial chest, pack, Collection, end card; framed hints "TUTORIAL k/12" + toolbar pointer; skip.
- Shop: Collection "Buy a pack" (3,000). Profile: Skills (Coin Rate), Look (4 outfits, 6 colors), Pets (4),
  free items + coin unlocks (`Profile/ProfileSystem`); the pet follows the explorer on the map.
- Device scene `Assets/Scenes/Game.unity` (built by `Editor/GameSceneBuilder`), first in Build Settings.
  `AnchorZoneBootstrap.withDebugUi` OFF = game mode.
- Tooling (Editor, trigger files in `Temp/`): smoke test (22 checks), onboarding test (23 checks), HUD
  snapshots at the 30 deg view, refresh/compile without focusing Unity, APK build (`Editor/DeviceBuild`).
- APK `../Build/RayneoIdle_Prototype.apk`; device logs streamed to `../Build/device_log_*.txt` during tests.
- 28 commits, each compiled on its own; branch pushed. Iteration log with screenshots: shared doc
  "Prototype Iteration Log (2026-11-07)", also copied to Google Docs in Chinese and English with all
  31 screenshots (links in the working-folder notes); screenshots archived in `../Screenshots/2026-11-07/`.

**Decisions** (what + why)
- D8: code-built HUD with own pixel art, following the RayNeo spec. All tasks are Yaxin's; no Canvas merge.
- Palette: RayNeo official colors (cyan #4DE2FF, green #44FF66, gray-cyan #7FA8B8); the first muted
  parchment palette was rejected on device ("strange"). Rarity uses the GDD colors (silver / gold / purple).
- Island = flat treasure map that follows the body (55 deg right, ~8 deg below the eyes, recenters after
  1.5 s facing a new direction); no Map tool, an arrow says "look right". Device feedback: fixed map felt bad.
- 18 cards per rarity (54). Economy values were NOT re-tuned for it yet.
- Anchors are 3 named slots (presets + voice rename via a temporary To-do item); renaming rewrites the
  slot's todo prefixes with `todoEdit`.
- Offline earnings window shows 5 decimals (D4 said 10; Yaxin: shorter). Logs keep 10.
- Shop prices are placeholders: pack 3,000; outfits 0/0/1,000/2,500; colors 0/0/500/800/1,200/2,000;
  pets 1,500/1,500/3,000/5,000.
- Every launch shows offline earnings (if a save existed) then Start; onboarding begins after Start.

**Device findings** (also copied to DEVICE_FINDINGS.md)
- `todoEdit` works from our app; `startTodo` works while the app runs in SLAM mode and returns by itself.
- One temple swipe fires 2 step events, usually < 40 ms apart, sometimes > 120 ms.
- With the screen dozing the app does not start rendering; `adb shell input keyevent KEYCODE_WAKEUP` fixes it.

**Open issues / blockers**
- Economy sheet: re-tune for 54 cards (D6), add shop/cosmetic prices, fix pity "75" -> "78".
- GDD (EN + ZH) not yet updated for D3/D4/D6/D8, palette, shop, Profile, Start screen.
- Not tested on device: anchor persistence across sessions, voice rename (`deleteTodo`), shop / Look / Pets.
- No PR to `main` yet.

**Next**
- [ ] Monday 11/9 playtest: recruit testers, playtest script + feedback form, run it, summarize.
- [ ] Device-test shop, Look, Pets, voice rename; anchor persistence (start elsewhere, walk back).
- [ ] Update economy sheet + both GDDs with today's decisions.
- [ ] Open a PR from `Yaxin-Mu/Prototype` to `main`.

## 2026-11-01 - Yaxin - Yaxin-Mu-Anchor/Voice-Detect (design docs only, no code)
**Done**
- GDD (Google Doc, English + Chinese versions kept in sync) moved to v0.8. New tabs in both:
  "MVP Scope" (GDD section 9 draft) and "Economy Draft". Tab 1 updated with the agreed MVP decisions.
- MVP scope (9.1-9.7): 10-minute demo from a fresh install, 13-step demo path with device acceptance
  criteria, scope table, decisions D1-D10, work breakdown W1-W11, milestones M0-M4, risks R1-R11.
- Economy first draft: full design (sections 1-7) plus MVP subset (section 8) and open questions (9).
  Workbook `Economy_Draft.xlsx` (params sheet drives every other sheet; 19 full + 22 MVP checks pass).
  Kept outside the repo; a Google Sheet copy is now kept in sync with it (link in the working-folder notes).
- GDD tab 1 review pass: TO VERIFY items split into "verified on device" (To-do create/complete sync,
  voice handoff outside SLAM), a D4 rule, and what is still open. Many TO CONFIRM items resolved as
  full-game design (see decisions below). Economy draft tabs updated to match.

**Decisions** (what + why)
- GDD tab 1 describes the whole game, not the MVP; MVP cuts live only in the MVP Scope tab.
- Pomodoro is an independent tool: not tied to anchors or zones, keeps running when leaving a zone.
  Default 45 min work / 15 min break, pause and skip allowed. Skip = end the work segment early.
  Below 30% of the work time a prompt warns there is no reward; from 30% the reward is pro rata to focus
  time (24 coins per focus minute, 1,080 for a full 45 min), so short sessions cannot be farmed.
- Chests: three chest areas on the island, one per chest; a chest spawns at a random spot in its area.
  While searching only a dashed outline with "?" shows the area; the chest drops in when found.
- One island only. Island themes, pets (follow the character) and skin colors are shop skins, no stat effect.
- Out of scope: ambient events, desk objects, room building, plants. Later option: break-time puzzles.
- No cloud sync by default. Collection reference (Kingdom Two Crowns) is the silhouette-to-detail unlock.
- Economy tables are edited as Google Sheets with formulas; for now the local xlsx is the source and is
  re-imported into the Sheet (File > Import > Replace spreadsheet) after each change.
- D1 core loop = GDD loop (todo -> coins, cards from chests). Cards show directly when a chest/pack opens,
  no mosaic/blur/tile reveal; collection shows silhouettes (>= 2 px outline, black is invisible) until owned.
- D2 SLAM on for the whole MVP session; power/heat deferred (gameplay first, re-check before release).
- D3 zone tag lives in the todo text: in-game todos are renamed to `(Zone)-text` via provider `todoEdit`,
  so the system To-do app shows it and Reset/reinstall keeps it. No prefix = General.
- D4 only app close / system kill is a real exit (settle toast on next Start); any pause is the same
  session and is credited silently. Offline cap 24 h, no minimum away time, coins stored as double.
- D5 onboarding never requires voice (no-network fallback: pick an existing todo).
- D6 first card set = 9 cards, 3 per rarity (3x3 gallery). Only 8 artworks exist, 1 more needed.
- D7 Pomodoro = toolbar icon only in MVP. D8 UI integration path still open.
- D9 all game time goes through one injectable `IGameClock` (UTC elapsed, clock set back = 0).
  Demo Mode multiplies income only (1x / 60x / 1800x, character animation 1x / 2x / 3x) and never changes time.
- D10 Reset in Settings (second confirmation) for the demo; SaveSystem stores per slot, slot UI is v2.
- MVP economy: To-do 320, Coin Rate 250 x 1.21^(L-1) (20 levels), chest slots 73/22/5, rarity 60/30/10,
  Special pity 25 draws, tutorial chest 500 coins + 1 Common + 1 pack. Reason: To-do is the only utility
  source left, one Coin Rate level must stay affordable every day of week 1, collection completes ~day 7.

**Open issues / blockers**
- D8: compare Devanshi's Canvas clarity with our HUD on device before choosing.
- D3 depends on provider `todoEdit`, reverse-engineered and not tested on device.
- 9th card artwork (R11). Tutorial chest vs the 3 island chest slots needs confirming with W3.
- MVP 9.2b acceptance still points to the Timeline sheet; the economy draft's QA reference table
  (passive income + chest finds only) is the right check.

**Next**
- [x] Move the economy workbook to a Google Sheet (verified: no errors, values and all checks match the xlsx).
- [ ] Link the economy Sheet from the Economy Draft tab in both GDDs.
- [ ] GDD section 7: drop "(MVP)" from the visual style line; decide whether "map unlock visuals" stays.
- [x] Test `todoEdit` on device (works, D3 confirmed) and `startTodo` in SLAM mode (works) - 2026-11-07. Launcher QUIT / double tap still untested.
- [x] D8 decided without the Canvas comparison: code-built HUD, own pixel art (2026-11-07).
- [ ] Ask Devanshi for the 9th artwork.

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
- [x] ~~Agree with Devanshi how her Canvas buttons call our features~~ - moot: D8 = code-built HUD (2026-11-07).

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
