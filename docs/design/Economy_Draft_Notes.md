# Economy Draft v0.1: Working Notes

Team-facing notes for `Economy_Draft.xlsx`. The paste-ready GDD tab version is `Economy_Draft_Doc.md`.
First draft, not playtested.

## How the economy works

### 1. The loop as a flow
```
Passive (always, offline <= 24 h) ─┐
Chests (3 rewards each)  ──────────┤
To-do / Pomodoro (utility) ────────┼──> COINS ──> Upgrades (coin rate / exploration / luck)
Daily login ───────────────────────┤          └─> Blind packs ──> CARDS (collection goal)
Duplicate cards ───────────────────┘                                  │
            ^                                                         │
            └──────────── duplicates convert back to a few coins <────┘
```
- Upgrades feed back into the sources. Coin rate raises passive income, exploration raises chests per day, luck raises cards per chest.
- The only exit from the loop is the collection. Coins have no value of their own.

### 2. Targets first, numbers second
No number was picked freely. Each one is derived from a target below, and the `Constraint Checks` sheet turns red when a target breaks.

| Target | Value | Why it matters |
|---|---|---|
| Price unit | 1 h of base passive = 100 coins | Every price can be read as "hours of idling" |
| Utility share of daily coins | >= 25% at start, >= 20% fully upgraded | Utility-first: real work must visibly pay |
| Passive share | 20-35% at start | Idle genre: progress while away, but work still matters |
| First upgrade | Session 1 | Onboarding teaches the spend loop |
| First pack | Day 1 | GDD onboarding opens a pack |
| Chest waiting | At most session starts | Reason to come back |
| 9-card collection | Not before about day 7 | One-week MVP; last card is the chase |
| All upgrades | Not in week 1 | Spending stays meaningful |
| No loop | Packs never fund packs | GDD 5 |

### 3. Derivation chain (in this order)
1. **Base rate 100/h.** This sets the unit. A day of passive = 2,400.
2. **To-do 250, Pomodoro 1,080 (45 min).** From the utility-share target. 5 To-dos + 1 Pomodoro = 2,330/day, about equal to passive, which gives about 32% of the day once chests are added. The Pomodoro is priced at 24 coins per focus minute (45 x 24 = 1,080, about 4.3 To-dos) because focus is the behaviour we most want.
3. **Discovery 5 h.** Offline search finds at most 3 chests per gap, so gap length sets the chest count. 6 h day gaps find 1 chest, a 10 h night finds 2, and online time adds about 0.3. That gives about 5 chests/day and a chest waiting at most session starts. 3 h would give about 8/day and flood rewards.
4. **Chest weights 78 / 20 / 2.** From the card budget. To finish 9 cards around day 7 with a 1% Special, the player needs about 12 draws/day in total: about 5 from chests and the rest from shop packs. 3 slots x (20% card + 2% x 5-card pack) = 0.9 cards per chest. The GDD example 50:35:15 gives 3.3 cards per chest and completes the pool on day 1.
5. **Coin reward 150.** Worth 1.5 h of passive per slot. A typical chest is worth about 3.5 h of idling, so opening one feels like a jump.
6. **Pack price 3,000.** Worth 30 h of passive, about 0.4 base days. With 25% of income going to packs that is about 1 pack every 1.5 days, i.e. about 3.5 draws/day, which closes the card budget from step 4.
7. **Rarity 79 / 20 / 1 + pity at 100.** Commons (15.8% each) are owned on day 1-2 and Treasured (6.7% each) on day 2-4. The single Special at 1% with about 12 draws/day gives P(complete by day 6) = 49%. Pity at draw 100 caps bad luck at about day 8.
8. **Duplicates 50 / 100 / 250.** GDD hard rule: 5 Specials (1,250) < 1 pack (3,000). Expected duplicate return is 310 per pack (about 10%), so duplicates soften bad luck but never fund packs.
9. **Upgrade curves.** Cost = Base x Growth^(L-1), effect linear, so payback slows every level:
   - The coin rate L1 costs 500 (5 h of passive) so it fits in session 1. Payback goes from 50 h (L1) to 1,417 h (L10). Max 2x income keeps utility >= 20%.
   - Exploration starts dearer (800) because it pays back in chests. Max -40% discovery time = about +60% chests.
   - Luck is the collector upgrade, with the steepest curve (1.6x).
   - All 20 levels = 70,900 = about 9 base days, so the week ends at 17/20.
10. **Tutorial 500 coins.** Back-solved: 500 + 1 To-do (250) + 30 min passive (50) = 800 >= cheapest upgrade (500).
11. **Login 300, To-do cap 10.** Login is a nudge (about 4% of income). The cap is 2x typical use, so only farming hits it.

## Change log
- 2026-11-01 (GDD 3.3): full-design Pomodoro is an independent tool (not tied to zones, keeps running when the player leaves a zone), default 45 min, pause and skip allowed. `Pomo_Work_Min` 25 -> 45. New `Pomo_Coins_Per_Min` = 24 (unchanged rule); `Pomo_Reward` is now derived (45 x 24 = 1,080, was 600). Profile `Pomos_Per_Day` 2 -> 1, so daily focus time stays about the same (50 -> 45 min) and fits in 1.5 h online. Early end and skip follow one rule: >= 30% active (13.5 min), paid pro rata. Utility share 31.8% at start, 22.4% fully upgraded; all checks still pass. MVP is unchanged (Pomodoro icon only).
- 2026-11-01 (Yaxin): Pomodoro skip = ending the current work segment early (same rule as early end). Below 30% of the work duration the game shows a confirmation prompt that ending now gives no reward. Full vs pro rata at >= 30% stays open question 1.
- 2026-11-01 (Yaxin): open question 1 closed. Early end or skip at >= 30% pays pro rata to active focus time (same coins per minute); below 30% pays nothing (with the warning prompt); full completion pays the full reward. Matches `Pomo_Prorate` = 1, so no number or check changed. Remaining open questions renumbered.
- 2026-11-01: pets are a shop cosmetic counted as skins (no stat effect); listed under the skins sink, prices not set.
- 2026-11-01 (Yaxin): only one island; island themes are island skins sold in the shop. Cosmetics sink = character skin colors, pets, island skins / decorations (no stat effect, prices not set). Open questions reworded from "new island" to "new card pool or seasonal pool, new island skin".

## Sheet guide
| Sheet | Use |
|---|---|
| Params | Change numbers here only (blue). Column E says why each value is what it is. |
| Sources & Sinks | Coins/day and share per source, at start vs fully upgraded; sink totals in days of income |
| Upgrades | Cost / effect per level; purchase ladder (cheapest first) used by both sims |
| Constraint Checks | 19 TRUE/FALSE rules; one FALSE = something broke. Also the Pomodoro early-end payout table |
| 7-Day Sim | One row per day, expected values |
| Timeline | One row per hour (168), for the time-lapse demo. Light-blue headers = chart series; Events column = caption text; milestones under the table |
| MVP Summary | MVP subset: daily pacing, sources/sinks, 10-minute demo (fresh install) coin flow, demo tiers 1x / 60x / 1800x, QA reference per game hour, 22 MVP checks, Coin Rate ladder. Reads Params column F (`M_` names) |
| MVP Timeline | Same hourly model on the MVP column |

MVP subset: Params column F holds the MVP value of every parameter. Orange = changed for the MVP (with the reason in column G); green = same as full. Change the full design in column B and the MVP follows, except for the orange cells.

MVP state (MVP_Scope_Draft, 2026-11-01): tutorial chest is back (GDD 2.1 onboarding is in the MVP), so there is no pre-found chest. Card pool 3 / 3 / 3 (D6) with rarity 60 / 30 / 10 and pity 25 (a new Special after 25 draws without one). Coin Rate 250 x 1.21^(L-1), 20 levels, +5/h. Demo tiers are an income multiplier only (passive + chest discovery); offline settle always runs on real time and only after a real exit (D4).

Pity model: `Pity_Special` = N draws without a new Special -> the next draw is a new Special. Expected Specials owned = max(random-draw estimate, min(pool, INT(draws / N))); complete for sure at N x Special pool draws. With 1 Special (full design) this is the same as "guaranteed by draw N".

## How to tune
- **Chests feel too rare or too common:** `Disc_Base_H`.
- **Collection too fast or too slow:** `R_Special` first, then `W_Card_Base` / `Pack_Price`.
- **Work feels pointless vs idle:** raise `Todo_Reward` / `Pomo_Reward`, or lower `CR_Step` / `CR_Max`.
- After big cost changes, re-sort the purchase ladder on the Upgrades sheet (it is cheapest-first for the defaults).
- The Timeline schedule is set with `Session_Hour_1..3`.

## Results (current defaults, full design)
- Session 1: first upgrade. Day 1: 5 upgrades, 5.3 of 9 cards.
- First shop pack: day 2 at 21:00 (hour 37).
- Day 6: 8.5 cards, P(complete) 49%. Day 7: 8.5 cards, P(complete) 54%.
- Week 1 ends with 17 of 20 upgrades.
- 168 h: 42 chests opened, 62,423 coins earned.
- Utility share: 32% at start, 22% fully upgraded.

## Results (MVP column)
- Session 1: onboarding To-do (+320) and tutorial chest (+500, 1 Common, 1 pack); Coin Rate level 1 at demo step 11.
- Day 1: Coin Rate 8, 5.2 of 9 cards. Day 6: P(complete) 43%. Day 7: 8.4 cards, P(complete) 53%, Coin Rate 19 of 20.
- At least 1 Coin Rate level bought every day of week 1.
- 168 h: 32 chests opened, 50,365 coins earned. Utility share 28.5% at start, 20.3% at the day-7 level.
- 10-minute demo: about 948 coins before step 11; 1800x for 30 s = 15 game h = +1,575 coins and 3 chests; 30 s quit = settle toast 0.8750000000.

## Open questions
1. Rewarded To-do cap (10/day) and a minimum age before a To-do pays?
2. Keep chest weights 78:20:2, or grow the pool and return to the GDD example 50:35:15?
3. Card pool beyond the MVP: about 24-30 cards for a 2+ week study.
4. Purpose of packs after the collection is complete (v2: new card pool or seasonal pool, new island skin).
5. Login clock abuse: accept for the MVP?
6. Should utility rewards scale with the passive rate (share falls from 32% to 22% as coin rate is upgraded)?
7. The player profile is a guess; replace it with device-test usage.
8. Expected values only. Run a Monte Carlo pass for variance before the user study.
9. Full-design pool is still 5 / 3 / 1; align with D6 (3 / 3 / 3) once the post-MVP pool is known.
10. Tutorial chest assumed separate from the 3 island slots; normal search starts at 0 found chests when onboarding ends.
