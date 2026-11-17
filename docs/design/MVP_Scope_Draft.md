# Section 9 Draft — Production Scope and Ownership (MVP)

Draft for GDD v0.8. Status: proposal, not agreed. Owner of this draft: Yaxin. Review: Devanshi.
All numbers (coin amounts, timers, prices, rates) come from `Economy_Draft.xlsx`; this section defines scope only.

---

## 9.1 MVP Goal

Prove the core loop on device in one short session: real work earns coins, the island explores on its own, chests give cards, coins buy progress, and progress survives leaving the app.

MVP = demo showcase build. Shippable version: [ ] (after MVP playtest).

Demo date: [ ]. Demo length: 10 minutes (onboarding + normal play + one quit and re-entry). Device: one RayNeo X3 Pro, one room. Each viewer sets the anchor during onboarding; Reset clears everything for the next viewer.

## 9.2 Demo Path and Acceptance

Precondition: fresh install or after Reset (no save, no anchors). 1–2 todos may be placed in the system To-do app beforehand as a no-network fallback (D5).

| # | Step | Player action | Acceptance (testable on device) |
|---|---|---|---|
| 1 | Start | Launch app, tap Start | No save → onboarding starts. First hint within 3 s. No error toast. |
| 2 | Controls | Tap, swipe, double tap as prompted | One hint card per gesture; advances when done correctly. Skip available at any time. |
| 3 | Set anchor | Stand at the work spot, create and name the first zone (Work) | Confirmation toast. Walk 2 m away and back: zone is detected as Work again. |
| 4 | Add To-do | Create by voice (`startTodo`) or pick an existing todo | New todo is tagged Work (D3) and shown at upper left. Without network, picking an existing todo keeps onboarding moving (D5). |
| 5 | Finish To-do | Select task, tap to finish | Task crossed out, marked finished in the system To-do app, coins +fixed task reward. |
| 6 | Tool menu | Find the tool menu below the forward view | Menu follows the glasses below the forward view; hint hides once found. |
| 7 | Turn to island | Turn head right | Island visible to the right, stays in place while the head moves. Character finds the tutorial chest after a short fixed onboarding delay (not the economy exploration time); the chest progress bar fills in step. |
| 8 | Open tutorial chest | Focus chest, tap | Fixed rewards: one coin reward + one Common card + one pack (GDD 2.1). Applied once; if onboarding was skipped the chest stays on the island, not auto-granted. |
| 9 | Open pack | Tap pack | Five cards revealed. New cards marked NEW; duplicates show coin conversion. |
| 10 | Collection | Open collection, return | New cards visible with rarity color. Swipe moves focus. Double tap returns. Onboarding ends; normal play begins. |
| 11 | Upgrade | Open shop, buy one Coin Rate level | Coins decrease by price. Passive rate on HUD increases immediately. Cannot buy without enough coins. |
| 12 | Quit + re-enter | Quit app, relaunch, Start | No onboarding. Offline settle toast for any away time (10 s counts): amount = saved rate × min(away time, 24 h), equal to the formula, 10 decimal places (below 1: 0.xxxxxxxxxx); only this toast shows decimals, HUD and elsewhere show whole coins (rounded down), balance keeps the fraction internally. Settles exactly once. Coins, cards, upgrade level, task state and chest state equal the values before quitting (plus offline earnings). Work zone still detected; if the anchor was not kept, use the R1 manual zone pick. |
| 13 | Reset | Settings → Reset → confirm | Second confirmation required. Clears this game's save and anchors, not the system To-do items. Returns to step 1. |

Demo pass = all 13 steps succeed in one run (within 10 minutes), twice in a row (Reset in between), without adb or Editor help.

## 9.2b Demo Mode (Income Acceleration)

Purpose: progress is driven by usage time, so presentations show income sped up. Demo Mode never changes the system time or the time the game reads: it only multiplies the income rate (passive coins + chest discovery progress).

- Three speed tiers, each with its own viewing goal (multipliers derived from the economy draft's 5 h per chest):

| Tier | Income multiplier | Character animation | What viewers see |
|---|---|---|---|
| 1 | 1x | 1x | Normal speed. |
| 2 | 60x | 2x | Character visibly moves faster; coins visibly rise faster (about 2 coins/s); chest progress bar fills in about 5 min. |
| 3 | 1800x | 3x (cap) | Chest progress bar fills in about 10 s: the whole search-to-find of one chest is seen within 10 s. All 3 chest slots full in about 30 s. |

- Plus "Skip offline N hours": runs one normal offline settle for N hours of income (same formula, same 24 h cap); no time is changed.
- The multiplier applies only to passive coins and chest discovery progress. Offline settle, toast durations, input and save interval run on real time. Character animation speeds up separately per the table, capped so it stays readable.
- Speed badge: always visible while tier ≠ 1 (e.g. "60x DEMO" / "1800x DEMO" in a corner, inside the safety margin), so viewers know it is accelerated.
- Off in normal builds. Enabled by a hidden toggle (e.g. a tap sequence on the clock, or a dev build flag). Demo mode never writes to the real save: it uses a separate demo save slot.

Acceptance:
- Tier 3: chest progress bar goes from 0% to chest found in 10 s (±1 s); tier 2: about 5 min.
- Tier 2: character moves visibly faster than tier 1 (2x).
- Coins, discovered chests and upgrade affordability after N game hours match the economy sheet's "Timeline" sheet within ~5%.
- "Skip offline N hours" gives the same result as really being away N hours (same settle, same 24 h cap).
- Returning to 1x keeps all progress; the badge disappears.

## 9.3 Scope by System

| System | In MVP | Out (v2) | Reason for cut |
|---|---|---|---|
| Anchors / zones | 3 anchors (Work / Kitchen / Bedroom, as in the current prototype), first one created and named during onboarding, enter/exit detection, zone task overlay | 4th anchor, overlap priority, renaming UI | The prototype already has 3 zones and detection works; a 4th only adds test cost. |
| To-do | Read system list, finish task → coins, voice create via `startTodo`, local zone tag | Delete/edit in game, offline task entry, general To-do panel rules | Finish + reward is the loop; edit flows are utility polish. |
| Pomodoro | Icon only, in the main-view toolbar; no function yet (D7) | Timer (45 min default, pause, skip; independent tool, not tied to zones), completion reward, 30% early-end rule, rest reminder | The icon completes the toolbar and shows "utility first"; timer and rewards are not needed to prove the loop. |
| Clock | Simple time in tool menu | Styles | Trivial, keeps "utility first" visible. |
| Island / exploration | 1 island, 1 character, up to 3 chests, timed discovery, offline discovery of existing chests, chest progress bar. Chest spawn: random pick from a few fixed spawn points; while searching, a dashed outline (≥2 px) with "?" marks the spot; when the search ends the chest drops in and the outline disappears. | Free random placement anywhere on the island, island skins (shop), map unlocks, ambient events | The full game has one island too. The spawn marker is in MVP: it is cheap in 2D and makes the 1800x demo tier readable (viewers see where the chest will land). |
| Chests | Fixed reward table (3 rewards, coins/card/pack) | Luck stat effects, rarity guarantees, fade-in animation polish | Luck is a v2 upgrade. |
| Cards / packs | Card set from existing artworks, 3 rarities, pack = 5 cards, duplicate → coins | Card reveal minigames, cat-meme set, buying packs in shop | Uses the 8 artworks (artwork_01–08) in `Assets/Art Assets/` on `origin/Devanshi`; shop packs add balance work. |
| Collection | Grid of all cards, locked/unlocked, card viewer | Sorting, filters, capacity rules | Viewer exists on Devanshi's branch. |
| Upgrades | Coin Rate only | Exploration ability, Luck, skins (incl. pets that follow the character), decorations | One stat proves the spend loop. |
| Daily login | Out | Daily coins, 5-day card reward | Needs multi-day play; cannot be shown in a 10-minute demo. |
| Onboarding | GDD 2.1 flow: controls, set anchor, add To-do, tool menu, tutorial chest, pack, collection; skip at any time | Pomodoro intro step, replay | Onboarding is the core of the demo; viewers set their own anchor. Pomodoro is not in MVP, so its step goes too. |
| Settings | Reset (second confirmation; clears this game's save and anchors, not system todos) | 3 save slots (new/delete), replay tutorial, audio settings | Demo only needs "clear for the next viewer"; slots need a slot-picker UI, so v2 (D10). |
| Art | 2D looping animation: island, character search and chests as 2D sprite / frame animations; card frames by rarity; HUD per RayNeo specs | 3D island and 3D character (later option), island skins (shop), seasonal visuals, decoration art | 2D loops cost less to build; the existing 3D island model is not used for now. |
| Audio | Out (or one SFX for chest open) | Lo-fi music, SFX set, mute | Scope; not needed to prove the loop. |

Chest progress bar (new, in MVP):
- One horizontal bar below the island showing the character's progress toward the next chest; full = chest found, then it restarts from 0 for the next one. Bar only, no percentage number.
- When all 3 chest slots are full, the bar stays full with a short label "Chests full" to prompt opening; search resumes once one is opened.
- After quit and re-entry, the bar shows the position reached during offline time.
- Per RayNeo specs: lines ≥ 2 px, cyan fill, no gradient. The Exploration upgrade (v2) shortens the time to fill.

## 9.4 Decisions Needed Before Build

| # | Decision | Recommended default | Blocks |
|---|---|---|---|
| D1 | Core loop: GDD (todo → coins, cards from chests) vs `milestone_A_tuesday` (todo → artwork sharpens → gallery unlock) | Use GDD loop. No mosaic / blur effect: cards show directly when a chest or pack opens; in the collection, cards not owned show as silhouettes and switch straight to full color once owned (reference: Kingdom Two Crowns map, unlocked vs locked). | Cards, Collection, To-do glue |
| D2 | DoF mode | SLAM (6DoF) on for the whole MVP session: zone detection always active, island fixed in space. Hardware performance (power, heat) is out of scope for MVP; focus on gameplay. | Island placement, anchors |
| D3 | Zone-task storage | Zone is written into the todo text itself; no separate map. A todo created in-game inside a zone is renamed right after creation via the provider `todoEdit` to `(Zone)-text`, e.g. `(Work)-Write report`, which is also how the system To-do app shows it. The game parses the leading `(Zone)-`, shows the todo only in that zone, and hides the prefix. Todos added outside the game (system To-do app) or with a broken prefix = General. Zone info lives with the system todo, so Reset or reinstall does not lose it. `todoEdit` not yet verified on device (first step of W7). | Zone → To-do glue |
| D4 | Pause / focus loss (`startTodo`, system dialogs, screen off, glasses off) vs real exit | Only an app close or a system kill is a real exit. A pause of any length is the same session: no settle toast; on resume, coins and exploration for the paused time are added directly (same 24 h cap). Always save on pause. The offline settle toast appears only on relaunch after a real exit. If the system kills the app while paused, the next launch settles from the time saved at pause. | SaveSystem, Pomodoro |
| D5 | Onboarding without network | Onboarding never requires voice. Todo step uses an existing system todo or a sample task stored locally. Voice create shown as optional. | Onboarding |
| D6 | Card count vs 3×3 gallery | 3 cards per rarity: 3 Common / 3 Treasured / 3 Special = 9 cards, filling the 3×3 grid. 8 existing artworks + at least 1 new artwork (R11). Artwork-to-rarity mapping TBD. Long term: paged grid. | Cards, Collection |
| D7 | Pomodoro in MVP | Pomodoro icon only, in the main-view toolbar; no function yet. Tap shows a short "Coming soon" toast. | W8 |
| D8 | UI integration path | Plan A: Devanshi's Canvas calls our features. Plan B launcher stays as fallback. Before deciding, verify clarity on device: compare Devanshi's Canvas UI with our current HUD on the glasses (text, icon edges, artwork). If the Canvas looks blurry, check the usual causes first: canvas scaler / reference resolution not 640×480, low Dynamic Pixels Per Unit on a World Space canvas, compressed or mipmapped textures, render scale (eyeTextureResolutionScale) below 1, text not using TextMeshPro. If it cannot be fixed, build on the Plan B HUD and reuse only the Canvas art assets. Decide before the HUD module starts. | InputAdapter/HUD |
| D9 | Time source | The real clock follows the glasses' system time (elapsed time computed in UTC to avoid time zone and DST jumps). IdleCore and OfflineProgress read time only from one injectable clock (`IGameClock`: real clock, test clock), never calling `DateTime.Now` all over the code. If the system time is set back, elapsed time counts as 0 (W2). Demo Mode never changes time; it only multiplies income (9.2b). Must be fixed before W1/W2 are built; retrofitting later is costly. | W1, W2, 9.2b |
| D10 | Reset vs save slots | MVP has Reset (in Settings, second confirmation). SaveSystem stores by slotId; MVP always uses slot 0, Demo Mode uses another slot; the 3-slot UI is v2. | W2, W9 |

## 9.5 Work Breakdown

Size: S ≤ 1 day, M 2–3 days, L 4+ days (one person).

| # | Module | Content | Owner | Size | Depends on |
|---|---|---|---|---|---|
| W1 | IdleCore | Plain C#: coins, passive rate, upgrade levels, reward grants, events. No SDK. Reads values from one config asset (filled from economy sheet). Time via `IGameClock` (D9). | Yaxin | M | Economy sheet, D9 |
| W2 | SaveSystem + OfflineProgress | JSON save, save on pause + every 30 s, settle once on Start, no minimum away time, 24 h cap, coins stored as fractional (double), focus-loss rule (D4), clock-change guard (negative elapsed = 0). | Yaxin | M | W1, D4, D9 |
| W3 | Island + Chests | Place the 2D island to the right (D2), 2D looping character search animation, up to 3 chests at random spawn points with dashed "?" outline + drop-in on discovery, discovery timer, chest progress bar, offline discovery, focus + tap to open. | Devanshi | L | W1, D2 |
| W4 | Cards / Packs | Card data (id, rarity, sprite; 3 Common / 3 Treasured / 3 Special), chest reward roll, pack roll (5 cards), duplicate conversion. Add the 9th artwork (R11). | Yaxin | M | W1, D6 |
| W5 | Collection | Grid + viewer, rarity colors, NEW marker; not owned = silhouette, owned = full color, no transition animation (D1). Silhouettes need a ≥ 2 px outline: black is transparent on the glasses, so a pure black silhouette is invisible; test the fill on device. Adapt `GalleryGrid`. | Devanshi | M | W4 |
| W6 | Upgrade shop | One Coin Rate upgrade, price curve from config, buy feedback. | Devanshi | S | W1 |
| W7 | Zone → To-do glue | Listen to `ModeManager.OnModeChanged`, show current zone tasks, finish → reward, `(Zone)-` prefix on new todos + parsing (D3). Reuse `SystemTodoClient` / `TodoList`. | Yaxin | M | W1, D3 |
| W8 | InputAdapter / HUD | One input router (tap / swipe / double tap) over `SimpleTouchForLite` + keyboard in Editor, focus between tool menu / island / collection, toasts, coin counter, toolbar icons (clock, To-do, Pomodoro placeholder). | Yaxin + Devanshi | M | D8 |
| W9 | Reset + Demo Mode | Reset in Settings (second confirmation, clears save and anchors, D10), income multiplier (1x/60x/1800x, on coins and chest progress) and character animation speed-up (1x/2x/3x), "Skip offline N hours", speed badge, hidden toggle, separate demo save slot (9.2b). | Yaxin | M | W1, W2, D9 |
| W9b | Time-lapse chart (optional) | Off-device presentation asset: coins / chests / upgrades over game hours, driven by the economy sheet's "Timeline" sheet. | [ ] | S | Economy sheet |
| W11 | Onboarding | Chain 9.2 steps 1–10: hint cards, set anchor, add To-do (with no-network fallback), tool menu, tutorial chest (fixed rewards), pack, collection; skip at any time; onboarding-done flag in the save. | Yaxin + Devanshi | L | W3, W4, W5, W7, W8 |
| W10 | Device test + tuning | Run 9.2 path on device, tune timers. | Both | M | All |

Milestones (dates [ ]):

| Milestone | Content | Done when | Date |
|---|---|---|---|
| M0 Decisions | D1–D9 agreed, economy sheet v1 | Decisions recorded in GDD | [ ] |
| M1 Core in Editor | W1, W2, W4, W9 | Coins, chests rolls, cards and save/offline work in Editor with keyboard | [ ] |
| M2 Playable loop | W3, W5, W6, W8 | Chests, packs, collection, upgrade, quit + re-entry and Reset work in Editor (demo steps 8–13) | [ ] |
| M3 Zone tasks + onboarding | W7, W11 | Full onboarding (demo steps 1–10) passes on device | [ ] |
| M4 Demo ready | W10 | Demo pass (9.2) on device twice in a row | [ ] |

W1/W2/W4 (Yaxin) and W3 placement + W6 (Devanshi) can run in parallel after M0. One owner per `.unity` scene at a time.

## 9.6 Risks and Fallbacks

| # | Risk | Signal | Fallback |
|---|---|---|---|
| R1 | SLAM anchors do not persist across sessions | AnchorZone test 3 | Manual zone pick from a list on Start (or after re-entry). Gameplay still listens only to `ModeManager.OnModeChanged`. |
| R2 | (Deferred) Power / heat | — | Hardware performance is out of scope for MVP; gameplay only (D2). Re-assess before release. |
| R3 | Voice todo needs network; may fail at demo venue | No Wi-Fi / cloud error | Onboarding "Add To-do" can pick a task already in the system To-do app; place 1–2 before the demo. Voice shown as optional. |
| R4 | `startTodo` breaks SLAM or our state | Launcher device test (open) | Treat as focus loss (D4); if SLAM is lost, show "re-check zone" toast. |
| R5 | System To-do provider changes with firmware | Provider call errors | Local-only task list (in-game) as fallback storage. |
| R6 | Island hard to see / uncomfortable at 30° FOV | Device test | Move island closer to forward view; add "look right" hint arrow. |
| R7 | Scene merge conflicts between two people | Git conflicts in `.unity` | One scene owner at a time; features built as prefabs / runtime bootstraps. |
| R8 | Economy feels slow or flat in 10 minutes | Demo rehearsal | Demo Mode income acceleration (9.2b). Never change real economy values for the demo. |
| R9 | Schedule slips | Milestone missed | Cut order: audio → onboarding animation polish → chest drop-in animation (chest just appears) → duplicate conversion → Pomodoro icon. Never cut save/offline, the onboarding main flow or Reset. |
| R10 | Audience cannot see the glasses view; system screen recording breaks SLAM (DEVICE_FINDINGS) | Recording on during demo | Film the lenses with a phone; or run Demo Mode in 0DoF / Editor on a screen; or show the off-device time-lapse chart (W9b). |
| R11 | Only 8 artworks exist; D6 needs 9 | 9th collection slot empty | Ask Devanshi for 1 more artwork in the same style (record source + license). If late, use a variant of an existing artwork (recolor / crop) as placeholder so no slot is empty. |

## 9.7 Ownership

| Area | Owner | Reviewer |
|---|---|---|
| IdleCore, Save/Offline, Cards/Packs logic, Zone → To-do, anchors, voice | Yaxin | Devanshi |
| Island, Collection UI, Shop UI, art integration | Devanshi | Yaxin |
| InputAdapter / HUD, device test | Both | — |
| Economy sheet | Yaxin | Devanshi |

Production tracker: [ ]. Capacity per week: [ ].
