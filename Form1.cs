using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EyeQ
{
    public partial class Form1 : Form
    {
        private const string AppName = "EyeQ";
        private static NotifyIcon trayIcon; // Static to ensure single instance
        private ContextMenuStrip trayMenu;
        private SelectionForm selectionForm;

        public Form1()
        {
            InitializeComponent();

            // Prevent multiple instances
            if (IsAlreadyRunning())
            {
                MessageBox.Show("EyeQ is already running.", "EyeQ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
                return;
            }

            // Initialize ContextMenuStrip
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Add/Remove to autorun", null, AddToStartupMenuItem_Click);
            trayMenu.Items.Add("Exit", null, OnExit);

            // Initialize and configure NotifyIcon if it's not already done
            if (trayIcon == null)
            {
                trayIcon = new NotifyIcon
                {
                    Text = AppName,
                    Icon = new Icon("ico.ico"), // Ensure this icon exists in your project resources
                    ContextMenuStrip = trayMenu,
                    Visible = true
                };

                trayIcon.Click += TrayIcon_Click;
            }
        }
        public static void ShowNotification(string title, string message)
        {
            if (trayIcon != null)
            {
                trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
        }

        private bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }

        private void AddToStartupMenuItem_Click(object sender, EventArgs e)
        {
            string appPath = Application.ExecutablePath;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName);
                        MessageBox.Show("Removed from autorun", AppName);
                    }
                    else
                    {
                        key.SetValue(AppName, appPath);
                        MessageBox.Show("Added to autorun", AppName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not change the autorun setting: {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Visible = false; // Hide form window
            ShowInTaskbar = false; // Remove from taskbar
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Clean up the NotifyIcon when the form is closed
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                if (selectionForm == null || selectionForm.IsDisposed)
                {
                    selectionForm = new SelectionForm();
                    selectionForm.FormClosed += (s, args) => { selectionForm = null; };
                    selectionForm.Show();
                }
                else
                {
                    selectionForm.BringToFront();
                }
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip contextMenu = new ContextMenuStrip();

                ToolStripMenuItem closeMenuItem = new ToolStripMenuItem("Close");
                closeMenuItem.Click += CloseMenuItem_Click;
                contextMenu.Items.Add(closeMenuItem);

                ToolStripMenuItem addToStartupMenuItem = new ToolStripMenuItem("Add/Remove to autorun");
                addToStartupMenuItem.Click += AddToStartupMenuItem_Click;
                contextMenu.Items.Add(addToStartupMenuItem);

                contextMenu.Show(this, Cursor.Position);
            }
        }

      

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

    }
}
