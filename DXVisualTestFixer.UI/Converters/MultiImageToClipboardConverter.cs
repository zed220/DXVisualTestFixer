using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DXVisualTestFixer.UI.Converters {
    public class MultiImageToClipboardConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values.Length != 3)
                return null;
            byte[] imgBeforeArr = values[0] as byte[];
            byte[] imgAfterArr = values[1] as byte[];
            byte[] imgDiffArr = values[2] as byte[];
            if(imgBeforeArr == null && imgAfterArr == null && imgDiffArr == null)
                return null;
            return new DelegateCommand(() => {
                Bitmap beforeImage = imgBeforeArr == null ? null : CreateImage(imgBeforeArr);
                Bitmap afterImage = imgAfterArr == null ? null : CreateImage(imgAfterArr);
                Bitmap diffImage = imgDiffArr == null ? null : CreateImage(imgDiffArr);

                var size = GetTargetImageSize(beforeImage, afterImage, diffImage);
                Bitmap b = new Bitmap(size.Width, size.Height);

                int left = 0;
                using(Graphics g = Graphics.FromImage(b)) {
                    g.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, size.Width, size.Height));
                    Draw(g, beforeImage, "BEFORE", size.Height, ref left);
                    Draw(g, afterImage, "CURRENT", size.Height, ref left);
                    Draw(g, diffImage, "DIFF", size.Height, ref left);
                }
                var targetImage = Imaging.CreateBitmapSourceFromHBitmap(b.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(size.Width, size.Height));
                Clipboard.SetImage(targetImage);
            });
        }

        static void Draw(Graphics g, Bitmap b, string header, int height, ref int left) {
            if(b == null)
                return;
            if(left > 0) {
                g.DrawLine(new Pen(Color.Aqua), left, 0, left++, height);
                g.DrawLine(new Pen(Color.Aqua), left, 0, left++, height);
            }
            g.DrawString(header, new Font("Arial", 10), new SolidBrush(Color.Aqua), new PointF(left, 0));
            g.DrawImage(b, new Rectangle(left, 30, b.Width, b.Height));
            left += b.Width;
        }

        static System.Drawing.Size GetTargetImageSize(params Bitmap[] imgSources) {
            int width = 0, height = 0;
            foreach(var source in imgSources) {
                if(source == null)
                    continue;
                width += source.Width + 2;
                height = Math.Max(height, source.Height);
            }
            return new System.Drawing.Size(width, height + 30);
        }

        static Bitmap CreateImage(byte[] imgArr) {
            if(imgArr == null)
                return null;
            using(MemoryStream ms = new MemoryStream(imgArr)) {
                using(MemoryStream outStream = new MemoryStream()) {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(ms));
                    enc.Save(outStream);
                    return new Bitmap(outStream);
                }
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
