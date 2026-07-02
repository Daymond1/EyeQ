using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EyeQ
{
    /// <summary>
    /// Dark-themed settings form. Lets the user configure the global hotkey,
    /// scanning behaviour, and history limit.  Calls <see cref="Form1.ReapplyHotkey"/>
    /// on save so the new hotkey takes effect immediately.
    /// </summary>
    public class SettingsForm : Form
    {
        // ── Dark title bar ────────────────────────────────────────────────────────
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // ── Owner ─────────────────────────────────────────────────────────────────
        private readonly Form1 _owner;

        // ── Hotkey controls ───────────────────────────────────────────────────────
        private CheckBox _chkCtrl, _chkShift, _chkAlt, _chkWin;
        private ComboBox _cmbKey;
        private Label    _lblPreview;

        // ── Scan controls ─────────────────────────────────────────────────────────
        private CheckBox _chkSound, _chkAutoUrl;

        // ── History controls ──────────────────────────────────────────────────────
        private NumericUpDown _nudHistory;

        // ── Key map ───────────────────────────────────────────────────────────────
        private static readonly List<(string Name, uint Vk)> KeyList = BuildKeyList();

        private static List<(string Name, uint Vk)> BuildKeyList()
        {
            var list = new List<(string, uint)>();
            for (char c = 'A'; c <= 'Z'; c++)
                list.Add((c.ToString(), (uint)c));
            for (int i = 0; i <= 9; i++)
                list.Add((i.ToString(), (uint)(0x30 + i)));
            for (int i = 1; i <= 12; i++)
                list.Add(($"F{i}", (uint)(0x6F + i)));
            return list;
        }

        // ── Colours ───────────────────────────────────────────────────────────────
        private static readonly Color BgDark    = Color.FromArgb(20, 20, 30);
        private static readonly Color BgInput   = Color.FromArgb(30, 30, 44);
        private static readonly Color TextMain  = Color.FromArgb(215, 215, 230);
        private static readonly Color TextSub   = Color.FromArgb(140, 140, 165);
        private static readonly Color AccentGrp = Color.FromArgb(100, 130, 200);

        // ── Constructor ───────────────────────────────────────────────────────────
        public SettingsForm(Form1 owner)
        {
            _owner = owner;
            BuildUI();
            LoadCurrentSettings();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int v = 1; DwmSetWindowAttribute(Handle, 20, ref v, 4); } catch { }
        }

        // ── UI Construction ───────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Text            = "EyeQ — Settings";
            this.Size            = new Size(440, 390);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = BgDark;
            this.ForeColor       = TextMain;
            this.Font            = new Font("Segoe UI", 9.5f);

            int y = 12;

            // ── Hotkey group ──────────────────────────────────────────────────────
            var grpHotkey = MakeGroup("Hotkey", y, 104);
            y += grpHotkey.Height + 10;

            // Modifiers row
            _chkCtrl  = MakeCheck("Ctrl",  14, 26);
            _chkShift = MakeCheck("Shift", 70, 26);
            _chkAlt   = MakeCheck("Alt",  134, 26);
            _chkWin   = MakeCheck("Win",  188, 26);

            // Key row
            var lblKey = MakeLbl("Key:", 14, 60);
            _cmbKey = new ComboBox
            {
                Left         = 50,
                Top          = 56,
                Width        = 72,
                DropDownStyle= ComboBoxStyle.DropDownList,
                FlatStyle    = FlatStyle.Flat,
                BackColor    = BgInput,
                ForeColor    = TextMain,
            };
            foreach (var (name, _) in KeyList) _cmbKey.Items.Add(name);

            _lblPreview = new Label
            {
                Left      = 132,
                Top       = 60,
                Width     = 230,
                Height    = 22,
                AutoSize  = false,
                ForeColor = Color.FromArgb(90, 185, 100),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            };

            grpHotkey.Controls.AddRange(new Control[]
                { _chkCtrl, _chkShift, _chkAlt, _chkWin, lblKey, _cmbKey, _lblPreview });

            _chkCtrl.CheckedChanged      += (s, e) => UpdatePreview();
            _chkShift.CheckedChanged     += (s, e) => UpdatePreview();
            _chkAlt.CheckedChanged       += (s, e) => UpdatePreview();
            _chkWin.CheckedChanged       += (s, e) => UpdatePreview();
            _cmbKey.SelectedIndexChanged += (s, e) => UpdatePreview();

            // ── Scanning group ────────────────────────────────────────────────────
            var grpScan = MakeGroup("Scanning", y, 88);
            y += grpScan.Height + 10;

            _chkSound   = MakeCheck("Play a sound when a code is detected", 14, 26);
            _chkAutoUrl = MakeCheck("Auto-open URLs in browser (single result only)", 14, 52);

            grpScan.Controls.AddRange(new Control[] { _chkSound, _chkAutoUrl });

            // ── History group ─────────────────────────────────────────────────────
            var grpHistory = MakeGroup("History", y, 66);
            y += grpHistory.Height + 16;

            var lblMax = MakeLbl("Max entries to keep:", 14, 30);
            _nudHistory = new NumericUpDown
            {
                Left        = 180,
                Top         = 26,
                Width       = 72,
                Minimum     = 5,
                Maximum     = 500,
                Value       = 20,
                BackColor   = BgInput,
                ForeColor   = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
            };

            grpHistory.Controls.AddRange(new Control[] { lblMax, _nudHistory });

            // ── Buttons ───────────────────────────────────────────────────────────
            var btnSave = new Button
            {
                Text      = "Save",
                Left      = 236,
                Top       = y,
                Width     = 90,
                Height    = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 120, 65),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 },
            };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Left      = 334,
                Top       = y,
                Width     = 80,
                Height    = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 40, 40),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 },
            };

            btnSave.Click   += BtnSave_Click;
            btnCancel.Click += (s, e) => this.Close();
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;

            this.Controls.AddRange(new Control[] { grpHotkey, grpScan, grpHistory, btnSave, btnCancel });
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private GroupBox MakeGroup(string title, int y, int height)
        {
            var g = new GroupBox
            {
                Text      = title,
                Left      = 12,
                Top       = y,
                Width     = 402,
                Height    = height,
                ForeColor = AccentGrp,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            };
            this.Controls.Add(g);
            return g;
        }

        private static CheckBox MakeCheck(string text, int left, int top) =>
            new CheckBox { Text = text, Left = left, Top = top, AutoSize = true,
                           ForeColor = Color.FromArgb(210, 210, 228) };

        private static Label MakeLbl(string text, int left, int top) =>
            new Label { Text = text, Left = left, Top = top, AutoSize = true,
                        ForeColor = Color.FromArgb(160, 160, 185) };

        // ── Load / Update / Save ──────────────────────────────────────────────────
        private void LoadCurrentSettings()
        {
            var s = AppSettings.Current;
            _chkCtrl.Checked  = (s.HotkeyModifiers & 0x0002) != 0;
            _chkShift.Checked = (s.HotkeyModifiers & 0x0004) != 0;
            _chkAlt.Checked   = (s.HotkeyModifiers & 0x0001) != 0;
            _chkWin.Checked   = (s.HotkeyModifiers & 0x0008) != 0;

            int keyIdx = KeyList.FindIndex(k => k.Vk == s.HotkeyVirtualKey);
            _cmbKey.SelectedIndex = keyIdx >= 0 ? keyIdx : KeyList.FindIndex(k => k.Name == "Q");

            _chkSound.Checked   = s.PlaySoundOnDetection;
            _chkAutoUrl.Checked = s.AutoOpenUrls;
            _nudHistory.Value   = Math.Max(5, Math.Min(500, s.MaxHistoryCount));

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var parts = new List<string>();
            if (_chkCtrl.Checked)  parts.Add("Ctrl");
            if (_chkShift.Checked) parts.Add("Shift");
            if (_chkAlt.Checked)   parts.Add("Alt");
            if (_chkWin.Checked)   parts.Add("Win");
            if (_cmbKey.SelectedIndex >= 0) parts.Add(KeyList[_cmbKey.SelectedIndex].Name);
            _lblPreview.Text = parts.Count > 1 ? "→ " + string.Join("+", parts) : "";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            uint mods = 0;
            if (_chkCtrl.Checked)  mods |= 0x0002;
            if (_chkShift.Checked) mods |= 0x0004;
            if (_chkAlt.Checked)   mods |= 0x0001;
            if (_chkWin.Checked)   mods |= 0x0008;

            if (mods == 0 || _cmbKey.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select at least one modifier (Ctrl / Shift / Alt / Win) and a key.",
                    "Invalid hotkey", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var s = AppSettings.Current;
            s.HotkeyModifiers      = mods;
            s.HotkeyVirtualKey     = KeyList[_cmbKey.SelectedIndex].Vk;
            s.PlaySoundOnDetection = _chkSound.Checked;
            s.AutoOpenUrls         = _chkAutoUrl.Checked;
            s.MaxHistoryCount      = (int)_nudHistory.Value;
            s.Save();

            _owner.ReapplyHotkey();
            this.Close();
        }
    }
}
