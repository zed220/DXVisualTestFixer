using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
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
    public abstract class MultiImageToClipboardConverterBase : BaseMultiValueConverter {
        const int textHeight = 20;

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values.Length != 5)
                return null;
            byte[] imgBeforeArr = values[0] as byte[];
            byte[] imgAfterArr = values[1] as byte[];
            byte[] imgDiffArr = values[2] as byte[];
            string theme = values[3] as string;
            Team team = values[4] as Team;
            if(imgBeforeArr == null && imgAfterArr == null && imgDiffArr == null)
                return null;
            if(theme == null || team == null)
                theme = String.Empty;
            return new DelegateCommand(() => {
                Bitmap beforeImage = imgBeforeArr == null ? null : CreateImage(imgBeforeArr);
                Bitmap afterImage = imgAfterArr == null ? null : CreateImage(imgAfterArr);
                Bitmap diffImage = imgDiffArr == null ? null : CreateImage(imgDiffArr);

                var size = GetTargetImageSize(beforeImage, afterImage, diffImage);
                Bitmap b = new Bitmap(size.Width, size.Height);

                int left = 0;
                using(Graphics g = Graphics.FromImage(b)) {
                    g.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, size.Width, size.Height));
                    g.DrawString($"Theme: {theme.ToUpper()}, Version: {team.Version}, Team: {team.Name}", new Font("Arial", 10), new SolidBrush(Color.Black), new PointF(0, 0));
                    Draw(g, beforeImage, "BEFORE", textHeight, size.Height - textHeight, ref left);
                    Draw(g, afterImage, "CURRENT", textHeight, size.Height - textHeight, ref left);
                    Draw(g, diffImage, "DIFF", textHeight, size.Height - textHeight, ref left);
                }
                var targetImage = Imaging.CreateBitmapSourceFromHBitmap(b.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(size.Width, size.Height));
                UpdateClipboard(theme, targetImage);
            });
        }

        static void Draw(Graphics g, Bitmap b, string header, int top, int height, ref int left) {
            if(b == null)
                return;
            if(left > 0) {
                g.DrawLine(new Pen(Color.Aqua), left, top, left++, height);
                g.DrawLine(new Pen(Color.Aqua), left, top, left++, height);
            }
            g.DrawString(header, new Font("Arial", 10), new SolidBrush(Color.Black), new PointF(left, top));
            g.DrawImage(b, new Rectangle(left, textHeight * 2, b.Width, b.Height));
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
            return new System.Drawing.Size(width, height + (textHeight * 2));
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

        protected abstract void UpdateClipboard(string theme, BitmapSource image);
    }

    public class MultiImageToClipboardConverter : MultiImageToClipboardConverterBase {
        protected override void UpdateClipboard(string theme, BitmapSource image) {
            Clipboard.SetImage(image);
        }
    }

    public class MultiImageToTempFileConverter : MultiImageToClipboardConverterBase {
        protected override void UpdateClipboard(string theme, BitmapSource image) {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            using(MemoryStream stream = new MemoryStream()) {
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                var filePath = ImageToTempFileConverter.GetTempImageFilePath(theme ?? "image");
                stream.Seek(0, SeekOrigin.Begin);
                File.WriteAllBytes(filePath, stream.ToArray());
                Clipboard.SetText(filePath);
            }
        }
    }
}
