using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Barracks_Scanner_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        List<Bitmap> mToScan = new List<Bitmap>();
        Mutex locker = new Mutex();
        Mutex soldiersLocker = new Mutex();
        Bitmap cogImage;
        public ObservableCollection<Soldier> Soldiers = new ObservableCollection<Soldier>();
        //readonly List<Task> _tasks = new List<Task>();
        readonly RectangleF SizeOfSoldier = new RectangleF(.25f, .17f, .52f, .06f);
        Thread[] Threads = new Thread[Environment.ProcessorCount];
        Thread ProcessThread;
        
        public MainWindow()
        {
            InitializeComponent();
            winData.DataContext = Soldiers;
            winTotalSoldiers.DataContext = Soldiers;
            BindingOperations.EnableCollectionSynchronization(Soldiers, soldiersLocker);
        }

        Soldier soldier = new Soldier();

        private void ProcessScan()
        {
            bool isChecked = false;
            Dispatcher.Invoke(DispatcherPriority.Send,
                (ThreadStart)delegate { isChecked = winScanCheckBox.IsChecked ?? false; });

            while (isChecked)
            {
                try
                {
                    Bitmap b = GameScanner.CaptureApplication("XCom2");
                    Dispatcher.Invoke(() =>
                    {
                        cogImage = new Bitmap(b);
                        winCogsImg.Source = ImageProcessing.loadBitmap(cogImage);
                    });

                    var matches = FindAllSoldiers(b);
                    List<System.Drawing.Rectangle> rectangles = new List<System.Drawing.Rectangle>();
                    for (int i = 0; i < matches.Length; i++)
                    {
                        rectangles.Add(new System.Drawing.Rectangle(b.Width / 4, matches[i], (b.Width * 52) / 100, (b.Height * 53) / 1000));
                    }
                    Bitmap temp1 = new Bitmap(b);
                    using (Graphics g = Graphics.FromImage(temp1))
                    {
                        g.DrawRectangles(new System.Drawing.Pen(System.Drawing.Color.Red, 1.5f), rectangles.ToArray());
                        g.Flush();
                    }
                    Dispatcher.Invoke(() =>
                    {
                        cogImage = new Bitmap(temp1);
                        winCogsImg.Source = ImageProcessing.loadBitmap(cogImage);
                    });
                    temp1.Dispose();
                    while (mToScan.Count > 0)
                    {
                        Thread.Sleep(500);
                    }

                    lock (locker)
                    {
                        for (int i = 0; i < matches.Length; i++)
                        {
                            Bitmap temp = ImageProcessing.Extract(b, new RectangleF(.25f, (float)matches[i] / (float)b.Height, .525f, .06f));
                            mToScan.Add(temp);
                        }
                    }
                    Thread.Sleep(1000);
                }
                catch(Exception e)
                {
                    //maybe one day I'll be bothered to put real exception handling in ¯\_(ツ)_/¯
                }

                Dispatcher.Invoke(DispatcherPriority.Send,
                    (ThreadStart)delegate { isChecked = winScanCheckBox.IsChecked ?? false; });
            }

            lock (locker)
            {
                mToScan.Clear();
            }
        }

        private void ExtractSoldierInfo()
        {
            Bitmap image = new Bitmap(1,1);
            OCRParser ocr = new OCRParser();
            List<RectangleF> Sections = new List<RectangleF>();
            Sections.Add(new RectangleF(0.128f, 0.26f, 0.045f, 0.46f));
            Sections.Add(new RectangleF(0.18f, 0.06f, 0.39f, 0.41f));
            Sections.Add(new RectangleF(0.202f, 0.5f, 0.035f, 0.41f));
            Sections.Add(new RectangleF(0.26f, 0.5f, 0.032f, 0.41f));
            Sections.Add(new RectangleF(0.315f, 0.5f, 0.041f, 0.41f));
            Sections.Add(new RectangleF(0.38f, 0.5f, 0.04f, 0.41f));
            Sections.Add(new RectangleF(0.44f, 0.5f, 0.036f, 0.41f));
            Sections.Add(new RectangleF(0.50f, 0.5f, 0.036f, 0.41f));
            Sections.Add(new RectangleF(0.625f, 0.07f, 0.135f, 0.41f));
            Sections.Add(new RectangleF(0.65f, 0.5f, 0.04f, 0.41f));
            Sections.Add(new RectangleF(0.715f, 0.5f, 0.04f, 0.41f));
            //Sections.Add(new RectangleF(0.768f, 0.3f, 0.2f, 0.5f));
            bool run = false;
            while (ProcessThread.IsAlive)
            {
                lock (locker)
                {
                    if (mToScan.Count > 0)
                    {
                        image = mToScan[0];
                        mToScan.RemoveAt(0);
                        run = true;
                    }
                }

                if (run)
                {
                    //image.Save("processedImages//img_" + BPieces + ".png");
                    //ImageProcessing.AutoCrop(image).Save("processedImages//img_" + BPieces + "-Cropped.png");
                    try
                    {
                        var Parts = ImageProcessing.BreakImage(Sections.ToArray(), ImageProcessing.Scale(image, 4.0d));
                        string debug = "";
                        try
                        {
                            string temp;
                            Soldier newS = new Soldier();
                            debug += (temp = ocr.ProcessImage(Parts[0])) + ", ";
                            newS.Rank = temp;
                            debug += (temp = ocr.ProcessImage(Parts[1])) + ", ";
                            newS.Name = temp;
                            debug += (temp = ocr.ProcessImage(Parts[2], true)) + ", ";
                            newS.Health = Convert.ToInt32(temp);
                            debug += (temp = ocr.ProcessImage(Parts[3], true)) + ", ";
                            newS.Mobility = Convert.ToInt32(temp);
                            debug += (temp = ocr.ProcessImage(Parts[4], true)) + ", ";
                            newS.Will = Convert.ToInt32(temp);
                            debug += (temp = ocr.ProcessImage(Parts[5], true)) + ", ";
                            newS.Hacking = Convert.ToInt32(temp);
                            debug += (temp = ocr.ProcessImage(Parts[6], true)) + ", ";
                            newS.Dodge = Convert.ToInt32(temp);
                            debug += (temp = ocr.ProcessImage(Parts[7], true)) + ", ";
                            try
                            {
                                newS.Psi = Convert.ToInt32(temp);
                            }
                            catch (Exception e)
                            {
                                newS.Psi = 0;
                            }
                            debug += (temp = ocr.ProcessImage(Parts[8])) + ", ";
                            newS.Class = temp;
                            debug += (temp = ocr.ProcessImage(Parts[9], true)) + ", ";
                            newS.Aim = Convert.ToInt32(temp);
                            debug += (temp = ocr.ProcessImage(Parts[10], true)) + ", ";
                            newS.Defense = Convert.ToInt32(temp);
                            //newS.Status = ocr.ProcessImage(Parts[10]);
                            newS.CorrectErrors();

                            lock (locker)
                            {
                                if (!Soldiers.Select(item => item.Name).Contains(newS.Name))
                                    Soldiers.Add(newS);
                            }
                            run = false;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Throwing away: " + debug);
                            int index = debug.Count(item => item == ',') - 1;
                            //Parts[index].Save("processedImages//Img_" + BPieces + "_" + index + ".png");
                            //for (int i = 0; i < Parts.Length; i++)
                            //{
                            //    Parts[i].Save("processedImages//img_" + BPieces + "-" + i +  ".png");
                            //}
                            run = false;
                        }
                    }
                    catch (Exception e)
                    {
                        run = false;
                    }
                }
                else
                    Thread.Sleep(200);
            }
            
        }
        private void winScanCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (winScanCheckBox.IsChecked ?? false)
            {
                ProcessThread = new Thread(new ThreadStart(ProcessScan));
                ProcessThread.Start();
                //Task.Factory.StartNew(ProcessScan);
                for (int i = 0; i < Threads.Length; i++)
                {
                    Threads[i] = new Thread(new ThreadStart(ExtractSoldierInfo));
                    Threads[i].Start();
                }
            }
        }
        
        public static int[] FindAllSoldiers(Bitmap screencap)
        {
            int approxHeight = (int)((float)screencap.Height * 0.0435f);
            int[] Matches = ImageProcessing.ScanVertColors(System.Drawing.Color.FromArgb(38, 74, 74), screencap, 5);
            List<int> SoldierMatches = new List<int>();
            for (int i = 0; i < Matches.Length - 1; i++)
            {
                int difference = Matches[i + 1] - Matches[i];
                if (Math.Abs(difference - approxHeight) < 7)
                    SoldierMatches.Add(Matches[i] - 3);
            }
            return SoldierMatches.ToArray();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Thread t in Threads)
                if (t != null)
                    t.Abort();
            if (ProcessThread != null)
                ProcessThread.Abort();
        }
    }
}
