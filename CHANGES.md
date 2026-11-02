# The Lost Robot Story - upgrade pass

Run `git diff` to see every change to existing files. Nothing here has been
compiled yet (it was written without a .NET toolchain), so expect possibly a
couple of small compile fixes on the first build.

## Bugs fixed
- Player moved horizontally twice per frame (HandleMovement + ApplyCollision).
  Speeds were doubled so the game feels the same as before.
- Evolution cores and health upgrades were updated twice per frame.
- Melee could only damage ONE enemy per 0.25s globally; now each enemy can be hit once per swing.
- One player bullet could damage several enemies in the same frame.
- Boss aimed at where the player would be in 20 *seconds* (`velocity * 20`); now ~0.35s.
- Boss spread shots came from its top-left corner; boss hit-flash lasted one frame.
- Boss hover bob was frame-rate dependent; `ArenaShrink` (static) never reset.
- Laser enemies shot at the player from across the whole map; now only within 450px.
- Enemies walked off ledges; they now turn around.
- Level 3 had no evolution core, so stage 2 (double jump) was skipped entirely. Added one.
- Level files were never copied to the build output (fresh clone = crash). Fixed in .csproj.
- Falling out of a level fell forever; there is now a kill plane.

## New systems
- Scenes/ : scene stack (main menu, pause, options, choices, dialogue, story pages, gameplay)
- Core/SaveSystem.cs : JSON save + settings in %APPDATA%/TheLostRobotStory (atomic writes)
- Core/AudioManager.cs : `AudioManager.Play("jump")`, music per level, missing files are ignored
- Story/ + Content/Story/story.json : all narrative text, editable without recompiling
- World/Terminal.cs : 'T' tile in level files = lore log
- Player: stage-3 dash (L / Shift), events, squash/stretch, sprite-sheet ready animation
- Gamepad support, 1280x720, autosave on checkpoints / doors / evolutions

## Controls
A/D move, SPACE jump, J strike, K shoot, L or SHIFT dash, E interact, ESC pause.
Gamepad: stick/D-pad, A jump, X strike, Y/RT shoot, RB dash, B interact, Start pause.

## Assets to add (all optional - the game runs without them)
Add files with the MGCB editor (Content/Content.mgcb). Names must match.

Sound effects - Content/Audio/Sfx/*.wav
  jump, double_jump, dash, shoot, attack, hurt, death,
  enemy_hit, enemy_die, boss_hit, boss_dash, boss_slam, boss_die,
  collect_crystal, health_up, evolve, checkpoint, door_open, terminal,
  ui_move, ui_select, text_blip, nexus_blip

Music - Content/Audio/Music/*.ogg (processor: Song)
  menu, level1, level2, level3, level4, boss, ending

Textures - Content/Textures/*.png
  player (sprite sheet, 32x32 frames, rows: idle 4, run 6, jump 2, fall 2, attack 4, dash 2)
  enemy, boss, projectile, tile, door, crystal, evolution, health, acid, laser, platform, checkpoint

## Not done yet (next steps)
- Boss ignores walls (flies/dashes through them) and its pull moves the player into walls
- Level rules are still hard-coded by file name in Level.cs (enemy types, next level, core stage)
- Enemies/textures other than the player still draw as rectangles
- Dead code: Systems/Collision.cs, World/Tile.cs, Core/GameEvents.cs, GameMessage.cs, GameProgress.cs, Level.Update

## Round 2 - audio integrated, graphics prepared
- AudioManager now loads from the real folders (`sfx/`, `Music/`), preloads all sounds,
  fades music between tracks, lowers music while paused, has a per-sound mix table
  and fallbacks for missing sounds.
- level1.ogg and level2.ogg were WAV files renamed to .ogg (they failed to build),
  boss/level3/menu were MP3s renamed to .ogg. All music is now real OGG Vorbis.
- Added boss_die.wav (stand-in) and registered it in Content.mgcb.
- Core/Sprites.cs + texture support in every object: drop a PNG in, it is used
  automatically (see ASSETS.md). Tile variations + top edges, parallax backgrounds,
  placeholder background pillars until real art exists.
- Art/Templates: empty templates at the exact sizes the code expects.

## Round 3 - full storyline
- Six areas following the story document: Assembly Line 07, Maintenance Tunnels,
  Research Laboratory, The Furnace, Departure Bay, Core Chamber (new maps).
- Level settings moved to Content/Levels/levels.json (no more hard-coded file names).
- New mechanics: conveyor belts, crushers, power switches, power relays that call in
  enemy waves, research data fragments, turrets, story trigger zones, escape countdown,
  M-4 as a companion who can be captured and rescued.
- Enemies rewritten: Security Units chase, Interceptors jump gaps, Heavy Enforcers have
  a front shield and are open after a charge, Sentries/turrets need line of sight.
- Bosses: PROTOTYPE G-0 (adapts to your attack type), WARDEN-7 (short encounter in
  Area 2, full fight in Area 4), THE CORE (orbs, sweeping laser, drones, ground slams).
- Upgrade order follows the story: mag-thrusters, welding arc (from M-4), arc splitter,
  overclock (salvaged from WARDEN-7, or found in Area 5).
- Three endings: together, alone, liberation. See LEVELS.md.

## Round 4 - art integration, traps, pixel collision
- Turret uses the new 5-frame sheet: static ceiling mount + gun head rotated toward the
  player around (14,14), with idle / alert / fire / recoil frames.
- Acid uses the 32x64 frames: surface on top, deep liquid below, all tiles in sync.
  Area 4 uses acid_level4.png (molten metal recolour).
- Pixel-perfect damage collision (transparent pixels ignored) and pixel outlines instead
  of rectangle highlights.
- Main menu: animated scene built from the game sprites (parallax factory, R-01 running
  on the belt, M-4, a tracking turret) with a title and menu panel.
- New traps: crumbling platforms, spikes, flame vents (with generated sprites);
  more moving platforms and a lift in Area 5.
- Laser gates are always placed in 3-tile-high doorways.
- Fixed: enemy animations ran at double speed; drone/relay animations included their
  destroyed frames; laser gates were drawn off-centre in their tile.

## Round 5 - pacing, polish, bigger world
- Player animation glitch fixed: the camera now moves in whole pixels, the sprite is no
  longer stretched by non-integer squash/stretch, and animation timing keeps its remainder.
- Slower, weightier pacing: player, enemies, bosses, bullets and all sprite animations
  run 15-25% slower.
- Bosses get a pulsing red outline; research data fragments and upgrade cores get a
  light pillar, orbiting sparks and a glowing outline.
- Enemies: walk / jump / attack animation states (3-row sheets supported, current
  single-row art still works), security units swing a claw, interceptors crouch and
  pounce, sentries recoil with a muzzle flash.
- All six levels redesigned and roughly doubled in size, with lockdown ambush rooms,
  more traps and secrets; checkpoints reduced to two per level (one in the finale).
- Energy cells now repair 1 point of damage.
- Story: chapter title cards, cinematic bars and camera focus on story moments,
  "NEW OBJECTIVE" banners, R-01's cradle room, M-4-aware dialogue variants.

## Round 6 - player feel, waypoints, reboots
- Fixed the player animation stutter: standing on the floor flickered between grounded
  and airborne (sub-pixel gravity), restarting the run cycle every few frames.
- Player hitbox now matches the robot's visible pixels (measured from player.png),
  so R-01 touches walls and ledge edges with its body instead of transparent padding.
- Slower, smoother movement with acceleration; jumps: hold for full height, release
  early for a short hop, a brief hang at the top, faster fall. Knockback uses momentum.
- Longer, taller melee swing (54 px).
- Waypoints: an arrow on the screen edge (or a marker above the target) points to the
  current objective - M-4, switches, research data, relays, the boss, upgrades, the exit.
  While M-4 is captured, a second orange arrow points to the cage.
- Reboots: 3 respawns at a charging station per area (shown in the HUD). Losing all of
  them restarts the area from the beginning with the progress you had on entering it.

## Round 7 - boss animations, standing jitter
- Fixed R-01 shaking up and down while standing: the ground check now snaps the feet
  exactly onto the tile instead of letting gravity sink them by fractions of a pixel.
- Bosses use the new 3-row sheets:
  PROTOTYPE G-0  row 0 walk, row 1 leap, row 2 wind-up / charge / burst / landing slam
  WARDEN-7       row 0 stalk, row 1 wind-up / charge / pounce, row 2 claw strike per shot
  THE CORE       row 0 hover, row 1 rise / dive / stuck after a slam, row 2 firing, laser
                 telegraph + sweep, summoning drones
- Pixel collision and the red boss outline follow the current boss frame.
