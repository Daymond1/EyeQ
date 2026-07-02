using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EyeQ
{
    public partial class Form1 : Form
    {
        // ── Constants ─────────────────────────────────────────────────────────────
        private const string AppName  = "EyeQ";
        private const int    HotkeyId = 9001;
        private const int    WM_HOTKEY = 0x0312;

        // ── P/Invoke ──────────────────────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // ── Fields ────────────────────────────────────────────────────────────────
        private NotifyIcon        trayIcon;
        private ContextMenuStrip  trayMenu;
        private ToolStripMenuItem autorunItem;
        private ToolStripMenuItem _scanItem;   // kept to update label after hotkey change

        private SelectionForm selectionForm;
        private HistoryForm   historyForm;
        private SettingsForm  settingsForm;

        // ── Constructor ───────────────────────────────────────────────────────────
        public Form1()
        {
            InitializeComponent();
            BuildTray();
        }

        // ── Tray ──────────────────────────────────────────────────────────────────
        private void BuildTray()
        {
            _scanItem = new ToolStripMenuItem($"Scan QR / Barcode  ({CurrentHotkeyLabel()})");
            _scanItem.Font = new Font(_scanItem.Font, FontStyle.Bold);
            _scanItem.Click += (s, e) => OpenSelectionForm();

            var clipItem = new ToolStripMenuItem("Scan clipboard image");
            clipItem.Click += (s, e) => ScanClipboardAsync();

            var fileItem = new ToolStripMenuItem("Open image file…");
            fileItem.Click += (s, e) => ScanFromFileAsync();

            var histItem = new ToolStripMenuItem("History…");
            histItem.Click += (s, e) => OpenHistoryForm();

            var setItem = new ToolStripMenuItem("Settings…");
            setItem.Click += (s, e) => OpenSettingsForm();

            autorunItem = new ToolStripMenuItem("Run at Windows startup")
            {
                Checked      = IsInStartup(),
                CheckOnClick = false,
            };
            autorunItem.Click += AutorunItem_Click;

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.AddRange(new ToolStripItem[]
            {
                _scanItem,
                new ToolStripSeparator(),
                clipItem,
                fileItem,
                new ToolStripSeparator(),
                histItem,
                setItem,
                new ToolStripSeparator(),
                autorunItem,
                new ToolStripSeparator(),
                exitItem,
            });

            trayMenu.Opening += (s, e) => autorunItem.Checked = IsInStartup();

            trayIcon = new NotifyIcon
            {
                Text             = AppName,
                Icon             = LoadAppIcon(),
                ContextMenuStrip = trayMenu,
                Visible          = true,
            };

            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) OpenSelectionForm();
            };
        }

        private static Icon LoadAppIcon()
        {
            string path = Path.Combine(Application.StartupPath, "ico.ico");
            return File.Exists(path) ? new Icon(path) : SystemIcons.Application;
        }

        // ── Hotkey ────────────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Visible       = false;
            ShowInTaskbar = false;
            RegisterCurrentHotkey();
        }

        private void RegisterCurrentHotkey()
        {
            var s = AppSettings.Current;
            if (!RegisterHotKey(Handle, HotkeyId, s.HotkeyModifiers, s.HotkeyVirtualKey))
            {
                trayIcon?.ShowBalloonTip(3000, AppName,
                    $"Could not register hotkey {CurrentHotkeyLabel()} (already in use by another app).",
                    ToolTipIcon.Warning);
            }
        }

        /// <summary>Called by SettingsForm after saving new hotkey settings.</summary>
        public void ReapplyHotkey()
        {
            UnregisterHotKey(Handle, HotkeyId);
            RegisterCurrentHotkey();
            _scanItem.Text = $"Scan QR / Barcode  ({CurrentHotkeyLabel()})";
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
                OpenSelectionForm();
            base.WndProc(ref m);
        }

        // ── Hotkey label helper ───────────────────────────────────────────────────
        private static string CurrentHotkeyLabel()
        {
            var s     = AppSettings.Current;
            var parts = new List<string>();
            if ((s.HotkeyModifiers & 0x0002) != 0) parts.Add("Ctrl");
            if ((s.HotkeyModifiers & 0x0004) != 0) parts.Add("Shift");
            if ((s.HotkeyModifiers & 0x0001) != 0) parts.Add("Alt");
            if ((s.HotkeyModifiers & 0x0008) != 0) parts.Add("Win");
            parts.Add(VkToName(s.HotkeyVirtualKey));
            return string.Join("+", parts);
        }

        private static string VkToName(uint vk)
        {
            if (vk >= 0x41 && vk <= 0x5A) return ((char)vk).ToString();
            if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x7B) return $"F{vk - 0x6F}";
            return $"0x{vk:X2}";
        }

        // ── Open child forms ──────────────────────────────────────────────────────
        private void OpenSelectionForm()
        {
            if (selectionForm == null || selectionForm.IsDisposed)
            {
                selectionForm = new SelectionForm();
                selectionForm.ScanCompleted        += ProcessScanResults;
                selectionForm.NotificationRequested += ShowNotification;
                selectionForm.FormClosed += (s, args) => selectionForm = null;
                selectionForm.Show();
            }
            else
            {
                selectionForm.BringToFront();
            }
        }

        private void OpenHistoryForm()
        {
            if (historyForm == null || historyForm.IsDisposed)
            {
                historyForm = new HistoryForm();
                historyForm.FormClosed += (s, args) => historyForm = null;
            }
            historyForm.Show();
            historyForm.BringToFront();
        }

        private void OpenSettingsForm()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm(this);
                settingsForm.FormClosed += (s, args) => settingsForm = null;
            }
            settingsForm.Show();
            settingsForm.BringToFront();
        }

        // ── Scan from clipboard ───────────────────────────────────────────────────
        private async void ScanClipboardAsync()
        {
            if (!Clipboard.ContainsImage())
            {
                ShowNotification("Clipboard", "No image found in the clipboard.");
                return;
            }
            try
            {
                using (Image img = Clipboard.GetImage())
                {
                    if (img == null) { ShowNotification("Clipboard", "Could not read clipboard image."); return; }
                    using (var bmp = new System.Drawing.Bitmap(img))
                        ProcessScanResults(await QRAnalyzer.AnalyzeMultipleAsync(bmp));
                }
            }
            catch (Exception ex) { ShowNotification("Clipboard error", ex.Message); }
        }

        // ── Scan from file ────────────────────────────────────────────────────────
        private async void ScanFromFileAsync()
        {
            using (var dlg = new OpenFileDialog
            {
                Title  = "Select image to scan — EyeQ",
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.webp|All files|*.*",
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    using (var bmp = new System.Drawing.Bitmap(dlg.FileName))
                        ProcessScanResults(await QRAnalyzer.AnalyzeMultipleAsync(bmp));
                }
                catch (Exception ex) { ShowNotification("File error", $"Could not open image: {ex.Message}"); }
            }
        }

        // ── Central result processor ──────────────────────────────────────────────
        private void ProcessScanResults(IList<ScanResult> results)
        {
            if (results == null || results.Count == 0)
            {
                ShowNotification("Nothing found", "No QR code or barcode was detected.");
                return;
            }

            // 1. Copy to clipboard (newline-separated if multiple)
            string combined = string.Join(Environment.NewLine, results.Select(r => r.Text));
            Clipboard.SetText(combined);

            // 2. Persist to history
            foreach (var r in results)
                ScanHistory.Add(r.Text, r.Format);

            // 3. Sound (optional)
            if (AppSettings.Current.PlaySoundOnDetection)
                System.Media.SystemSounds.Asterisk.Play();

            // 4. Auto-open URL (single result only)
            if (AppSettings.Current.AutoOpenUrls && results.Count == 1 && results[0].IsUrl)
                try { Process.Start(results[0].Text); } catch { }

            // 5. Balloon tip
            string typeLabel = results.Count == 1
                ? (results[0].Format == "QR_CODE" ? "QR Code" : $"Barcode ({results[0].Format})")
                : $"{results.Count} codes found";

            string preview = combined.Length > 120 ? combined.Substring(0, 117) + "…" : combined;
            ShowNotification($"{typeLabel} detected", $"Copied: {preview}");
        }

        // ── Notifications ─────────────────────────────────────────────────────────
        public void ShowNotification(string title, string message)
        {
            trayIcon?.ShowBalloonTip(3500, title, message, ToolTipIcon.Info);
        }

        // ── Autorun ───────────────────────────────────────────────────────────────
        private static bool IsInStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                    return key?.GetValue(AppName) != null;
            }
            catch { return false; }
        }

        private void AutorunItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName);
                        autorunItem.Checked = false;
                        trayIcon.ShowBalloonTip(2000, AppName, "Removed from startup.", ToolTipIcon.Info);
                    }
                    else
                    {
                        key.SetValue(AppName, Application.ExecutablePath);
                        autorunItem.Checked = true;
                        trayIcon.ShowBalloonTip(2000, AppName, "Added to startup.", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not change startup setting:\n{ex.Message}",
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnregisterHotKey(Handle, HotkeyId);
            base.OnFormClosed(e);
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
        }
    }
}
