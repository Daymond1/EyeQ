using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace EyeQ
{
    /// <summary>
    /// Full-screen transparent overlay for selecting a screen region.
    /// Spans all monitors, supports Escape to cancel, renders a dimensions overlay
    /// and corner markers during selection.
    /// </summary>
    public class SelectionForm : Form
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired after analysis completes (list may be empty = nothing found).</summary>
        public event Action<IList<ScanResult>> ScanCompleted;
        /// <summary>Fired for capture / unexpected errors.</summary>
        public event Action<string, string>    NotificationRequested;

        // ── Selection state ───────────────────────────────────────────────────────
        private Rectangle selectedArea;   // screen coordinates
        private Point     startPoint;     // screen coordinates
        private bool      isSelecting;

        // ── Drawing resources ─────────────────────────────────────────────────────
        private readonly Font       overlayFont;
        private readonly SolidBrush bgBrush;
        private readonly SolidBrush textBrush;

        // ── Constructor ───────────────────────────────────────────────────────────
        public SelectionForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost         = true;
            this.ShowInTaskbar   = false;
            this.BackColor       = Color.Black;
            this.Opacity         = 0.30;
            this.Cursor          = Cursors.Cross;

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint            |
                ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            // Span the virtual desktop (all monitors)
            Rectangle vScreen = SystemInformation.VirtualScreen;
            this.SetBounds(vScreen.X, vScreen.Y, vScreen.Width, vScreen.Height);

            overlayFont = new Font("Segoe UI", 10, FontStyle.Bold, GraphicsUnit.Point);
            bgBrush     = new SolidBrush(Color.FromArgb(185, 0, 0, 0));
            textBrush   = new SolidBrush(Color.White);
        }

        // ── Keyboard ──────────────────────────────────────────────────────────────
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { this.Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Mouse ─────────────────────────────────────────────────────────────────
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                startPoint   = PointToScreen(e.Location);
                isSelecting  = true;
                selectedArea = new Rectangle(startPoint, Size.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!isSelecting) return;
            Point current = PointToScreen(e.Location);
            selectedArea = new Rectangle(
                Math.Min(startPoint.X, current.X),
                Math.Min(startPoint.Y, current.Y),
                Math.Abs(current.X - startPoint.X),
                Math.Abs(current.Y - startPoint.Y));
            this.Invalidate();
        }

        protected override async void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!isSelecting) return;
            isSelecting = false;

            if (selectedArea.Width > 5 && selectedArea.Height > 5)
            {
                this.Visible = false; // hide overlay before capture
                try
                {
                    using (Bitmap screenshot = ScreenCapture.CaptureScreen(selectedArea))
                    {
                        IList<ScanResult> results = await QRAnalyzer.AnalyzeMultipleAsync(screenshot);
                        ScanCompleted?.Invoke(results);
                    }
                }
                catch (Exception ex)
                {
                    NotificationRequested?.Invoke("Capture error", ex.Message);
                }
            }

            this.Close();
        }

        // ── Painting ──────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!isSelecting || selectedArea.Width <= 0 || selectedArea.Height <= 0) return;

            // Convert screen → client coordinates for drawing
            Rectangle client = new Rectangle(
                PointToClient(new Point(selectedArea.X, selectedArea.Y)),
                selectedArea.Size);

            Graphics g = e.Graphics;

            // Border
            using (Pen borderPen = new Pen(Color.FromArgb(64, 196, 255), 1.5f))
                g.DrawRectangle(borderPen, client);

            // Corner markers
            int m = Math.Min(14, Math.Min(client.Width / 3, client.Height / 3));
            using (Pen cornerPen = new Pen(Color.White, 2.5f))
                DrawCorners(g, cornerPen, client, m);

            // Dimensions label
            if (selectedArea.Width >= 50 && selectedArea.Height >= 20)
            {
                string label = $"{selectedArea.Width} × {selectedArea.Height}";
                SizeF  sz    = g.MeasureString(label, overlayFont);

                float tx = client.Left + (client.Width - sz.Width) / 2f;
                float ty = client.Bottom + 6;
                if (ty + sz.Height + 4 > this.ClientSize.Height)
                    ty = client.Top - sz.Height - 10;

                g.FillRectangle(bgBrush, tx - 4, ty - 2, sz.Width + 8, sz.Height + 4);
                g.DrawString(label, overlayFont, textBrush, tx, ty);
            }
        }

        private static void DrawCorners(Graphics g, Pen pen, Rectangle r, int len)
        {
            g.DrawLine(pen, r.Left,  r.Top,    r.Left + len, r.Top);
            g.DrawLine(pen, r.Left,  r.Top,    r.Left,       r.Top + len);
            g.DrawLine(pen, r.Right, r.Top,    r.Right - len, r.Top);
            g.DrawLine(pen, r.Right, r.Top,    r.Right,       r.Top + len);
            g.DrawLine(pen, r.Left,  r.Bottom, r.Left + len,  r.Bottom);
            g.DrawLine(pen, r.Left,  r.Bottom, r.Left,        r.Bottom - len);
            g.DrawLine(pen, r.Right, r.Bottom, r.Right - len, r.Bottom);
            g.DrawLine(pen, r.Right, r.Bottom, r.Right,       r.Bottom - len);
        }

        // ── Dispose ───────────────────────────────────────────────────────────────
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                overlayFont?.Dispose();
                bgBrush?.Dispose();
                textBrush?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
