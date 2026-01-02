using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StopWatch
{
    public enum SaveTimerSetting
    {
        NoSave,
        SavePause,
        SaveRunActive
    }

    public enum PauseAndResumeSetting
    {
        NoPause,
        Pause,
        PauseAndResume
    }

    public enum WorklogCommentSetting
    {
        WorklogOnly,
        CommentOnly,
        WorklogAndComment
    }

    public sealed class Settings
    {
        public static readonly Settings Instance = Load();

        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jirastopwatch", "settings.json");

        public string JiraBaseUrl { get; set; } = "";
        public bool AlwaysOnTop { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public int IssueCount { get; set; } = 20;
        public bool AllowMultipleTimers { get; set; } = false;
        public bool IncludeProjectName { get; set; } = false;
        public SaveTimerSetting SaveTimerState { get; set; } = SaveTimerSetting.SavePause;
        public PauseAndResumeSetting PauseOnSessionLock { get; set; } = PauseAndResumeSetting.Pause;
        public WorklogCommentSetting PostWorklogComment { get; set; } = WorklogCommentSetting.WorklogOnly;
        public string Username { get; set; } = "";
        public string ApiToken { get; set; } = "";
        public bool FirstRun { get; set; } = true;
        public int CurrentFilter { get; set; } = 0;
        public string PersistedIssues { get; set; } = "";
        public string StartTransitions { get; set; } = "";
        public bool LoggingEnabled { get; set; } = false;
        public double WindowWidth { get; set; } = 800;
        public double WindowHeight { get; set; } = 600;
        public double WindowPositionX { get; set; } = double.NaN;
        public double WindowPositionY { get; set; } = double.NaN;
        public bool CheckForUpdate { get; set; } = true;
        public string LanguageCode { get; set; } = ""; // Empty means use system default

        public Settings() { }

        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var loaded = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                    loaded.MinimizeToTray = true; // Force tray support even if older settings disabled it
                    return loaded;
                }
            }
            catch { }
            return new Settings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public List<PersistedIssue> ReadIssues(string data)
        {
            if (string.IsNullOrEmpty(data))
                return new List<PersistedIssue>();
            try
            {
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(data)))
                {
                    return JsonSerializer.Deserialize<List<PersistedIssue>>(ms) ?? new List<PersistedIssue>();
                }
            }
            catch
            {
                return new List<PersistedIssue>();
            }
        }

        public string WriteIssues(List<PersistedIssue> issues)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    JsonSerializer.Serialize(ms, issues);
                    var bytes = ms.ToArray();
                    var base64 = Convert.ToBase64String(bytes);
                    return base64;
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
