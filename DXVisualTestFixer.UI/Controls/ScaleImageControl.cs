using DXVisualTestFixer.UI.Behaviors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public static readonly DependencyProperty ImageSourceProperty;
        public static readonly DependencyProperty ScaleProperty;
        public static readonly DependencyProperty ShowGridLinesProperty;

        static ScaleImageControl() {
            Type ownerType = typeof(ScaleImageControl);
            ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(BitmapSource), ownerType,
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((ScaleImageControl)d).Refresh()));
            ScaleProperty = DependencyProperty.Register("Scale", typeof(int), ownerType,
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((ScaleImageControl)d).Refresh()));
            ShowGridLinesProperty = DependencyProperty.Register("ShowGridLines", typeof(bool), ownerType, new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((ScaleImageControl)d).Refresh()));
        }

        public BitmapSource ImageSource {
            get { return (BitmapSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }
        public int Scale {
            get { return (int)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }
        public bool ShowGridLines {
            get { return (bool)GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }

        ImageSource scaledImage = null;
        void Refresh() {
            scaledImage = ScaleImage();
        }

        protected override Size MeasureOverride(Size availableSize) {
            if(scaledImage == null)
                return Size.Empty;
            return new Size(scaledImage.Width, scaledImage.Height);
        }

        static Bitmap BitmapSourceToBitmap(BitmapSource srs) {
            using(var outStream = new MemoryStream()) {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(srs));
                enc.Save(outStream);
                return new Bitmap(outStream);
            }
        }

        static byte[] ImageToByteArray(Image imageIn) {
            using(var ms = new MemoryStream()) {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }

        static Bitmap ResizeImage(Image image, int scale, bool showGridLines) {
            var destRect = new Rectangle(0, 0, image.Width * scale, image.Height * scale);
            Bitmap destImage = new Bitmap(destRect.Width, destRect.Height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using(var graphics = Graphics.FromImage(destImage)) {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.PageUnit = GraphicsUnit.Pixel;
                using(var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                    if(showGridLines && scale > 1) {
                        float thickness = 1f;
                        var pen = new System.Drawing.Pen(System.Drawing.Color.Gray, thickness);
                        pen.Alignment = PenAlignment.Right;
                        for(int x = scale; x < destRect.Width; x += scale) {
                            graphics.DrawLine(pen, x, 0, x, destRect.Width);
                        }
                        for(int y = scale; y < destRect.Height; y += scale) {
                            graphics.DrawLine(pen, 0, y, destRect.Height, y);
                        }
                    }
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
            return SafeScaleImage();
        }
        BitmapSource SafeScaleImage() {
            try {
                Bitmap result = ResizeImage(BitmapSourceToBitmap(ImageSource), Scale, ShowGridLines);
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
            //if(Scale < 3)
            //    return;
            //double thickness = 1;
            //var pen = new System.Windows.Media.Pen(new SolidColorBrush(Colors.Gray), thickness);
            //for(int x = Scale - 1; x < scaledImage.Width; x += Scale) {
            //    drawingContext.DrawLine(pen, new System.Windows.Point(x - thickness / 2, 0), new System.Windows.Point(x - thickness / 2, scaledImage.Height));
            //}
            //for(int y = Scale - 1; y < scaledImage.Height; y += Scale) {
            //    drawingContext.DrawLine(pen, new System.Windows.Point(0, y - thickness / 2), new System.Windows.Point(scaledImage.Width, y - thickness / 2));
            //}
        }
    }
}
