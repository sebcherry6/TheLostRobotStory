# Levels and story data

## Files
- `Content/Levels/levelN.txt`  - the maps (one character = one 32x32 tile)
- `Content/Levels/levels.json` - per-level settings: title, next level, music, boss,
  exit requirements, colours, trigger actions
- `Content/Story/story.json`   - intro, every dialogue, terminal logs, endings
- `Tools/LevelBuilder/`         - the scripts that generated the maps (optional)

## Map legend
| Char | Meaning | Char | Meaning |
|---|---|---|---|
| # | solid | < > | conveyor belt (left / right) |
| P | player start | D | exit door (top-left of 2x3) |
| C | charging station (2 tall) | T | lore terminal |
| E | Security Unit | F | Interceptor |
| K | Heavy Enforcer | S | Sentry (shooter) |
| U | turret | Y | power relay (2 tall) |
| B | boss (type from levels.json) | W | WARDEN-7 encounter point |
| G | reinforcement spawn point | V | crusher (under a ceiling) |
| Z | power switch | R | research data fragment |
| N | M-4 | J | M-4's cage |
| O | energy cell | X | upgrade core (stage from levels.json) |
| H | armour upgrade | A | acid / molten metal (colour from levels.json) |
| L | laser gate (3 tall) | M / I | moving platform / lift |
| Q | crumbling platform (3 wide) | ^ | spikes |
| f | flame vent (fire 3 tiles up) | g | lockdown gate |
| 1-9 | story trigger zone | | |

## levels.json fields
- `requires`: what opens the exit - `enemies`, `crystals`, `switches`, `relays`,
  `fragments` (number), `boss`, `meetM4`
- `coreStage`: upgrade given by X (1 mag-thrusters, 2 welding arc, 3 arc splitter, 4 overclock)
- `boss`: guardian | warden | core,  `bossDropStage` / `bossDropHealth`: salvage after the fight
- `switchesStopMachines`: all switches on -> crushers and belts stop
- `escapeSeconds`: countdown started by the `escape` action
- `startActions`: actions every time the level starts
- `triggers`: `"1": ["alarm", "escape"]` - actions every time trigger 1 is entered
- `background`, `tileColor`, `hazardColor`: hex colours for the placeholder look

## Story events (story.json -> events)
Played once per save. Ids used by the game:
- `<level>_start`, `<level>_t<N>` (trigger), `<level>_m4` (talking to M-4)
- `<level>_power` (all switches on), `<level>_relays` (all relays down)
- `<level>_frag<N>` (data fragment picked up), `first_checkpoint`
- `evolution_<stage>`, `<boss>_intro`, `<boss>_defeated` (guardian, warden, core),
  `warden_cameo_retreat`, `m4_rescued`, `level5_start_alone`, `hangar_m4`, `hangar_alone`

A dialogue line can run an action when it appears: `{"speaker": "", "text": "", "action": "surge"}`.
Actions: surge, alarm, alarm_off, escape, shake, save, give:<stage>,
m4:follow, m4:capture, warden, music:<name>, flag:<name>.
Speakers with special colours: R-01, THE CORE (glitches), M-4, WARDEN-7, SYSTEM, ALERT, DATA, "" (narration).

## Endings
- **Together**: M-4 was rescued in Area 4
- **Alone**: M-4 was left behind
- **Liberation**: all research data fragments collected (one choice appears at the end)

## Laser gates
A laser gate is exactly 3 tiles tall, so it must sit in a 3-tile-high opening with solid
tiles directly above it. The level builder's `laser()` adds that lintel automatically.

## Lockdowns (ambushes)
`g` marks a lockdown gate (open until an ambush). A trigger with the action
`ambush:EEFS` spawns those enemies (map letters E F K S D) at the nearest `G` points,
seals every gate within 30 tiles and opens them again when all are destroyed.
Dying during a lockdown resets it so it can be retried.

## Story presentation
- `chapter`, `chapterTitle`, `goal` in levels.json -> title card the first time a level starts.
- Story dialogues show cinematic bars; `focus:m4|cage|boss|warden|door|player` moves the
  camera to what the line is about (it returns to R-01 when the dialogue ends).
- Any event can have an `<id>_alone` version, used when M-4 is not with R-01
  (e.g. `level5_t1_alone`, `level6_start_alone`).
- A "NEW OBJECTIVE" banner appears whenever the current objective changes.
