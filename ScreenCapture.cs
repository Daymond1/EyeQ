using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace EyeQ
{
    /// <summary>
    /// Provides low-level screen capture via GDI BitBlt.
    /// All native handles are properly released in a try/finally block.
    /// </summary>
    public static class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr dcDest, int xDest, int yDest, int width, int height,
            IntPtr dcSrc, int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr dc);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        private const uint SRCCOPY = 0x00CC0020;

        /// <summary>
        /// Captures a region of the screen defined by <paramref name="bounds"/> in screen coordinates.
        /// Caller is responsible for disposing the returned <see cref="Bitmap"/>.
        /// </summary>
        public static Bitmap CaptureScreen(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new ArgumentException("Bounds must have positive dimensions.", nameof(bounds));

            IntPtr desktopDC = GetDC(IntPtr.Zero);
            IntPtr memDC     = IntPtr.Zero;
            IntPtr hBitmap   = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                memDC     = CreateCompatibleDC(desktopDC);
                hBitmap   = CreateCompatibleBitmap(desktopDC, bounds.Width, bounds.Height);
                oldBitmap = SelectObject(memDC, hBitmap);

                BitBlt(memDC, 0, 0, bounds.Width, bounds.Height,
                       desktopDC, bounds.X, bounds.Y, SRCCOPY);

                SelectObject(memDC, oldBitmap);

                // Clone immediately so we can free the native handle
                Bitmap result = Image.FromHbitmap(hBitmap);
                return result;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (memDC   != IntPtr.Zero) DeleteDC(memDC);
                ReleaseDC(IntPtr.Zero, desktopDC);
            }
        }
    }
}
