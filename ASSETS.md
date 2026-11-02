# Asset guide

## Audio - DONE
- Sound effects: `Content/sfx/<name>.wav`  (Importer: Wav Importer, Processor: Sound Effect)
- Music: `Content/Music/<name>.ogg`          (Importer: Ogg Importer, Processor: Song)
- Full list of names: `AudioManager.SoundNames` in Core/AudioManager.cs
- New story sounds play a stand-in until you add their own file (see `Fallbacks`):
  block, adapt, crusher, switch, fragment, laser_beam, alarm, surge, relay_destroyed, m4_talk
- Music: level5 and level6 currently reuse level2 / level4, and the three boss fights
  reuse boss.ogg. Add level5.ogg, level6.ogg, boss_guardian.ogg, boss_warden.ogg or
  boss_core.ogg to Content/Music (and Content.mgcb) and they are used automatically.
- Per-sound volume: the `Mix` table at the top of Core/AudioManager.cs
- New sound: add the .wav in the MGCB editor, add its name to `SoundNames`,
  then call `AudioManager.Play("name")` where it should happen.

Processing applied to the delivered files:
- Music re-encoded to real OGG Vorbis (~160 kbps), all at -16 LUFS so no track is louder than another.
- Sound effects levelled to the same average loudness (16-bit mono WAV).
- boss_die.wav created from enemy_die + boss_slam (stand-in, replace any time).
- nexus_blip.wav is a pitched-down, bit-crushed copy of ui_move (it was an identical file before).

## Graphics - NEXT STEP
Every texture is optional. Put the PNG in `Content/Textures/`, add it in the MGCB
editor (Importer: Texture Importer, Processor: Texture), build, run. If a file is
missing, the placeholder rectangles are drawn instead.

Rule for animations: frames go in ONE horizontal strip, all the same size.
Ready-made empty templates at the exact sizes are in `Art/Templates/`
(each coloured box = one frame). Paint over them, delete the guide colours, save.
Do NOT add the templates themselves to Content.mgcb.

| File (Content/Textures/) | Frame size | Frames | Notes |
|---|---|---|---|
| player.png | 32x32 | rows: idle 4, run 6, jump 2, fall 2, attack 4, dash 2 | 192x192 sheet, faces RIGHT. See Art/player_sheet_guide.png |
| enemy_normal.png | 32x32 | 1+ (loops) | Security Unit, faces RIGHT (flipped automatically) |
| enemy_fast.png | 28x24 | 1+ | Interceptor |
| enemy_tank.png | 40x40 | 1+ | Heavy Enforcer (front faces RIGHT = shield side) |
| enemy_laser.png | 32x32 | 1+ | Sentry Unit (shooter) |
| enemy.png | 32x32 | 1+ | fallback for any enemy type without its own file |
| boss.png | 96x96 | 1+ | THE CORE's combat body, tinted per phase |
| projectile.png | 8x8 | 1+ | player shot |
| projectile_enemy.png | 8x8 | 1+ | enemy shot (falls back to projectile.png) |
| tile.png | 32x32 | 1+ variations | a random variation is picked per tile |
| tile_top.png | 32x32 | 1+ variations | used where a tile has open air above (grass/metal edge) |
| crystal.png | 16x16 | 1+ | energy cell |
| health.png | 24x24 | 1+ | armor upgrade |
| evolution.png | 32x32 | 1+ | glow + bobbing added in code |
| door.png | 64x96 | 2 | 0 = sealed, 1 = unlocked |
| checkpoint.png | 32x64 | 2 | 0 = inactive, 1 = active |
| terminal.png | 24x48 | 2 | 0 = unread, 1 = read |
| guardian.png | 64x64 | 1+ | PROTOTYPE G-0 (Area 3 boss) |
| warden.png | 40x64 | 1+ | WARDEN-7 |
| m4.png | 26x26 | 1+ | M-4, floating maintenance drone |

| acid.png | 32x64 | 1+ | top half = surface, bottom half = deep liquid (used under another acid tile) |
| acid_level4.png | 32x64 | 1+ | optional per-level version (acid_<levelId>.png), here molten metal |
| laser.png | 16x96 | 2 | 0 = off, 1 = on |
| platform.png | 96x16 | 1 | moving platform |
| turret.png | 28x28 | 5 | 0 = ceiling mount (static); 1-4 = gun head idle / alert / fire / recoil, barrel pointing right, rotated around (14,14) |
| relay.png | 32x64 | 1+ | power relay (Area 5) |
| drone.png | 22x22 | 1+ | security drone summoned by THE CORE |

| cage.png | 40x40 | 1 | M-4's cage |
| conveyor.png | 32x32 | 1+ | belt tile, animates while running |
| crusher.png | 32x24 | 2 | 0 = idle, 1 = about to slam |
| switch.png | 24x40 | 2 | 0 = off, 1 = on |
| fragment.png | 16x16 | 1+ | research data fragment |
| orb.png | 14x14 | 1+ | THE CORE's energy orb |
| shockwave.png | 16x26 | 1+ | ground shockwave |
| spikes.png | 32x32 | 1 | floor spikes (only visible pixels hurt) |
| crumble.png | 96x16 | 2 | crumbling platform: 0 = intact, 1 = cracking |
| flame_vent.png | 32x96 | 4 | 0 = off, 1 = warning, 2-3 = fire; nozzle at the bottom |
| drone.png / relay.png | | | the animation loops the first 4 frames; extra frames are free for destroyed states |
| Menu/background.png | any | 1 | optional: replaces the animated menu scene |
| Menu/title.png | any | 1 | optional: logo instead of the text title |
| Backgrounds/far.png | any (e.g. 640x360) | 1 | slow parallax, scaled to screen height, repeats sideways |
| Backgrounds/near.png | any, transparent | 1 | faster parallax layer |
| Backgrounds/level1_far.png ... | | | optional per-level backgrounds (level1..level4, _far/_near) |

Tips
- Draw at 1x pixel size; the game uses point filtering so pixels stay sharp.
- Keep the robot readable: a strong silhouette and the cyan eye matter more than detail.
- Start with player.png, tile.png, tile_top.png and one background - that alone changes the whole look.

## Collision and highlights
- Damage uses the sprites' visible pixels (alpha > 40): enemies, bullets, spikes, flames,
  lasers, acid and crushers only hit where something is actually drawn.
  Walls and floors still use the 32x32 tiles so movement stays solid.
- Highlights (terminals, switches, M-4, upgrade cores, G-0's shield) are outlines that
  follow the sprite's pixels. Transparent areas are never highlighted.

## Enemy animations (walk / jump / attack)
Enemy sheets can now have up to three rows, frames left to right, same frame size:
- row 0 = walk, row 1 = jump / airborne, row 2 = attack (wind-up -> strike)
A sheet with only row 0 (like the current ones) still works: the game holds a stride
frame in the air, shudders during wind-ups, recoils after shots, and interceptors lean
into their leap. `Art/Templates/enemy_*_3rows.png` contain your walk frames in row 0 and
faint copies in rows 1-2 to draw the new poses over.
Attack row timing: security units (wind-up 0.45s, claw swing 0.18s), interceptors
(crouch 0.3s, then the jump row during the pounce), enforcers (wind-up/charge),
sentries (0.3s after each shot).

## Lockdown gates
`gate.png` (optional) = 2 frames of 32x32: 0 = open emitter, 1 = sealed energy barrier.

## Boss sheets
Same layout as enemies: frames of the boss size, one row per animation.
| Sheet | Frame | Row 0 | Row 1 | Row 2 |
|---|---|---|---|---|
| guardian.png | 64x64 | walk | leap (0 rising ... 3 falling) | 0-1 wind-up, 1-2 charge, 2 shot, 3 landing |
| warden.png | 40x64 | stalk | 0 wind-up, 1-3 charge, pounce by height | strike 0-3 per shot, 3 landing |
| boss.png | 96x96 | hover | 0 rise, 1-2 dive, 3 stuck | 0-1 laser warning, 2 sweep / summon, 1 then 3 after a shot |
