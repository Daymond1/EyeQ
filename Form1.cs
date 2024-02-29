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

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public Form1()
        {
            InitializeComponent();

            if (IsAlreadyRunning())
            {
                MessageBox.Show("EyeQ is already running.", "EyeQ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
                return;
            }


            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Add/Remove to autorun", null, AddToStartupMenuItem_Click);
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "EyeQ";
            trayIcon.Icon = new Icon("ico.ico");

            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            trayIcon.Click += TrayIcon_Click;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Left)
            {
                // Створюємо та показуємо форму вибору області
                using (SelectionForm selectionForm = new SelectionForm())
                {
                    if (selectionForm.ShowDialog() == DialogResult.OK)
                    {
                        Rectangle selectedArea = selectionForm.SelectedArea;
                    }
                }
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false;
            ShowInTaskbar = false;

            base.OnLoad(e);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenu contextMenu = new ContextMenu();

                MenuItem closeMenuItem = new MenuItem("close");
                closeMenuItem.Click += CloseMenuItem_Click;
                contextMenu.MenuItems.Add(closeMenuItem);

                MenuItem addToStartupMenuItem = new MenuItem("add");
                addToStartupMenuItem.Click += AddToStartupMenuItem_Click;
                contextMenu.MenuItems.Add(addToStartupMenuItem);

                contextMenu.Show(this, Cursor.Position);
            }
        }

        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AddToStartupMenuItem_Click(object sender, EventArgs e)
        {
            string appPath = Application.ExecutablePath;

            try
            {
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (registryKey.GetValue(AppName) != null)
                    {
                        registryKey.DeleteValue(AppName);
                        MessageBox.Show("Removed from autorun");
                    }
                    else
                    {
                        registryKey.SetValue(AppName, appPath);
                        MessageBox.Show("Added to autorun");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not change the autorun: {ex.Message}");
            }
        }


        // Check if another instance of the application is already running
        private bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }
    }

}
