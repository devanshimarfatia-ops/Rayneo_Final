# GDD Update Blocks (tab 1, v0.7 → v0.8)

Source: decisions agreed in `MVP_Scope_Draft.md` (D1–D7, D9, D10) as of November 1, 2026.
Rules for applying:
- Each block: find the **Old** text in the named section (exact quote from the GDD), replace it with **New**.
- "Insert after" blocks add a new paragraph; nothing is deleted.
- Comment anchors: [a] "@Devanshi Please check this first-time onboarding flow" is on the heading `2.1 Onboarding` (keep the heading text exactly; the list under it may change). [b] "Economic systems design in PRIORITY!!" is on the §5 paragraph `TO DESIGN: base income/exploration rate, ...` (never replaced; §5 only inserts a paragraph after it).
- MVP-only cuts (Pomodoro icon only, Coin Rate only, 3 anchors, no login rewards, Demo Mode) stay in §9 / the MVP Scope tab. The full design in §1–8 is not cut back.

---

## Header

**Old:** `GDD Version: 0.7`
**New:** `GDD Version: 0.8`

**Old:** `Updated: October 28, 2026`
**New:** `Updated: November 1, 2026`

---

## 1.2 Research References (table row "Kingdom Two Crowns", column "Our differentiation")

**Old:** `TO CONFIRM: adapt map visuals`
**New:** `Apply the locked / unlocked contrast to the collection: cards not owned show as silhouettes, owned cards show in full color (see Section 4). TO CONFIRM: map visuals.`

---

## 2.1 Onboarding (third list item)

**Old:** `Add a To-do: create a zone task and see it at the upper left.`
**New:** `Add a To-do: create a zone task by voice and see it at the upper left. Voice is never required: without network, the player picks an existing system todo instead.`

---

## 2.2 Re-entry and Daily Use

**Old:** `TO CONFIRM: reward notifications, settlement presentation and whether to provide session summaries.`
**New:**
`Settlement presentation: after a real exit (Section 6), one settle toast on Start shows coins earned while away, with 10 decimal places (e.g. 0.0277777778 for a short absence). Everywhere else coins show as whole numbers.`
`TO CONFIRM: reward notifications and whether to provide session summaries.`

---

## 3.2 Clock and To-do

**Old:** `TO CONFIRM: General To-do accessible everywhere without staying expanded; entry point and display rules inside/outside zones.`
**New:**
`Zone tagging: a todo created in game inside a zone is renamed to "(Zone)-text" right after creation (e.g. "(Work)-Write report"), so the system To-do app shows the zone too. The game reads the prefix, shows the todo only in that zone and hides the prefix. Todos added outside the game (system To-do app) or with a broken prefix are General and show in every zone. The zone lives in the system todo itself, so a game reset or reinstall keeps it.`
`TO CONFIRM: General To-do accessible everywhere without staying expanded; entry point. TO VERIFY: provider todoEdit on device (Section 8).`

---

## 4. Island Exploration and Collection

### Block 4a: replace the first TO CONFIRM line

**Old:** `TO CONFIRM: online refresh timing/positions, discovery detection, opening controls, sampling algorithm, fade-in animation and rarity guarantees.`
**New:**
`Card reveal: cards show directly when a chest or blind pack opens; no mosaic, blur or fade-in reveal.`
`Collection display: cards not owned show as silhouettes with a ≥2 px outline (black alone is invisible on the glasses); owned cards show in full color. Reference: Kingdom Two Crowns locked / unlocked map areas.`
`TO CONFIRM: online refresh timing/positions, discovery detection, opening controls, sampling algorithm and rarity guarantees.`

### Block 4b: replace the second TO CONFIRM line

**Old:** `TO CONFIRM: island count/themes, card inventory, collection layout/capacity and long-term goals; cat memes are a Special-content candidate.`
**New:**
`First card set: 9 cards, 3 per rarity (3 Common / 3 Treasured / 3 Special), filling a 3×3 collection grid. 8 artworks exist; at least 1 more is needed.`
`TO CONFIRM: island count/themes, artwork-to-rarity mapping, card inventory beyond the first set, long-term collection capacity (paged grid) and goals; cat memes are a Special-content candidate.`

---

## 5. Coins, Upgrades and Login Rewards (insert only)

Do not replace the `TO DESIGN: base income/exploration rate, ...` paragraph (comment [b]).
**Insert after** that paragraph, as a new paragraph:
`Status (November 1, 2026): first draft done for all items above, and the MVP subset (Economy Draft section 8) is updated for the current MVP scope (tutorial chest, 10-minute demo, 1x / 60x / 1800x Demo Mode, 9-card set of 3 per rarity). See the Economy Draft tab and Economy_Draft.xlsx. Not playtested.`

---

## 6. Offline Progress and Persistence

**Old:** `TO CONFIRM: online/offline boundaries for pause/close/removal, save timing, clock-change and duplicate-settlement protection, reset/deletion/sync and anchor restoration.`
**New:**
`Real exit vs pause: only closing the app or a system kill counts as an exit; the settle toast appears on the next Start. Any pause (voice To-do, system dialogs, screen off, glasses removed) is the same session: on resume, the paused time is credited silently (24 h cap). If the app is killed while paused, the next Start settles from the pause save.`
`No minimum away time: any absence is settled. Coins are stored as fractional values.`
`Save timing: save on pause and every 30 s; settle exactly once per Start.`
`Time source: the glasses' system time; elapsed time is computed in UTC. If the clock is set back, elapsed time counts as 0. All game code reads time through one injectable clock.`
`Reset: Settings offers Reset with a second confirmation; it clears this game's save and anchors, not system todos. Saves are stored per slot internally; a multi-slot UI is later.`
`TO CONFIRM: sync and anchor restoration.`

---

## 7. Art and Audio

**Old:** `TO CONFIRM: Visual style, typography, themes (Zen garden/seasons), map unlock visuals/conditions, card presentation and asset ownership/licensing/credits.`
**New:**
`Card presentation: see Section 4 (direct reveal; silhouettes until owned).`
`TO CONFIRM: typography, themes (Zen garden/seasons), map unlock visuals/conditions and asset ownership/licensing/credits, including the 9th card artwork.`

(The next line, `Visual style (MVP): 2D looping sprite animation ...`, already covers visual style, so "Visual style" is dropped from the TO CONFIRM list.)

---

## 8. Technical Records and Validation

### Block 8a

**Old:** `Planned architecture: ModeManager events, IdleCore, OfflineProgress, SaveSystem, InputAdapter and HUD.`
**New:** `Planned architecture: ModeManager events, IdleCore, OfflineProgress, SaveSystem (per-slot saves), an injectable game clock (IGameClock: real clock, test clock), InputAdapter and HUD.`

### Block 8b

**Old:** `TO VERIFY: anchor accuracy/drift/re-entry/restoration, To-do create/complete/delete sync, voice handoff, launcher controls and foreground/background accounting.`
**New:** `TO VERIFY: anchor accuracy/drift/re-entry/restoration, To-do create/complete/delete sync, provider todoEdit for zone prefixes, voice handoff, launcher controls and foreground/background accounting.`

### Block 8c

**Old:** `TO VERIFY: viewing distance/scale/comfort, continuous-6DoF power/heat versus 0DoF fallback and island performance. Proposed budgets: models <5,000 faces, textures ≤512 px.`
**New:** `TO VERIFY: viewing distance/scale/comfort and island performance. Continuous-6DoF power/heat is deferred: the MVP keeps SLAM on and focuses on gameplay; re-evaluate before release. Proposed budgets: models <5,000 faces, textures ≤512 px.`

---

## 9. Production Scope and Ownership

**Old:** `For demo showcase: [ ]`
**New:** `For demo showcase: MVP, see the MVP Scope tab (10-minute demo from a fresh install, onboarding through one quit and re-entry, Reset between viewers).`

**Old:** `TO CONFIRM: MVP/exclusions, island/card/screen counts, asset inventory, dates, milestones, acceptance, scope cuts, risks and fallbacks.`
**New:** `MVP scope, decisions, work breakdown, milestones, acceptance, risks and fallbacks: MVP Scope tab. TO CONFIRM: screen count, asset inventory and dates.`

---

## 10. Terms and Version

**Old:** `Version: v0.7, October 28, 2026. Consolidated systems, current rules, and uncertainty markings.`
**New:** `Version: v0.8, November 1, 2026. Applied MVP decisions: card reveal and collection silhouettes, zone-tagged To-dos, real exit vs pause, time source, Reset, 9-card first set.`

---

## Other tabs (not tab 1)

- **MVP Scope tab**: outdated (5-minute demo, 600x, prepared demo save, pixel-to-sharp reveal). Replace its whole content with `MVP_Scope_Draft.md` (9.1–9.7). It has no comments.
- **Economy Draft tab**: section 8 (MVP subset) still cuts the tutorial chest and uses the 5-minute / 600x demo script and demo save. Needs an update from the economy session; not covered here. Its card pool also uses 5 / 3 / 1 per rarity, which conflicts with D6 (3 / 3 / 3).

## Conflicts

- **§5 `TO DESIGN:` paragraph (comment [b])**: not replaced. Its wording ("TO DESIGN") is now partly out of date because a first draft exists. The insert-after status paragraph covers this. Changing the paragraph itself (e.g. to "DRAFTED:") would require moving or re-creating comment [b]; left for Yaxin to decide.
- **§2.1 (comment [a])**: no conflict. Only the third list item under the heading changes; the heading text stays exactly `2.1 Onboarding`. Devanshi will review the updated wording.
