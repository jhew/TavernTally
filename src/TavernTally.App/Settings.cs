using System;
using System.IO;
using System.Text.Json;

namespace TavernTally.App
{
    public class Settings
    {
        public bool ShowOverlay { get; set; } = true;
        public double UiScale { get; set; } = 1.0;

        // Global pixel nudges (applied after % anchoring and window tracking)
        public double OffsetX { get; set; } = 0;
        public double OffsetY { get; set; } = 0;

        // Calibrated Y positions for rows (percent of Hearthstone client height)
        // Tweak via future calibration UI or by editing config.json
        public double ShopYPct  { get; set; } = 0.12; // row above tavern shop
        public double BoardYPct { get; set; } = 0.63; // your minion row
        public double HandYPct  { get; set; } = 0.92; // your hand fan baseline

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
                    var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(PathFile));
                    if (s != null) return s;
                }
            }
            catch { /* ignore */ }

            Directory.CreateDirectory(Dir);
            var def = new Settings();
            File.WriteAllText(PathFile, JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true }));
            return def;
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(PathFile, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
