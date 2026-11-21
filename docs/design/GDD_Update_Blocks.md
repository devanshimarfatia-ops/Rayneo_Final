# GDD Update Blocks (tab 1, v0.8 → v0.9)

Source: the prototype as built on branch `Yaxin-Mu/Prototype` (code read on November 21, 2026) and the decisions
logged in `Rayneo_Final/docs/DEVLOG.md` (2026-11-07 entry: D8, palette, body-follow map, 54 cards, anchor slots,
shop / Profile, Start screen). This pass only brings the GDD in line with the build. It does not add new design
(the active-play / clicker proposal is a separate, later pass).

Rules for applying:
- Each block: find the **Old** text in the named section (exact quote from the GDD), replace it with **New**.
- "Insert after" blocks add a new paragraph; nothing is deleted.
- Comment anchors: [a] "@Devanshi Please check this first-time onboarding flow" is on the heading `2.1 Onboarding`
  (keep the heading text exactly). [b] "Economic systems design in PRIORITY!!" is on the §5 paragraph that starts
  `Economy values (first draft, November 1, 2026)` (never replaced; this pass does not touch it).
- MVP-only cuts stay in §9 / the MVP Scope tab. The full design in §1–8 is not cut back.
- New in this version: every open marker gets an ID (`TO CONFIRM (Q7): ...`) and §11 Open Items lists them all.

---

## Header

**Old:** `GDD Version: 0.8`
**New:** `GDD Version: 0.9`

**Old:** `Updated: November 1, 2026`
**New:** `Updated: November 21, 2026`

**Old:** `Game Name: [ ] `
**New:** `Game Name: [ ] (Q1)`

**Old:** `Typical session length: [ ]`
**New:** `Typical session length: [ ] (Q2)`

**Old:** `Distribution and business model: [ ]`
**New:** `Distribution and business model: [ ] (Q3)`

---

## 1.1 Experience Principles

**Old:** `TO CONFIRM: brief interaction, low distraction, visible progress, and reward browsing around breaks.`
**New:** `TO CONFIRM (Q4): brief interaction, low distraction, visible progress, and reward browsing around breaks. Playtest feedback (November 2026): the start feels too passive; how much active play the opening needs is open.`

---

## 1.2 Research References (table row "Spirit City: Lofi Sessions", column "Our differentiation")

**Old:** `Prioritize practical utility. The tool menu sits below the forward view and follows the glasses; the island sits to the right and requires a deliberate head turn.`
**New:** `Prioritize practical utility. The toolbar sits at the bottom of the view and follows the glasses; the island sits to the right and requires a deliberate head turn.`

---

## 2.1 Onboarding (list items; keep the heading, comment [a])

**Old:** `Tool menu: locate the menu below the forward view.`
**New:** `Toolbar: swipe along the toolbar at the bottom of the view.`

**Old:** `View the island: turn right; the character briefly searches and discovers the tutorial chest.`
**New:** `View the island: turn right (an arrow at the right edge points the way); the character briefly searches and discovers the tutorial chest.`

**Old:** `Open the blind pack: receive five cards.`
**New:** `Open the blind pack: tap it in the reward window and receive five cards.`

### Insert after the list item that starts `Skip: available throughout.`

**Insert after** that paragraph, as a new paragraph:
`Presentation: an intro card first, then one framed hint per step in the bottom zone, marked "TUTORIAL k/n", with a blinking arrow over the toolbar icon to use; an end card closes it. Each step advances on the action it teaches. Skip: double tap while no panel is open asks "Skip tutorial?"; tap confirms. The current step is saved, so a closed app resumes at the same step.`

---

## 2.2 Re-entry and Daily Use

**Old:** `Re-entry: select Start to restore saved data and settle offline earnings; Pomodoro resets to its unstarted state.`
**New:** `Re-entry: every launch shows the Start screen (Start button and coin counter). After a real exit (Section 6) an offline earnings window comes first: time away, coins earned and chests found; tap to continue, and the coin counter on the Start screen counts up to the new balance. A fresh install or a Reset goes straight to the Start screen. Onboarding (2.1) begins after Start. Pomodoro resets to its unstarted state.`

**Old:** `Settlement presentation: after a real exit (Section 6), one settle toast on Start shows coins earned while away, with 10 decimal places (e.g. 0.0277777778 for a short absence). Everywhere else coins show as whole numbers.`
**New:** `Settlement presentation: the offline earnings window shows coins earned while away with 5 decimal places (e.g. 0.02778 for a short absence); logs keep 10. Everywhere else coins show as whole numbers.`

**Old:** `TO CONFIRM: reward notifications and whether to provide session summaries.`
**New:** `TO CONFIRM (Q5): reward notifications and whether to provide session summaries.`

---

## 3.1 Spatial Layout and Anchors

**Old:** `Layout: the tool menu follows the glasses below the forward view; zone tasks appear at the upper left; the island is placed to the right, outside straight-ahead view.`
**New:**
`Layout (head-locked HUD, RayNeo zones): the clock and the coin counter side by side at the upper left, at the same level (same row, same text size); coins/h and then the current zone's tasks below them; toasts at the top center; the toolbar at the bottom; chest search status and progress bar above the toolbar. The island is a flat treasure map to the right, outside straight-ahead view (Section 4).`
`Island placement: the map follows the body, not the head. It sits 55° to the right of the body direction, about 8° below eye level, at 2.9 m. Looking right at it never moves it; after facing a new direction for 1.5 s it moves to keep 55° to the right of that direction. A blinking arrow at the right edge says "look right" while the map is out of view and a chest waits. Settings offers Recenter map.`

**Old:** `Anchors: up to four per user, each with its own tasks. Entering a zone shows its tasks; all tasks remain accessible in To-do. Anchors do not gate income or exploration; Pomodoro is independent of anchors (Section 3.3).`
**New:**
`Anchors: up to four per user, each with its own tasks. Entering a zone shows its tasks; all tasks remain accessible in To-do. Anchors do not gate income or exploration; Pomodoro is independent of anchors (Section 3.3).`
`Anchor slots: each anchor is a numbered slot with a name. The player places it where they stand (no hit test on the glasses; placing again replaces it) and names it from presets (Work, Study, Office, Desk, Kitchen, Bedroom, Living room, Gym; works offline) or by voice through the system To-do flow (needs network). Renaming rewrites the zone prefix of that slot's open todos (3.2). Zone detection: enter within 1 m for 2 s, exit beyond 2 m.`

**Old:** `TO CONFIRM: zone size, overlap priority, persistence, and island angle, distance, scale and repositioning.`
**New:** `TO CONFIRM (Q6): overlap priority and anchor persistence across sessions. Zone size and island placement are prototype values (above), to be re-tuned after playtests.`

---

## 3.2 Clock and To-do

**Old:** `Tools: clock, To-do and Pomodoro. Pomodoro interaction references Spirit City.`
**New:** `Tools: the clock, To-do and Pomodoro. The clock is always on the HUD, at the upper left next to the coin counter and at the same level (Section 3.1): the glasses' system time as HH:mm, updated every minute, no interaction. The toolbar holds six icons: Anchors, To-do, Collection, Profile, Pomodoro and Settings. Swipe moves the focus, the focused icon shows its label, tap opens it. Pomodoro interaction references Spirit City.`

**Old:** `Task rewards: each completed To-do grants a fixed coin reward, independent of complexity. Amounts belong in the economy design.`
**New:** `Task rewards: each completed To-do grants a fixed coin reward, independent of complexity, paid once per todo (finishing it again after reopening it pays nothing). Amounts belong in the economy design. To-do panel: the current zone's todos plus a "New to-do" row; tap a todo to finish it, tap "New to-do" to create one by voice.`

**Old:** `TO CONFIRM: General To-do accessible everywhere without staying expanded; entry point. TO VERIFY: provider todoEdit on device (Section 8).`
**New:**
`General todos show in every zone and are the only ones shown outside all zones.`
`Verified on device (2026-11-07): the provider todoEdit renames todos from the game; the system To-do app shows the new text.`
`TO CONFIRM (Q7): offline task entry, empty-list and error handling.`

**Old:** `TO CONFIRM: offline task entry, task selection, empty-list and error handling.`
**New:** *(delete this line; merged into Q7 above)*

---

## 3.3 Pomodoro and Rest Reminders

**Old:** `TO CONFIRM: reminder after 90 minutes of continuous work; appearance, repeat interval, dismissal and timer reset conditions.`
**New:** `TO CONFIRM (Q8): reminder after 90 minutes of continuous work; appearance, repeat interval, dismissal and timer reset conditions.`

---

## 3.4 Input and Lists

**Old:** `TO CONFIRM: screen hierarchy, focus, wireframes, recovery, accessibility and notification presentation.`
**New:**
`Panels: each tool opens a panel over the HUD; panels with sections use three boxes side by side (Anchors: slots 1–3; Collection: the three rarities; Profile: Skills / Look / Pets). Swipe moves between boxes, tap opens one, double tap goes back one level and then closes the panel. Modal windows (reward window, confirmations) take input until closed. The map takes input only while it is in view (within 20° of the head direction).`
`TO CONFIRM (Q9): recovery, accessibility and notification presentation.`

---

## 4. Island Exploration and Collection

**Old:** `Collection display: cards not owned show as silhouettes with a ≥2 px outline (black alone is invisible on the glasses); owned cards show in full color. Reference: Kingdom Two Crowns' silhouette-to-detail unlock.`
**New:** `Collection display: the overview shows one box per rarity with "owned / total" and an "Open pack (N)" row while packs wait; a rarity page shows that rarity's cards in a grid with an info pane (name, copies, NEW). Cards not owned show as silhouettes with a ≥2 px outline and "?" (black alone is invisible on the glasses); owned cards show in full color; a NEW dot stays until the card is focused once. Reference: Kingdom Two Crowns' silhouette-to-detail unlock.`

**Old:** `Chest progress bar: a horizontal bar below the island shows search progress toward the next chest; it stays full with "Chests full" when all three slots hold chests.`
**New:** `Chest progress bar: a horizontal bar below the island shows search progress toward the next chest, with a status label ("Treasure hunt 42%"); it stays full with "Chests full - open one" when all three slots hold chests, and the label reads "Tap to open" while a found chest is focused.`

### Insert after the paragraph that starts `Chest rewards: three rewards appear together`

`Reward window: opening a chest shows a window over the HUD with the rewards appearing one after another. A blind pack inside it opens with a tap and its five cards appear right after it. Tap skips the animation, opens the next pack or closes; double tap closes, and an unopened pack waits in the Collection.`

**Old:** `First card set: 9 cards, 3 per rarity (3 Common / 3 Treasured / 3 Special), filling a 3×3 collection grid. 8 artworks exist; at least 1 more is needed.`
**New:** `First card set: 54 cards, 18 per rarity (18 Common / 18 Treasured / 18 Special), shown as a 6 × 3 grid per rarity. The prototype uses placeholder pixel art drawn in code.`

**Old:** `TO CONFIRM: artwork-to-rarity mapping, card inventory beyond the first set, long-term collection capacity (paged grid) and goals; cat memes are a Special-content candidate.`
**New:** `TO CONFIRM (Q10): card artwork for the 54 cards (source, style, rarity mapping), card inventory beyond the first set and long-term collection goals; cat memes are a Special-content candidate.`

**Old:** `Pets: a character customization bought in the shop. A pet always follows the character and counts as a skin.`
**New:** `Character looks: outfits (including male and female clothes) and clothing colors, in the Profile tool. Some are free, the rest are bought with coins; a locked option previews on the character before buying. Pets: a character customization bought in the Profile tool. A pet always follows the character on the map and counts as a skin. Looks and pets have no stat effect.`

---

## 5. Coins, Upgrades and Login Rewards

**Old:** `Coin spending: upgrades, blind packs and shop cosmetics (skin colors, pets, island skins, decorations).`
**New:** `Coin spending: upgrades (Profile → Skills), blind packs ("Buy a pack" in the Collection; the pack opens at once) and shop cosmetics (outfits, clothing colors, pets in Profile → Look / Pets; island skins and decorations later).`

---

## 6. Offline Progress and Persistence

**Old:** `Real exit vs pause: only closing the app or a system kill counts as an exit; the settle toast appears on the next Start.` (start of the paragraph)
**New:** replace `the settle toast appears on the next Start` with `the offline earnings window appears on the next Start`

**Old:** `TO CONFIRM: anchor restoration (Section 8, AnchorZone test 3).`
**New:** `TO CONFIRM (Q6): anchor restoration (Section 8, AnchorZone test 3).` (same open item as 3.1)

---

## 7. Art and Audio

**Old:** `Visual style (MVP): 2D looping sprite animation for the island, character search and chests, to lower development cost and power use. 3D island and character may be considered later.`
**New:**
`Visual style: line art and pixel art on black, in the RayNeo palette: cyan #4DE2FF as the main color, green #44FF66 as the accent, gray-cyan #7FA8B8 for secondary text. Rarity colors: silver (Common), gold (Treasured), purple (Special). Red only for destructive warnings. The island is a flat line-art treasure map with a walking explorer, chests and dashed search areas; 2D animation only, to lower development cost and power use. 3D island and character may be considered later.`
`UI is built in code with its own pixel art, following the RayNeo design spec (decision D8, November 7, 2026). A first muted parchment palette was rejected on device.`

**Old:** `TO CONFIRM: typography, island skin designs (Zen garden/seasons), map unlock visuals/conditions and asset ownership/licensing/credits, including the 9th card artwork.`
**New:** `TO CONFIRM (Q11): typography, island skin designs (Zen garden/seasons), map unlock visuals/conditions and asset ownership/licensing/credits.`

**Old:** `TO CONFIRM: Lo-fi, SFX, triggers/mixing/mute, roles, budget and delivery; inclusion of external music, notification blocking, app integrations, calendar and daily summaries.`
**New:** `TO CONFIRM (Q12): Lo-fi, SFX, triggers/mixing/mute, roles, budget and delivery; inclusion of external music, notification blocking, app integrations, calendar and daily summaries.`

---

## 8. Technical Records and Validation

**Old:** `Record scope: October 18–19 implementation records and constraints, not evidence of current acceptance. Prototype status remains TBD.`
**New:** `Record scope: implementation records and device findings up to November 7, 2026. Prototype status: the full MVP loop runs on the glasses (branch Yaxin-Mu/Prototype, four device test rounds on November 7); not yet merged to main.`

**Old:** `Stack: Unity 2022.3.36f1, Android, RayNeo OpenXR ARDK 1.1.2. Planned architecture: ModeManager events, IdleCore, OfflineProgress, SaveSystem (per-slot saves), an injectable game clock (IGameClock: real clock, test clock), InputAdapter and HUD.`
**New:** `Stack: Unity 2022.3.36f1, Android, RayNeo OpenXR ARDK 1.1.2 (embedded in the repository as a package). Built architecture: ModeManager events, IdleCore (plain C#), offline settle, SaveSystem (JSON per slot, temp file + backup), an injectable game clock (IGameClock), InputAdapter (one input route, handler stack) and a code-built HUD with panels.`

### Insert after the paragraph that starts `Verified on device (2026-10-19)`

`Verified on device (2026-11-07): todoEdit works from the game (zone prefixes, D3). startTodo also works while the app runs in SLAM mode: the voice screen opens and returns by itself, SLAM keeps tracking and anchors stay valid. One temple swipe fires two step events (usually < 40 ms apart, sometimes > 120 ms); the game ignores a second step within 0.18 s. Rapid taps closer than 0.4 s merge into a double or triple tap (ARDK touch setting), so the game cannot use fast repeated tapping.`

**Old:** `TO VERIFY: anchor accuracy/drift/re-entry/restoration (AnchorZone tests 1–4; no relocalization API exists, so the R1 manual zone pick is the fallback), provider todoEdit for zone prefixes (D3), startTodo in SLAM mode and SLAM recovery afterwards, launcher bottom layout / QUIT / double-tap back.`
**New:** `TO VERIFY (Q13): anchor accuracy/drift/re-entry/restoration (AnchorZone tests 1–4; no relocalization API exists, so the R1 manual zone pick is the fallback); deleteTodo from the game (used by the voice anchor rename); shop, Look and Pets on device.`

**Old:** `TO VERIFY: viewing distance/scale/comfort and island performance (after W3). Continuous-6DoF power/heat is deferred (D2): the MVP keeps SLAM on and focuses on gameplay; re-evaluate before release.`
**New:** `TO VERIFY (Q14): viewing comfort of the map placement over a long session and island performance. Continuous-6DoF power/heat is deferred (D2): the MVP keeps SLAM on and focuses on gameplay; re-evaluate before release.`

**Old:** `TO CONFIRM: tracking-loss fallback, recalibration, use environment, permissions/network/privacy/analytics and integration plan.`
**New:** `TO CONFIRM (Q15): tracking-loss fallback, recalibration, use environment, permissions/network/privacy/analytics and integration plan.`

**Old:** `TO CONFIRM: one-minute onboarding and 20-minute comfort targets; participants, procedure, usability/distraction/reward-satisfaction measures, thresholds, owner and results.`
**New:** `TO CONFIRM (Q16): one-minute onboarding and 20-minute comfort targets; participants, procedure, usability/distraction/reward-satisfaction measures, thresholds and results.`

---

## 9. Production Scope and Ownership

**Old (heading):** `9. Production Scope and Ownership`
**New (heading):** `9. Production Scope`

**Old:** `For shippable version: [ ]`
**New:** `For shippable version: [ ] (Q17)`

**Old:** `MVP scope, decisions, work breakdown, milestones, acceptance, risks and fallbacks: MVP Scope tab. TO CONFIRM: screen count, asset inventory and dates.`
**New:** `MVP scope, decisions, work breakdown, milestones, acceptance, risks and fallbacks: MVP Scope tab. TO CONFIRM (Q18): asset inventory and dates.`

**Old:** `Recorded workflow: feature branches and PRs; one editor per Unity scene. Yaxin's work includes anchors, voice To-do and launcher; Devanshi's includes Canvas integration.`
**New:** `Recorded workflow: feature branches and PRs; one editor per Unity scene. The UI is code-built (D8), so there is no Canvas integration step; the device scene is generated by an editor tool. The GDD does not assign tasks: owners, schedule and progress live in the team's Trello board.`

**Old:** `TO CONFIRM: final owners/reviewers, Canvas integration ownership, capacity, schedule and production tracker.`
**New:** *(delete this line; task assignment is not part of the GDD)*

**Old:** `Decisions: unresolved items appear in their relevant sections in yellow. Update the rule when agreed; record owner, date, rationale and affected sections as needed.`
**New:** `Decisions: unresolved items appear in their relevant sections in yellow. Update the rule when agreed; record date, rationale and affected sections as needed.`

---

## 10. Terms and Version

**Old:** `Version: v0.8, November 1, 2026. Applied MVP decisions: card reveal and collection silhouettes, zone-tagged To-dos, real exit vs pause, time source, Reset, 9-card first set.`
**New:**
`Version: v0.9, November 21, 2026. Synced with the prototype: toolbar and HUD layout, body-following map, anchor slots and naming, Start screen and offline earnings window (5 decimals), reward window, 54-card first set, Profile (upgrades, looks, pets) and pack purchase, RayNeo palette, code-built UI (D8). Open items now have IDs (Section 11).`
`v0.8, November 1, 2026: card reveal and collection silhouettes, zone-tagged To-dos, real exit vs pause, time source, Reset, 9-card first set.`

---

## 11. Open Items (new section; insert after Section 10)

Rows are closed, never deleted. Type: C = TO CONFIRM, V = TO VERIFY, F = empty field.

| ID | Item | Section | Type | Blocks | Status |
|---|---|---|---|---|---|
| Q1 | Game name | Header | F | Store page, title screen | Open |
| Q2 | Typical session length | Header | F | Economy player profile, playtest plan | Open |
| Q3 | Distribution and business model | Header | F | Shippable scope | Open |
| Q4 | Experience principles; how much active play the opening needs (playtest feedback) | 1.1 | C | Early economy, onboarding, next design pass | Open |
| Q5 | Reward notifications, session summaries | 2.2 | C | HUD toasts | Open |
| Q6 | Anchor overlap priority and persistence across sessions | 3.1 | C | R1 fallback, re-entry | Open |
| Q7 | Offline task entry, empty list, error handling | 3.2 | C | To-do panel | Open |
| Q8 | Rest reminder after 90 min of work | 3.3 | C | Pomodoro (v2) | Open |
| Q9 | Recovery, accessibility, notification presentation | 3.4 | C | HUD | Open |
| Q10 | Card artwork for 54 cards, inventory beyond the first set, long-term goals | 4 | C | Collection art, economy pool size | Open |
| Q11 | Typography, island skins, map unlocks, asset licensing | 7 | C | Shop skins, credits | Open |
| Q12 | Audio and integrations | 7 | C | Audio | Open |
| Q13 | Anchor accuracy / restoration; deleteTodo; shop, Look, Pets on device | 8 | V | Voice rename, R1, demo step 12 | Open |
| Q14 | Map viewing comfort over a long session; island performance | 8 | V | Map placement | Open |
| Q15 | Tracking loss, recalibration, permissions / privacy / analytics | 8 | C | Release | Open |
| Q16 | Playtest targets, measures, results | 8 | C | User study | Open |
| Q17 | Shippable version scope | 9 | F | Post-MVP plan | Open |
| Q18 | Asset inventory and dates | 9 | C | Milestones | Open |
| — | Settlement presentation (was 2.2) | 2.2 | C | — | Closed v0.9: offline earnings window, 5 decimals |
| — | Zone size, island angle / distance / scale / repositioning (was 3.1) | 3.1 | C | — | Closed v0.9: prototype values recorded |
| — | Provider todoEdit; startTodo in SLAM (was 3.2 / 8) | 3.2, 8 | V | — | Closed v0.9: verified on device 2026-11-07 |
| — | Screen hierarchy and focus (was 3.4) | 3.4 | C | — | Closed v0.9: panel / box / modal rules recorded |
| — | 9th card artwork (was 4 / 7) | 4, 7 | C | — | Closed v0.9: superseded by the 54-card set (Q10) |
| — | Canvas integration ownership (was 9) | 9 | C | — | Closed v0.9: D8 code-built UI |
| — | Final owners / reviewers, capacity, schedule, production tracker (was 9) | 9 | C | — | Closed v0.9: the GDD does not assign tasks (Trello) |

---

## Check (done on the local text)
- Every marker in tab 1 now carries a Q# (Q1–Q18) and has an Open row; closed markers are listed as Closed rows.
- No commented paragraph is replaced: heading `2.1 Onboarding` keeps its text; the §5 `Economy values ...` paragraph is untouched.
- Anchors stay "up to four per user" in the full design; the MVP keeps 3 (MVP Scope tab).
- Pomodoro stays in the full design (icon only in the MVP).
- The clock is a HUD element next to the coin counter (3.1, 3.2), not a toolbar icon.
- No task assignment anywhere in the GDD: the §9 heading drops "Ownership", the owners marker is removed, the Open Items table has no Owner column.
