using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Barracks_Scanner_WPF
{
    class ImageProcessing
    {
        public static Bitmap[] BreakImage(RectangleF[] parts, Bitmap source)
        {
            if (source.Width == 0 || source.Height == 0)
                throw new Exception("Source must have width and height");
            //source.Save("Extract//" + extractInc + "precrop.png");
            Bitmap src = AutoCrop(source);
            //src.Save("Extract//" + extractInc + "crop.png");
            Bitmap[] toReturn = new Bitmap[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                toReturn[i] = Extract(src, parts[i]);
            }
            return toReturn;
        }

        public static int[] ScanVertColors(Color TargetColor, Bitmap src, int Closeness)
        {
            List<int> ys = new List<int>();
            Color lastPixel;
            for (int i = 0; i < src.Height; i++)
            {
                lastPixel = src.GetPixel(src.Width / 2, i);
                if (CheckCloseness(lastPixel, TargetColor) < Closeness)
                {
                    ys.Add(i);
                }
            }
            return ys.ToArray();
        }
        static int extractInc = 0;
        public static Bitmap Extract(Bitmap src, RectangleF area)
        {
            Rectangle crop = new Rectangle((int)(area.X * (double)src.Width), (int)(area.Y * (double)src.Height),
                (int)((double)src.Width * area.Width), (int)((double)src.Height * area.Height));
            var post = new Bitmap(crop.Width, crop.Height);
            using (Graphics g = Graphics.FromImage(post))
            {
                g.DrawImage(src, new Rectangle(0, 0, post.Width, post.Height),
                                 crop,
                                 GraphicsUnit.Pixel);
            }
            //src.Save("Extract//" + extractInc + "src.png");
            //post.Save("Extract//" + extractInc + "post.png");
            extractInc++;
            return post;
        }
        public static Bitmap Scale (Bitmap src, double scale)
        {
            var toReturn = new Bitmap((int)((double)src.Width * scale), (int)((double)src.Height * scale));
            using (Graphics g = Graphics.FromImage(toReturn))
            {
                g.DrawImage(src, new Rectangle(0, 0, toReturn.Width, toReturn.Height),
                                 new Rectangle(0, 0, src.Width, src.Height),
                                 GraphicsUnit.Pixel);
            }
            return toReturn;
        }

        public static Bitmap AutoCrop(Bitmap src)
        {
            int x1 = 0, x2 = src.Width, y1 = 0, y2 = src.Height;
            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            Color lastPixel;
            Color desiredPixel = Color.FromArgb(38, 74, 74);
            int CLOSENESS = 15;
            for (int i = 0; i < src.Height; i++)
            {
                lastPixel = src.GetPixel(src.Width / 2, i);
                if (CheckCloseness(lastPixel, desiredPixel) < CLOSENESS)
                {
                    ys.Add(i);
                }
            }
            for (int i = 0; i < src.Width; i++)
            {
                lastPixel = src.GetPixel(i, src.Height / 2);
                if (CheckCloseness(lastPixel, desiredPixel) < CLOSENESS)
                {
                    x1 = i;
                    break;
                }
            }
            for (int i = src.Width - 1; i > 0; i--)
            {
                lastPixel = src.GetPixel(i, src.Height / 2);
                if (CheckCloseness(lastPixel, desiredPixel) < CLOSENESS)
                {
                    x2 = i;
                    break;
                }
            }
            var yGap = LargestGap(ys.ToArray());
            y1 = ys[yGap[0]]; y2 = ys[yGap[1]];
            var crop = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            var post = new Bitmap(crop.Width, crop.Height);
            using (Graphics g = Graphics.FromImage(post))
            {
                g.DrawImage(src, new Rectangle(0, 0, post.Width, post.Height),
                                 crop,
                                 GraphicsUnit.Pixel);
            }
            //post.Save("Autocrop.png");
            return post;
        }

        private static int[] LargestGap(int[] input)
        {
            int largestGap = 0;
            int index = 0;
            for (int i = 0; i < input.Length - 1; i++)
            {
                if (largestGap < input[i + 1] - input[i])
                {
                    largestGap = input[i + 1] - input[i];
                    index = i;
                }
            }
            return new int[] { index, index + 1 };
        }

        public static int CheckCloseness(Color one, Color two)
        {
            int toReturn = 0;
            toReturn += Math.Abs(one.B - two.B);
            toReturn += Math.Abs(one.G - two.G);
            toReturn += Math.Abs(one.R - two.R);
            return toReturn;
        }
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        public static BitmapSource loadBitmap(System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }
    }
}
