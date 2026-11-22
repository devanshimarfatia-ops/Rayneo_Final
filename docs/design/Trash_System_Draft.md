# Trash and Gradual Automation (Design Draft)

Status: draft, agreed in discussion with Yaxin on 2026-11-22; the parts marked "Prototype" are built on branch
`Yaxin-Mu/Prototype-2`. Not yet in the GDD (Google Docs). Numbers are placeholders until the economy re-tune.

## Why

Playtest feedback: the start is too passive (the game is automated from the first second). The opening should ask for
light, repeated actions that turn into automation the player earns (reference: Cookie Clicker style "manual -> automated").
On the glasses fast repeated tapping is impossible (taps closer than 0.4 s merge into a double tap = Back), so the
repeated action is aim + single tap on the map.

## Core loop

Clear trash -> room to explore -> explore (manual at first, automatic after an upgrade) -> coins -> upgrades, pets,
decorations. The economy is otherwise unchanged: passive income and chests with cards stay.

## Rules

1. **Trash regrows** at one piece every 30 minutes, also while the app is closed.
2. **Capacity 48** = 24 h of regrowth = the offline cap. The map full of trash is the 24 h cap made visible.
3. **Full map stops exploring.** Exploration (and its income) pauses until the player clears some trash.
4. **Clearing pays a little.** Prototype: 5 coins per piece (48 a day = 240 coins, about 2.4 h of base passive income).
5. **Trash beats exploring.** Tapping trash sends the explorer: he stops exploring, walks there, clears it, "+N" coins
   float over his head, then he goes back to exploring. Several taps queue up. Prototype: done (chest search pauses
   while errands run).
6. **Manual exploration first.** Early on the player sends the explorer to explore with a tap; this earns the idle
   coins. An early upgrade unlocks automatic exploration (the current idle behaviour).
7. **Pets pick up trash.** Pets are bought with coins and clear trash on their own (pets gain a function; GDD currently
   says pets are cosmetic only).
8. **Chests and cards.** Chests still come from exploration. Early game: only the tutorial chest. Later (after an
   upgrade) exploration finds chests; chest contents shift from mostly coins early to mostly cards later.
9. **Tutorial.** The island starts with 3 trash; the player clears them (learns aim + tap) and is told trash comes back
   and a full map stops exploring. One trash is spawned on purpose so the player sees it regrow.
10. **Long-term look.** The island grows from wild to stone age, iron age ... cyber island through upgrades and
    decorations bought with coins (reference: Kingdom Two Crowns). No wood/stone crafting.

## Interaction (Prototype: done)

- Map pointer: looking at the map shows a reticle (the SDK gaze dot is hidden everywhere). Aiming at a chest or trash
  draws a cyan frame and a hint; a temple tap runs it. The target is taken at finger-down because the SDK delivers a tap
  about 0.4 s after the finger lifts.
- While the player aims, the map does not follow the body.
- Panels stay open while the player looks at the map (suspended, then shown again).

## Display preferences (Prototype: done)

| Setting | Options | Recommended |
|---|---|---|
| Map size | S 2.9 m / M 2.4 m / L 2.0 m | M |
| Trash style | 48 single pieces / 12 piles x 4 layers | 48 pieces |

Saved per device; Reset does not change them.

## Open questions

1. What drives the chest coin -> card shift: the Luck upgrade (already in the GDD), the era, or both?
2. When does exploration start finding chests (after auto-explore, or a separate upgrade)?
3. Pets: do they also clear offline (then the 24 h cap needs to stay a separate hard rule)? Same speed for every pet?
4. Is "auto clean" a separate upgrade or replaced by pets?
5. How much does one manual exploration pay (suggest: N minutes of the current hourly rate)?
6. Era list, unlock prices and what each era changes.

## Not built yet

Regrowth and save, blocking exploration when full, tutorial trash, manual exploration and the auto-explore upgrade,
pets picking up trash, chest unlock and contents shift, eras.
