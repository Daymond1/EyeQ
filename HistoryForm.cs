using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EyeQ
{
    /// <summary>
    /// Dark-themed form that shows the scan history as an interactive grid.
    /// Auto-refreshes when new items are added via <see cref="ScanHistory.HistoryChanged"/>.
    /// </summary>
    public class HistoryForm : Form
    {
        // ── Dark title bar (Windows 10 1809+ / Windows 11) ────────────────────────
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // ── UI controls ───────────────────────────────────────────────────────────
        private DataGridView _grid;
        private Button       _copyBtn, _openBtn, _deleteBtn, _clearBtn;
        private Label        _countLabel;

        // ── Colours ───────────────────────────────────────────────────────────────
        private static readonly Color BgDark       = Color.FromArgb(20, 20, 30);
        private static readonly Color BgMid        = Color.FromArgb(26, 26, 38);
        private static readonly Color BgAlt        = Color.FromArgb(30, 30, 44);
        private static readonly Color BgPanel      = Color.FromArgb(14, 14, 22);
        private static readonly Color AccentBlue   = Color.FromArgb(40, 90, 175);
        private static readonly Color AccentRed    = Color.FromArgb(140, 40, 40);
        private static readonly Color TextMain     = Color.FromArgb(215, 215, 230);
        private static readonly Color TextSub      = Color.FromArgb(130, 130, 158);
        private static readonly Color SelectionBg  = Color.FromArgb(50, 100, 185);
        private static readonly Color GridLine     = Color.FromArgb(40, 40, 58);

        // ── Constructor ───────────────────────────────────────────────────────────
        public HistoryForm()
        {
            BuildUI();
            RefreshGrid();
            ScanHistory.HistoryChanged += OnHistoryChanged;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int v = 1; DwmSetWindowAttribute(Handle, 20, ref v, 4); } catch { }
        }

        // ── UI construction ───────────────────────────────────────────────────────
        private void BuildUI()
        {
            this.Text            = "EyeQ — Scan History";
            this.Size            = new Size(800, 520);
            this.MinimumSize     = new Size(600, 380);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = BgDark;
            this.ForeColor       = TextMain;
            this.Font            = new Font("Segoe UI", 9.5f);

            // ── DataGridView ──────────────────────────────────────────────────────
            _grid = new DataGridView
            {
                Dock                              = DockStyle.Fill,
                AllowUserToAddRows                = false,
                AllowUserToDeleteRows             = false,
                AllowUserToResizeRows             = false,
                ReadOnly                          = true,
                SelectionMode                     = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                       = false,
                ColumnHeadersHeightSizeMode       = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight               = 34,
                RowTemplate                       = { Height = 28 },
                BackgroundColor                   = BgDark,
                GridColor                         = GridLine,
                BorderStyle                       = BorderStyle.None,
                RowHeadersVisible                 = false,
                CellBorderStyle                   = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles         = false,
                ShowCellToolTips                  = true,
                ScrollBars                        = ScrollBars.Both,
            };

            _grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor         = BgMid,
                ForeColor         = TextMain,
                SelectionBackColor = SelectionBg,
                SelectionForeColor = Color.White,
                Padding           = new Padding(6, 0, 6, 0),
                WrapMode          = DataGridViewTriState.False,
            };

            _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor         = BgPanel,
                ForeColor         = TextSub,
                SelectionBackColor = BgPanel,
                SelectionForeColor = TextSub,
                Font              = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding           = new Padding(6, 0, 4, 0),
            };

            _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor         = BgAlt,
                SelectionBackColor = SelectionBg,
                SelectionForeColor = Color.White,
            };

            // ── Columns ───────────────────────────────────────────────────────────
            var colTime = new DataGridViewTextBoxColumn
                { HeaderText = "Time", Width = 132, SortMode = DataGridViewColumnSortMode.NotSortable };
            var colType = new DataGridViewTextBoxColumn
                { HeaderText = "Type", Width = 116, SortMode = DataGridViewColumnSortMode.NotSortable };
            var colContent = new DataGridViewTextBoxColumn
            {
                HeaderText   = "Content",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode     = DataGridViewColumnSortMode.NotSortable,
            };

            _grid.Columns.AddRange(colTime, colType, colContent);

            // ── Grid events ───────────────────────────────────────────────────────
            _grid.CellDoubleClick    += (s, e) => { if (e.RowIndex >= 0) CopySelected(); };
            _grid.SelectionChanged   += (s, e) => UpdateButtonStates();
            _grid.KeyDown            += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)               { DeleteSelected(); e.Handled = true; }
                if (e.Control && e.KeyCode == Keys.C)       { CopySelected();   e.Handled = true; }
            };

            // ── Right-click context menu ──────────────────────────────────────────
            var ctxMenu = new ContextMenuStrip { BackColor = BgPanel, ForeColor = TextMain };
            var ctxCopy   = new ToolStripMenuItem("Copy");
            var ctxOpen   = new ToolStripMenuItem("Open URL in browser");
            var ctxDelete = new ToolStripMenuItem("Delete entry");
            var ctxSep    = new ToolStripSeparator();
            var ctxClear  = new ToolStripMenuItem("Clear all history");

            ctxCopy.Click   += (s, e) => CopySelected();
            ctxOpen.Click   += (s, e) => OpenSelected();
            ctxDelete.Click += (s, e) => DeleteSelected();
            ctxClear.Click  += (s, e) => ConfirmClear();

            ctxMenu.Items.AddRange(new ToolStripItem[] { ctxCopy, ctxOpen, ctxDelete, ctxSep, ctxClear });

            ctxMenu.Opening += (s, e) =>
            {
                bool hasRow = _grid.SelectedRows.Count > 0;
                var entry = GetSelectedEntry();
                ctxCopy.Enabled   = hasRow;
                ctxOpen.Enabled   = entry?.IsUrl == true;
                ctxDelete.Enabled = hasRow;
            };

            _grid.ContextMenuStrip = ctxMenu;

            // ── Bottom panel ──────────────────────────────────────────────────────
            var bottomPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 50,
                BackColor = BgPanel,
                Padding   = new Padding(10, 9, 10, 9),
            };

            _copyBtn   = MakeBtn("Copy",     AccentBlue,                  80);
            _openBtn   = MakeBtn("Open URL", Color.FromArgb(30, 100, 80), 86);
            _deleteBtn = MakeBtn("Delete",   Color.FromArgb(90, 45, 45),  72);
            _clearBtn  = MakeBtn("Clear All",AccentRed,                   86);

            _copyBtn.Click   += (s, e) => CopySelected();
            _openBtn.Click   += (s, e) => OpenSelected();
            _deleteBtn.Click += (s, e) => DeleteSelected();
            _clearBtn.Click  += (s, e) => ConfirmClear();

            _countLabel = new Label
            {
                AutoSize  = true,
                Dock      = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = TextSub,
                Font      = new Font("Segoe UI", 9f),
            };

            var btnFlow = new FlowLayoutPanel
            {
                Dock         = DockStyle.Left,
                AutoSize     = true,
                FlowDirection= FlowDirection.LeftToRight,
                BackColor    = Color.Transparent,
                WrapContents = false,
                Padding      = Padding.Empty,
            };
            btnFlow.Controls.AddRange(new Control[] { _copyBtn, _openBtn, _deleteBtn, _clearBtn });

            bottomPanel.Controls.Add(_countLabel);
            bottomPanel.Controls.Add(btnFlow);

            this.Controls.Add(_grid);
            this.Controls.Add(bottomPanel);
        }

        // ── Helper: make a styled button ─────────────────────────────────────────
        private static Button MakeBtn(string text, Color back, int width) =>
            new Button
            {
                Text      = text,
                Width     = width,
                Height    = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f),
                Margin    = new Padding(0, 0, 6, 0),
                Cursor    = Cursors.Hand,
                FlatAppearance = { BorderColor = Color.FromArgb(80, 80, 110), BorderSize = 1 },
            };

        // ── Grid population ───────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (ScanEntry entry in ScanHistory.Entries)
            {
                int idx = _grid.Rows.Add(
                    entry.Timestamp.ToString("MM/dd  HH:mm:ss"),
                    entry.FormatFriendly,
                    entry.Text);
                _grid.Rows[idx].Tag = entry;
                _grid.Rows[idx].Cells[2].ToolTipText = entry.Text;
            }

            int count = ScanHistory.Entries.Count;
            _countLabel.Text = count == 0 ? "No history" : $"{count} entr{(count == 1 ? "y" : "ies")}";
            UpdateButtonStates();
        }

        private void OnHistoryChanged()
        {
            if (InvokeRequired) Invoke((Action)RefreshGrid);
            else                RefreshGrid();
        }

        // ── Button state ──────────────────────────────────────────────────────────
        private void UpdateButtonStates()
        {
            var entry = GetSelectedEntry();
            _copyBtn.Enabled   = entry != null;
            _deleteBtn.Enabled = entry != null;
            _openBtn.Enabled   = entry?.IsUrl == true;
            _clearBtn.Enabled  = ScanHistory.Entries.Count > 0;
        }

        // ── Actions ───────────────────────────────────────────────────────────────
        private ScanEntry GetSelectedEntry() =>
            _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Tag as ScanEntry : null;

        private void CopySelected()
        {
            var e = GetSelectedEntry();
            if (e != null) Clipboard.SetText(e.Text);
        }

        private void OpenSelected()
        {
            var e = GetSelectedEntry();
            if (e?.IsUrl == true)
                try { Process.Start(e.Text); } catch { }
        }

        private void DeleteSelected()
        {
            var e = GetSelectedEntry();
            if (e != null) ScanHistory.Remove(e);
        }

        private void ConfirmClear()
        {
            if (MessageBox.Show("Clear all scan history?", "EyeQ",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                ScanHistory.Clear();
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ScanHistory.HistoryChanged -= OnHistoryChanged;
            base.OnFormClosed(e);
        }
    }
}
