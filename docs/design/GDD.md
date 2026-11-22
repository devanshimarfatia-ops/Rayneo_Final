# AR IDLE GAME — GAME DESIGN DOCUMENT

> Markdown copy of the GDD tab of the Google Doc (master):
> https://docs.google.com/document/d/1neT60ecY91JCXXXcK3oA2VLGAo85USD44nZEfYv5wss
> This file is the whole-game design. Phase plans live in the scope docs next to it (`MVP_Scope.md`, `Phase2_Scope.md`);
> the economy lives in `Economy_Design.md`. Keep this file and the Google Doc in sync.

**Game Name:** [ ] (Q1)

**GDD Version:** 1.0

**Updated:** November 22, 2026

**Project Team Members:** Devanshi Marfatia, Yaxin Mu

## 1. Game Overview

**Genre:** Lightweight AR idle game.

**Platform:** RayNeo X3 Pro; Unity-based Android application.

**Player mode:** single-player, for individual use.

**Core concept:** real-world work is connected to island exploration and collection, supported by productivity tools and an anchored work zone.

**Spatial interaction:** see Section 3 for tools, zone tasks and island layout.

**Collection direction:** explore an island and collect artwork cards.

**Audience:** people who are easily distracted, heavy smartphone users, and people with time-management difficulties, including ADHD users. Intended benefits are productivity and stress management; these are product goals, not established health outcomes.

**Positioning:** a utility-first lightweight AR idle game. Practical usefulness takes priority; gameplay provides motivation, while the tools support management. Gameplay is secondary to the utility experience.

**One-sentence public pitch:** A lightweight AR productivity companion that helps you manage daily tasks while a small island explores, earns coins, and uncovers art collectibles beside you.

**Typical session length:** [ ] (Q2)

**Distribution and business model:** [ ] (Q3)

## 1.1 Experience Principles

**TO CONFIRM (Q4):** brief interaction, low distraction, visible progress, and reward browsing around breaks. Playtest feedback (November 2026): the start feels too passive; how much active play the opening needs is open. Direction chosen (2026-11-22): trash clearing and gradual automation (Section 4); tuning is open.

## 1.2 Research References

Recorded references, grouped by research purpose. These references do not imply adopted features.

**Focus and task management**

- Spirit City: Lofi Sessions — Steam
- Pomodoro: Focus Timer — Steam
- Forest — Official website
- Focus To-Do — Official website

**Desktop companions**

- Bongo Cat — Steam
- Ropuka's Idle Island — Steam
- Tiny Pasture — Steam

**Idle collection**

- Chillquarium — Steam
- Travel Frog / 旅かえる — Google Play

**Incremental progression**

- (the) Gnorp Apologue — Steam

**Island design**

- Kingdom Two Crowns — Steam

**Specific lessons and differentiation:**

| Reference | Specific lessons | Our differentiation |
|---|---|---|
| Spirit City: Lofi Sessions | Combine productivity tools, Pomodoro and collection rewards. | Prioritize practical utility. The toolbar sits at the bottom of the view and follows the glasses; the island sits to the right and requires a deliberate head turn. |
| Ropuka’s Idle Island | Small-island companionship and coin-funded progression. | Present the island in real space and connect real-world work to it through work-zone detection and time recording. |
| Forest | Turn work time into visible, cumulative results. | Use island exploration, chests and card collection, with coins, cards and blind packs as rewards.<br>Differentiation: ongoing idle accumulation; offline rules are specified in Section 6. |
| Focus To-Do | Bring task lists and time management into the same utility workflow. | Glasses tools and voice To-do reduce phone checking. See Section 3 for interaction rules. |
| Kingdom Two Crowns | Primarily reference the unlock method: locked areas show as silhouettes and gain detail once unlocked. | Apply the same silhouette-to-detail unlock to the card collection: cards not owned show as silhouettes, owned cards show in full detail (see Section 4). |

# 2. User Flow

**Core loop:** idle → passive income and exploration → discover and open chests → collect cards → spend coins on upgrades and cosmetics.

## 2.1 Onboarding

- **Basic controls:** tap to confirm, swipe to select, double-tap to go back.
- **Set an anchor:** create and name the first zone.
- **Add a To-do:** create a zone task by voice and see it at the upper left. Voice is never required: without network, the player picks an existing system todo instead.
- **Toolbar:** swipe along the toolbar at the bottom of the view.
- **View the island:** turn right (an arrow at the right edge points the way); the character briefly searches and discovers the tutorial chest.
- **Clear trash:** the island starts with 3 trash. Aim at each with the reticle and tap; the explorer clears it. A hint explains that trash comes back over time and that a full island stops exploring; one new trash appears on purpose so the player sees it.
- **Open the chest:** receive one coin reward, one Common card and one blind pack together.
- **Open the blind pack:** tap it in the reward window and receive five cards.
- **View the collection:** inspect the new cards and learn to return.
- **Meet Pomodoro:** open its introduction and dismiss it with “Got it”; starting the timer is optional.
- **Finish:** enter normal use.

**Skip:** available throughout. An unopened tutorial chest stays on the island for later opening; rewards are not automatically granted.

**Presentation:** an intro card first, then one framed hint per step in the bottom zone, marked "TUTORIAL k/n", with a blinking arrow over the toolbar icon to use; an end card closes it. Each step advances on the action it teaches. Skip: double tap while no panel is open asks "Skip tutorial?"; tap confirms. The current step is saved, so a closed app resumes at the same step.

**Replay:** accessed in Settings, reusing anchors and tasks with prompts only. Chest and pack demonstrations grant no additional rewards. Omit replay if it materially harms runtime performance.

## 2.2 Re-entry and Daily Use

**Re-entry:** every launch shows the Start screen (Start button and coin counter). After a real exit (Section 6) an offline earnings window comes first: time away, coins earned and chests found; tap to continue, and the coin counter on the Start screen counts up to the new balance. A fresh install or a Reset goes straight to the Start screen. Onboarding (2.1) begins after Start. Pomodoro resets to its unstarted state.

**Daily use:** view To-do, use Pomodoro or turn to the island. See Section 3 for spatial layout and zone tasks.

**Settlement presentation:** the offline earnings window shows coins earned while away with 5 decimal places (e.g. 0.02778 for a short absence); logs keep 10. Everywhere else coins show as whole numbers.

**TO CONFIRM (Q5):** reward notifications and whether to provide session summaries.

# 3. Space, Tools and Controls

## 3.1 Spatial Layout and Anchors

**Layout (head-locked HUD, RayNeo zones):** the clock and the coin counter side by side at the upper left, at the same level (same row, same text size); coins/h and then the current zone's tasks below them; toasts at the top center; the toolbar at the bottom; chest search status and progress bar above the toolbar. The island is a flat treasure map to the right, outside straight-ahead view (Section 4).

**Island placement:** the map follows the body, not the head. It sits 55° to the right of the body direction, about 8° below eye level, at 2.9 m. Looking right at it never moves it; after facing a new direction for 1.5 s it moves to keep 55° to the right of that direction. A blinking arrow at the right edge says "look right" while the map is out of view and a chest waits. Settings offers Recenter map.

**Map size:** S, M or L (2.9 / 2.4 / 2.0 m; default M), a display preference in Settings, saved per device.

**Anchors:** up to four per user, each with its own tasks. Entering a zone shows its tasks; all tasks remain accessible in To-do. Anchors do not gate income or exploration; Pomodoro is independent of anchors (Section 3.3).

**Anchor slots:** each anchor is a numbered slot with a name. The player places it where they stand (no hit test on the glasses; placing again replaces it) and names it from presets (Work, Study, Office, Desk, Kitchen, Bedroom, Living room, Gym; works offline) or by voice through the system To-do flow (needs network). Renaming rewrites the zone prefix of that slot's open todos (3.2). Zone detection: enter within 1 m for 2 s, exit beyond 2 m.

**TO CONFIRM (Q6):** overlap priority and anchor persistence across sessions. Zone size and island placement are prototype values (above), to be re-tuned after playtests.

## 3.2 Clock and To-do

**Tools:** the clock, To-do and Pomodoro. The clock is always on the HUD, at the upper left next to the coin counter and at the same level (Section 3.1): the glasses' system time as HH:mm, updated every minute, no interaction. The toolbar holds six icons: Anchors, To-do, Collection, Profile, Pomodoro and Settings. Swipe moves the focus, the focused icon shows its label, tap opens it. Pomodoro interaction references Spirit City.

**Zone-task example:** Work shows homework and drawing; Kitchen shows defrosting meat and washing grapes. Other zones' tasks do not appear in the current zone overlay.

**Task rewards:** each completed To-do grants a fixed coin reward, independent of complexity, paid once per todo (finishing it again after reopening it pays nothing). Amounts belong in the economy design. To-do panel: the current zone's todos plus a "New to-do" row; tap a todo to finish it, tap "New to-do" to create one by voice.

**Zone tagging:** a todo created in game inside a zone is renamed to "(Zone)-text" right after creation (e.g. "(Work)-Write report"), so the system To-do app shows the zone too. The game reads the prefix, shows the todo only in that zone and hides the prefix. Todos added outside the game (system To-do app) or with a broken prefix are General and show in every zone. The zone lives in the system todo itself, so a game reset or reinstall keeps it.

General todos show in every zone and are the only ones shown outside all zones.

**Verified on device (2026-11-07):** the provider todoEdit renames todos from the game; the system To-do app shows the new text.

**TO CONFIRM (Q7):** offline task entry, empty-list and error handling.

## 3.3 Pomodoro and Rest Reminders

**Pomodoro state:** Pomodoro is an independent tool. It is not tied to any anchor or work zone, keeps running when the player leaves a zone, and looks the same everywhere. Default: 45-minute work, 15-minute break; the player can pause and skip. Ending it or reopening the app resets it. Pauses do not count as active work time.

**Completion rewards:** full completion earns the full reward. Ending early after at least 30% of the configured work duration has actively elapsed earns a reward pro rata to the active focus time (same coins per minute); below 30% earns none. Passive income and exploration do not pause with Pomodoro.

**Skip:** skip means ending the current work segment early. Below 30% of the work duration, a confirmation prompt tells the player that ending now gives no reward.

**Rest:** no mandatory breaks or break-gated exploration.

**TO CONFIRM (Q8):** reminder after 90 minutes of continuous work; appearance, repeat interval, dismissal and timer reset conditions.

## 3.4 Input and Lists

**Controls:** tap to confirm, double-tap to go back/exit; horizontal swipes change focus, vertical swipes scroll lists/adjust parameters. Physical forward swipe maps to previous/left; long press and the shortcut key are system-reserved.

**Lists:** at most six items visible at once; scroll for more. This is not a total-content limit.

**Panels:** each tool opens a panel over the HUD; panels with sections use three boxes side by side (Anchors: slots 1–3; Collection: the three rarities; Profile: Skills / Look / Pets). Swipe moves between boxes, tap opens one, double tap goes back one level and then closes the panel. Modal windows (reward window, confirmations) take input until closed. The map takes input only while it is in view (within 20° of the head direction).

**Map pointer:** looking at the map shows a reticle (the SDK gaze dot is hidden). Aiming at a chest or trash draws a cyan frame and a hint; a temple tap runs it. The target is the one under the reticle when the finger touches the temple (the tap itself arrives about 0.4 s later). While the player aims, the map does not follow the body. An open panel is hidden while the map is in view and comes back when the player looks away.

**TO CONFIRM (Q9):** recovery, accessibility and notification presentation.

# 4. Island Exploration and Collection

**Exploration:** the character searches for chests. The island has three chests that refresh during use. The player opens discovered chests. Offline handling is in Section 6.

**Chest rewards:** three rewards appear together, each coins, one card or one blind pack. Repeated types are allowed; the minimum outcome is three coin rewards.

**Reward window:** opening a chest shows a window over the HUD with the rewards appearing one after another. A blind pack inside it opens with a tap and its five cards appear right after it. Tap skips the animation, opens the next pack or closes; double tap closes, and an unopened pack waits in the Collection.

**Chest progress bar:** a horizontal bar below the island shows search progress toward the next chest, with a status label ("Treasure hunt 42%"); it stays full with "Chests full - open one" when all three slots hold chests, and the label reads "Tap to open" while a found chest is focused.

**Blind packs:** each contains five cards. New cards enter the collection; duplicates convert to coins under Section 5.

**Card categories:** Common (silver-gray), Treasured (gold), Special (purple); Special is the highest rarity.

**Card reveal:** cards show directly when a chest or blind pack opens; no mosaic, blur or fade-in reveal.

**Collection display:** the overview shows one box per rarity with "owned / total" and an "Open pack (N)" row while packs wait; a rarity page shows that rarity's cards in a grid with an info pane (name, copies, NEW). Cards not owned show as silhouettes with a ≥2 px outline and "?" (black alone is invisible on the glasses); owned cards show in full color; a NEW dot stays until the card is focused once. Reference: Kingdom Two Crowns' silhouette-to-detail unlock.

**Chest spawn:** the island has three chest areas, one per chest. Each chest spawns at a random position inside its area. While the character searches, the exact spot is hidden: only the area is shown, outlined with a dashed line and marked with a "?". When the search time ends, the chest drops into place and the dashed outline disappears.

**Search time and rewards:** discovery time, chest reward weights and rarity guarantees follow the economy (Section 5 and the Economy tab). Opening: aim at the found chest and tap (map pointer, Section 3.4).

**First card set:** 54 cards, 18 per rarity (18 Common / 18 Treasured / 18 Special), shown as a 6 × 3 grid per rarity. The prototype uses placeholder pixel art drawn in code.

**Island:** one island only. Island themes are skins sold in the shop.

### Trash and gradual automation

**Trash and gradual automation:** trash appears on the island at one piece every 30 minutes, also while the app is closed. The island holds 48 (24 hours of trash, the same as the offline cap). When it is full, exploration stops until the player clears some.

**Clearing trash:** aim at trash and tap. The explorer stops exploring, walks there and clears it; the coins it pays float over his head ("+5"). Several taps queue up. Trash beats exploring. Clearing pays a small amount (Economy tab).

**Exploration starts manual:** the player sends the explorer with a tap, which earns the idle coins. An early upgrade unlocks automatic exploration. Pets bought with coins pick up trash on their own.

**Chests:** early on only the tutorial chest exists; after an upgrade, exploration finds chests, and chest contents shift from mostly coins to mostly cards as the player progresses.

**Trash style:** 48 single pieces (recommended) or 12 piles of 4, a display preference in Settings.

**Long term:** upgrades and decorations bought with coins take the island from wild land through stone age and iron age to a cyber island (reference: Kingdom Two Crowns). No crafting materials.

**TO CONFIRM (Q19):** what shifts chest contents from coins to cards (Luck upgrade, era, or both) and when exploration starts finding chests.

**TO CONFIRM (Q20):** pets and trash: offline pickup, speed per pet, and whether "auto clean" stays a separate upgrade.

**TO CONFIRM (Q21):** pay for one manual exploration; era list and era unlocks.

**TO CONFIRM (Q10):** card artwork for the 54 cards (source, style, rarity mapping), card inventory beyond the first set and long-term collection goals; cat memes are a Special-content candidate.

**Out of scope:** ambient events, desk objects, room building and plants.

**Later option:** short break-time puzzles (e.g. riddles and brain teasers) that give the player a change of pace during rest.

**Character looks:** outfits (including male and female clothes) and clothing colors, in the Profile tool. Some are free, the rest are bought with coins; a locked option previews on the character before buying. Pets: a character customization bought in the Profile tool. A pet always follows the character on the map and counts as a skin. Looks have no stat effect; pets also pick up trash (see Trash and gradual automation).

# 5. Coins, Upgrades and Login Rewards

**Coin sources:** automatic idle-time accumulation and collection, plus chests, duplicate cards, To-do, Pomodoro, daily login and clearing trash.

**Coin spending:** upgrades (Profile → Skills), blind packs ("Buy a pack" in the Collection; the pack opens at once) and shop cosmetics (outfits, clothing colors, pets in Profile → Look / Pets; island skins and decorations later).

**Upgrades:** three independent coin-funded stats. Coin rate increases passive income; exploration ability shortens chest discovery time; luck changes coin/card/pack probabilities inside chests. Exploration and luck do not change passive coin rate.

**Duplicate conversion:** each card returns far less than pack price, slightly more at higher rarity. No separate pack cap; even five highest-rarity cards must return less than one new pack costs.

**Daily login:** each system-calendar date counts as a day and awards coins. Every five accumulated days awards cards, then repeats. 23:59 and the next 00:00 count as separate days.

**Economy values (first draft, November 1, 2026):** base income/exploration rate, upgrade prices/levels/curves, shop prices, conversion values, task/Pomodoro/login amounts, five-day card pool and drop rates are all drafted; see the Economy tab and Economy.xlsx (MVP subset in its section 8). Not playtested.

**Chest reward weights (draft):** coins:cards:packs = 78:20:2 (full design), 73:22:5 (MVP); the worst chest is 3 coin rewards. Details: Economy tab and Economy.xlsx.

# 6. Offline Progress and Persistence

**Offline coins:** saved coin rate × min(offline duration, 24 hours); a continuous-duration cap, not a daily reset.

**Offline trash:** trash keeps appearing while the app is closed (one per 30 minutes). After 24 hours the island is full and exploration stops, so the trash shows the offline cap.

**Offline exploration:** search only chests present at departure, then stop once all are discovered. No new chests spawn. Found chests await player opening; at most the existing three are involved.

**Catch-up:** save departure time, progress, undiscovered/discovered chests, coin rate and exploration ability. Calculate progress on Start from elapsed time; continuous background execution is unnecessary.

**Real exit vs pause:** only closing the app or a system kill counts as an exit; the offline earnings window appears on the next Start. Any pause (voice To-do, system dialogs, screen off, glasses removed) is the same session: on resume, the paused time is credited silently (24 h cap). If the app is killed while paused, the next Start settles from the pause save.

**No minimum away time:** any absence is settled. Coins are stored as fractional values.

**Save timing:** save on pause and every 30 s; settle exactly once per Start.

**Time source:** the glasses' system time; elapsed time is computed in UTC. If the clock is set back, elapsed time counts as 0. All game code reads time through one injectable clock.

**Reset:** Settings offers Reset with a second confirmation; it clears this game's save and anchors, not system todos. Saves are stored per slot internally; a multi-slot UI is later.

**Sync:** no cloud sync by default; saves stay on the glasses.

**TO CONFIRM (Q6):** anchor restoration (Section 8, AnchorZone test 3).

# 7. Art and Audio

**Interface language:** English.

**Card presentation:** see Section 4 (direct reveal; silhouettes until owned).

**TO CONFIRM (Q11):** typography, island skin designs (Zen garden/seasons), map unlock visuals/conditions and asset ownership/licensing/credits.

**Visual style:** line art and pixel art on black, in the RayNeo palette: cyan #4DE2FF as the main color, green #44FF66 as the accent, gray-cyan #7FA8B8 for secondary text. Rarity colors: silver (Common), gold (Treasured), purple (Special). Red only for destructive warnings. The island is a flat line-art treasure map with a walking explorer, chests and dashed search areas; 2D animation only, to lower development cost and power use. 3D island and character may be considered later.

UI is built in code with its own pixel art, following the RayNeo design spec (decision D8, November 7, 2026). A first muted parchment palette was rejected on device.

**TO CONFIRM (Q12):** Lo-fi, SFX, triggers/mixing/mute, roles, budget and delivery; inclusion of external music, notification blocking, app integrations, calendar and daily summaries.

# 8. Technical Records and Validation

**Record scope:** implementation records and device findings up to November 7, 2026. Prototype status: the full MVP loop runs on the glasses (branch Yaxin-Mu/Prototype, four device test rounds on November 7); not yet merged to main.

**Stack:** Unity 2022.3.36f1, Android, RayNeo OpenXR ARDK 1.1.2 (embedded in the repository as a package). Built architecture: ModeManager events, IdleCore (plain C#), offline settle, SaveSystem (JSON per slot, temp file + backup), an injectable game clock (IGameClock), InputAdapter (one input route, handler stack) and a code-built HUD with panels.

**Anchor prototype:** SLAM horizontal distance, placed at the user's position; 1 m entry with 2 s dwell, 2 m exit. These are prototype tuning values.

**Device record:** unavailable plane/depth/hit-test/object recognition and no persistent-anchor/relocalization API; SLAM status disagrees with moving poses, and screenshots/recording interrupt SLAM.

**Voice prototype:** online creation through foreground system To-do, then provider readback on return. Tested Android SpeechRecognizer was unavailable. A fallback launcher supports integration.

**Display guidance:** black background, 640×480 reference canvas, 30° FOV, type ≥16 px, lines ≥2 px, margins 16–30 px.

**Verified on device (2026-10-19):** To-do create (system startTodo voice flow) and complete (todoSetFinished) stay in sync with the system To-do app. Voice handoff works outside SLAM mode: the system voice screen opens on top, returns to the game by itself, and the new todo is found by re-reading the list. In-game delete is out of MVP scope (v2).

**Verified on device (2026-11-07):** todoEdit works from the game (zone prefixes, D3). startTodo also works while the app runs in SLAM mode: the voice screen opens and returns by itself, SLAM keeps tracking and anchors stay valid. One temple swipe fires two step events (usually < 40 ms apart, sometimes > 120 ms); the game ignores a second step within 0.18 s. Rapid taps closer than 0.4 s merge into a double or triple tap (ARDK touch setting), so the game cannot use fast repeated tapping.

**Rule (D4):** foreground/background accounting follows D4 (real exit vs pause); it is checked in the W2 and demo step 12 acceptance, not as a research item.

**TO VERIFY (Q13):** anchor accuracy/drift/re-entry/restoration (AnchorZone tests 1–4; no relocalization API exists, so the R1 manual zone pick is the fallback); deleteTodo from the game (used by the voice anchor rename); shop, Look and Pets on device.

**TO VERIFY (Q14):** viewing comfort of the map placement over a long session and island performance. Continuous-6DoF power/heat is deferred (D2): the MVP keeps SLAM on and focuses on gameplay; re-evaluate before release.

**Asset budgets (RayNeo design spec):** models <5,000 faces, textures ≤512 px.

**TO CONFIRM (Q15):** tracking-loss fallback, recalibration, use environment, permissions/network/privacy/analytics and integration plan.

**TO CONFIRM (Q16):** one-minute onboarding and 20-minute comfort targets; participants, procedure, usability/distraction/reward-satisfaction measures, thresholds and results.

# 9. Production Scope

**For demo showcase:** MVP, see the MVP Scope tab (10-minute demo from a fresh install, onboarding through one quit and re-entry, Reset between viewers).

**Phase 2:** trash and gradual automation, see the Phase 2 Scope tab. The MVP is complete.

**For shippable version:** [ ] (Q17)

MVP scope, decisions, work breakdown, milestones, acceptance, risks and fallbacks: MVP Scope tab. TO CONFIRM (Q18): asset inventory and dates.

**Recorded workflow:** feature branches and PRs; one editor per Unity scene. The UI is code-built (D8), so there is no Canvas integration step; the device scene is generated by an editor tool. The GDD does not assign tasks: owners, schedule and progress live in the team's Trello board.

**Decisions:** unresolved items appear in their relevant sections in yellow. Update the rule when agreed; record date, rationale and affected sections as needed.

# 10. Terms and Version

**Terms:** card means artwork collectible; blind pack contains five cards; anchor defines a task context; active timer time excludes Pomodoro pauses.

**Version:** v1.0, November 22, 2026. Added trash and gradual automation (Section 4), clear-trash onboarding step (2.1), map size and map pointer (Section 3), trash coins (Section 5), offline trash (Section 6), Phase 2 Scope tab; MVP marked complete. Open items Q19-Q21.

**v0.9, November 21, 2026:** Synced with the prototype: clock beside the coins, toolbar and HUD layout, body-following map, anchor slots and naming, Start screen and offline earnings window (5 decimals), reward window, 54-card first set, Profile (upgrades, looks, pets) and pack purchase, RayNeo palette, code-built UI (D8). No task assignment in the GDD. Open items now have IDs (Section 11).

**v0.8, November 1, 2026:** card reveal and collection silhouettes, zone-tagged To-dos, real exit vs pause, time source, Reset, 9-card first set.

# 11. Open Items

Rows are closed, never deleted. Type: C = confirm, V = verify, F = empty field.

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
| Q19 | Chest contents shift (coins to cards) and chest unlock | 4 | C | Economy, upgrades | Open |
| Q20 | Pets and trash: offline, speed, auto clean | 4 | C | Pets, economy | Open |
| Q21 | Manual exploration pay; eras | 4 | C | Economy, island art | Open |
| — | Settlement presentation (was 2.2) | 2.2 | C | — | Closed v0.9: offline earnings window, 5 decimals |
| — | Zone size, island angle / distance / scale / repositioning (was 3.1) | 3.1 | C | — | Closed v0.9: prototype values recorded |
| — | Provider todoEdit; startTodo in SLAM (was 3.2 / 8) | 3.2, 8 | V | — | Closed v0.9: verified on device 2026-11-07 |
| — | Screen hierarchy and focus (was 3.4) | 3.4 | C | — | Closed v0.9: panel / box / modal rules recorded |
| — | 9th card artwork (was 4 / 7) | 4, 7 | C | — | Closed v0.9: superseded by the 54-card set (Q10) |
| — | Canvas integration ownership (was 9) | 9 | C | — | Closed v0.9: D8 code-built UI |
| — | Final owners / reviewers, capacity, schedule, production tracker (was 9) | 9 | C | — | Closed v0.9: the GDD does not assign tasks (Trello) |
