using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Entities;
using TheLostRobotStory.World;

namespace TheLostRobotStory.Core
{
    // Runs the simulation of the current level: player, enemies, projectiles,
    // machinery and pickups. Story reactions are left to GameplayScene,
    // which listens to the events below.
    public class GameManager
    {
        public Level CurrentLevel { get; private set; }
        public LevelInfo Info => CurrentLevel?.Info;
        public Player Player { get; private set; }

        public Door ActiveDoor { get; private set; }
        public PowerSwitch NearSwitch { get; private set; }

        public bool ChangeLevelRequested { get; private set; }
        public string NextLevel { get; private set; }

        // Persistent progress (collected items, flags). Set by GameplayScene.
        public SaveData Save;

        // Extra exit condition only the story knows about (e.g. "met M-4")
        public Func<bool> ExtraDoorCondition;

        public event Action<Enemy> EnemyKilled;
        public event Action<string> HealthUpgradeCollected;
        public event Action<DataFragment> FragmentCollected;
        public event Action<PowerSwitch> SwitchActivated;
        public event Action<StoryTrigger> TriggerEntered;
        public event Action<Boss> BossDefeated;
        public event Action<Boss> BossDespawned;

        private readonly WorldManager _worldManager;
        private readonly ParticleManager _particles;
        private readonly List<Projectile> _projectiles;
        private readonly PlayerData _playerData;

        private readonly HashSet<Enemy> _hitThisSwing = new();
        private readonly List<Enemy> _pendingSpawns = new();

        public GameManager(ParticleManager particles, List<Projectile> projectiles)
        {
            _particles = particles;
            _projectiles = projectiles;
            _playerData = new PlayerData();
            _worldManager = new WorldManager(particles);
        }

        // =====================================================
        // LOADING
        // =====================================================

        public void LoadFromSave(SaveData save)
        {
            Save = save;

            _worldManager.LoadLevel(save.CurrentLevel);
            CurrentLevel = _worldManager.CurrentLevel;

            Vector2 spawn = save.HasCheckpoint
                ? new Vector2(save.CheckpointX, save.CheckpointY)
                : CurrentLevel._spawnPoint;

            Player = new Player(spawn);
            Player.RestoreFromSave(save.EvolutionStage, save.MaxHealth, spawn);
            _playerData.Save(Player);

            ApplySaveState();
            _projectiles.Clear();
        }

        public void ChangeLevel(string levelName)
        {
            PlayerData data = Player.GetPlayerData();

            _worldManager.LoadLevel(levelName);
            CurrentLevel = _worldManager.CurrentLevel;

            Player.position = CurrentLevel._spawnPoint;
            Player.LoadPlayerData(data);
            Player.SpawnPoint = CurrentLevel._spawnPoint;
            Player.ResetVelocity();

            ApplySaveState();
            _projectiles.Clear();
        }

        // Re-apply things already done in this level (after loading a save)
        private void ApplySaveState()
        {
            if (Save == null)
                return;

            string id = Info.Id;

            for (int i = 0; i < CurrentLevel._healthUpgrades.Count; i++)
            {
                if (Save.CollectedItems.Contains(HealthUpgradeKey(i)))
                    CurrentLevel._healthUpgrades[i].IsCollected = true;
            }

            foreach (DataFragment fragment in CurrentLevel._fragments)
                fragment.Collected = Save.CollectedItems.Contains(fragment.Key);

            foreach (PowerSwitch powerSwitch in CurrentLevel._switches)
                powerSwitch.On = Save.CollectedItems.Contains(powerSwitch.Key);

            int relaysRemoved = CurrentLevel._enemies.RemoveAll(e =>
                e.Type == EnemyType.Relay && e.Tag != null && Save.CollectedItems.Contains(e.Tag));

            CurrentLevel.RelaysDestroyed += relaysRemoved;

            if (Save.Flags.Contains("boss:" + id))
            {
                CurrentLevel._enemies.RemoveAll(e => e is Boss);
                CurrentLevel.BossDefeated = true;
            }

            foreach (EvolutionCore core in CurrentLevel._evolutionCores)
            {
                if (core.TargetEvolutionStage <= Player.EvolutionStage)
                    core.Collected = true;
            }

            UpdateMachines();
        }

        public string HealthUpgradeKey(int index) => CurrentLevel.CurrentLevelName + ":hp" + index;

        // =====================================================
        // UPDATE
        // =====================================================

        public void Update(GameTime gameTime, InputManager input)
        {
            if (CurrentLevel == null || Player == null)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _worldManager.Update(gameTime, Player);

            UpdateConveyorPush();

            Player.Update(gameTime, input, _projectiles, CurrentLevel._solids, CurrentLevel._movingPlatforms);
            Player.ApplyCollision(CurrentLevel._solids, dt);
            Player.ApplyPlatformCollision(CurrentLevel._movingPlatforms);

            if (Player.AttackStarted)
                _hitThisSwing.Clear();

            foreach (Crusher crusher in CurrentLevel._crushers)
                crusher.Update(dt, Player, CurrentLevel._enemies, _particles);

            foreach (Spikes spikes in CurrentLevel._spikes)
                spikes.Update(Player);

            foreach (FlameVent vent in CurrentLevel._flameVents)
                vent.Update(dt, Player, _particles);

            UpdatePickups(dt);
            UpdateSwitches(input, dt);
            UpdateTriggers();
            UpdateDoors(gameTime, input);
            UpdateEnemies(gameTime);
            UpdateProjectiles(gameTime);

            CurrentLevel.M4?.Update(dt, Player);

            Player.CheckEnemyCollision(CurrentLevel._enemies);
            Player.CheckDeath();
        }

        private void UpdateConveyorPush()
        {
            Player.ConveyorPush = 0;

            if (CurrentLevel.MachinesDisabled)
                return;

            Rectangle feet = new Rectangle(Player.Bounds.X + 2, Player.Bounds.Bottom, Player.Bounds.Width - 4, 2);

            foreach (Conveyor conveyor in CurrentLevel._conveyors)
            {
                if (feet.Intersects(conveyor.Bounds))
                {
                    Player.ConveyorPush = conveyor.Direction * Conveyor.Speed;
                    return;
                }
            }
        }

        private void UpdatePickups(float dt)
        {
            foreach (var collectible in CurrentLevel._collectibles)
            {
                if (!collectible.IsCollected && Player.Bounds.Intersects(collectible.Bounds))
                {
                    collectible.Collect(Player);
                    AudioManager.Play("collect_crystal");

                    // energy cells repair one point of damage
                    if (Player.Health < Player.MaxHealth)
                        Player.Health++;
                    _particles.SpawnExplosion(collectible.Position, Color.Yellow);
                }
            }

            foreach (DataFragment fragment in CurrentLevel._fragments)
            {
                fragment.Update(dt);

                if (!fragment.Collected && Player.Bounds.Intersects(fragment.Bounds))
                {
                    fragment.Collected = true;
                    _particles.SpawnExplosion(fragment.Position, Color.MediumPurple);
                    FragmentCollected?.Invoke(fragment);
                }
            }

            foreach (var core in CurrentLevel._evolutionCores)
            {
                if (!core.Collected && Player.Bounds.Intersects(core.Bounds))
                {
                    int oldStage = Player.EvolutionStage;
                    core.Collect(Player);

                    if (Player.EvolutionStage > oldStage)
                        _particles.SpawnExplosion(core.Position, Color.Cyan);
                }
            }

            for (int i = 0; i < CurrentLevel._healthUpgrades.Count; i++)
            {
                var upgrade = CurrentLevel._healthUpgrades[i];

                if (!upgrade.IsCollected && Player.Bounds.Intersects(upgrade.Bounds))
                {
                    upgrade.Collect(Player);
                    HealthUpgradeCollected?.Invoke(HealthUpgradeKey(i));
                    _particles.SpawnExplosion(upgrade.Position, Color.Red);
                }
            }
        }

        private void UpdateSwitches(InputManager input, float dt)
        {
            NearSwitch = null;

            foreach (PowerSwitch powerSwitch in CurrentLevel._switches)
            {
                powerSwitch.Update(dt);
                powerSwitch.IsPlayerNear = !powerSwitch.On && Player.Bounds.Intersects(powerSwitch.InteractArea);

                if (powerSwitch.IsPlayerNear)
                    NearSwitch = powerSwitch;
            }

            if (NearSwitch != null && input.InteractPressed())
            {
                NearSwitch.On = true;
                AudioManager.Play("switch");
                _particles.SpawnExplosion(NearSwitch.Bounds.Center.ToVector2(), Color.LimeGreen);

                UpdateMachines();
                SwitchActivated?.Invoke(NearSwitch);
            }
        }

        // all switches on -> machinery stops (if the level says so)
        private void UpdateMachines()
        {
            CurrentLevel.MachinesDisabled =
                Info.SwitchesStopMachines &&
                CurrentLevel.SwitchesTotal > 0 &&
                CurrentLevel.SwitchesOn == CurrentLevel.SwitchesTotal;

            foreach (Crusher crusher in CurrentLevel._crushers)
                crusher.Disabled = CurrentLevel.MachinesDisabled;
        }

        private void UpdateTriggers()
        {
            foreach (StoryTrigger trigger in CurrentLevel._triggers)
            {
                if (!trigger.Fired && Player.Bounds.Intersects(trigger.Bounds))
                {
                    trigger.Fired = true;
                    TriggerEntered?.Invoke(trigger);
                }
            }
        }

        public bool ExitOpen =>
            CurrentLevel.RequirementsMet() &&
            (ExtraDoorCondition?.Invoke() ?? true);

        private void UpdateDoors(GameTime gameTime, InputManager input)
        {
            bool open = ExitOpen;

            ActiveDoor = null;

            foreach (var door in CurrentLevel._doors)
            {
                door.CanOpen = open;
                door.IsPlayerNear = Player.Bounds.Intersects(door.Bounds);
                door.Update(gameTime);

                if (!door.IsPlayerNear)
                    continue;

                ActiveDoor = door;

                if (door.CanOpen && input.InteractPressed() && !string.IsNullOrEmpty(door.DestinationLevel))
                {
                    _playerData.Save(Player);
                    ChangeLevelRequested = true;
                    NextLevel = door.DestinationLevel;
                    AudioManager.Play("door_open");
                    break;
                }
            }
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            List<Enemy> enemies = CurrentLevel._enemies;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];

                enemy.Update(gameTime, CurrentLevel._solids, Player, _projectiles);

                // melee
                if (!enemy.IsDead && !enemy.Despawned &&
                    PixelCollision.Overlaps(enemy.GetShape(), Player.AttackHitbox) &&
                    _hitThisSwing.Add(enemy))
                {
                    int side = Player.Center.X < enemy.Center.X ? -1 : 1;
                    bool landed = enemy.TakeHit(1, false, side);

                    _particles.SpawnExplosion(enemy.Center, landed ? Color.Orange : Color.White);
                }

                if (enemy is Boss boss && boss.SpawnedEnemies.Count > 0)
                {
                    _pendingSpawns.AddRange(boss.SpawnedEnemies);
                    boss.SpawnedEnemies.Clear();
                }

                if (enemy.IsDead)
                {
                    if (enemy is Boss defeated)
                    {
                        CurrentLevel.BossDefeated = true;
                        BossDefeated?.Invoke(defeated);
                    }
                    else if (enemy.Type == EnemyType.Relay)
                    {
                        CurrentLevel.RelaysDestroyed++;
                        SpawnWave(enemy);
                    }

                    EnemyKilled?.Invoke(enemy);
                    _particles.SpawnExplosion(enemy.Center, Color.Red);
                    enemies.RemoveAt(i);
                }
                else if (enemy.Despawned)
                {
                    if (enemy is Boss gone)
                        BossDespawned?.Invoke(gone);

                    _particles.SpawnExplosion(enemy.Center, Color.Gray);
                    enemies.RemoveAt(i);
                }
            }

            if (_pendingSpawns.Count > 0)
            {
                enemies.AddRange(_pendingSpawns);
                _pendingSpawns.Clear();
            }
        }

        // Destroying a relay calls in reinforcements from the nearest spawn points
        private void SpawnWave(Enemy relay)
        {
            if (CurrentLevel._waveSpawns.Count == 0)
                return;

            EnemyType[] wave = CurrentLevel.RelaysDestroyed switch
            {
                1 => new[] { EnemyType.Normal, EnemyType.Fast },
                2 => new[] { EnemyType.Normal, EnemyType.Laser, EnemyType.Fast },
                3 => new[] { EnemyType.Tank, EnemyType.Fast, EnemyType.Laser },
                _ => new[] { EnemyType.Tank, EnemyType.Normal, EnemyType.Fast, EnemyType.Laser }
            };

            List<Vector2> points = CurrentLevel._waveSpawns
                .OrderBy(p => Vector2.DistanceSquared(p, relay.Center))
                .Take(2)
                .ToList();

            for (int i = 0; i < wave.Length; i++)
            {
                Vector2 point = points[i % points.Count];
                _pendingSpawns.Add(new Enemy(point, wave[i]));
                _particles.SpawnExplosion(point + new Vector2(16, 16), Color.Cyan);
            }

            AudioManager.Play("alarm", 0.7f);
        }

        private void UpdateProjectiles(GameTime gameTime)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = _projectiles[i];
                projectile.Update(gameTime, CurrentLevel._solids);

                if (projectile.FromPlayer)
                {
                    foreach (Enemy enemy in CurrentLevel._enemies)
                    {
                        if (projectile.HitEnemy(enemy))
                            break;
                    }
                }
                else
                {
                    projectile.HitPlayer(Player);
                }

                if (projectile.IsDead)
                    _projectiles.RemoveAt(i);
            }
        }

        // =====================================================
        // DRAW / MISC
        // =====================================================

        public void Draw(SpriteBatch spriteBatch)
        {
            if (CurrentLevel == null)
                return;

            _worldManager.Draw(spriteBatch);
            Player.Draw(spriteBatch);

            foreach (var projectile in _projectiles)
                projectile.Draw(spriteBatch);
        }

        public void ClearLevelRequest()
        {
            ChangeLevelRequested = false;
            NextLevel = null;
        }

        public bool HasLevelLoaded() => CurrentLevel != null;
    }
}
