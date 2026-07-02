using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace EyeQ
{
    /// <summary>
    /// Persisted application settings stored in %AppData%\EyeQ\settings.json.
    /// Use <see cref="Current"/> singleton and call <see cref="Save"/> after modifying.
    /// </summary>
    [DataContract]
    public class AppSettings
    {
        // ── Storage ────────────────────────────────────────────────────────────────
        public static readonly string DataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EyeQ");

        private static readonly string SettingsFile = Path.Combine(DataFolder, "settings.json");

        // ── Singleton ──────────────────────────────────────────────────────────────
        private static AppSettings _current;
        public static AppSettings Current => _current ?? (_current = Load());

        // ── Hotkey ────────────────────────────────────────────────────────────────
        /// <summary>RegisterHotKey modifier flags (MOD_CTRL=2, MOD_SHIFT=4, MOD_ALT=1, MOD_WIN=8).</summary>
        [DataMember] public uint HotkeyModifiers  { get; set; } = 0x0006; // Ctrl+Shift
        /// <summary>RegisterHotKey virtual-key code. Default: Q (0x51).</summary>
        [DataMember] public uint HotkeyVirtualKey { get; set; } = 0x51;

        // ── Scanning behaviour ─────────────────────────────────────────────────────
        [DataMember] public bool PlaySoundOnDetection { get; set; } = false;
        [DataMember] public bool AutoOpenUrls         { get; set; } = false;

        // ── History ────────────────────────────────────────────────────────────────
        [DataMember] public int MaxHistoryCount { get; set; } = 20;

        // ── Persistence ────────────────────────────────────────────────────────────
        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    using (var fs = File.OpenRead(SettingsFile))
                    {
                        var ser = new DataContractJsonSerializer(typeof(AppSettings));
                        return (AppSettings)ser.ReadObject(fs) ?? new AppSettings();
                    }
                }
            }
            catch { /* fall through to defaults */ }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                using (var fs = File.Create(SettingsFile))
                    new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(fs, this);
            }
            catch { }
        }
    }
}
