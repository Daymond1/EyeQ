using System;
using System.Threading;
using System.Windows.Forms;

namespace EyeQ
{
    internal static class Program
    {
        private static Mutex _mutex;

        /// <summary>
        /// The main entry point for the application.
        /// Uses a named Mutex to guarantee a single running instance.
        /// </summary>
        [STAThread]
        static void Main()
        {
            _mutex = new Mutex(initiallyOwned: true, name: "EyeQ_SingleInstance_9F3A", out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show(
                    "EyeQ is already running.\nCheck your system tray.",
                    "EyeQ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            finally
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}
