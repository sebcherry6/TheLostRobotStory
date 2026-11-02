using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostRobotStory.Core;
using TheLostRobotStory.Entities;
using TheLostRobotStory.Story;
using TheLostRobotStory.World;

namespace TheLostRobotStory.Scenes
{
    // Runs a level and acts as the "story director":
    // story events, trigger actions, M-4, bosses, escape timer, endings,
    // saving, plus all the game-feel feedback (shake, hit-stop, flashes).
    public class GameplayScene : Scene
    {
        private readonly int _slot;
        private readonly SaveData _save;

        // -------- core systems --------
        private readonly List<Projectile> _projectiles = new();
        private readonly ParticleManager _particles = new();
        private GameManager _gameManager;
        private Camera _camera;
        private HUD _hud;

        // -------- feedback --------
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private float _shakeTimer;
        private float _shakeMagnitude;
        private Vector2 _shakeOffset;
        private float _hitStop;
        private float _damageFlash;
        private double _surgeStart = -100;
        private bool _alarm;

        private string _messageText = "";
        private float _messageTimer;
        private string _titleText = "";
        private float _titleTimer;
        private float _saveIndicatorTimer;

        // -------- level flow --------
        private bool _isTransitioning;
        private bool _changingLevel;
        private float _fade;
        private string _pendingLevel;
        private const float FadeSpeed = 2.5f;

        private float _escapeTimer = -1f;
        private bool _sealedDeath;

        // -------- reboots (respawns) --------
        public const int MaxReboots = 3;
        private bool _restartPending;
        private bool _restarting;

        // -------- story --------
        private readonly Queue<(List<DialogueLine> Lines, Action OnDone, bool Cinematic)> _dialogueQueue = new();

        // -------- cinematic story moments --------
        private bool _cinematic;
        private float _letterbox;          // 0..1 black bars
        private Vector2? _focus;           // camera target during a story moment

        private string _bannerText = "";
        private float _bannerTimer;
        private string _lastObjectiveKey;

        // -------- lockdown ambush --------
        private readonly List<Enemy> _ambush = new();
        private StoryTrigger _ambushTrigger;
        private StoryTrigger _currentTrigger;
        private readonly HashSet<Boss> _bossesStarted = new();
        private int _lastEvolution;
        private float _endingDelay = -1f;
        private bool _endingStarted;
        private Terminal _nearTerminal;
        private List<(string Text, bool Done)> _objectives = new();

        private Player Player => _gameManager.Player;
        private Level Level => _gameManager.CurrentLevel;
        private LevelInfo Info => Level.Info;
        private string LevelId => Info.Id;

        public GameplayScene(int slot, SaveData save)
        {
            _slot = slot;
            _save = save;
        }

        // =====================================================
        // ENTER / EXIT
        // =====================================================

        public override void OnEnter()
        {
            _camera = new Camera(Scenes.ScreenWidth, Scenes.ScreenHeight);
            _hud = new HUD(Scenes.Font);

            _gameManager = new GameManager(_particles, _projectiles);
            _gameManager.EnemyKilled += OnEnemyKilled;
            _gameManager.HealthUpgradeCollected += OnHealthUpgradeCollected;
            _gameManager.FragmentCollected += OnFragmentCollected;
            _gameManager.SwitchActivated += OnSwitchActivated;
            _gameManager.TriggerEntered += OnTriggerEntered;
            _gameManager.BossDefeated += OnBossDefeated;
            _gameManager.BossDespawned += OnBossDespawned;
            _gameManager.ExtraDoorCondition = () => !Info.Requires.MeetM4 || HasFlag("m4_met");

            _gameManager.LoadFromSave(_save);

            Player.Hurt += OnPlayerHurt;
            Player.Died += OnPlayerDied;

            Boss.BossSlamEvent += OnBossSlam;
            Checkpoint.CheckpointActivated += OnCheckpointActivated;

            _lastEvolution = Player.EvolutionStage;

            if (_save.LevelStart == null)
                SnapshotLevelStart();

            OnLevelStarted();
        }

        public override void OnExit()
        {
            Boss.BossSlamEvent -= OnBossSlam;
            Checkpoint.CheckpointActivated -= OnCheckpointActivated;
        }

        private void OnLevelStarted()
        {
            _camera.SetWorldBounds(new Rectangle(0, 0, Level.WidthInPixels, Level.HeightInPixels));
            _camera.Reset(PlayerCenter);

            foreach (Terminal terminal in Level._terminals)
                terminal.IsRead = _save.ReadLogs.Contains(terminal.LogId);

            _bossesStarted.Clear();
            _escapeTimer = -1f;
            _alarm = false;

            SetupM4();

            _titleText = string.IsNullOrEmpty(Info.Title) ? StoryData.Instance.GetLevelTitle(Level.CurrentLevelName) : Info.Title;
            _titleTimer = 3.5f;

            PlayLevelMusic();

            foreach (string action in Info.StartActions)
                RunAction(action);

            _lastObjectiveKey = null;
            _ambush.Clear();

            // chapter card the first time this level is entered
            string chapterId = "chapter_" + LevelId;

            if (!string.IsNullOrEmpty(Info.ChapterTitle) && !_save.SeenStoryEvents.Contains(chapterId))
            {
                _save.SeenStoryEvents.Add(chapterId);
                _titleTimer = 0f;
                Scenes.Push(new ChapterCardScene(Info.Chapter, Info.ChapterTitle, Info.Title, Info.Goal));
            }

            TriggerStory(StartEventId());

            // reloaded after THE CORE fell but before the ending finished
            if (Info.Boss == "core" && Level.BossDefeated)
                _endingDelay = 1f;
        }

        private string StartEventId() => LevelId + "_start";

        private void PlayLevelMusic()
        {
            AudioManager.PlayMusic(string.IsNullOrEmpty(Info.Music) ? LevelId : Info.Music);
        }

        private Vector2 PlayerCenter => Player.position + Player.size / 2f;

        // =====================================================
        // FLAGS
        // =====================================================

        private bool HasFlag(string flag) => _save.Flags.Contains(flag);

        private void AddFlag(string flag)
        {
            if (!_save.Flags.Contains(flag))
                _save.Flags.Add(flag);
        }

        private void RemoveFlag(string flag) => _save.Flags.Remove(flag);

        // =====================================================
        // M-4
        // =====================================================

        private void SetupM4()
        {
            bool met = HasFlag("m4_met");
            bool rescued = HasFlag("m4_rescued");
            bool captured = HasFlag("m4_captured") && !rescued;
            bool lost = HasFlag("m4_lost");

            Level.M4 = null;

            if (Level.M4Spawn.HasValue && !met)
            {
                Level.M4 = new NpcM4(Level.M4Spawn.Value);
            }
            else if (captured && Level.CagePosition.HasValue)
            {
                Level.M4 = new NpcM4(Level.CagePosition.Value) { State = NpcM4.NpcState.Captured };
            }
            else if (HasFlag("m4_companion") && !lost && !captured)
            {
                Level.M4 = new NpcM4(Player.position + new Vector2(-40, -30)) { State = NpcM4.NpcState.Following };
            }

            if (Level.M4 != null)
                Level.M4.CagePosition = Level.CagePosition;
        }

        private void UpdateM4Interaction(InputManager input)
        {
            NpcM4 m4 = Level.M4;

            if (m4 == null)
                return;

            m4.IsPlayerNear = m4.State != NpcM4.NpcState.Following && Player.Bounds.Intersects(m4.InteractArea);

            if (!m4.IsPlayerNear || !input.InteractPressed())
                return;

            if (m4.State == NpcM4.NpcState.Idle)
            {
                AudioManager.Play("m4_talk");

                if (!TriggerStory(LevelId + "_m4"))
                    QueueDialogue(new List<DialogueLine> { new DialogueLine("M-4", "Keep moving, kid. I'll catch up.") }, null);
            }
            else if (m4.State == NpcM4.NpcState.Captured)
            {
                if (!Level.BossDefeated)
                {
                    ShowMessage("THE CAGE IS LOCKED TO WARDEN-7'S SIGNAL", 2.5f);
                    AudioManager.Play("block");
                }
                else
                {
                    RescueM4();
                }
            }
        }

        private void RescueM4()
        {
            AddFlag("m4_rescued");
            AddFlag("m4_companion");
            RemoveFlag("m4_captured");

            Level.M4.State = NpcM4.NpcState.Following;

            _particles.SpawnExplosion(Level.M4.Position, Color.Gold);
            AudioManager.Play("checkpoint");

            TriggerStory("m4_rescued");
            SaveGame();
        }

        // =====================================================
        // UPDATE
        // =====================================================

        public override void Update(GameTime gameTime, InputManager input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // queued dialogue opens as soon as nothing else is happening
            if (_dialogueQueue.Count > 0 && !_isTransitioning)
            {
                var (lines, onDone, cinematic) = _dialogueQueue.Dequeue();

                _cinematic = cinematic;

                Scenes.Push(new DialogueScene(
                    lines,
                    () =>
                    {
                        _cinematic = false;
                        _focus = null;
                        onDone?.Invoke();
                    },
                    line => RunAction(line.Action),
                    TickCinematic));
                return;
            }

            if (_restartPending && !_isTransitioning)
            {
                _restartPending = false;
                _restarting = true;
                StartLevelTransition(Level.CurrentLevelName);
            }

            if (input.PausePressed() && !_isTransitioning)
            {
                AudioManager.Play("ui_select");
                Scenes.Push(new PauseScene(this));
                return;
            }

            _save.PlayTimeSeconds += dt;

            UpdateTimers(dt);

            // hit-stop: freeze the world for a few frames on big hits
            if (_hitStop > 0)
            {
                _hitStop -= dt;
                UpdateShake(dt);
                return;
            }

            if (!_isTransitioning)
            {
                _gameManager.Update(gameTime, input);

                UpdateTerminals(input, dt);
                UpdateM4Interaction(input);
                UpdateAmbush();
                UpdateEscapeTimer(dt);
                CheckKillPlane();
                CheckStoryProgress(dt);

                if (_gameManager.ChangeLevelRequested)
                {
                    BeforeLeavingLevel();
                    StartLevelTransition(_gameManager.NextLevel);
                    _gameManager.ClearLevelRequest();
                }
            }

            UpdateTransition(dt);

            _objectives = BuildObjectives();
            UpdateObjectiveBanner(dt);

            _letterbox = Math.Max(0f, _letterbox - dt * 3f);

            _camera.Follow(PlayerCenter, gameTime);
            Sprites.Update(dt);
            _particles.Update(gameTime);
            UpdateShake(dt);
        }

        private void UpdateTimers(float dt)
        {
            if (_messageTimer > 0) _messageTimer -= dt;
            if (_titleTimer > 0) _titleTimer -= dt;
            if (_saveIndicatorTimer > 0) _saveIndicatorTimer -= dt;
            if (_damageFlash > 0) _damageFlash -= dt;
        }

        private void CheckKillPlane()
        {
            if (Player.position.Y > Level.HeightInPixels + 200)
            {
                Player.TakeDamage(1);
                Player.position = Player.SpawnPoint;
                Player.ResetVelocity();
            }
        }

        // "Reach the exit before the sector seals"
        private void UpdateEscapeTimer(float dt)
        {
            if (_escapeTimer < 0)
                return;

            _escapeTimer -= dt;

            if (_escapeTimer <= 0)
            {
                _escapeTimer = Info.EscapeSeconds;
                _sealedDeath = true;
                Player.Health = 0; // handled as a normal death next frame
            }
        }

        private void BeforeLeavingLevel()
        {
            _escapeTimer = -1f;

            // walked away from M-4's cage
            if (HasFlag("m4_captured") && !HasFlag("m4_rescued"))
            {
                AddFlag("m4_lost");
                RemoveFlag("m4_companion");
            }
        }

        // =====================================================
        // TERMINALS
        // =====================================================

        private void UpdateTerminals(InputManager input, float dt)
        {
            _nearTerminal = null;

            foreach (Terminal terminal in Level._terminals)
            {
                terminal.Update(dt);
                terminal.IsPlayerNear = Player.Bounds.Intersects(terminal.Bounds);

                if (terminal.IsPlayerNear)
                    _nearTerminal = terminal;
            }

            if (_nearTerminal != null && input.InteractPressed())
            {
                _nearTerminal.IsRead = true;

                if (!_save.ReadLogs.Contains(_nearTerminal.LogId))
                    _save.ReadLogs.Add(_nearTerminal.LogId);

                AudioManager.Play("terminal");
                QueueDialogue(StoryData.Instance.GetLog(_nearTerminal.LogId), null, false);
            }
        }

        // =====================================================
        // STORY
        // =====================================================

        // Plays a story event once per save file. onDone always runs
        // (immediately when the event was already seen or doesn't exist).
        private bool TriggerStory(string id, Action onDone = null)
        {
            // M-4 isn't with R-01 (not met yet, or left in the furnace):
            // use the "<id>_alone" version of the event when the story has one
            if (!HasFlag("m4_companion") && StoryData.Instance.GetEvent(id + "_alone") != null)
                id += "_alone";

            List<DialogueLine> lines = StoryData.Instance.GetEvent(id);

            if (lines == null || _save.SeenStoryEvents.Contains(id))
            {
                onDone?.Invoke();
                return false;
            }

            _save.SeenStoryEvents.Add(id);
            QueueDialogue(lines, onDone);
            return true;
        }

        private void QueueDialogue(List<DialogueLine> lines, Action onDone, bool cinematic = true)
        {
            if (lines == null || lines.Count == 0)
            {
                onDone?.Invoke();
                return;
            }

            _dialogueQueue.Enqueue((lines, onDone, cinematic));
        }

        // runs while a story dialogue is open (the world itself is paused)
        private void TickCinematic(float dt)
        {
            if (_cinematic)
                _letterbox = Math.Min(1f, _letterbox + dt * 3f);

            if (_focus.HasValue)
                _camera.Focus(_focus.Value, dt, 2.5f);

            UpdateShake(dt);
        }

        // Actions used by levels.json triggers and story.json lines:
        //   surge, alarm, alarm_off, escape, shake, save,
        //   give:<stage>, m4:follow, m4:capture, warden, music:<name>, flag:<name>
        public void RunAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return;

            string[] parts = action.Split(':', 2);
            string name = parts[0].Trim();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";

            switch (name)
            {
                case "surge":
                    _surgeStart = Clock.Elapsed.TotalSeconds;
                    StartShake(0.7f, 9f);
                    AudioManager.Play("surge");
                    break;

                case "alarm":
                    _alarm = true;
                    AudioManager.Play("alarm");
                    break;

                case "alarm_off":
                    _alarm = false;
                    break;

                case "escape":
                    if (_escapeTimer < 0 && Info.EscapeSeconds > 0)
                        _escapeTimer = Info.EscapeSeconds;
                    break;

                case "shake":
                    StartShake(0.5f, 6f);
                    break;

                case "save":
                    SaveGame();
                    break;

                case "give":
                    if (int.TryParse(arg, out int stage) && stage > Player.EvolutionStage)
                    {
                        Player.Evolve(stage);
                        Player.Health = Player.MaxHealth;
                    }
                    break;

                case "m4":
                    RunM4Action(arg);
                    break;

                case "warden":
                    SpawnWardenEncounter();
                    break;

                case "music":
                    AudioManager.PlayMusic(arg);
                    break;

                case "flag":
                    AddFlag(arg);
                    break;

                case "focus":
                    _focus = FocusPoint(arg);
                    break;

                case "ambush":
                    StartAmbush(arg);
                    break;

                default:
                    Debug.WriteLine($"[Story] unknown action: {action}");
                    break;
            }
        }

        // camera targets for story moments: focus:m4 / cage / boss / warden / door / player
        private Vector2? FocusPoint(string target)
        {
            switch (target)
            {
                case "m4":
                    return Level.M4 != null ? Level.M4.Bounds.Center.ToVector2() : null;

                case "cage":
                    return Level.CagePosition.HasValue ? Level.CagePosition.Value + new Vector2(16, 16) : null;

                case "warden":
                    foreach (Enemy enemy in Level._enemies)
                    {
                        if (enemy is WardenBoss)
                            return enemy.Center;
                    }

                    return Level.WardenSpawn.HasValue ? Level.WardenSpawn.Value + new Vector2(20, 32) : null;

                case "boss":
                    foreach (Enemy enemy in Level._enemies)
                    {
                        if (enemy is Boss)
                            return enemy.Center;
                    }

                    return null;

                case "door":
                    return Level._doors.Count > 0 ? Level._doors[0].Bounds.Center.ToVector2() : null;

                default:
                    return null; // "player": back to following R-01
            }
        }

        // =====================================================
        // LOCKDOWN AMBUSH
        // "ambush:EEFS" spawns those enemies (map letters) at the nearest
        // G points and seals nearby gates until they are all destroyed.
        // =====================================================

        private void StartAmbush(string roster)
        {
            if (_ambush.Count > 0 || string.IsNullOrEmpty(roster))
                return;

            List<Vector2> points = Level._waveSpawns
                .Where(p => Vector2.Distance(p, PlayerCenter) < 32 * 30)
                .OrderBy(p => Vector2.Distance(p, PlayerCenter))
                .ToList();

            if (points.Count == 0)
                return;

            for (int i = 0; i < roster.Length; i++)
            {
                EnemyType? type = roster[i] switch
                {
                    'E' => EnemyType.Normal,
                    'F' => EnemyType.Fast,
                    'K' => EnemyType.Tank,
                    'S' => EnemyType.Laser,
                    'D' => EnemyType.Drone,
                    _ => null
                };

                if (type == null)
                    continue;

                Vector2 point = points[i % points.Count];
                Enemy enemy = new Enemy(point, type.Value) { Tag = "ambush" };

                _ambush.Add(enemy);
                Level._enemies.Add(enemy);
                _particles.SpawnExplosion(point + new Vector2(16, 16), Color.Red);
            }

            _ambushTrigger = _currentTrigger;
            Level.SetGates(true, PlayerCenter, 32 * 30);

            _alarm = true;
            AudioManager.Play("alarm");
            StartShake(0.3f, 4f);
            ShowMessage("LOCKDOWN - DESTROY ALL SECURITY UNITS", 3f);
        }

        private void UpdateAmbush()
        {
            if (_ambush.Count == 0)
                return;

            _ambush.RemoveAll(e => e.IsDead || e.Despawned);

            if (_ambush.Count > 0)
                return;

            Level.SetGates(false, PlayerCenter, 0);
            _alarm = Info.StartActions.Contains("alarm");
            _ambushTrigger = null;

            AudioManager.Play("checkpoint");
            ShowMessage("LOCKDOWN LIFTED", 2.5f);
        }

        // dying during an ambush resets it so it can be retried
        private void CancelAmbush()
        {
            if (_ambush.Count == 0)
                return;

            foreach (Enemy enemy in _ambush)
                enemy.Despawned = true;

            _ambush.Clear();
            Level.SetGates(false, PlayerCenter, 0);
            _alarm = Info.StartActions.Contains("alarm");

            if (_ambushTrigger != null)
                _ambushTrigger.Fired = false;

            _ambushTrigger = null;
        }

        private void RunM4Action(string arg)
        {
            switch (arg)
            {
                case "follow":
                    AddFlag("m4_met");
                    AddFlag("m4_companion");

                    if (Level.M4 != null)
                        Level.M4.State = NpcM4.NpcState.Following;
                    break;

                case "capture":
                    if (HasFlag("m4_rescued") || Level.M4 == null || !Level.CagePosition.HasValue)
                        return;

                    AddFlag("m4_captured");
                    _particles.SpawnExplosion(Level.M4.Position, Color.Red);

                    Level.M4.State = NpcM4.NpcState.Captured;
                    Level.M4.CagePosition = Level.CagePosition;
                    break;
            }
        }

        private void SpawnWardenEncounter()
        {
            if (HasFlag("warden_cameo_done") || !Level.WardenSpawn.HasValue)
                return;

            Vector2 spawn = Level.WardenSpawn.Value;

            WardenBoss warden = new WardenBoss(spawn, true)
            {
                Arena = Level.ComputeArena(spawn)
            };

            Level._enemies.Add(warden);

            _particles.SpawnExplosion(spawn + new Vector2(20, 32), Color.OrangeRed);
            AudioManager.Play("boss_slam");
            StartShake(0.5f, 8f);
        }

        private void CheckStoryProgress(float dt)
        {
            // evolution
            if (Player.EvolutionStage > _lastEvolution)
            {
                _lastEvolution = Player.EvolutionStage;

                AudioManager.Play("evolve");
                StartShake(0.4f, 5f);
                _hitStop = 0.15f;

                TriggerStory("evolution_" + Player.EvolutionStage);
                SaveGame();
            }

            // boss intros
            Boss boss = Level.ActiveBoss;

            if (boss != null && boss.Activated && _bossesStarted.Add(boss))
            {
                AudioManager.PlayMusic("boss_" + boss.StoryId);
                TriggerStory(boss.StoryId + "_intro");
            }

            // final ending
            if (_endingDelay > 0)
            {
                _endingDelay -= dt;

                if (_endingDelay <= 0 && !_endingStarted)
                {
                    _endingStarted = true;
                    TriggerStory("core_defeated", StartEnding);
                }
            }
        }

        // =====================================================
        // OBJECTIVES (HUD)
        // =====================================================

        private string BossDisplayName =>
            Info.Boss switch
            {
                "guardian" => "PROTOTYPE G-0",
                "warden" => "WARDEN-7",
                "core" => "THE CORE",
                _ => "the guardian"
            };

        private List<(string Text, bool Done)> BuildObjectives()
        {
            var list = new List<(string, bool)>();
            LevelRequirements r = Info.Requires;

            if (_ambush.Count > 0)
                list.Add(($"Survive the lockdown: {_ambush.Count} left", false));

            if (r.MeetM4)
                list.Add(("Find whoever is hiding down here", HasFlag("m4_met")));

            if (r.Switches && Level.SwitchesTotal > 0)
                list.Add(($"{Info.SwitchLabel} {Level.SwitchesOn}/{Level.SwitchesTotal}", Level.SwitchesOn == Level.SwitchesTotal));

            if (r.Fragments > 0)
                list.Add(($"Research data {Math.Min(Level.FragmentsCollected, r.Fragments)}/{r.Fragments}", Level.FragmentsCollected >= r.Fragments));

            if (r.Relays)
                list.Add(($"{Info.RelayLabel} {Level.RelaysDestroyed}/{Level.RelaysTotal}", Level.RelaysRemaining == 0));

            if (r.Enemies)
                list.Add(($"Security units left: {Level.EnemiesRemaining}", Level.EnemiesRemaining == 0));

            if (r.Crystals)
                list.Add(($"Energy cells left: {Level.CollectiblesRemaining}", Level.CollectiblesRemaining == 0));

            if (r.Boss)
                list.Add(($"Defeat {BossDisplayName}", Level.BossDefeated));

            if (HasFlag("m4_captured") && !HasFlag("m4_rescued") && Level.CagePosition.HasValue)
                list.Add(("Optional: rescue M-4", false));

            if (Level._doors.Count > 0)
                list.Add((_gameManager.ExitOpen ? "Reach the exit" : "Exit sealed", false));

            return list;
        }

        // "NEW OBJECTIVE" banner when the first open objective changes
        private void UpdateObjectiveBanner(float dt)
        {
            if (_bannerTimer > 0)
                _bannerTimer -= dt;

            string current = null;

            foreach (var (text, done) in _objectives)
            {
                if (!done)
                {
                    current = text;
                    break;
                }
            }

            if (current == null)
                return;

            // ignore counters ("1/2" -> "2/2") so it only fires on real changes
            string key = new string(current.Where(c => !char.IsDigit(c)).ToArray());

            if (_lastObjectiveKey != null && key != _lastObjectiveKey && !_isTransitioning)
            {
                _bannerText = "NEW OBJECTIVE: " + current;
                _bannerTimer = 3.5f;
                AudioManager.Play("terminal", 0.5f);
            }

            _lastObjectiveKey = key;
        }

        public string ObjectiveText()
        {
            if (Level == null)
                return "";

            var lines = new List<string> { Info.Title };

            foreach (var (text, done) in BuildObjectives())
                lines.Add((done ? "[x] " : "[ ] ") + text);

            lines.Add($"Logs read: {_save.ReadLogs.Count}/{StoryData.Instance.Logs.Count}   " +
                      $"Research data: {CollectedFragments}/{LevelDatabase.TotalFragments}");

            return string.Join("\n", lines);
        }

        private int CollectedFragments => _save.CollectedItems.Count(k => k.StartsWith("frag:"));

        // =====================================================
        // ENDING
        // =====================================================

        private void StartEnding()
        {
            bool withM4 = HasFlag("m4_rescued");

            AudioManager.PlayMusic("ending");
            TriggerStory(withM4 ? "hangar_m4" : "hangar_alone", ChooseEnding);
        }

        private void ChooseEnding()
        {
            string baseEnding = HasFlag("m4_rescued") ? "together" : "alone";
            bool allData = LevelDatabase.TotalFragments > 0 && CollectedFragments >= LevelDatabase.TotalFragments;

            if (!allData)
            {
                PlayEnding(baseEnding);
                return;
            }

            Scenes.Push(new ChoiceScene(
                "The research data could free every machine in ECHO-9.",
                false,
                new ChoiceOption("Broadcast it. Free them all.", () => PlayEnding("liberation")),
                new ChoiceOption("Leave quietly.", () => PlayEnding(baseEnding))));
        }

        private void PlayEnding(string id)
        {
            GameSettings settings = Scenes.Game.Settings;

            if (!settings.EndingsSeen.Contains(id))
                settings.EndingsSeen.Add(id);

            SaveSystem.SaveSettings(settings);

            var pages = new List<string>(StoryData.Instance.GetEnding(id));
            pages.AddRange(StoryData.Instance.GetEnding("final"));
            pages.AddRange(StoryData.Instance.Credits);

            Scenes.ClearAndPush(new StoryScene(
                pages,
                manager => manager.ClearAndPush(new MainMenuScene())));
        }

        // =====================================================
        // SAVE
        // =====================================================

        public void SaveGame()
        {
            if (Player == null || Level == null)
                return;

            _save.CurrentLevel = Level.CurrentLevelName;
            _save.EvolutionStage = Player.EvolutionStage;
            _save.MaxHealth = Player.MaxHealth;
            _save.HasCheckpoint = true;
            _save.CheckpointX = Player.SpawnPoint.X;
            _save.CheckpointY = Player.SpawnPoint.Y;

            if (SaveSystem.Save(_slot, _save))
                _saveIndicatorTimer = 1.5f;
        }

        private void Remember(string key)
        {
            if (!string.IsNullOrEmpty(key) && !_save.CollectedItems.Contains(key))
                _save.CollectedItems.Add(key);
        }

        // =====================================================
        // EVENT HANDLERS
        // =====================================================

        private void OnTriggerEntered(StoryTrigger trigger)
        {
            _currentTrigger = trigger;

            if (Info.Triggers.TryGetValue(trigger.Number, out List<string> actions))
            {
                foreach (string action in actions)
                    RunAction(action);
            }

            TriggerStory(trigger.Id);
            _currentTrigger = null;
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            if (enemy is Boss)
                return; // handled in OnBossDefeated

            if (enemy.Type == EnemyType.Relay)
            {
                Remember(enemy.Tag);
                AudioManager.Play("relay_destroyed");
                StartShake(0.4f, 6f);

                if (Level.RelaysRemaining == 0)
                {
                    _alarm = false;
                    TriggerStory(LevelId + "_relays");
                }
                else
                {
                    ShowMessage($"RELAY DESTROYED {Level.RelaysDestroyed}/{Level.RelaysTotal} - SECURITY INBOUND", 2.5f);
                }

                SaveGame();
                return;
            }

            AudioManager.Play("enemy_die");
            StartShake(0.1f, 2f);
            _hitStop = Math.Max(_hitStop, 0.05f);
        }

        private void OnHealthUpgradeCollected(string key)
        {
            Remember(key);
            AudioManager.Play("health_up");
            ShowMessage("ARMOUR PLATING INSTALLED - MAX HEALTH +1", 2.5f);
        }

        private void OnFragmentCollected(DataFragment fragment)
        {
            Remember(fragment.Key);
            AudioManager.Play("fragment");

            ShowMessage($"RESEARCH DATA FRAGMENT RECOVERED ({CollectedFragments}/{LevelDatabase.TotalFragments})", 2.5f);

            // "frag:level3:0" -> story event "level3_frag0"
            string[] parts = fragment.Key.Split(':');

            if (parts.Length == 3)
                TriggerStory($"{parts[1]}_frag{parts[2]}");

            SaveGame();
        }

        private void OnSwitchActivated(PowerSwitch powerSwitch)
        {
            Remember(powerSwitch.Key);

            ShowMessage($"{Info.SwitchLabel.ToUpperInvariant()} {Level.SwitchesOn}/{Level.SwitchesTotal}", 2.5f);

            if (Level.SwitchesOn == Level.SwitchesTotal)
                TriggerStory(LevelId + "_power");

            SaveGame();
        }

        private void OnCheckpointActivated()
        {
            AudioManager.Play("checkpoint");
            ShowMessage("CHARGING STATION LINKED", 2f);
            SaveGame();
            TriggerStory("first_checkpoint");
        }

        private void OnBossDefeated(Boss boss)
        {
            AddFlag("boss:" + LevelId);

            AudioManager.StopMusic();
            AudioManager.Play("boss_die");
            StartShake(1f, 10f);
            _hitStop = 0.3f;

            for (int i = 0; i < 6; i++)
            {
                _particles.SpawnExplosion(
                    boss.Center + new Vector2(Random.Shared.Next(-48, 49), Random.Shared.Next(-48, 49)),
                    i % 2 == 0 ? Color.OrangeRed : Color.White);
            }

            if (boss is CoreBoss)
            {
                // the summoned drones power down with their master
                foreach (Enemy enemy in Level._enemies)
                {
                    if (enemy.Type == EnemyType.Drone)
                        enemy.Health = 0;
                }

                _endingDelay = 2.5f;
                return;
            }

            // salvage left behind by the boss
            Vector2 drop = new Vector2(boss.Center.X - 16, boss.Bounds.Bottom - 56);

            if (Info.BossDropStage > Player.EvolutionStage)
                Level._evolutionCores.Add(new EvolutionCore(drop, Info.BossDropStage));

            if (Info.BossDropHealth)
                Level._healthUpgrades.Add(new HealthUpgrade(drop + new Vector2(48, 8)));

            TriggerStory(boss.StoryId + "_defeated", PlayLevelMusic);
            SaveGame();
        }

        private void OnBossDespawned(Boss boss)
        {
            AddFlag("warden_cameo_done");
            AudioManager.Play("boss_dash");
            TriggerStory(boss.StoryId + "_retreat", PlayLevelMusic);
        }

        private void OnBossSlam()
        {
            AudioManager.Play("boss_slam");
            StartShake(0.25f, 6f);
        }

        private void OnPlayerHurt()
        {
            _damageFlash = 0.2f;
            StartShake(0.15f, 3f);
            _hitStop = Math.Max(_hitStop, 0.06f);
        }

        private void OnPlayerDied()
        {
            CancelAmbush();

            AudioManager.Play("death");
            _damageFlash = 0.6f;
            StartShake(0.4f, 8f);

            if (_save.RebootsLeft > 0)
            {
                _save.RebootsLeft--;

                string reason = _sealedDeath ? "SECTOR SEALED" : "CRITICAL FAILURE";
                string left = _save.RebootsLeft == 1 ? "1 REBOOT LEFT" : $"{_save.RebootsLeft} REBOOTS LEFT";

                ShowMessage($"{reason} - REBOOTING AT CHARGING STATION ({left})", 3f);
                SaveGame();
            }
            else
            {
                // no reboots left: the whole area starts over
                _restartPending = true;
            }

            _sealedDeath = false;
        }

        // =====================================================
        // AREA RESTART
        // =====================================================

        private void SnapshotLevelStart()
        {
            _save.LevelStart = new LevelStartState
            {
                EvolutionStage = Player.EvolutionStage,
                MaxHealth = Player.MaxHealth,
                CollectedItems = new List<string>(_save.CollectedItems),
                Flags = new List<string>(_save.Flags),
                SeenStoryEvents = new List<string>(_save.SeenStoryEvents)
            };
        }

        private void RestartLevel()
        {
            LevelStartState start = _save.LevelStart;

            if (start != null)
            {
                // keep "chapter card already shown" so it doesn't play again
                var chapterCards = _save.SeenStoryEvents.Where(e => e.StartsWith("chapter_")).ToList();

                _save.CollectedItems = new List<string>(start.CollectedItems);
                _save.Flags = new List<string>(start.Flags);
                _save.SeenStoryEvents = new List<string>(start.SeenStoryEvents);

                foreach (string card in chapterCards)
                {
                    if (!_save.SeenStoryEvents.Contains(card))
                        _save.SeenStoryEvents.Add(card);
                }
            }

            _save.RebootsLeft = MaxReboots;
            _save.HasCheckpoint = false;

            _dialogueQueue.Clear();
            _escapeTimer = -1f;
            _endingDelay = -1f;

            _gameManager.ChangeLevel(Level.CurrentLevelName);

            if (start != null)
                Player.ResetProgress(start.EvolutionStage, start.MaxHealth);

            _lastEvolution = Player.EvolutionStage;

            OnLevelStarted();
            SaveGame();
            ShowMessage("ALL REBOOTS USED - AREA RESTARTED", 3f);
        }

        // =====================================================
        // LEVEL TRANSITION (fade out -> load -> fade in)
        // =====================================================

        private void StartLevelTransition(string nextLevel)
        {
            if (_isTransitioning)
                return;

            _isTransitioning = true;
            _changingLevel = true;
            _pendingLevel = nextLevel;
            _fade = 0f;
        }

        private void UpdateTransition(float dt)
        {
            if (!_isTransitioning)
                return;

            if (_changingLevel)
            {
                _fade += FadeSpeed * dt;

                if (_fade >= 1f)
                {
                    _fade = 1f;
                    _changingLevel = false;

                    if (_restarting)
                    {
                        _restarting = false;
                        RestartLevel();
                    }
                    else
                    {
                        _gameManager.ChangeLevel(_pendingLevel);

                        // a new area: full reboots and a fresh restart point
                        _save.RebootsLeft = MaxReboots;
                        _save.HasCheckpoint = false;
                        SnapshotLevelStart();

                        OnLevelStarted();
                        SaveGame();
                    }
                }
            }
            else
            {
                _fade -= FadeSpeed * dt;

                if (_fade <= 0f)
                {
                    _fade = 0f;
                    _isTransitioning = false;
                }
            }
        }

        // =====================================================
        // FEEDBACK HELPERS
        // =====================================================

        private void ShowMessage(string text, float duration)
        {
            _messageText = text;
            _messageTimer = duration;
        }

        private void StartShake(float duration, float magnitude)
        {
            if (magnitude >= _shakeMagnitude || _shakeTimer <= 0)
            {
                _shakeTimer = duration;
                _shakeMagnitude = magnitude;
            }
        }

        private void UpdateShake(float dt)
        {
            if (_shakeTimer > 0)
            {
                _shakeTimer -= dt;

                _shakeOffset = new Vector2(
                    (float)(Random.Shared.NextDouble() * 2 - 1),
                    (float)(Random.Shared.NextDouble() * 2 - 1)) * _shakeMagnitude;
            }
            else
            {
                _shakeOffset = Vector2.Zero;
                _shakeMagnitude = 0;
            }
        }

        // =====================================================
        // DRAW
        // =====================================================

        public override void Draw(SpriteBatch spriteBatch)
        {
            SpriteFont font = Scenes.Font;
            int w = Scenes.ScreenWidth;
            int h = Scenes.ScreenHeight;
            double now = Clock.Elapsed.TotalSeconds;

            Matrix cameraMatrix =
                _camera.GetViewMatrix() *
                Matrix.CreateTranslation((float)Math.Round(_shakeOffset.X), (float)Math.Round(_shakeOffset.Y), 0);

            // ---------------- BACKGROUND ----------------
            DrawBackground(spriteBatch);

            // ---------------- WORLD ----------------
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: cameraMatrix);

            _gameManager.Draw(spriteBatch);
            _particles.Draw(spriteBatch, TextureManager.Pixel);

            spriteBatch.End();

            // ---------------- SCREEN EFFECTS + UI ----------------
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_alarm)
            {
                float pulse = 0.5f + 0.5f * (float)Math.Sin(now * 5.0);
                Color alarm = Color.Red * (0.10f + 0.12f * pulse);
                Ui.Rect(spriteBatch, new Rectangle(0, 0, w, 24), alarm);
                Ui.Rect(spriteBatch, new Rectangle(0, h - 24, w, 24), alarm);
                Ui.Rect(spriteBatch, new Rectangle(0, 0, 24, h), alarm);
                Ui.Rect(spriteBatch, new Rectangle(w - 24, 0, 24, h), alarm);
            }

            if (_letterbox < 0.5f && !_isTransitioning)
                DrawWaypoints(spriteBatch, w, h);

            if (_letterbox < 0.5f)
            {
                _hud.Draw(spriteBatch, Player, _objectives, w, _save.RebootsLeft, MaxReboots);

                DrawBossBar(spriteBatch, w);
                DrawEscapeTimer(spriteBatch, w);
                DrawPrompt(spriteBatch, w, h);
            }

            // cinematic bars for story moments
            if (_letterbox > 0)
            {
                int bar = (int)(80 * _letterbox);
                Ui.Rect(spriteBatch, new Rectangle(0, 0, w, bar), Color.Black);
                Ui.Rect(spriteBatch, new Rectangle(0, h - bar, w, bar), Color.Black);
            }

            if (_bannerTimer > 0 && _letterbox < 0.5f)
            {
                float alpha = Math.Min(1f, _bannerTimer * 2f);
                Vector2 size = font.MeasureString(Ui.Safe(_bannerText));
                Ui.Rect(spriteBatch, new Rectangle((int)(w / 2f - size.X / 2f) - 14, 168, (int)size.X + 28, (int)size.Y + 10), Color.Black * 0.6f * alpha);
                Ui.TextCentered(spriteBatch, font, _bannerText, w / 2f, 173, new Color(255, 210, 120) * alpha);
            }

            if (_messageTimer > 0)
                Ui.TextCentered(spriteBatch, font, _messageText, w / 2f, 130, Color.White * Math.Min(1f, _messageTimer * 2f));

            if (_titleTimer > 0)
                Ui.TextCentered(spriteBatch, font, _titleText, w / 2f, h * 0.3f, Color.Cyan * Math.Min(1f, _titleTimer), 2f);

            if (_saveIndicatorTimer > 0)
                Ui.Text(spriteBatch, font, "Saving...", new Vector2(w - 110, h - 36), Color.LightGreen * Math.Min(1f, _saveIndicatorTimer * 2f));

            if (_damageFlash > 0)
                Ui.Rect(spriteBatch, Scenes.Screen, Color.Red * (_damageFlash * 0.5f));

            // power surge: white flash that flickers out
            double surge = now - _surgeStart;

            if (surge >= 0 && surge < 1.6)
            {
                float flicker = (int)(surge * 20) % 3 == 0 ? 0.3f : 1f;
                Ui.Rect(spriteBatch, Scenes.Screen, Color.White * ((float)(1.0 - surge / 1.6) * 0.8f * flicker));
            }

            if (_fade > 0)
            {
                Ui.Rect(spriteBatch, Scenes.Screen, Color.Black * MathHelper.Clamp(_fade, 0f, 1f));

                if (_restarting)
                {
                    Ui.TextCentered(spriteBatch, font, "SYSTEM FAILURE", w / 2f, h * 0.4f, Color.Red * _fade, 3f);
                    Ui.TextCentered(spriteBatch, font, "All reboots used. Restarting the area.", w / 2f, h * 0.4f + 60, Color.Gray * _fade);
                }
            }

            spriteBatch.End();
        }

        // =====================================================
        // WAYPOINTS
        // Arrow toward the current objective (gold) and, while M-4 is
        // captured, toward the cage (orange). On screen: a bobbing marker.
        // =====================================================

        private (Vector2 Target, string Label)? MainWaypoint()
        {
            if (_ambush.Count > 0 || Level == null)
                return null;

            LevelRequirements r = Info.Requires;

            if ((_escapeTimer >= 0) && Level._doors.Count > 0)
                return (Level._doors[0].Bounds.Center.ToVector2(), "EXIT");

            if (r.MeetM4 && !HasFlag("m4_met") && Level.M4 != null)
                return (Level.M4.Bounds.Center.ToVector2(), "?");

            if (r.Switches)
            {
                var target = Nearest(Level._switches.Where(sw => !sw.On).Select(sw => sw.Bounds.Center.ToVector2()));
                if (target.HasValue) return (target.Value, "POWER");
            }

            if (r.Fragments > 0 && Level.FragmentsCollected < r.Fragments)
            {
                var target = Nearest(Level._fragments.Where(f => !f.Collected).Select(f => f.Bounds.Center.ToVector2()));
                if (target.HasValue) return (target.Value, "DATA");
            }

            if (r.Relays)
            {
                var target = Nearest(Level._enemies.Where(e => e.Type == EnemyType.Relay && !e.IsDead).Select(e => e.Center));
                if (target.HasValue) return (target.Value, "RELAY");
            }

            if (r.Boss && !Level.BossDefeated)
            {
                foreach (Enemy enemy in Level._enemies)
                {
                    if (enemy is Boss boss)
                        return (boss.Center, boss.Name);
                }
            }

            // an upgrade R-01 doesn't have yet (the route may depend on it)
            var core = Nearest(Level._evolutionCores
                .Where(c => !c.Collected && c.TargetEvolutionStage > Player.EvolutionStage)
                .Select(c => c.Bounds.Center.ToVector2()));

            if (core.HasValue)
                return (core.Value, "UPGRADE");

            if (Level._doors.Count > 0 && _gameManager.ExitOpen)
                return (Level._doors[0].Bounds.Center.ToVector2(), "EXIT");

            return null;
        }

        private (Vector2 Target, string Label)? RescueWaypoint()
        {
            if (HasFlag("m4_captured") && !HasFlag("m4_rescued") && Level.CagePosition.HasValue)
                return (Level.CagePosition.Value + new Vector2(16, 16), "M-4");

            return null;
        }

        private Vector2? Nearest(IEnumerable<Vector2> points)
        {
            Vector2? best = null;
            float bestDistance = float.MaxValue;

            foreach (Vector2 p in points)
            {
                float d = Vector2.DistanceSquared(p, PlayerCenter);

                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }

            return best;
        }

        private void DrawWaypoints(SpriteBatch spriteBatch, int w, int h)
        {
            var main = MainWaypoint();
            var rescue = RescueWaypoint();

            if (main.HasValue)
                DrawWaypoint(spriteBatch, main.Value.Target, main.Value.Label, new Color(255, 210, 120), w, h);

            if (rescue.HasValue)
                DrawWaypoint(spriteBatch, rescue.Value.Target, rescue.Value.Label, new Color(255, 140, 60), w, h);
        }

        private void DrawWaypoint(SpriteBatch spriteBatch, Vector2 world, string label, Color color, int w, int h)
        {
            SpriteFont font = Scenes.Font;
            Vector2 screen = world + new Vector2((float)Math.Round(_camera.Position.X), (float)Math.Round(_camera.Position.Y));
            float pulse = 0.75f + 0.25f * (float)Math.Sin(Clock.Elapsed.TotalSeconds * 5.0);
            int metres = (int)(Vector2.Distance(world, PlayerCenter) / 32f);

            const int margin = 56;
            bool onScreen = screen.X > margin && screen.X < w - margin && screen.Y > margin + 80 && screen.Y < h - margin;

            if (onScreen)
            {
                // bobbing marker above the target
                float bob = (float)Math.Sin(Clock.Elapsed.TotalSeconds * 4.0) * 4f;
                Vector2 tip = new Vector2(screen.X, screen.Y - 30 + bob);

                DrawArrowHead(spriteBatch, tip, MathHelper.PiOver2, 12, color * pulse);
                Ui.TextCentered(spriteBatch, font, label, tip.X, tip.Y - 34, color * pulse);
                return;
            }

            // arrow on the screen edge pointing toward the target
            Vector2 centre = new Vector2(w / 2f, h / 2f);
            Vector2 dir = screen - centre;

            if (dir == Vector2.Zero)
                return;

            float halfW = w / 2f - margin;
            float halfH = h / 2f - margin;
            float scale = Math.Min(halfW / Math.Max(0.001f, Math.Abs(dir.X)), halfH / Math.Max(0.001f, Math.Abs(dir.Y)));
            Vector2 edge = centre + dir * scale;
            float angle = (float)Math.Atan2(dir.Y, dir.X);

            Ui.Rect(spriteBatch, new Rectangle((int)edge.X - 22, (int)edge.Y - 22, 44, 44), Color.Black * 0.35f);
            DrawArrowHead(spriteBatch, edge + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 14f, angle, 14, color * pulse);

            string text = $"{label} {metres}m";
            Vector2 size = font.MeasureString(Ui.Safe(text));
            Vector2 textPos = edge - new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 34f;

            textPos.X = MathHelper.Clamp(textPos.X, size.X / 2f + 8, w - size.X / 2f - 8);
            textPos.Y = MathHelper.Clamp(textPos.Y, 8, h - size.Y - 8);

            Ui.TextCentered(spriteBatch, font, text, textPos.X, textPos.Y - size.Y / 2f, color);
        }

        // filled triangle pointing along angle, tip at 'tip'
        private static void DrawArrowHead(SpriteBatch spriteBatch, Vector2 tip, float angle, int length, Color color)
        {
            Vector2 back = new Vector2(-(float)Math.Cos(angle), -(float)Math.Sin(angle));
            float perpendicular = angle + MathHelper.PiOver2;

            for (int i = 0; i < length; i++)
            {
                Vector2 centre = tip + back * i;
                float half = i * 0.6f + 1f;

                spriteBatch.Draw(
                    TextureManager.Pixel,
                    centre,
                    null,
                    color,
                    perpendicular,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(half * 2f, 2f),
                    SpriteEffects.None,
                    0f);
            }
        }

        private void DrawPrompt(SpriteBatch spriteBatch, int w, int h)
        {
            string prompt = null;

            if (_gameManager.ActiveDoor != null)
            {
                prompt = _gameManager.ActiveDoor.CanOpen
                    ? "[E] Open the door"
                    : "SEALED - complete the objectives first";
            }
            else if (_gameManager.NearSwitch != null)
            {
                prompt = "[E] " + Info.SwitchLabel;
            }
            else if (_nearTerminal != null)
            {
                prompt = _nearTerminal.IsRead ? "[E] Read log again" : "[E] Access terminal";
            }
            else if (Level.M4 != null && Level.M4.IsPlayerNear)
            {
                prompt = Level.M4.State == NpcM4.NpcState.Captured ? "[E] Open the cage" : "[E] Talk";
            }

            if (prompt == null)
                return;

            Vector2 size = Scenes.Font.MeasureString(Ui.Safe(prompt));
            Ui.Rect(spriteBatch, new Rectangle((int)(w / 2f - size.X / 2f) - 12, h - 76, (int)size.X + 24, (int)size.Y + 12), Color.Black * 0.6f);
            Ui.TextCentered(spriteBatch, Scenes.Font, prompt, w / 2f, h - 70, Color.White);
        }

        private void DrawEscapeTimer(SpriteBatch spriteBatch, int w)
        {
            if (_escapeTimer < 0)
                return;

            int seconds = (int)Math.Ceiling(_escapeTimer);
            string text = $"SECTOR SEALING  {seconds / 60:00}:{seconds % 60:00}";
            Color color = _escapeTimer < 15 && (int)(_escapeTimer * 4) % 2 == 0 ? Color.White : Color.Red;

            Ui.TextCentered(spriteBatch, Scenes.Font, text, w / 2f, 80, color, 1.5f);
        }

        private void DrawBossBar(SpriteBatch spriteBatch, int screenWidth)
        {
            Boss boss = Level.ActiveBoss;

            if (boss == null || !boss.Activated)
                return;

            int barWidth = screenWidth / 2;
            Rectangle back = new Rectangle(screenWidth / 4, 30, barWidth, 14);

            float hp = MathHelper.Clamp((float)boss.Health / boss.MaxHealth, 0f, 1f);
            Rectangle front = new Rectangle(back.X, back.Y, (int)(barWidth * hp), back.Height);

            Ui.TextCentered(spriteBatch, Scenes.Font, boss.Name, screenWidth / 2f, 8, Color.Red);
            Ui.Rect(spriteBatch, back, Color.DarkRed * 0.6f);
            Ui.Rect(spriteBatch, front, Color.Red);
            Ui.Border(spriteBatch, back, Color.White * 0.5f, 1);

            if (!string.IsNullOrEmpty(boss.Status))
                Ui.TextCentered(spriteBatch, Scenes.Font, boss.Status, screenWidth / 2f, 50, Color.Orange);
        }

        // =====================================================
        // PARALLAX BACKGROUND
        // Textures/Backgrounds/<level>_far.png and <level>_near.png
        // (or shared far.png / near.png). Without art: placeholder pillars.
        // =====================================================

        private void DrawBackground(SpriteBatch spriteBatch)
        {
            Texture2D far =
                TextureManager.Get($"Backgrounds/{LevelId}_far") ??
                TextureManager.Get("Backgrounds/far");

            Texture2D near =
                TextureManager.Get($"Backgrounds/{LevelId}_near") ??
                TextureManager.Get("Backgrounds/near");

            float cameraX = -_camera.Position.X;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            Ui.Rect(spriteBatch, Scenes.Screen, Info.BackgroundColor);

            if (far != null)
                DrawLayer(spriteBatch, far, cameraX, 0.15f);
            else
                DrawPlaceholderPillars(spriteBatch, cameraX);

            if (near != null)
                DrawLayer(spriteBatch, near, cameraX, 0.4f);

            spriteBatch.End();
        }

        private void DrawLayer(SpriteBatch spriteBatch, Texture2D texture, float cameraX, float factor)
        {
            int screenW = Scenes.ScreenWidth;
            int screenH = Scenes.ScreenHeight;

            float scale = (float)screenH / texture.Height;
            int drawWidth = Math.Max(1, (int)(texture.Width * scale));

            float offset = -(cameraX * factor) % drawWidth;

            if (offset > 0)
                offset -= drawWidth;

            for (float x = offset; x < screenW; x += drawWidth)
                spriteBatch.Draw(texture, new Rectangle((int)x, 0, drawWidth + 1, screenH), Color.White);
        }

        private void DrawPlaceholderPillars(SpriteBatch spriteBatch, float cameraX)
        {
            int screenW = Scenes.ScreenWidth;
            int screenH = Scenes.ScreenHeight;
            const int spacing = 220;

            float shift = cameraX * 0.2f;
            int first = (int)Math.Floor(shift / spacing) - 1;

            Color pillar = Color.Lerp(Info.BackgroundColor, Color.White, 0.05f);
            Color detail = Color.Lerp(Info.BackgroundColor, Color.White, 0.09f);

            for (int i = first; i < first + screenW / spacing + 3; i++)
            {
                int x = (int)(i * spacing - shift);
                int width = 50 + Sprites.CellHash(i, 7) % 50;
                int top = 60 + Sprites.CellHash(i, 3) % 200;

                Ui.Rect(spriteBatch, new Rectangle(x, top, width, screenH - top), pillar);
                Ui.Rect(spriteBatch, new Rectangle(x + 6, top + 20, 4, screenH), detail);
            }
        }
    }
}
