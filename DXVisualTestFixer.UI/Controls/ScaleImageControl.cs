using DXVisualTestFixer.UI.Behaviors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Windows.Media.PixelFormat;
using Size = System.Windows.Size;

namespace DXVisualTestFixer.UI.Controls {
    public class ScaleImageControl : FrameworkElement {
        #region Inner Classes

        class MyColor {
            public readonly byte A;
            public readonly byte R;
            public readonly byte G;
            public readonly byte B;

            public MyColor(byte a, byte r, byte g, byte b) {
                A = a;
                R = r;
                G = g;
                B = b;
            }
        }

        #endregion

        public static readonly DependencyProperty ImageSourceProperty;
        public static readonly DependencyProperty ScaleProperty;

        static ScaleImageControl() {
            Type ownerType = typeof(ScaleImageControl);
            ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(BitmapSource), ownerType,
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((ScaleImageControl)d).OnImageSourceChanged()));
            ScaleProperty = DependencyProperty.Register("Scale", typeof(int), ownerType,
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((ScaleImageControl)d).OnImageSourceChanged()));
        }

        public BitmapSource ImageSource {
            get { return (BitmapSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }
        public int Scale {
            get { return (int)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        ImageSource scaledImage = null;
        void OnImageSourceChanged() {
            scaledImage = ScaleImage();
        }

        protected override Size MeasureOverride(Size availableSize) {
            if(scaledImage == null)
                return Size.Empty;
            return new Size(scaledImage.Width, scaledImage.Height);
        }

        static Bitmap ResizeImage(Bitmap image, int width, int height) {
            Bitmap resizedImage = new Bitmap(width, height);
            using(Graphics gfx = Graphics.FromImage(resizedImage)) {
                gfx.DrawImage(image, new Rectangle(0, 0, width, height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            }
            return resizedImage;
        }

        static Bitmap BitmapSourceToBitmap(BitmapSource srs) {
            int width = srs.PixelWidth;
            int height = srs.PixelHeight;
            int stride = width * (srs.Format.BitsPerPixel / 8);
            IntPtr ptr = IntPtr.Zero;
            try {
                ptr = Marshal.AllocHGlobal(height * stride);
                srs.CopyPixels(new Int32Rect(0, 0, width, height), ptr, height * stride, stride);
                using(var btm = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, ptr)) {
                    return new System.Drawing.Bitmap(btm);
                }
            }
            finally {
                if(ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }
        static Bitmap ResizeImage(Image image, int scale) {
            var destRect = new Rectangle(0, 0, image.Width * scale, image.Height * scale);
            Bitmap destImage = new Bitmap(destRect.Width, destRect.Height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using(var graphics = Graphics.FromImage(destImage)) {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

                using(var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        BitmapSource Convert(Bitmap src) {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(src.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        BitmapSource ScaleImage() {
            if(ImageSource == null)
                return null;
            return SafeScaleImage(Scale);
        }
        BitmapSource SafeScaleImage(int scale) {
            try {
                Bitmap result = ResizeImage(BitmapSourceToBitmap(ImageSource), scale);
                return Convert(result);
            }
            catch {
                ImageScaleBehavior.ZoomOut();
                return null;
            }
        }


        protected override void OnRender(DrawingContext drawingContext) {
            if(scaledImage == null)
                return;
            drawingContext.DrawImage(scaledImage, new Rect(new Size(scaledImage.Width, scaledImage.Height)));
        }
    }
}
