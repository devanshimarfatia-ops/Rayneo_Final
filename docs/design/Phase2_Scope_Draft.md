# Phase 2 Scope — Trash and Gradual Automation

Status: in progress (started November 22, 2026, branch `Yaxin-Mu/Prototype-2`). The MVP is complete (MVP Scope tab). Full-game rules live in GDD tab 1 (Section 4 "Trash and gradual automation"); this tab only says what Phase 2 builds and how we check it. Numbers are placeholders until the economy re-tune. Task assignment and schedule are in Trello, not here.

## P.1 Goal

Make the opening engaging. Playtest feedback: the MVP is the right direction, but the game is automated from the first second, so players do not feel involved. Phase 2 adds a light repeated action (aim at trash on the map, tap) that the player gradually hands over to automation they earn.

## P.2 Starting point

Built in the MVP: idle core, offline settle, island map with chests, cards and collection, Coin Rate upgrade, cosmetics, anchors and zone To-dos, onboarding, Start screen, Demo Mode, Reset.

## P.3 Scope

| System | In Phase 2 | Later | Status |
|---|---|---|---|
| Map pointer | Reticle on the map, cyan highlight, temple tap runs the target (taken at finger-down); SDK gaze dot hidden; the map does not move while aiming | Pointer for other world objects | Done (prototype) |
| Trash | Trash on the map; tap orders the explorer to clear it (exploring pauses), coins float over his head; regrowth one per 30 min incl. offline; 48 = full = 24 h; full map stops exploring; saved | Trash types, decorations dirtied by trash | Partly: errand, coins and display done; regrowth, blocking and save open |
| Exploration | Manual exploration first (tap sends the explorer), early upgrade unlocks automatic exploration | Exploration routes | Open |
| Pets | Pets pick up trash on their own | Different pet abilities | Open |
| Chests | Only the tutorial chest early; an upgrade unlocks chest finding; chest contents shift from coins to cards with progress | Luck / era tuning | Open |
| Eras | — | Wild → stone age → iron age → … → cyber island, via upgrades and decorations | Later |
| Onboarding | Swipe step with visible feedback; double tap taught in the anchor step; anchor: name first, then place, clear "placed" check; clear 3 trash and see one regrow | Replay tutorial | Partly: swipe, double tap and anchor done; trash step open |
| Settings | Map size S / M / L (default M) and Trash style (48 pieces recommended, 12 piles) saved per device; panels stay open while the player looks at the map | More display options | Done |
| HUD clock | Time at the upper left beside the coins, same level (GDD 3.1) | Clock styles | Open |
| Economy | Re-tune for 54 cards, shop packs, cosmetics, trash coins, manual vs auto exploration | — | Open |

## P.4 Decisions

| # | Decision | Agreed rule |
|---|---|---|
| P1 | Opening engagement (GDD Q4) | Trash clearing and gradual automation (GDD 4). |
| P2 | Trash rate and cap | One per 30 min, also offline; 48 = 24 h = the offline cap; a full map stops exploring. |
| P3 | Priority | Trash beats exploring: a tapped trash sends the explorer at once; several taps queue. |
| P4 | Map size | Presets S / M / L; default and recommended M (2.4 m), chosen on device. |
| P5 | Trash style | 48 single pieces recommended; 12 piles of 4 as an option. |
| P6 | Panels and the map | Looking at the map suspends an open panel instead of closing it. |

## P.5 Work breakdown

Size: S ≤ 1 day, M 2–3 days, L 4+ days (one person).

| # | Work | Content | Size | Status |
|---|---|---|---|---|
| P-W1 | Map pointer | MapAimController: reticle, highlight, tap at finger-down, chest open, trash order; GazeDotHider | M | Done |
| P-W2 | Trash prototype | Test fill of 48, explorer errands, live search pause, trash coins with floating "+N", display styles | M | Done |
| P-W3 | Settings and panels | Map size and trash style preferences; suspended panels | S | Done |
| P-W4 | Onboarding rework | Swipe feedback, double tap in the anchor step, name-first anchor flow | S | Done |
| P-W5 | Trash system | Regrowth incl. offline (settle), save, block exploring when full, clear on Reset | M | Open |
| P-W6 | Onboarding trash step | Clear 3 trash, regrowth hint, one scripted regrowth | S | Open |
| P-W7 | Manual + auto exploration | Tap to explore, auto-explore upgrade, chest unlock, chest contents shift | L | Open |
| P-W8 | Pets pick up trash | Pet clears trash on its own (online; offline TBD) | M | Open |
| P-W9 | HUD clock | HH:mm beside the coins | S | Open |
| P-W10 | Economy re-tune | Workbook update: cards, shop, cosmetics, trash, exploration | M | Open |
| P-W11 | Playtest | Device playtest of the new opening (GDD Q16) | M | Open |

## P.6 Acceptance (device)

1. Fresh start: the player clears 3 trash with aim + tap without help, and can say that trash comes back.
2. Tapping trash while exploring: the explorer stops, walks there, clears it, "+N" shows, exploring resumes.
3. Leave the app for 24 h (or Demo "Skip"): the map shows 48 trash and exploring is paused; clearing one resumes it.
4. Manual exploration pays; buying auto exploration makes the explorer explore without taps.
5. A pet clears trash without player taps.

## P.7 Risks

| # | Risk | Fallback |
|---|---|---|
| PR1 | Trash feels like a chore that interrupts real work | Keep 24 h to full; pets early; no alerts for trash |
| PR2 | Aiming at small trash is tiring | Map size M/L, piles option, larger aim radius |
| PR3 | Offline trash makes returns feel punishing | Full map only pauses exploring; nothing is lost |

## P.8 Open questions

GDD Q19 (chest contents shift and chest unlock), Q20 (pets and trash: offline, speed, auto clean), Q21 (manual exploration pay, eras), Q16 (playtest targets).
