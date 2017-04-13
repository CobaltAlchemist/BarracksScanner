using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace Barracks_Scanner_WPF
{
    class GameCapture
    {
        public static IntPtr WinGetHandle(string wName)
        {
            var titles = Process.GetProcesses().Where(pList => pList.MainWindowTitle.ToLower().Contains(wName)).ToList();
            switch (titles.Count)
            {
                case 1:
                    return titles[0].MainWindowHandle;
                case 0:
                    throw new ApplicationException("No application was found");
                default:
                    throw new ApplicationException("More than one application was found");
            }
        }

        public static void ExtractTextFromImage(IntPtr hWnd, ConcurrentBag<string> list, RectangleF area)
        {
            var b = new Bitmap(CaptureWindow(hWnd));
            Rectangle crop = new Rectangle((int)(area.X * (double)b.Width), (int)(area.Y * (double)b.Height),
                (int)((double)b.Width * area.Width), (int)((double)b.Height * area.Height));
            var post = new Bitmap(crop.Width, crop.Height);

            using (Graphics g = Graphics.FromImage(post))
            {
                g.DrawImage(b, new Rectangle(0, 0, post.Width, post.Height),
                                 crop,
                                 GraphicsUnit.Pixel);
            }
            // for testing if you want to save 
            //b.Save(@"d:\down\"+Guid.NewGuid()+".png", ImageFormat.Png);
            // Initialize scan engine
            post.Save("Testing.png");
            using (var engine = new TesseractEngine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata")
                , "eng", EngineMode.TesseractOnly))
            {
                using (var page = engine.Process(post))
                {
                    var s = page.GetText();
                    if (!string.IsNullOrWhiteSpace(s))
                        list.Add(s);
                }
            }
        }

        public static Image CaptureWindow(IntPtr handle)
        {
            var hdcSrc = User32.GetWindowDC(handle);
            var windowRect = new User32.Rect();
            User32.GetWindowRect(handle, ref windowRect);
            var width = windowRect.right - windowRect.left;
            var height = windowRect.bottom - windowRect.top;
            var hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);
            var hBitmap = Gdi32.CreateCompatibleBitmap(hdcSrc, width, height);
            var hOld = Gdi32.SelectObject(hdcDest, hBitmap);
            Gdi32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, Gdi32.Srccopy);
            Gdi32.SelectObject(hdcDest, hOld);
            Gdi32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            Image img = Image.FromHbitmap(hBitmap);
            Gdi32.DeleteObject(hBitmap);
            return img;
        }
        
        private class Gdi32
        {
            public const int Srccopy = 0x00CC0020; // BitBlt dwRop parameter

            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);

            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDc);

            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDc, int nWidth,
                int nHeight);

            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDc);

            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);

        }
        
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public readonly int top;
                public readonly int left;
                public readonly int bottom;
                public readonly int right;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
        }
    }
}
