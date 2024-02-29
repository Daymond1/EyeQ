using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ZXing;
using System.IO; // Ensure ZXing is properly referenced for QR code functionality

namespace EyeQ
{
    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr dcDest, int xDest, int yDest, int width, int height, IntPtr dcSrc, int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr dc);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr obj);

        public const int SRCCOPY = 0x00CC0020;

        public static Bitmap CaptureScreen(Rectangle bounds)
        {
            IntPtr desktopDC = GetDC(IntPtr.Zero);
            IntPtr memDC = CreateCompatibleDC(desktopDC);
            IntPtr hBitmap = CreateCompatibleBitmap(desktopDC, bounds.Width, bounds.Height);
            IntPtr oldBitmap = SelectObject(memDC, hBitmap);

            BitBlt(memDC, 0, 0, bounds.Width, bounds.Height, desktopDC, bounds.X, bounds.Y, SRCCOPY);
            SelectObject(memDC, oldBitmap);

            Bitmap bmp = Image.FromHbitmap(hBitmap);
            DeleteObject(hBitmap);
            ReleaseDC(IntPtr.Zero, desktopDC);
            DeleteDC(memDC);

            return bmp;
        }
    }

    public class SelectionForm : Form
    {
        private Rectangle selectedArea;
        private Point startPoint;
        private static NotifyIcon trayIcon;
        private bool isSelecting;

        public SelectionForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.Opacity = 0.3;
            this.Cursor = Cursors.Cross;

            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            if (trayIcon == null)
            {
                trayIcon = new NotifyIcon
                {
                    Visible = true,
                    Icon = SystemIcons.Application,
                    Text = "EyeQ"
                };
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location;
                isSelecting = true;
                selectedArea = new Rectangle(e.Location, new Size());
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isSelecting)
            {
                selectedArea = new Rectangle(
                    Math.Min(startPoint.X, e.X),
                    Math.Min(startPoint.Y, e.Y),
                    Math.Abs(e.X - startPoint.X),
                    Math.Abs(e.Y - startPoint.Y));

                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (selectedArea.Width > 0 && selectedArea.Height > 0)
            {
                Bitmap screenshot = ScreenCapture.CaptureScreen(selectedArea);
                string filePath = Path.Combine(Application.StartupPath, "screenshot.png");
                screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                AnalyzeScreenshotForQRCode(screenshot);

                screenshot.Dispose();
            }
            this.Close();
        }

        public void AnalyzeScreenshotForQRCode(Bitmap screenshot)
        {
            var reader = new BarcodeReader();
            var result = reader.Decode(screenshot);
            if (result != null)
            {
                Form1.ShowNotification("QR Code Detected", "QR Code content copied to clipboard: " + result.Text);
                Clipboard.SetText(result.Text);
            }
            else
            {
                Form1.ShowNotification("QR Code Not Found", "No QR code was found in the selected area.");
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isSelecting)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, selectedArea);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
        }
    }
}
