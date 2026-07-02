using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace EyeQ
{
    /// <summary>A single decoded scan result stored in history.</summary>
    [DataContract]
    public class ScanEntry
    {
        [DataMember] public string   Text      { get; set; }
        [DataMember] public string   Format    { get; set; }
        [DataMember] public DateTime Timestamp { get; set; }

        public bool IsUrl =>
            Text != null &&
            (Text.StartsWith("http://",  StringComparison.OrdinalIgnoreCase) ||
             Text.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        public string FormatFriendly =>
            Format == "QR_CODE"      ? "QR Code"      :
            Format == "DATA_MATRIX"  ? "Data Matrix"  :
            string.IsNullOrEmpty(Format) ? "Unknown"  :
            $"Barcode ({Format})";
    }

    /// <summary>
    /// Thread-safe (UI-thread-only) in-memory scan history backed by
    /// %AppData%\EyeQ\history.json.  Raises <see cref="HistoryChanged"/>
    /// whenever the list is mutated so UI can refresh without polling.
    /// </summary>
    public static class ScanHistory
    {
        private static readonly string HistoryFile =
            Path.Combine(AppSettings.DataFolder, "history.json");

        private static List<ScanEntry> _entries = LoadFromFile();

        /// <summary>Raised (not necessarily on UI thread) after any mutation.</summary>
        public static event Action HistoryChanged;

        public static IReadOnlyList<ScanEntry> Entries => _entries.AsReadOnly();

        // ── Mutations ─────────────────────────────────────────────────────────────
        public static void Add(string text, string format)
        {
            if (string.IsNullOrEmpty(text)) return;

            // De-duplicate: newer scan replaces older identical text
            _entries.RemoveAll(e => e.Text == text);
            _entries.Insert(0, new ScanEntry { Text = text, Format = format, Timestamp = DateTime.Now });

            // Trim to user-configured limit
            int max = AppSettings.Current.MaxHistoryCount;
            if (_entries.Count > max)
                _entries.RemoveRange(max, _entries.Count - max);

            SaveToFile();
            HistoryChanged?.Invoke();
        }

        public static void Remove(ScanEntry entry)
        {
            if (_entries.Remove(entry)) { SaveToFile(); HistoryChanged?.Invoke(); }
        }

        public static void Clear()
        {
            _entries.Clear();
            SaveToFile();
            HistoryChanged?.Invoke();
        }

        // ── Persistence ──────────────────────────────────────────────────────────
        private static List<ScanEntry> LoadFromFile()
        {
            try
            {
                if (File.Exists(HistoryFile))
                {
                    using (var fs = File.OpenRead(HistoryFile))
                    {
                        var ser = new DataContractJsonSerializer(typeof(List<ScanEntry>));
                        return (List<ScanEntry>)ser.ReadObject(fs) ?? new List<ScanEntry>();
                    }
                }
            }
            catch { }
            return new List<ScanEntry>();
        }

        private static void SaveToFile()
        {
            try
            {
                Directory.CreateDirectory(AppSettings.DataFolder);
                using (var fs = File.Create(HistoryFile))
                    new DataContractJsonSerializer(typeof(List<ScanEntry>)).WriteObject(fs, _entries);
            }
            catch { }
        }
    }
}
