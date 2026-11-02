using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace TheLostRobotStory.Core
{
    // What has to be done before the exit door opens
    public class LevelRequirements
    {
        public bool Enemies { get; set; }
        public bool Crystals { get; set; }
        public bool Switches { get; set; }
        public bool Relays { get; set; }
        public int Fragments { get; set; }
        public bool Boss { get; set; }
        public bool MeetM4 { get; set; }
    }

    // One entry of Content/Levels/levels.json
    public class LevelInfo
    {
        public string File { get; set; } = "";
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Next { get; set; } = "";
        public string Music { get; set; } = "";

        // chapter card shown when the level is entered for the first time
        public string Chapter { get; set; } = "";
        public string ChapterTitle { get; set; } = "";
        public string Goal { get; set; } = "";

        // evolution stage given by 'X' cores in this level
        public int CoreStage { get; set; } = 1;

        // "guardian", "warden" or "core" for the 'B' tile
        public string Boss { get; set; } = "";
        public int BossDropStage { get; set; }
        public bool BossDropHealth { get; set; }

        public LevelRequirements Requires { get; set; } = new();

        // Objective labels shown in the HUD
        public string SwitchLabel { get; set; } = "Restore power";
        public string RelayLabel { get; set; } = "Destroy power relays";

        // switches also shut down crushers and conveyors
        public bool SwitchesStopMachines { get; set; }

        // >0: countdown started by the "escape" trigger action
        public float EscapeSeconds { get; set; }

        // visual identity (hex colours, e.g. "#14161E")
        public string Background { get; set; } = "#10121A";
        public string TileColor { get; set; } = "#5A5F6E";
        public string HazardColor { get; set; } = "#32CD32";

        // actions run every time the level starts (e.g. "alarm")
        public List<string> StartActions { get; set; } = new();

        // trigger number ("1".."9") -> actions run every time it is entered
        public Dictionary<string, List<string>> Triggers { get; set; } = new();

        public Color BackgroundColor => ParseColor(Background, new Color(16, 18, 26));
        public Color TileTint => ParseColor(TileColor, Color.DarkGray);
        public Color HazardTint => ParseColor(HazardColor, Color.LimeGreen);

        public static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return fallback;

            hex = hex.TrimStart('#');

            if (hex.Length != 6)
                return fallback;

            try
            {
                return new Color(
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16));
            }
            catch (FormatException)
            {
                return fallback;
            }
        }
    }

    public class LevelFile
    {
        public string FirstLevel { get; set; } = "level1.txt";
        public List<LevelInfo> Levels { get; set; } = new();
    }

    public static class LevelDatabase
    {
        private static readonly Dictionary<string, LevelInfo> _levels = new();

        public static string FirstLevel { get; private set; } = "level1.txt";
        public static IReadOnlyCollection<LevelInfo> All => _levels.Values;

        // 'R' tiles counted across every level (for the secret ending)
        public static int TotalFragments { get; private set; }

        public static void Load()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "Content", "Levels");
            string path = Path.Combine(folder, "levels.json");

            try
            {
                if (File.Exists(path))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };

                    LevelFile data = JsonSerializer.Deserialize<LevelFile>(File.ReadAllText(path), options);

                    if (data != null)
                    {
                        FirstLevel = data.FirstLevel;

                        foreach (LevelInfo info in data.Levels)
                        {
                            if (string.IsNullOrEmpty(info.Id))
                                info.Id = Path.GetFileNameWithoutExtension(info.File);

                            _levels[info.File] = info;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"[Levels] not found: {path}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Levels] failed to load: {ex.Message}");
            }

            TotalFragments = 0;

            foreach (LevelInfo info in _levels.Values)
            {
                string levelPath = Path.Combine(folder, info.File);

                if (!File.Exists(levelPath))
                    continue;

                foreach (char c in File.ReadAllText(levelPath))
                {
                    if (c == 'R')
                        TotalFragments++;
                }
            }
        }

        public static LevelInfo Get(string file)
        {
            if (file != null && _levels.TryGetValue(file, out LevelInfo info))
                return info;

            // unknown level: old behaviour (kill everything, collect everything)
            return new LevelInfo
            {
                File = file ?? "",
                Id = Path.GetFileNameWithoutExtension(file ?? "level"),
                Title = file ?? "",
                Requires = new LevelRequirements { Enemies = true, Crystals = true }
            };
        }
    }
}
