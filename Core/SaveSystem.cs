using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace TheLostRobotStory.Core
{
    // Everything needed to resume a run. Plain floats instead of Vector2
    // because System.Text.Json ignores public fields (Vector2.X/Y are fields).
    public class SaveData
    {
        public int Version { get; set; } = 1;

        public string CurrentLevel { get; set; } = "level1.txt";

        public bool HasCheckpoint { get; set; }
        public float CheckpointX { get; set; }
        public float CheckpointY { get; set; }

        public int EvolutionStage { get; set; }
        public int MaxHealth { get; set; } = 3;

        public List<string> CollectedItems { get; set; } = new();
        public List<string> ReadLogs { get; set; } = new();
        public List<string> SeenStoryEvents { get; set; } = new();

        // story state: "m4_met", "m4_rescued", "boss:level3" ...
        public List<string> Flags { get; set; } = new();

        // reboots left in the current area (respawns at a charging station)
        public int RebootsLeft { get; set; } = 3;

        // progress when the current area was entered (used to restart it)
        public LevelStartState LevelStart { get; set; }

        public double PlayTimeSeconds { get; set; }
        public DateTime SavedAt { get; set; }

        public static SaveData NewGame() => new SaveData();
    }

    public class LevelStartState
    {
        public int EvolutionStage { get; set; }
        public int MaxHealth { get; set; } = 3;
        public List<string> CollectedItems { get; set; } = new();
        public List<string> Flags { get; set; } = new();
        public List<string> SeenStoryEvents { get; set; } = new();
    }

    // Options + meta progress shared by every save slot
    public class GameSettings
    {
        public float MusicVolume { get; set; } = 0.6f;
        public float SfxVolume { get; set; } = 0.8f;
        public bool Fullscreen { get; set; }
        public List<string> EndingsSeen { get; set; } = new();
    }

    public static class SaveSystem
    {
        // Windows: %APPDATA%\TheLostRobotStory   Linux/macOS: ~/.config/TheLostRobotStory
        private static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TheLostRobotStory");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };

        private static string SlotPath(int slot) => Path.Combine(Folder, $"save{slot}.json");
        private static string SettingsPath => Path.Combine(Folder, "settings.json");

        public static bool Exists(int slot) => File.Exists(SlotPath(slot));

        public static bool Save(int slot, SaveData data)
        {
            data.SavedAt = DateTime.Now;
            return WriteJson(SlotPath(slot), data);
        }

        public static SaveData Load(int slot) => ReadJson<SaveData>(SlotPath(slot));

        public static void Delete(int slot)
        {
            try
            {
                if (Exists(slot))
                    File.Delete(SlotPath(slot));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Save] delete failed: {ex.Message}");
            }
        }

        public static GameSettings LoadSettings() =>
            ReadJson<GameSettings>(SettingsPath) ?? new GameSettings();

        public static void SaveSettings(GameSettings settings) =>
            WriteJson(SettingsPath, settings);

        // Write to a temp file first, then swap: a crash mid-write
        // can never leave a half-written (corrupted) save behind.
        private static bool WriteJson<T>(string path, T data)
        {
            try
            {
                Directory.CreateDirectory(Folder);

                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
                File.Move(temp, path, overwrite: true);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Save] write failed: {ex.Message}");
                return false;
            }
        }

        private static T ReadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Save] read failed ({path}): {ex.Message}");
                return null;
            }
        }
    }
}
