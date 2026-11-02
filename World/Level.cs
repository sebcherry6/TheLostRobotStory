using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;

namespace TheLostRobotStory.World
{
    // Level file legend (32x32 tiles)
    //   #  solid            <  >  conveyor (moves left / right, solid)
    //   P  player start     D  exit door (top-left of a 2x3 door)
    //   C  checkpoint (2 tiles tall)        T  lore terminal
    //   E  security unit    F  interceptor   K  heavy enforcer
    //   S  sentry (shooter) U  turret        Y  power relay (2 tall)
    //   B  boss (type from levels.json)      W  WARDEN-7 encounter point
    //   G  enemy wave spawn point            V  crusher (under a ceiling)
    //   Z  power switch     R  research data fragment
    //   N  M-4              J  M-4's cage
    //   O  energy cell      X  evolution core   H  armour upgrade
    //   A  acid / molten    L  laser gate (3 tiles tall)
    //   M  moving platform (5 tiles right)    I  lift (5 tiles up)
    //   Q  crumbling platform   ^  spikes   f  flame vent (fire 3 tiles up)
    //   g  lockdown gate (open, seals during an ambush)
    //   1-9 story trigger zones
    public class Level
    {
        // OBJECT LISTS
        public List<Rectangle> _solids = new();
        public List<Collectible> _collectibles = new();
        public List<Enemy> _enemies = new();
        public List<Door> _doors = new();
        public List<EvolutionCore> _evolutionCores = new();
        public List<Checkpoint> _checkpoints = new();
        public List<HealthUpgrade> _healthUpgrades = new();
        public List<Acid> _acidPools = new();
        public List<LaserGate> _laserGates = new();
        public List<MovingPlatform> _movingPlatforms = new();
        public List<Terminal> _terminals = new();

        public List<Conveyor> _conveyors = new();
        public List<Crusher> _crushers = new();
        public List<PowerSwitch> _switches = new();
        public List<DataFragment> _fragments = new();
        public List<StoryTrigger> _triggers = new();
        public List<Vector2> _waveSpawns = new();
        public List<Spikes> _spikes = new();
        public List<FlameVent> _flameVents = new();

        // lockdown gates ('g'): open until an ambush seals them
        public List<Rectangle> _gates = new();
        private readonly List<Rectangle> _closedGates = new();
        public bool GatesClosed => _closedGates.Count > 0;

        public NpcM4 M4;
        public Vector2? M4Spawn;
        public Vector2? CagePosition;
        public Vector2? WardenSpawn;

        public Vector2 _spawnPoint;
        public string CurrentLevelName;
        public LevelInfo Info { get; private set; } = new LevelInfo();

        // progress inside this level
        public bool BossDefeated;
        public bool MachinesDisabled;
        public int RelaysDestroyed;

        public int WidthInPixels { get; private set; }
        public int HeightInPixels { get; private set; }

        private readonly ParticleManager _particles;
        private const int TileSize = 32;

        private HashSet<Point> _solidCells = new();

        public Level(ParticleManager particles = null)
        {
            _particles = particles;
        }

        // =====================================================
        // COUNTERS
        // =====================================================

        public int CollectiblesRemaining => _collectibles.FindAll(c => !c.IsCollected).Count;

        public int EnemiesRemaining =>
            _enemies.FindAll(e => e.CountsAsEnemy && !e.IsDead && !e.Despawned).Count;

        public int SwitchesTotal => _switches.Count;
        public int SwitchesOn => _switches.FindAll(s => s.On).Count;

        public int RelaysRemaining =>
            _enemies.FindAll(e => e.Type == EnemyType.Relay && !e.IsDead).Count;

        public int RelaysTotal => RelaysRemaining + RelaysDestroyed;

        public int FragmentsTotal => _fragments.Count;
        public int FragmentsCollected => _fragments.FindAll(f => f.Collected).Count;

        public bool HasBoss => !string.IsNullOrEmpty(Info.Boss);

        public Boss ActiveBoss
        {
            get
            {
                foreach (Enemy enemy in _enemies)
                {
                    if (enemy is Boss boss && !boss.IsDead && !boss.Despawned)
                        return boss;
                }

                return null;
            }
        }

        // Exit requirements from levels.json (M-4 meeting is checked by the scene)
        public bool RequirementsMet()
        {
            LevelRequirements r = Info.Requires;

            if (r.Enemies && EnemiesRemaining > 0) return false;
            if (r.Crystals && CollectiblesRemaining > 0) return false;
            if (r.Switches && SwitchesOn < SwitchesTotal) return false;
            if (r.Relays && RelaysRemaining > 0) return false;
            if (r.Fragments > 0 && FragmentsCollected < r.Fragments) return false;
            if (r.Boss && !BossDefeated) return false;

            return true;
        }

        public bool IsCleared() => RequirementsMet();

        public bool IsSolidAt(Vector2 point)
        {
            if (point.X < 0 || point.Y < 0)
                return true;

            return _solidCells.Contains(new Point((int)point.X / TileSize, (int)point.Y / TileSize));
        }

        // =====================================================
        // LOAD
        // =====================================================

        public void Load(string filePath)
        {
            CurrentLevelName = Path.GetFileName(filePath);
            Info = LevelDatabase.Get(CurrentLevelName);

            string levelId = Info.Id;
            string[] lines = File.ReadAllLines(filePath);

            var bossCells = new List<(Boss boss, Point cell)>();

            int terminalIndex = 0;
            int switchIndex = 0;
            int fragmentIndex = 0;
            int relayIndex = 0;
            int crusherIndex = 0;

            int width = 0;

            for (int y = 0; y < lines.Length; y++)
            {
                width = Math.Max(width, lines[y].Length);

                for (int x = 0; x < lines[y].Length; x++)
                {
                    char tile = lines[y][x];
                    Vector2 pos = new Vector2(x * TileSize, y * TileSize);
                    Rectangle rect = new Rectangle((int)pos.X, (int)pos.Y, TileSize, TileSize);

                    switch (tile)
                    {
                        case '#':
                            AddSolid(rect, x, y);
                            break;

                        case '<':
                        case '>':
                            AddSolid(rect, x, y);
                            _conveyors.Add(new Conveyor(rect, tile == '<' ? -1 : 1));
                            break;

                        case 'P': _spawnPoint = pos; break;

                        case 'E': _enemies.Add(new Enemy(pos, EnemyType.Normal)); break;
                        case 'F': _enemies.Add(new Enemy(pos, EnemyType.Fast)); break;
                        case 'K': _enemies.Add(new Enemy(pos, EnemyType.Tank)); break;
                        case 'S': _enemies.Add(new Enemy(pos, EnemyType.Laser)); break;
                        case 'U': _enemies.Add(new Enemy(pos, EnemyType.Turret)); break;

                        case 'Y':
                            _enemies.Add(new Enemy(pos, EnemyType.Relay) { Tag = $"relay:{levelId}:{relayIndex++}" });
                            break;

                        case 'B':
                            Boss boss = CreateBoss(pos);
                            _enemies.Add(boss);
                            bossCells.Add((boss, new Point(x, y)));
                            break;

                        case 'W': WardenSpawn = pos; break;
                        case 'G': _waveSpawns.Add(pos); break;
                        case 'N': M4Spawn = pos; break;
                        case 'J': CagePosition = pos; break;

                        case 'D': _doors.Add(new Door(pos, Info.Next)); break;
                        case 'O': _collectibles.Add(new Collectible(pos, CollectibleType.EnergyCrystal)); break;
                        case 'X': _evolutionCores.Add(new EvolutionCore(pos, Info.CoreStage)); break;
                        case 'C': _checkpoints.Add(new Checkpoint(pos)); break;
                        case 'H': _healthUpgrades.Add(new HealthUpgrade(pos)); break;
                        case 'A': _acidPools.Add(new Acid(pos, _particles)); break;
                        case 'L': _laserGates.Add(new LaserGate(pos, _particles)); break;

                        case 'M':
                            _movingPlatforms.Add(new MovingPlatform(pos, pos + new Vector2(160, 0)));
                            break;

                        case 'I':
                            _movingPlatforms.Add(new MovingPlatform(pos, pos + new Vector2(0, -160), speed: 60f));
                            break;

                        case 'T':
                            _terminals.Add(new Terminal(pos, levelId + "_log" + terminalIndex++));
                            break;

                        case 'Z':
                            _switches.Add(new PowerSwitch(pos, $"switch:{levelId}:{switchIndex++}"));
                            break;

                        case 'R':
                            _fragments.Add(new DataFragment(pos, $"frag:{levelId}:{fragmentIndex++}"));
                            break;

                        case 'V':
                            _crushers.Add(new Crusher(pos, 0, crusherIndex++ * 0.7f));
                            break;

                        case 'Q':
                            _movingPlatforms.Add(MovingPlatform.Crumbling(pos));
                            break;

                        case '^':
                            _spikes.Add(new Spikes(pos));
                            break;

                        case 'g':
                            _gates.Add(rect);
                            break;

                        case 'f':
                            _flameVents.Add(new FlameVent(pos, _flameVents.Count * 0.9f));
                            break;

                        default:
                            if (tile >= '1' && tile <= '9')
                                _triggers.Add(new StoryTrigger(pos, levelId, tile));
                            break;
                    }
                }
            }

            WidthInPixels = width * TileSize;
            HeightInPixels = lines.Length * TileSize;

            // crushers travel down until the floor below them
            for (int i = 0; i < _crushers.Count; i++)
            {
                Rectangle head = _crushers[i].Head;
                int cx = head.X / TileSize;
                int cy = head.Y / TileSize;
                int free = 0;

                while (free < 12 && !_solidCells.Contains(new Point(cx, cy + free)))
                    free++;

                _crushers[i] = new Crusher(new Vector2(head.X, head.Y), free * TileSize - 24, i * 0.7f);
            }

            foreach (var (boss, cell) in bossCells)
                boss.Arena = ComputeArena(cell);

            Acid.Tint = Info.HazardTint;
            Acid.Sheet = TextureManager.Get("acid_" + levelId) ?? TextureManager.Acid;

            // acid tiles with acid above them draw the deep part of the sheet
            var acidCells = new HashSet<Point>();

            foreach (Acid acid in _acidPools)
                acidCells.Add(new Point(acid.Bounds.X / TileSize, acid.Bounds.Y / TileSize));

            foreach (Acid acid in _acidPools)
                acid.IsSurface = !acidCells.Contains(new Point(acid.Bounds.X / TileSize, acid.Bounds.Y / TileSize - 1));
            Enemy.SolidQuery = IsSolidAt;
        }

        // Seal (or open) every gate within radius of a point
        public void SetGates(bool closed, Vector2 near, float radius)
        {
            if (closed)
            {
                foreach (Rectangle gate in _gates)
                {
                    if (_closedGates.Contains(gate) || Vector2.Distance(gate.Center.ToVector2(), near) > radius)
                        continue;

                    _closedGates.Add(gate);
                    _solids.Add(gate);
                    _solidCells.Add(new Point(gate.X / TileSize, gate.Y / TileSize));
                }
            }
            else
            {
                foreach (Rectangle gate in _closedGates)
                {
                    _solids.Remove(gate);
                    _solidCells.Remove(new Point(gate.X / TileSize, gate.Y / TileSize));
                }

                _closedGates.Clear();
            }
        }

        private void DrawGates(SpriteBatch spriteBatch)
        {
            Texture2D texture = TextureManager.Get("gate");
            float t = Sprites.Time;

            foreach (Rectangle gate in _gates)
            {
                bool closed = _closedGates.Contains(gate);

                if (texture != null)
                {
                    Sprites.DrawFrame(spriteBatch, texture, gate, closed ? 1 : 0, Color.White, 32);
                    continue;
                }

                if (closed)
                {
                    // flickering red energy bars
                    for (int i = 0; i < 4; i++)
                    {
                        float flicker = 0.6f + 0.4f * (float)Math.Sin(t * 20f + i * 1.7f + gate.Y * 0.1f);
                        spriteBatch.Draw(TextureManager.Pixel, new Rectangle(gate.X + 6 + i * 6, gate.Y, 3, gate.Height), new Color(255, 50, 60) * flicker);
                    }
                }
                else
                {
                    // dormant emitter marks
                    spriteBatch.Draw(TextureManager.Pixel, new Rectangle(gate.X + 4, gate.Y + gate.Height - 3, gate.Width - 8, 3), new Color(255, 50, 60) * 0.25f);
                }
            }
        }

        private void AddSolid(Rectangle rect, int x, int y)
        {
            _solids.Add(rect);
            _solidCells.Add(new Point(x, y));
        }

        private Boss CreateBoss(Vector2 pos)
        {
            switch (Info.Boss)
            {
                case "guardian": return new GuardianBoss(pos);
                case "warden": return new WardenBoss(pos, false);
                default: return new CoreBoss(pos);
            }
        }

        // The open room around a cell: scan left/right/up/down to the walls
        public Rectangle ComputeArena(Point cell)
        {
            int left = cell.X, right = cell.X, top = cell.Y, bottom = cell.Y;

            while (left > 0 && !_solidCells.Contains(new Point(left - 1, cell.Y))) left--;
            while (right < 500 && !_solidCells.Contains(new Point(right + 1, cell.Y))) right++;
            while (top > 0 && !_solidCells.Contains(new Point(cell.X, top - 1))) top--;
            while (bottom < 500 && !_solidCells.Contains(new Point(cell.X, bottom + 1))) bottom++;

            return new Rectangle(left * TileSize, top * TileSize, (right - left + 1) * TileSize, (bottom - top + 1) * TileSize);
        }

        public Rectangle ComputeArena(Vector2 worldPosition) =>
            ComputeArena(new Point((int)worldPosition.X / TileSize, (int)worldPosition.Y / TileSize));

        // =====================================================
        // DRAW
        // =====================================================

        public void Draw(SpriteBatch spriteBatch)
        {
            DrawSolids(spriteBatch);

            foreach (Conveyor conveyor in _conveyors)
                conveyor.Draw(spriteBatch, MachinesDisabled);

            foreach (var platform in _movingPlatforms) platform.Draw(spriteBatch);
            foreach (var acid in _acidPools) acid.Draw(spriteBatch);
            foreach (var laser in _laserGates) laser.Draw(spriteBatch);
            foreach (var item in _collectibles) item.Draw(spriteBatch);
            foreach (var fragment in _fragments) fragment.Draw(spriteBatch);
            foreach (var core in _evolutionCores) core.Draw(spriteBatch);
            foreach (var health in _healthUpgrades) health.Draw(spriteBatch);
            foreach (var checkpoint in _checkpoints) checkpoint.Draw(spriteBatch);
            foreach (var door in _doors) door.Draw(spriteBatch);
            foreach (var terminal in _terminals) terminal.Draw(spriteBatch);
            foreach (var powerSwitch in _switches) powerSwitch.Draw(spriteBatch);

            if (CagePosition.HasValue && (M4 == null || M4.State != NpcM4.NpcState.Captured))
            {
                // empty, opened cage
                NpcM4.DrawCage(spriteBatch, CagePosition.Value);
            }

            M4?.Draw(spriteBatch);

            DrawGates(spriteBatch);

            foreach (var spikes in _spikes) spikes.Draw(spriteBatch);
            foreach (var vent in _flameVents) vent.Draw(spriteBatch);

            foreach (var enemy in _enemies) enemy.Draw(spriteBatch);
            foreach (var crusher in _crushers) crusher.Draw(spriteBatch);
        }

        // tile.png     : strip of 32x32 variations (picked per cell)
        // tile_top.png : same, used where the tile has open air above it
        private void DrawSolids(SpriteBatch spriteBatch)
        {
            Color baseColor = Info.TileTint;
            Color edge = Color.Lerp(baseColor, Color.White, 0.35f);
            Color dark = Color.Lerp(baseColor, Color.Black, 0.25f);

            foreach (Rectangle tile in _solids)
            {
                int cx = tile.X / TileSize;
                int cy = tile.Y / TileSize;

                bool openTop = !_solidCells.Contains(new Point(cx, cy - 1));

                Texture2D texture = openTop
                    ? TextureManager.TileTop ?? TextureManager.Tile
                    : TextureManager.Tile;

                if (texture != null)
                {
                    int variants = Sprites.FrameCount(texture, TileSize);
                    Sprites.DrawFrame(spriteBatch, texture, tile, Sprites.CellHash(cx, cy) % variants, Color.White, TileSize);
                    continue;
                }

                // placeholder: subtle checker so large walls read as structure
                spriteBatch.Draw(TextureManager.Pixel, tile, (cx + cy) % 2 == 0 ? baseColor : dark);

                if (openTop)
                    spriteBatch.Draw(TextureManager.Pixel, new Rectangle(tile.X, tile.Y, tile.Width, 3), edge);
            }
        }
    }
}
