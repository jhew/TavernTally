using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace TavernTally.App
{
    public class Settings
    {
        private bool _showOverlay = true;
        private double _uiScale = 1.0;
        private double _shopYPct = 0.12;
        private double _boardYPct = 0.63;
        private double _handYPct = 0.92;

        public bool ShowOverlay 
        { 
            get => _showOverlay; 
            set => _showOverlay = value; 
        }

        public bool DebugAlwaysShowOverlay { get; set; } = true; // temporarily force-draw overlay to align

        public double UiScale 
        { 
            get => _uiScale; 
            set => _uiScale = Math.Clamp(value, 0.1, 5.0); // Reasonable scale limits
        }

        // Global pixel nudges (applied after % anchoring and window tracking)
        public double OffsetX { get; set; } = 0;
        public double OffsetY { get; set; } = 0;

        // Calibrated Y positions for rows (percent of Hearthstone client height)
        // Tweak via future calibration UI or by editing config.json
        public double ShopYPct  
        { 
            get => _shopYPct; 
            set => _shopYPct = Math.Clamp(value, 0.0, 1.0); 
        }

        public double BoardYPct 
        { 
            get => _boardYPct; 
            set => _boardYPct = Math.Clamp(value, 0.0, 1.0); 
        }

        public double HandYPct  
        { 
            get => _handYPct; 
            set => _handYPct = Math.Clamp(value, 0.0, 1.0); 
        }

        // Optional update feed (leave null/empty to disable network)
        public string? UpdateJsonUrl { get; set; } = null;
        public bool AllowUpdateChecks => !string.IsNullOrWhiteSpace(UpdateJsonUrl);

        public static string Dir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TavernTally"
        );
        public static string PathFile => System.IO.Path.Combine(Dir, "config.json");

        public static Settings Load()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    var json = File.ReadAllText(PathFile);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null) 
                    {
                        Log.Debug("Successfully loaded settings from {Path}", PathFile);
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load settings from {Path}, using defaults", PathFile);
            }

            // Create default settings and save them
            Log.Information("Creating default settings at {Path}", PathFile);
            Directory.CreateDirectory(Dir);
            var defaultSettings = new Settings();
            try
            {
                File.WriteAllText(PathFile, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to save default settings to {Path}", PathFile);
            }
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(PathFile, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
                Log.Debug("Settings saved to {Path}", PathFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings to {Path}", PathFile);
            }
        }

        public static void Save(Settings settings)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(PathFile, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                Log.Debug("Static settings save completed to {Path}", PathFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings (static method) to {Path}", PathFile);
            }
        }
    }
}
