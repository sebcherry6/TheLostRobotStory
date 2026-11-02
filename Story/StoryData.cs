using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace TheLostRobotStory.Story
{
    public class DialogueLine
    {
        public string Speaker { get; set; } = "";
        public string Text { get; set; } = "";

        // optional gameplay action run when the line appears (see GameplayScene.RunAction)
        public string Action { get; set; } = "";

        public DialogueLine() { }

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    public class LogEntry
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public List<string> Text { get; set; } = new();
    }

    // All narrative text lives in Content/Story/story.json so it can be
    // written and edited without recompiling. Keep it plain ASCII:
    // the sprite font has no other characters.
    public class StoryData
    {
        public List<string> Intro { get; set; } = new();
        public Dictionary<string, string> LevelTitles { get; set; } = new();
        public Dictionary<string, List<DialogueLine>> Events { get; set; } = new();
        public Dictionary<string, LogEntry> Logs { get; set; } = new();
        public Dictionary<string, List<string>> Endings { get; set; } = new();
        public List<string> Credits { get; set; } = new();

        public static StoryData Instance { get; private set; } = new();

        public static void Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Content", "Story", "story.json");

            try
            {
                if (!File.Exists(path))
                {
                    Debug.WriteLine($"[Story] not found: {path}");
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                Instance = JsonSerializer.Deserialize<StoryData>(File.ReadAllText(path), options) ?? new StoryData();
            }
            catch (Exception ex)
            {
                // A typo in the JSON should never crash the game
                Debug.WriteLine($"[Story] failed to load: {ex.Message}");
            }
        }

        public List<DialogueLine> GetEvent(string id) =>
            Events.TryGetValue(id, out var lines) ? lines : null;

        public string GetLevelTitle(string levelFile) =>
            LevelTitles.TryGetValue(levelFile, out var title) ? title : levelFile;

        public List<string> GetEnding(string id) =>
            Endings.TryGetValue(id, out var pages) ? pages : new List<string> { "THE END" };

        // A terminal log turned into dialogue lines (speaker = author)
        public List<DialogueLine> GetLog(string id)
        {
            if (!Logs.TryGetValue(id, out LogEntry log))
            {
                return new List<DialogueLine>
                {
                    new DialogueLine("TERMINAL", "[DATA CORRUPTED]")
                };
            }

            var lines = new List<DialogueLine>
            {
                new DialogueLine("TERMINAL", "> " + log.Title)
            };

            foreach (string text in log.Text)
                lines.Add(new DialogueLine(log.Author, text));

            return lines;
        }
    }
}
