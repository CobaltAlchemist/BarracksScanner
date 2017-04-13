using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace Barracks_Scanner_WPF
{
    class OCRParser
    {
        TesseractEngine mEngine;
         public OCRParser()
        {
            mEngine = new TesseractEngine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata")
                , "eng", EngineMode.TesseractOnly);
            mEngine.DefaultPageSegMode = PageSegMode.Auto;
        }
        static int increment;
        public string ProcessImage(Bitmap image, bool number = false)
        {
            //useImage.Save("processedImages//img_" + increment++ + ".png");
            if (number)
                mEngine.SetVariable("tessedit_char_whitelist", "-0123456789");
            else
                mEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890(). -'");

            mEngine.DefaultPageSegMode = PageSegMode.Auto;
            using (var page = mEngine.Process(image))
            {
                var s = page.GetText();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    //Debug.WriteLine("OCR Found: {0} in {1},{2} with c: {3}", s.Trim(), image.Width, image.Height, page.GetMeanConfidence());
                    return s.Trim();
                }
            }
            mEngine.DefaultPageSegMode = PageSegMode.SingleChar;
            using (var page = mEngine.Process(image))
            {
                var s = page.GetText();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    //Debug.WriteLine("OCR Found: {0} in {1},{2} with c: {3}", s.Trim() ,image.Width, image.Height, page.GetMeanConfidence());
                    return s.Trim();
                }
                else
                    return "null";
            }
        }
    }
}
