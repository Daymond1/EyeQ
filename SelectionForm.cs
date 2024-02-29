using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ZXing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.VisualStyles;
using ToastNotifications.Core;
using ToastNotifications;
using ToastNotifications.Position;
using System.IO;

namespace EyeQ
{
    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr ptr);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
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
            IntPtr desktopPtr = GetDesktopWindow();
            IntPtr desktopDC = GetDC(desktopPtr);
            IntPtr compatibleDC = CreateCompatibleDC(desktopDC);
            IntPtr compatibleBitmap = CreateCompatibleBitmap(desktopDC, bounds.Width, bounds.Height);
            IntPtr oldBitmap = SelectObject(compatibleDC, compatibleBitmap);

            BitBlt(compatibleDC, 0, 0, bounds.Width, bounds.Height, desktopDC, bounds.X, bounds.Y, SRCCOPY);

            Bitmap bitmap = Image.FromHbitmap(compatibleBitmap);

            // Clean up
            SelectObject(compatibleDC, oldBitmap);
            DeleteObject(compatibleBitmap);
            DeleteDC(compatibleDC);
            ReleaseDC(desktopPtr, desktopDC);

            return bitmap;
        }
    }

    public class SelectionForm : Form
    {
        public Rectangle SelectedArea { get; private set; }

        private readonly NotifyIcon _notifyIcon;

        public SelectionForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.Opacity = 0.3;
            this.Cursor = Cursors.Cross;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = true;

            string appDirectory = Path.GetDirectoryName(Application.ExecutablePath);

            string iconPath = Path.Combine(appDirectory, "ico.ico");

            if (File.Exists(iconPath))
            {
                Icon appIcon = new Icon(iconPath);
                _notifyIcon.Icon = appIcon;
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Information;
            }


        }

        private Point start;
        private float scalingFactor;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            start = e.Location;
            scalingFactor = GetScalingFactor();
            SelectedArea = new Rectangle((int)(e.X / scalingFactor), (int)(e.Y / scalingFactor), 0, 0);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                SelectedArea = new Rectangle(
                    Math.Min((int)(start.X / scalingFactor), (int)(e.X / scalingFactor)),
                    Math.Min((int)(start.Y / scalingFactor), (int)(e.Y / scalingFactor)),
                    Math.Abs((int)((e.X - start.X) / scalingFactor)),
                    Math.Abs((int)((e.Y - start.Y) / scalingFactor)));
                this.Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (SelectedArea.Width > 0 && SelectedArea.Height > 0)
            {
                this.DialogResult = DialogResult.OK;

                Bitmap screenshot = ScreenCapture.CaptureScreen(new Rectangle((int)(SelectedArea.X * scalingFactor), (int)(SelectedArea.Y * scalingFactor), (int)(SelectedArea.Width * scalingFactor), (int)(SelectedArea.Height * scalingFactor)));

                screenshot.Save("screenshot.png", System.Drawing.Imaging.ImageFormat.Png);

                AnalyzeScreenshotForQRCode(screenshot);
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
            }
            this.Close();
        }

        public void AnalyzeScreenshotForQRCode(Bitmap screenshot)
        {
            var reader = new BarcodeReader();
            var result = reader.Decode(screenshot);
            if (result != null)
            {
                ShowToastNotification("QR Code detected and copied to clipboard", result.Text);
            }
            else
            {
                ShowToastNotification("QR Code Not Found", "No QR code was found in the selected area.");
            }
            screenshot.Dispose();
        }

        private void ShowToastNotification(string title, string message)
        {
           

            _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.None);

            Clipboard.SetText(message);
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(this.ClientRectangle);

            if (SelectedArea.Width > 0 && SelectedArea.Height > 0)
            {
                path.AddRectangle(new RectangleF(SelectedArea.X * scalingFactor, SelectedArea.Y * scalingFactor, SelectedArea.Width * scalingFactor, SelectedArea.Height * scalingFactor));
            }

            Region region = new Region(path);
            this.Region = region;

            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 0)), this.ClientRectangle);

            this.Region = new Region(this.ClientRectangle);
        }

        private float GetScalingFactor()
        {
            using (Graphics graphics = this.CreateGraphics())
            {
                return graphics.DpiX / 96f;
            }
        }
    }
}
