# Section 9 Draft — Production Scope (MVP)

Status: **MVP complete** (closed November 22, 2026). The prototype on branch `Yaxin-Mu/Prototype` implements this scope (device tests November 7, 2026); this tab is kept as the record of the MVP. New work (trash and gradual automation, map pointer, onboarding rework) belongs to Phase 2, not to this tab.
All numbers (coin amounts, timers, prices, rates) come from `Economy.xlsx`; this section defines scope only. Exception: shop and cosmetic prices are placeholders in code until the economy sheet has them (see Economy tab, section 8).

---

## 9.1 MVP Goal

Prove the core loop on device in one short session: real work earns coins, the island explores on its own, chests give cards, coins buy progress, and progress survives leaving the app.

MVP = demo showcase build. Shippable version: [ ] (after MVP playtest).

Demo date: [ ]. Demo length: 10 minutes (onboarding + normal play + one quit and re-entry). Device: one RayNeo X3 Pro, one room. Each viewer sets the anchor during onboarding; Reset clears everything for the next viewer.

## 9.2 Demo Path and Acceptance

Precondition: fresh install or after Reset (no save, no anchors). 1–2 todos may be placed in the system To-do app beforehand as a no-network fallback (D5).

| # | Step | Player action | Acceptance (testable on device) |
|---|---|---|---|
| 1 | Start | Launch app, tap Start | No save → Start screen with 0 coins (no offline earnings window); after Start, the onboarding intro card shows within 3 s. No error toast. |
| 2 | Controls | Tap, swipe, double tap as prompted | One framed hint per gesture ("TUTORIAL k/12"); advances when done correctly. Skip at any time: double tap with no panel open → "Skip tutorial?" → tap. |
| 3 | Set anchor | Open Anchors, open slot 1, stand at the work spot, Place anchor here, pick a name | Toast "Anchor 1 ready: <name>". Walk 2 m away and back: the zone is detected again. |
| 4 | Add To-do | Open To-do; "New to-do" (voice, `startTodo`) or pick an existing todo | New todo is tagged with the zone (D3) and shown at upper left. Without network, finishing an existing todo keeps onboarding moving (D5). |
| 5 | Finish To-do | Tap the task | Task removed from the list, marked finished in the system To-do app, coins + fixed task reward (once per todo). |
| 6 | Toolbar | Swipe along the toolbar at the bottom of the view | Six icons (Anchors, To-do, Collection, Profile, Pomodoro, Settings); the focused icon shows its label. Pomodoro tap → "Coming soon". |
| 7 | Turn to island | Turn head right (the right-edge arrow points the way) | Treasure map visible about 55° to the right, follows the body (not the head). Character finds the tutorial chest after a fixed 10 s onboarding search (not the economy exploration time); the progress bar fills in step. |
| 8 | Open tutorial chest | Focus chest, tap | Reward window shows the fixed rewards: one coin reward + one Common card + one pack (GDD 2.1). Applied once; if onboarding was skipped the chest stays on the island, not auto-granted. |
| 9 | Open pack | Tap the pack in the reward window (or "Open pack" in Collection) | Five cards shown. New cards marked NEW; duplicates show coin conversion. |
| 10 | Collection | Open Collection, open a rarity, return | Rarity boxes show "owned / 18"; the rarity page shows new cards in full color, others as silhouettes. Swipe moves focus. Double tap goes back. End card; normal play begins. |
| 11 | Upgrade | Open Profile → Skills, buy one Coin Rate level | Coins decrease by price. Passive rate on HUD increases immediately. Cannot buy without enough coins. |
| 12 | Quit + re-enter | Quit app, relaunch | No onboarding. Offline earnings window for any away time (10 s counts): time away, chests found, amount = saved rate × min(away time, 24 h), equal to the formula, 5 decimal places (logs keep 10); HUD and elsewhere show whole coins (rounded down), balance keeps the fraction internally. Tap → Start screen, the counter counts up to the balance. Settles exactly once. Coins, cards, upgrade level, cosmetics, task state and chest state equal the values before quitting (plus offline earnings). Work zone still detected; if the anchor was not kept, use the R1 manual zone pick. |
| 13 | Reset | Settings → Reset → confirm twice | Second confirmation required (red warning text). Clears this game's save and anchors, not the system To-do items. Returns to step 1. |

Demo pass = all 13 steps succeed in one run (within 10 minutes), twice in a row (Reset in between), without adb or Editor help.

## 9.2b Demo Mode (Income Acceleration)

Purpose: progress is driven by usage time, so presentations show income sped up. Demo Mode never changes the system time or the time the game reads: it only multiplies the income rate (passive coins + chest discovery progress).

- Three speed tiers, each with its own viewing goal (multipliers derived from the economy draft's 5 h per chest):

| Tier | Income multiplier | Character animation | What viewers see |
|---|---|---|---|
| 1 | 1x | 1x | Normal speed. |
| 2 | 60x | 2x | Character visibly moves faster; coins visibly rise faster (about 2 coins/s); chest progress bar fills in about 5 min. |
| 3 | 1800x | 3x (cap) | Chest progress bar fills in about 10 s: the whole search-to-find of one chest is seen within 10 s. All 3 chest slots full in about 30 s. |

- Plus "Skip 8 hours": runs one normal offline settle for 8 hours of income (same formula, same 24 h cap); no time is changed.
- The multiplier applies only to passive coins and chest discovery progress. Offline settle, toast durations, input and save interval run on real time. Character animation speeds up separately per the table, capped so it stays readable.
- Speed badge: always visible while tier ≠ 1 (e.g. "60x DEMO" / "1800x DEMO" in a corner, inside the safety margin), so viewers know it is accelerated.
- Hidden toggle: tap the version row in Settings 5 times within 3 s ("Demo mode on/off" toast). While on, Settings shows the speed tier row and "Skip 8 hours". Can be disabled per build (`allowDemoMode`). Demo mode never writes to the real save: it uses a separate demo save slot.

Acceptance:
- Tier 3: chest progress bar goes from 0% to chest found in 10 s (±1 s); tier 2: about 5 min.
- Tier 2: character moves visibly faster than tier 1 (2x).
- Coins, discovered chests and upgrade affordability after N game hours match the economy sheet's "Timeline" sheet within ~5%.
- "Skip 8 hours" gives the same result as really being away 8 hours (same settle, same 24 h cap).
- Returning to 1x keeps all progress; the badge disappears.

## 9.3 Scope by System

| System | In MVP | Out (v2) | Reason for cut |
|---|---|---|---|
| Anchors / zones | 3 anchor slots, first one placed and named during onboarding; naming from presets (offline) or by voice (system To-do flow, needs network); renaming rewrites the slot's todo prefixes; enter/exit detection (1 m / 2 s in, 2 m out); zone task overlay | 4th anchor, overlap priority | The prototype has 3 slots and detection works; a 4th only adds test cost. |
| To-do | Read system list, finish task → coins (once per todo), voice create via `startTodo`, zone tag in the todo text (D3) | Delete/edit in game, offline task entry, general To-do panel rules | Finish + reward is the loop; edit flows are utility polish. |
| Pomodoro | Icon only, in the toolbar; tap shows "Coming soon" (D7) | Timer (45 min default, pause, skip; independent tool, not tied to zones), completion reward, 30% early-end rule, rest reminder | The icon completes the toolbar and shows "utility first"; timer and rewards are not needed to prove the loop. |
| Clock | Current time (HH:mm, glasses system time) on the HUD at the upper left, next to the coin counter and at the same level (same row, same text size); no interaction | Clock styles | — (utility first: check the time without the phone) |
| Start screen | Every launch: offline earnings window (after a real exit) then Start button + coin counter | Session summary | Makes the offline settle visible and gives a clean start point for each viewer. |
| Island / exploration | 1 island as a flat line-art treasure map that follows the body (55° right, about 8° low, recenters after 1.5 s facing a new direction; Recenter map in Settings), 1 character, up to 3 chests, timed discovery, offline discovery of existing chests, chest progress bar with status label. Chest spawn: random spot inside one of 3 chest areas; while searching, a blinking dashed outline (≥2 px) with "?" marks the area; when the search ends the chest drops in and the outline disappears. "Look right" arrow at the right edge while a chest waits and the map is out of view. | Island skins (shop), map unlocks, ambient events | The full game has one island too. A fixed world-locked island felt bad on device, so the map follows the body. |
| Chests | Fixed reward table (3 rewards, coins/card/pack), reward window | Luck stat effects, rarity guarantees | Luck is a v2 upgrade. |
| Cards / packs | 54 cards (18 per rarity, placeholder pixel art drawn in code), 3 rarities, pack = 5 cards, duplicate → coins, Special pity; packs from chests and "Buy a pack" in the Collection (opens at once) | Card reveal minigames, cat-meme set | The pool was raised from 9 (D6) to 54 for a longer collection; the economy is not yet re-tuned for it (Economy tab, section 8). |
| Collection | Overview with one box per rarity ("owned / 18") + "Open pack (N)"; rarity page = 6 × 3 grid + info pane; locked = silhouette with "?", owned = full color, NEW dot | Sorting, filters | — |
| Upgrades | Coin Rate only (Profile → Skills; Explorer speed and Luck shown as "coming soon") | Exploration ability, Luck, decorations | One stat proves the spend loop. |
| Cosmetics | Profile → Look: 4 outfits (2 free), 6 clothing colors (2 free); Profile → Pets: 4 pets that follow the character on the map; locked items preview before buying. No stat effect. Placeholder prices. | Island skins, decorations | Added after device test 4 so coins have something to buy besides Coin Rate. |
| Daily login | Out | Daily coins, 5-day card reward | Needs multi-day play; cannot be shown in a 10-minute demo. |
| Onboarding | GDD 2.1 flow: intro card, controls, set anchor, add To-do, toolbar, tutorial chest, pack, collection, end card; framed "TUTORIAL k/12" hints with a toolbar pointer; skip at any time; resumes at the saved step | Pomodoro intro step, replay | Onboarding is the core of the demo; viewers set their own anchor. Pomodoro is not in MVP, so its step goes too. |
| Settings | Recenter map; Reset (second confirmation; clears this game's save and anchors, not system todos); version row (hidden Demo Mode toggle) | 3 save slots (new/delete), replay tutorial, audio settings | Demo only needs "clear for the next viewer"; slots need a slot-picker UI, so v2 (D10). |
| Art | Code-built UI (D8) with own pixel and line art on black, RayNeo palette (cyan #4DE2FF, green #44FF66, gray-cyan #7FA8B8), rarity silver / gold / purple; 2D animation of the explorer, pet and chests | 3D island and 3D character (later option), card artwork (GDD Q10), island skins, seasonal visuals | 2D costs less to build; the first parchment palette was rejected on device. |
| Audio | Out | Lo-fi music, SFX set, mute | Scope; not needed to prove the loop. |

Chest progress bar (in MVP):
- One horizontal bar below the island (and in the HUD above the toolbar) showing the character's progress toward the next chest, with a status label ("Treasure hunt 42%"); full = chest found, then it restarts from 0 for the next one.
- When all 3 chest slots are full, the bar stays full with "Chests full - open one"; search resumes once one is opened. While a found chest is focused the label reads "Tap to open".
- After quit and re-entry, the bar shows the position reached during offline time.
- Per RayNeo specs: lines ≥ 2 px, cyan fill, no gradient. The Exploration upgrade (v2) shortens the time to fill.

## 9.4 Decisions

| # | Decision | Agreed rule | Blocks |
|---|---|---|---|
| D1 | Core loop: GDD (todo → coins, cards from chests) vs `milestone_A_tuesday` (todo → artwork sharpens → gallery unlock) | Use GDD loop. No mosaic / blur effect: cards show directly when a chest or pack opens; in the collection, cards not owned show as silhouettes and switch straight to full color once owned (reference: Kingdom Two Crowns map, unlocked vs locked). | Cards, Collection, To-do glue |
| D2 | DoF mode | SLAM (6DoF) on for the whole MVP session: zone detection always active. Hardware performance (power, heat) is out of scope for MVP; focus on gameplay. The island map follows the body instead of being fixed in space (device test, November 7). | Island placement, anchors |
| D3 | Zone-task storage | Zone is written into the todo text itself; no separate map. A todo created in-game inside a zone is renamed right after creation via the provider `todoEdit` to `(Zone)-text`, e.g. `(Work)-Write report`, which is also how the system To-do app shows it. The game parses the leading `(Zone)-`, shows the todo only in that zone, and hides the prefix. Todos added outside the game (system To-do app) or with a broken prefix = General. Zone info lives with the system todo, so Reset or reinstall does not lose it. `todoEdit` verified on device (November 7). Renaming an anchor slot rewrites the prefix of its open todos. | Zone → To-do glue |
| D4 | Pause / focus loss (`startTodo`, system dialogs, screen off, glasses off) vs real exit | Only an app close or a system kill is a real exit. A pause of any length is the same session: no offline earnings window; on resume, coins and exploration for the paused time are added directly (same 24 h cap). Always save on pause. The offline earnings window appears only on relaunch after a real exit, with 5 decimals (changed from 10 on November 7). If the system kills the app while paused, the next launch settles from the time saved at pause. | SaveSystem, Pomodoro |
| D5 | Onboarding without network | Onboarding never requires voice. Todo step uses an existing system todo. Voice create shown as optional. | Onboarding |
| D6 | Card count | 18 cards per rarity: 18 Common / 18 Treasured / 18 Special = 54 cards, one 6 × 3 page per rarity (changed November 7; was 3 per rarity in a 3×3 grid). The first 9 cards keep their save indices. Artwork: GDD Q10. Economy re-tune for 54 cards pending. | Cards, Collection, economy |
| D7 | Pomodoro in MVP | Pomodoro icon only, in the toolbar; no function yet. Tap shows a short "Coming soon" toast. | W8 |
| D8 | UI integration path | Code-built HUD and panels with own pixel art, following the RayNeo spec (decided November 7). No Canvas merge. Devanshi's Canvas art may still be reused as assets. | InputAdapter/HUD |
| D9 | Time source | The real clock follows the glasses' system time (elapsed time computed in UTC to avoid time zone and DST jumps). IdleCore and offline settle read time only from one injectable clock (`IGameClock`: real clock, test clock), never calling `DateTime.Now` all over the code. If the system time is set back, elapsed time counts as 0 (W2). Demo Mode never changes time; it only multiplies income (9.2b). | W1, W2, 9.2b |
| D10 | Reset vs save slots | MVP has Reset (in Settings, second confirmation). SaveSystem stores by slotId; MVP always uses slot 0, Demo Mode uses another slot; the 3-slot UI is v2. | W2, W9 |

## 9.5 Work Breakdown

Size: S ≤ 1 day, M 2–3 days, L 4+ days (one person). Status as of November 21, 2026.
This document does not assign tasks: owners and schedule live in the team's Trello board.

| # | Module | Content | Size | Depends on | Status |
|---|---|---|---|---|---|
| W1 | IdleCore | Plain C#: coins, passive rate, upgrade levels, reward grants, events. No SDK. Reads values from one config (filled from economy sheet). Time via `IGameClock` (D9). | M | Economy sheet, D9 | Done |
| W2 | SaveSystem + OfflineProgress | JSON save per slot (temp file + backup), save on pause + every 30 s, settle once on Start, no minimum away time, 24 h cap, coins stored as fractional (double), focus-loss rule (D4), clock-change guard (negative elapsed = 0). | M | W1, D4, D9 | Done |
| W3 | Island + Chests | Body-following line-art treasure map to the right, explorer walking in the searched area, up to 3 chests at random spots in 3 areas with dashed "?" outline + drop-in on discovery, discovery timer, chest progress bar, offline discovery, focus + tap to open, reward window. | L | W1, D2 | Done |
| W4 | Cards / Packs | Card data (id, rarity; 18 per rarity), chest reward roll, pack roll (5 cards), duplicate conversion, Special pity, pack purchase. Card artwork: GDD Q10. | M | W1, D6 | Done (placeholder art) |
| W5 | Collection | Rarity boxes + 6 × 3 page + info pane, rarity colors, NEW marker; not owned = silhouette with ≥ 2 px outline and "?", owned = full color (D1). | M | W4 | Done |
| W6 | Upgrade shop + cosmetics | Coin Rate in Profile → Skills, price curve from config, buy feedback; Look (outfits, colors) and Pets with coin unlocks. | S | W1 | Done (cosmetics not device-tested) |
| W7 | Zone → To-do glue | Listen to `ModeManager.OnModeChanged`, show current zone tasks, finish → reward, `(Zone)-` prefix on new todos + parsing (D3), slot rename rewrites prefixes. | M | W1, D3 | Done (voice rename not device-tested) |
| W8 | InputAdapter / HUD | One input router (tap / swipe / double tap) over `SimpleTouchForLite` + keyboard in Editor, handler stack, toasts, clock and coin counter side by side at the upper left, six-icon toolbar, panels, Start screen. | M | D8 | Done (clock to add) |
| W9 | Reset + Demo Mode | Reset in Settings (second confirmation, clears save and anchors, D10), income multiplier (1x/60x/1800x, on coins and chest progress) and character animation speed-up (1x/2x/3x), "Skip 8 hours", speed badge, hidden toggle, separate demo save slot (9.2b). | M | W1, W2, D9 | Done |
| W9b | Time-lapse chart (optional) | Off-device presentation asset: coins / chests / upgrades over game hours, driven by the economy sheet's "Timeline" sheet. | S | Economy sheet | Open |
| W11 | Onboarding | Chain 9.2 steps 1–10: intro card, framed hints, set anchor, add To-do (with no-network fallback), toolbar, tutorial chest (fixed rewards), pack, collection, end card; skip at any time; step saved. | L | W3, W4, W5, W7, W8 | Done |
| W10 | Device test + tuning | Run 9.2 path on device, tune timers; economy re-tune for 54 cards and shop prices. | M | All | Closed: re-tune moves to Phase 2 |

Milestones (dates [ ]):

| Milestone | Content | Done when | Date |
|---|---|---|---|
| M0 Decisions | D1–D10 agreed, economy sheet v1 | Decisions recorded in GDD | Done (Nov 7) |
| M1 Core in Editor | W1, W2, W4, W9 | Coins, chests rolls, cards and save/offline work in Editor with keyboard | Done (Nov 7) |
| M2 Playable loop | W3, W5, W6, W8 | Chests, packs, collection, upgrade, quit + re-entry and Reset work in Editor (demo steps 8–13) | Done (Nov 7) |
| M3 Zone tasks + onboarding | W7, W11 | Full onboarding (demo steps 1–10) passes on device | Done (Nov 7) |
| M4 Demo ready | W10 | Demo pass (9.2) on device twice in a row | Closed (Nov 22): MVP complete |

Editor tooling (prototype): smoke test, onboarding test, HUD snapshots, APK build and device-scene build run from trigger files without focusing Unity. One editor per `.unity` scene at a time; the device scene is generated by an editor tool.

## 9.6 Risks and Fallbacks

| # | Risk | Signal | Fallback |
|---|---|---|---|
| R1 | SLAM anchors do not persist across sessions | AnchorZone test 3 (start elsewhere, walk back) | Manual zone pick from a list on Start (or after re-entry). Gameplay still listens only to `ModeManager.OnModeChanged`. |
| R2 | (Deferred) Power / heat | — | Hardware performance is out of scope for MVP; gameplay only (D2). Re-assess before release. |
| R3 | Voice todo needs network; may fail at demo venue | No Wi-Fi / cloud error | Onboarding "Add To-do" can finish a task already in the system To-do app; place 1–2 before the demo. Voice shown as optional. Anchor names have offline presets. |
| R4 | `startTodo` breaks SLAM or our state | — | Closed: verified on device November 7 (SLAM keeps tracking, anchors stay valid). Still treated as focus loss (D4). |
| R5 | System To-do provider changes with firmware | Provider call errors | Local-only task list (in-game) as fallback storage. |
| R6 | Island hard to see / uncomfortable at 30° FOV | Device test | Done: body-following map, about 8° low, "look right" arrow. Re-check comfort over a long session (GDD Q14). |
| R7 | Scene merge conflicts between two people | Git conflicts in `.unity` | One editor per scene at a time; the device scene is generated by an editor tool, UI is built in code. |
| R8 | Economy feels slow or flat in 10 minutes | Demo rehearsal, playtest feedback ("too passive at the start") | Demo Mode income acceleration (9.2b). Never change real economy values for the demo. Opening engagement is GDD Q4. |
| R9 | Schedule slips | Milestone missed | Cut order: audio → onboarding animation polish → chest drop-in animation (chest just appears) → duplicate conversion → Pomodoro icon. Never cut save/offline, the onboarding main flow or Reset. |
| R10 | Audience cannot see the glasses view; system screen recording breaks SLAM (DEVICE_FINDINGS) | Recording on during demo | Film the lenses with a phone; or run Demo Mode in 0DoF / Editor on a screen; or show the off-device time-lapse chart (W9b). |
| R11 | No final card artwork for 54 cards | Placeholder art at the demo | Placeholder pixel art drawn in code stays until artwork is chosen (GDD Q10); record source + license for any external art. |

