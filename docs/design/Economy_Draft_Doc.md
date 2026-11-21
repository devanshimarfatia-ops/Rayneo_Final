# Economy Design (Draft v0.1)

Status: first draft, not playtested. Source of truth for every number and formula: `Economy_Draft.xlsx` (sheet Params; all other sheets recalculate from it). This tab summarises its results as of November 1, 2026 (MVP section updated for MVP_Scope_Draft decisions D3, D4, D6 and the 10-minute demo; full-design Pomodoro changed to 45 min per GDD 3.3). Section 8 covers the MVP subset (Params column `MVP value`, sheets `MVP Summary` and `MVP Timeline`).

**Sync note (November 21, 2026).** Section 8 now records what the prototype build actually uses. The build differs from the workbook in three ways: the card pool is 54 cards (18 / 18 / 18, decision D6 changed on November 7), blind packs can be bought in the Collection for 3,000, and cosmetics (outfits, colors, pets) are sold for coins at placeholder prices (8.8). The workbook and the pacing results in 8.3–8.5 and 8.7 still assume the 9-card pool and no shop, so they are out of date until the re-tune (open question 11). The workbook itself was not changed in this pass.

## 1. How the Economy Works

### 1.1 Core loop as a flow

**Sources (faucets) → Coins → Sinks (drains)**

- **Passive income**: runs all the time, online and offline (offline capped at 24 h). Coin rate upgrades raise it.
- **Chests**: the character searches the island one chest at a time. Offline it only finds chests already there (max 3). Each chest gives 3 rewards: coins, a card or a blind pack.
- **To-do and Pomodoro**: the utility actions. Fixed coins per completion. The Pomodoro is an independent tool: it is not tied to zones and keeps running when the player leaves a zone.
- **Daily login**: small coins per calendar date, plus cards every 5th day.
- **Duplicate cards**: converted to a few coins.

Coins go to two sinks:

- **Upgrades**: coin rate, exploration, luck. Exponential cost.
- **Blind packs**: 5 cards each, which complete the collection.

Cards are the long-term goal. The collection is the visible progress; coins are the means.

### 1.2 Design targets (all numbers are derived from these)

| Target | Value | Why |
|---|---|---|
| Unit of price | 1 h of base passive = 100 coins | Every price reads as "hours of idling", easy to reason about |
| Utility share of daily coins | >= 25% at start, >= 20% fully upgraded | Utility-first positioning: real work must visibly pay |
| Passive share of daily coins | 20-35% at start | Idle genre: progress while away, but it must not make work pointless |
| First upgrade | In session 1 | Teach the spend loop during onboarding |
| First blind pack | Day 1 | GDD onboarding opens a pack |
| A chest is waiting | At most session starts | "Something happened while I was away" pulls the player back |
| Collection (9 cards) complete | Not before about day 7 | One-week MVP study; the last card is a chase |
| All upgrades bought | After week 1 | Spending stays meaningful through the study |
| No coin loop | Packs never pay for packs | GDD 5 rule |

### 1.3 Typical player (assumption)

- 3 sessions a day of 30 min each (08:00, 14:00, 21:00).
- 10 h overnight gap.
- 5 To-dos and 1 Pomodoro (45 min) a day.
- 25% of coins spent on packs, the rest on upgrades, cheapest first.

Replace this profile with real usage after device tests.

## 2. Parameters

| Parameter | Value | Why |
|---|---|---|
| Base passive rate | 100 coins/h | Anchor unit. 2,400/day = about 33% of a base day (target 20-35%). |
| Offline cap | 24 h continuous | GDD 6. No extra reward for staying away longer. |
| To-do reward | 250 | Worth 2.5 h of passive. 5/day = 17% of income. |
| Rewarded To-dos per day | 10 | Voice todos are free to create; cap = 2x typical use. **Not in GDD yet.** |
| Pomodoro | 45 min (default), 1,080 coins | GDD 3.3 default 45 min; the player can pause (clock stops) and skip. Priced at 24 coins per focus minute (unchanged from the 25-min draft): worth 10.8 h of passive, about 4.3 To-dos. |
| Pomodoro early end (including skip) | >= 30% active (13.5 of 45 min), paid pro rata | Skip = ending the current work segment early, same rule as early end. Below 30%: no reward, and the game shows a confirmation prompt saying that ending now gives no reward. Decided: at >= 30% the reward is pro rata to active focus time (same coins per minute as a full session, so short sessions are no exploit); full completion pays the full reward. |
| Daily login | 300 + 3 cards every 5th day | Worth 3 h of passive; about 4% of income. |
| Chests on island | 3 | GDD 4. Also caps offline finds. |
| Rewards per chest | 3 | GDD 4. |
| Discovery time | 5 h per chest | About 5 chests/day: 1 after a 6 h day gap, 2 after the night. |
| Chest slot weights | coins 78% / card 20% / pack 2% | Card budget of about 5 draws/day from chests for a 9-card pool. |
| Coin reward | 150 per slot | Worth 1.5 h of passive; worst chest = 450. |
| Tutorial chest | 500 coins + 1 Common + 1 pack | 500 + 1 To-do + 30 min passive = 800 >= first upgrade (500). |
| Blind pack | 5 cards, 3,000 coins | Worth 30 h of passive = 0.4 base days. First shop pack on day 2. |
| Card pool | 5 Common / 3 Treasured / 1 Special | Fills the 3x3 gallery (8 artworks exist, 1 needed). |
| Rarity weights | 79% / 20% / 1% | Special at 1% sets "complete around day 7". |
| Special pity | After 100 draws without a new Special, the next draw is a new Special | With 1 Special: guaranteed by draw 100, which caps bad luck at about day 8. |
| Duplicate values | 50 / 100 / 250 | Far below pack price; 5 Specials = 1,250 < 3,000. |
| Coin rate upgrade | 500 x 1.45^(L-1), 10 levels, +10 coins/h each | Max = 2x income; payback grows from 50 h to about 1,400 h. |
| Exploration upgrade | 800 x 1.5^(L-1), 5 levels, -8% discovery each | Max -40% time = about +60% chests/day. |
| Luck upgrade | 1,000 x 1.6^(L-1), 5 levels, +1.5% card and +0.4% pack weight each | Collector upgrade; steepest curve. |

Exploration and luck never change the passive rate (GDD 5).

## 3. Sources and Sinks

### 3.1 Sources (typical day, day 2+)

| Source | Coins/day at start | Share | Coins/day fully upgraded | Share | Coins per hour (24 h day) at start |
|---|---|---|---|---|---|
| Passive | 2,400 | 33% | 4,800 | 46% | 100 |
| Chest coins | 1,685 | 23% | 2,363 | 23% | 70 |
| To-do | 1,250 | 17% | 1,250 | 12% | 52 |
| Pomodoro (1 x 45 min) | 1,080 | 15% | 1,080 | 10% | 45 |
| Daily login | 300 | 4% | 300 | 3% | 13 |
| Duplicates | 614 | 8% | 614 | 6% | 26 |
| **Total** | **7,329** | | **10,407** | | **305** |

- Utility share (To-do + Pomodoro): **32% at start, 22% fully upgraded.**
- Per online hour (1.5 h/day) the base day is worth about 4,890 coins.
- Chests: 4.8/day at start, 7.7/day with max exploration.

### 3.2 Sinks

| Sink | Coins | Days of base income |
|---|---|---|
| Coin rate, all 10 levels | 44,540 | 6.1 |
| Exploration, all 5 levels | 10,550 | 1.4 |
| Luck, all 5 levels | 15,810 | 2.2 |
| **All upgrades** | **70,900** | **9.7** |
| One blind pack | 3,000 | 0.4 |
| Skins: character skin colors, pets, island skins / decorations | Out of MVP; prices not set | - |

Skins (character skin colors, pets, island skins and decorations) are cosmetic shop items with no stat effect. There is only one island; island themes are sold as island skins. Because skins have no stat effect, they never change passive income, chests or luck. They are a late sink for coins once upgrades run out; price them after the post-MVP pool size is known.

## 4. Upgrade Curve

Cost of level L = ROUND(Base x Growth^(L-1), -1). The effect grows linearly while the cost grows exponentially, so every upgrade pays back more slowly than the one before.

| Level | Coin rate cost | Coins/h after | Exploration cost | Discovery h after | Luck cost | Card / pack weight after |
|---|---|---|---|---|---|---|
| 1 | 500 | 110 | 800 | 4.6 | 1,000 | 21.5% / 2.4% |
| 2 | 730 | 120 | 1,200 | 4.2 | 1,600 | 23.0% / 2.8% |
| 3 | 1,050 | 130 | 1,800 | 3.8 | 2,560 | 24.5% / 3.2% |
| 4 | 1,520 | 140 | 2,700 | 3.4 | 4,100 | 26.0% / 3.6% |
| 5 | 2,210 | 150 | 4,050 | 3.0 | 6,550 | 27.5% / 4.0% |
| 6 | 3,200 | 160 | - | - | - | - |
| 7 | 4,650 | 170 | - | - | - | - |
| 8 | 6,740 | 180 | - | - | - | - |
| 9 | 9,770 | 190 | - | - | - | - |
| 10 | 14,170 | 200 | - | - | - | - |

## 5. Chests, Packs, Rarity and Duplicates

### 5.1 Chest reward slots (each chest rolls 3 slots)

| Slot result | Weight (luck 0) | Weight (luck 5) | Payout |
|---|---|---|---|
| Coins | 78% | 68.5% | 150 coins |
| Card | 20% | 27.5% | 1 card |
| Blind pack | 2% | 4% | 5 cards |

- Expected per chest at luck 0: 2.3 coin slots (351 coins) and 0.9 cards.
- Worst chest: 3 coin slots = 450 coins (GDD floor).

### 5.2 Rarity (per card drawn, packs included)

| Rarity | Cards in pool | Weight | Chance of one specific card |
|---|---|---|---|
| Common (silver-gray) | 5 | 79% | 15.8% |
| Treasured (gold) | 3 | 20% | 6.7% |
| Special (purple) | 1 | 1% | 1.0% (guaranteed by draw 100) |

### 5.3 Duplicates

| Rarity | Coins per duplicate | 5 duplicates vs pack price |
|---|---|---|
| Common | 50 | 250 < 3,000 |
| Treasured | 100 | 500 < 3,000 |
| Special | 250 | 1,250 < 3,000 |

Expected duplicate return per pack is 310 coins (about 10% of the price), so packs can never fund packs.

## 6. Constraint Checks

All 19 checks pass with the current parameters (`Constraint Checks` sheet).

| Rule | Result | Values |
|---|---|---|
| 5 Special duplicates < 1 pack | Pass | 1,250 < 3,000 |
| Duplicate value rises with rarity | Pass | 50 < 100 < 250 |
| Minimum chest = 3 coin rewards (coin weight > 0 at max luck) | Pass | 450 coins; coin weight 68.5% |
| Offline = rate x min(t, 24 h) | Pass | 30 h away pays 2,400; 8 h pays 800 |
| No loop: all-Special pack of duplicates < pack price | Pass | 1,250 < 3,000 |
| No loop: expected duplicate return < pack price | Pass | 310 < 3,000 |
| Rarity weights sum to 100% | Pass | 100% |
| Exploration / luck do not change passive rate | Pass | - |
| Utility share >= 25% at start | Pass | 31.8% |
| Utility share >= 20% fully upgraded | Pass | 22.4% |
| Passive share >= 20% at start | Pass | 32.7% |
| Pomodoro early end pays no more per minute than full | Pass | 24 = 24 coins/min |
| To-do cap covers typical use | Pass | 10 >= 5 |
| First upgrade in session 1 | Pass | 800 >= 500 |
| First blind pack on day 1 | Pass | Tutorial pack |
| Collection not finished before about day 7 | Pass | P(complete after day 6) = 49% |
| Collection close by day 7 | Pass | 8.5 of 9 cards expected |
| Upgrades not all bought in week 1 | Pass | 17 of 20 |

## 7. Seven-Day Pacing

### 7.1 Daily summary (expected values)

| Day | Coins earned | Upgrades owned (end) | Coin rate lvl | Shop packs (total) | Chests found | Cards owned (of 9) | P(complete) |
|---|---|---|---|---|---|---|---|
| 1 | 5,513 | 5 | 3 | 0 | 2.8 + tutorial | 5.3 | 0% |
| 2 | 7,719 | 8 | 4 | 1 | 5.2 | 7.2 | 5% |
| 3 | 8,378 | 11 | 5 | 1 | 5.7 | 7.6 | 12% |
| 4 | 8,630 | 13 | 6 | 2 | 6.3 | 8.1 | 25% |
| 5 | 9,495 | 15 | 6 | 3 | 7.1 | 8.3 | 39% |
| 6 | 9,885 | 16 | 7 | 4 | 7.7 | 8.5 | 49% |
| 7 | 10,051 | 17 | 7 | 4 | 7.7 | 8.5 | 54% |

### 7.2 Time-lapse milestones (`Timeline` sheet, hourly, hour 0 = install at 08:00)

| Hour | Day / time | Event |
|---|---|---|
| 0 | Day 1 08:00 | Install, tutorial chest, first upgrade |
| 6 | Day 1 14:00 | First island chest opened; half the collection (mostly from the tutorial pack) |
| 24 | Day 2 08:00 | Three chests waiting after the night |
| 37 | Day 2 21:00 | First shop pack |
| 48 | Day 3 08:00 | 10 upgrades owned, coin rate level 5 |
| 144 | Day 7 08:00 | Collection complete for half of players |
| 167 | Day 8 07:00 | End: 42 chests opened, 62,423 coins earned, 17 upgrades |

The Timeline sheet has one row per hour (168 rows). It holds coin balance, cumulative coins per source, chests found and opened, expected cards owned, upgrade levels and an event label, ready for an animated chart.

## 8. MVP Subset

The MVP build (Section 9.3, Production Scope) cuts several systems that the full economy relies on. The MVP numbers are in the `MVP value` column on the Params sheet. The `MVP Summary` and `MVP Timeline` sheets recalculate from that column, so the full design above is unchanged.

### 8.1 What is cut and why

| System | MVP | Why | Economy impact |
|---|---|---|---|
| Daily login | Out | Needs multi-day play; cannot be shown in a 10-minute demo | -300 coins/day, no 5-day cards |
| Pomodoro | Icon only, no function (D7) | Timer and rewards are not needed to prove the loop | -1,080 coins/day; utility depends on To-do alone |
| Exploration, Luck | Out | One stat proves the spend loop | Coin Rate is the only coin sink |
| Shop packs | In (build): "Buy a pack" in the Collection, 3,000, opens at once. Workbook: still out | Added after device test 4 so coins have a second use | Packs no longer come only from chests; the card budget and pacing must be re-modelled (open question 11) |
| Cosmetics | In (build): outfits, clothing colors, pets at placeholder prices (8.8). Workbook: not modelled | Added after device test 4 | A new coin sink with no stat effect; competes with Coin Rate for coins |
| Kept | In | Passive income with the 24 h offline cap. To-do. Onboarding with the tutorial chest (GDD 2.1). 1 island with 3 chest slots and a chest progress bar. Packs from chests. Duplicates. 54-card pool (18 / 18 / 18) in the build; the workbook still uses 9 (3 / 3 / 3). Coin Rate. | - |

### 8.2 Changed parameters

| Parameter | Full | MVP | Why |
|---|---|---|---|
| Login coins / bonus cards | 300 / 3 | 0 / 0 | Cut |
| Pomodoros per day | 1 (45 min) | 0 | Cut: icon only (D7) |
| Exploration / Luck max level | 5 / 5 | 0 / 0 | Cut |
| Pack spend share | 25% | 0% | Workbook: no shop. The build sells packs (3,000); the share is to be set in the re-tune |
| To-do reward | 250 | 320 | To-do is the only utility source left. Keeps utility >= 25% at start and >= 20% at the day-7 level |
| Coin reward per chest slot | 150 | 100 | Otherwise chest coins outgrow To-do coins |
| Chest slot weights (coins / card / pack) | 78 / 20 / 2 | 73 / 22 / 5 | Chests carry the whole card budget: 1.4 cards per chest, and 1 chest in 7 holds a pack |
| Card pool (Common / Treasured / Special) | 5 / 3 / 1 | 18 / 18 / 18 (build); 3 / 3 / 3 (workbook) | D6 changed November 7: 54 cards, 18 per rarity (one 6 × 3 page per rarity). The workbook still models 9 cards |
| Rarity weights (Common / Treasured / Special) | 79 / 20 / 1 | 60 / 30 / 10 | 3 Specials instead of 1. At 3.3% per Special card and about 7 draws a day, P(complete) is under 50% after day 6 and over 50% after day 7 |
| Special pity | Guaranteed by draw 100 | 25 draws without a new Special | Pity rule: after N draws without a new Special, the next draw is a Special the player does not own. The forced draw is the 26th, so the worst case for 3 Specials is draw 78 (about day 10; earlier drafts said 75). With the build's 18 Specials the worst case is draw 468, far past week 1: weights and pity were not re-tuned for 54 cards |
| Coin Rate level-1 cost | 500 | 250 | Cheaper than one To-do, so the onboarding To-do alone pays for level 1 |
| Coin Rate cost growth | 1.45 | 1.21 | Coin Rate is the only sink, so a level must stay affordable every day of week 1 |
| Coin Rate max level | 10 | 20 | The only sink must last past week 1 (total 52,700) |
| Coin Rate effect per level | +10 coins/h | +5 coins/h | 20 levels still top out at 2x income |

All other values match the full design. The tutorial chest is the same as in the full design: 500 coins, 1 Common and 1 pack. The pack price (3,000) is kept only for the duplicate rule.

### 8.3 MVP sources and sinks (typical day 2+)

| Source | Coins/day at start | Share | Coins/day at day-7 level (Coin Rate 19) | Share | Coins per hour at start |
|---|---|---|---|---|---|
| Passive | 2,400 | 43% | 4,680 | 59% | 100 |
| Chest coins | 1,051 | 19% | 1,051 | 13% | 44 |
| To-do | 1,600 | 29% | 1,600 | 20% | 67 |
| Duplicates | 560 | 10% | 560 | 7% | 23 |
| **Total** | **5,611** | | **7,891** | | **234** |

- Chests: 4.8 per day, giving 6.8 card draws and 0.7 packs per day.
- The only sink is Coin Rate: 20 levels, 52,700 coins in total, which is 9.4 days of start income.

MVP Coin Rate curve: cost of level L = ROUND(250 x 1.21^(L-1), -1).

| Level | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Cost | 250 | 300 | 370 | 440 | 540 | 650 | 780 | 950 | 1,150 | 1,390 |
| Coins/h after | 105 | 110 | 115 | 120 | 125 | 130 | 135 | 140 | 145 | 150 |

Levels 11-20 cost 1,680 to 9,350 each.

MVP chest slots and rarity:

| Slot | Weight | Payout |
|---|---|---|
| Coins | 73% | 100 coins |
| Card | 22% | 1 card |
| Blind pack | 5% | 5 cards |

| Rarity | Cards | Weight | Chance of one specific card |
|---|---|---|---|
| Common | 3 | 60% | 20% |
| Treasured | 3 | 30% | 10% |
| Special | 3 | 10% | 3.3% (pity: a new Special after 25 draws without one) |

### 8.4 Seven-day MVP pacing (hourly model, end of each day)

| Day | Coins earned | Coin Rate lvl (end) | Levels bought | Chests opened (total, incl. tutorial) | Packs from chests (total, expected) | Cards owned (of 9) | P(complete) |
|---|---|---|---|---|---|---|---|
| 1 | 4,763 | 8 | 8 | 3 | 1.3 | 5.2 | 0% |
| 2 | 6,662 | 12 | 4 | 8 | 2.1 | 6.6 | 4% |
| 3 | 7,093 | 14 | 2 | 13 | 2.8 | 7.3 | 12% |
| 4 | 7,371 | 16 | 2 | 18 | 3.6 | 7.8 | 23% |
| 5 | 7,267 | 17 | 1 | 22 | 4.2 | 8.0 | 32% |
| 6 | 7,758 | 18 | 1 | 27 | 4.9 | 8.3 | 43% |
| 7 | 7,890 | 19 | 1 | 32 | 5.7 | 8.4 | 53% |

MVP Timeline milestones (hour 0 = install at 08:00):

- Hour 0: onboarding, tutorial chest and pack, first upgrade.
- Hour 6: half the collection; Coin Rate level 5.
- Hour 24: Coin Rate level 10.
- Hour 150: collection complete for half of players.
- 168 h total: 32 chests opened, 50,365 coins earned.

### 8.5 MVP checks

All 22 MVP checks pass (`MVP Summary` sheet).

| Rule | Result | Values |
|---|---|---|
| Cut systems pay nothing | Pass | 0 |
| 5 Special duplicates < 1 pack | Pass | 1,250 < 3,000 |
| Duplicate value rises with rarity | Pass | 50 < 100 < 250 |
| Minimum chest = 3 coin rewards | Pass | 300 coins; coin weight 73% |
| Offline = rate x min(t, 24 h) | Pass | 30 h pays 2,400 |
| Rarity weights sum to 100% | Pass | 100% |
| D6: 9 cards, 3 per rarity | Pass | 3 / 3 / 3 |
| Utility (To-do only) >= 25% at start | Pass | 28.5% |
| Utility >= 20% at the day-7 level | Pass | 20.3% |
| Passive >= 20% at start | Pass | 42.8% |
| First upgrade in session 1 | Pass | 1 To-do + 30 min = 370 >= 250 |
| Tutorial chest gives the first pack in session 1 | Pass | 1 pack |
| Collection not finished before about day 7 | Pass | P(complete after day 6) = 43% |
| Collection close by day 7 | Pass | 8.4 of 9 cards expected |
| Coin Rate not maxed in week 1 | Pass | Level 19 of 20 |
| A Coin Rate level bought every day of week 1 | Pass | Fewest per day = 1 |
| Demo: level 1 not affordable before the To-do | Pass | 5 < 250 |
| Demo: level 1 affordable at step 11 | Pass | about 948 >= 250 |
| Demo tier 2: one chest in about 5 min | Pass | 300 s |
| Demo tier 2: coins about 2/s | Pass | 1.75/s at Coin Rate 1 |
| Demo tier 3: one chest in 10 s | Pass | 10 s |
| Demo tier 3: 30 s fills all 3 chest slots | Pass | 3 |

### 8.6 Time and settle rules (D4, D9)

- **Real exit only.** Only an app close or a system kill is a real exit. The next launch shows the offline earnings window (before the Start screen): saved rate x min(away time, 24 h). There is no minimum away time, so 10 s counts.
- **Pause is not an exit.** A pause of any length (`startTodo`, system dialogs, screen off, glasses off) stays the same session. On resume, the coins and chest discovery for the paused time are added silently, with the same 24 h cap. The economy formula is the same as offline income; only the window is skipped.
- **Fractional coins.** The balance is stored as a fraction (double). Only the offline earnings window shows decimals (5; logs keep 10); the HUD shows whole coins, rounded down.
- **Clock.** Elapsed time comes from the glasses' system time, computed in UTC. A negative elapsed time counts as 0.
- **Chest search.** Offline and paused time only find chests up to the 3 slots. The chest progress bar shows the progress on the next chest. When all 3 slots are full, the search stops until a chest is opened.

### 8.7 Ten-minute demo beats (fresh install or after Reset)

There is no prepared demo save. Every number below is an expected value at 1x unless the speed column says otherwise. The tutorial chest is found after a fixed onboarding delay, not after the 5 h discovery time. Normal chest search starts when onboarding ends; at 1x it takes 5 h per chest, so a normal chest find is shown only at tier 2 or 3.

| Time | Speed | Demo step | What the audience sees | Coins after |
|---|---|---|---|---|
| 0:00-0:15 | 1x | 1 Start | No save, so onboarding starts | 0 |
| 0:15-1:30 | 1x | 2 Controls | One hint card per gesture | about 2 |
| 1:30-2:30 | 1x | 3 Set anchor | Create and name the Work zone; walk away and back | about 4 |
| 2:30-3:00 | 1x | 4-5 Add + finish To-do | Todo tagged `(Work)-`; finish it: +320 | 325 |
| 3:00-3:30 | 1x | 6 Tool menu | Menu below the forward view | about 326 |
| 3:30-4:30 | 1x | 7-8 Island + tutorial chest | Progress bar fills over the onboarding delay; chest gives +500, 1 Common and 1 pack | about 827 |
| 4:30-5:30 | 1x | 9-10 Pack + collection | 5 cards: about 3 NEW and 2 duplicates (+about 118). Collection shows about 4 of 9. With the build's 54 cards almost every card is NEW (about 6 of 54, duplicate coins about 0), so coins are about 830 | about 948 (9-card model) |
| 5:30-6:00 | 1x | 11 Upgrade | Buy Coin Rate level 1 (-250). HUD goes from 100 to 105 coins/h. 3 levels would be affordable | about 698 |
| 6:00-7:00 | 60x, then 1800x | Normal play (optional showcase) | 60x for about 15 s: "60x DEMO" badge, about 1.75 coins/s, the bar moves. Then 1800x for 30 s: one chest every 10 s, all 3 slots full, +1,575 coins. Open one chest (about +219 coins and 1.6 cards). Back to 1x; the badge disappears | about 2,500, about 5 more levels affordable |
| 7:00-8:30 | 1x | 12 Quit + re-enter | Closed for 30 s. Offline earnings window "+0.87500" (105/h x 30 s), then Start. Coins, cards, level, task and chest state are unchanged | previous + 0.875 |
| 8:30-9:30 | 1x | 13 Reset | Two confirmations, back to step 1 | 0 |
| 9:30-10:00 | - | Buffer | - | - |

Demo tiers (income multiplier on passive coins and chest discovery only; values at Coin Rate level 1):

| Tier | Multiplier | Game hours per real minute | Coins per real second | One chest | 3 slots from empty |
|---|---|---|---|---|---|
| 1 | 1x | 0.017 | 0.03 | 5 h | 15 h |
| 2 | 60x | 1 | 1.75 | 5 min | 15 min |
| 3 | 1800x | 30 | 52.5 | 10 s | 30 s |

"Skip offline N hours" runs one normal settle: 8 h at Coin Rate 1 gives 840 coins and 1 chest found (24 h cap).

QA reference for the 9.2b acceptance (passive income and chest discovery only: Coin Rate 1, nothing spent, no To-dos, slots start empty). The `Timeline` sheet also includes To-dos, sessions and purchases, so use this table to check the demo multiplier:

| Game hours | Passive coins | Chests found (max 3) | Real time at 60x | Real time at 1800x |
|---|---|---|---|---|
| 1 | 105 | 0 | 1 min | 2 s |
| 5 | 525 | 1 | 5 min | 10 s |
| 10 | 1,050 | 2 | 10 min | 20 s |
| 15 | 1,575 | 3 | 15 min | 30 s |
| 24 | 2,520 | 3 | 24 min | 48 s |

### 8.8 Shop and cosmetic prices in the build (placeholders)

Set in code on November 7, 2026, not derived from the targets in 1.2. Read in "hours of base passive" (100 coins = 1 h).

| Item | Price | Hours of base passive |
|---|---|---|
| Blind pack ("Buy a pack", Collection) | 3,000 | 30 |
| Outfits: Explorer / Long dress / Overalls / Hooded cloak | 0 / 0 / 1,000 / 2,500 | 0 / 0 / 10 / 25 |
| Clothing colors: Cyan / Green / Gold / Silver / Purple / Pink | 0 / 0 / 500 / 800 / 1,200 / 2,000 | 0 / 0 / 5 / 8 / 12 / 20 |
| Pets: Cat / Dog / Bird / Turtle | 1,500 / 1,500 / 3,000 / 5,000 | 15 / 15 / 30 / 50 |
| All cosmetics | 19,000 | 190 (about 3.4 days of MVP start income) |

Cosmetics have no stat effect. Buying them delays Coin Rate levels, which the MVP pacing in 8.4 does not model.

## 9. Open Questions

1. **To-do farming.** Accept a daily cap of 10 rewarded To-dos? Also consider no reward for a To-do completed less than 1 min after creation.
2. **Chest weights vs GDD example.** GDD example 50:35:15 completes a 9-card pool on day 1. Keep 78:20:2 (full) and 73:22:5 (MVP), or grow the pool to about 40 cards.
3. **Card pool size.** 9 cards last about one week. A longer study needs about 24-30 cards and a retune.
4. **After the collection is complete**, every draw becomes duplicate coins and packs lose their purpose. MVP: acceptable. Later: new card pool or seasonal pool, new island skin.
5. **Login clock abuse.** Changing the system date grants extra login days. Accept for the MVP, or require monotonic time.
6. **Utility rewards do not scale.** Passive doubles with upgrades while To-do and Pomodoro stay fixed (utility share falls from 32% to 22%). Option: pay "N hours of current passive rate". Decide before adding coin-rate levels.
7. **Player profile is a guess.** Session count and gap lengths drive chest count most. Replace it with device-test data.
8. **Expected values only.** With rare Specials (1% full, 3.3% per card in the MVP), individual players differ by several days. Run a Monte Carlo pass before the user study.
9. **Full-design card pool.** The MVP pool is 3 / 3 / 3 (D6). The full-design column still uses 5 / 3 / 1 with 79 / 20 / 1. Align it once the post-MVP pool size is known (see question 3).
10. **Tutorial chest and the 3 slots.** This draft assumes the tutorial chest is separate from the 3 island chest slots, and normal search starts with 0 found chests when onboarding ends. Confirm with the island design (W3). The build does this: onboarding places the tutorial chest in a free chest area, but it does not count as one of the 3 island chest slots, and normal search starts at 0 found chests when onboarding ends.
11. **Re-tune for the build (TO DESIGN).** 54 cards (18 / 18 / 18), shop packs and cosmetics are in the build but not in the workbook. Re-model rarity weights, pity, chest weights, pack price and the collection pace target, and add the cosmetic sink. This also depends on the opening-engagement redesign (GDD Q4), which may change the coin scale.
12. **Cosmetic prices.** The prices in 8.8 are placeholders. Derive them from a target (e.g. one cosmetic per 1-2 days of play) during the re-tune.
