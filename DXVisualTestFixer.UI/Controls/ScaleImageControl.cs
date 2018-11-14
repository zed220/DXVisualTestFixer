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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Windows.Media.PixelFormat;
using Image = System.Drawing.Image;
using Size = System.Windows.Size;
using Point = System.Windows.Point;
using System.Windows.Controls.Primitives;

namespace DXVisualTestFixer.UI.Controls {
    public class ScaleImageControl : FrameworkElement, IScrollInfo {
        public static readonly DependencyProperty ImageSourceProperty;
        public static readonly DependencyProperty ScaleProperty;
        public static readonly DependencyProperty ShowGridLinesProperty;
        public static readonly DependencyProperty ShowHighlightedPointProperty;
        public static readonly DependencyProperty HighlightedPointProperty;

        static ScaleImageControl() {
            Type ownerType = typeof(ScaleImageControl);
            ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(BitmapSource), ownerType,
                new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) =>((ScaleImageControl)d).OnImageSourceChanged()));
            ScaleProperty = DependencyProperty.Register("Scale", typeof(int), ownerType,
                new FrameworkPropertyMetadata(1,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
            ShowGridLinesProperty = DependencyProperty.Register("ShowGridLines", typeof(bool), ownerType,
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
            ShowHighlightedPointProperty = DependencyProperty.Register("ShowHighlightedPoint", typeof(bool), ownerType,
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
            HighlightedPointProperty = DependencyProperty.Register("HighlightedPoint", typeof(Point), ownerType, new FrameworkPropertyMetadata(default(Point), 
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
        }

        void OnImageSourceChanged() {
            renderParameters = new ImageRenderParameters();
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
        public bool ShowHighlightedPoint {
            get { return (bool)GetValue(ShowHighlightedPointProperty); }
            set { SetValue(ShowHighlightedPointProperty, value); }
        }
        public Point HighlightedPoint {
            get { return (Point)GetValue(HighlightedPointProperty); }
            set { SetValue(HighlightedPointProperty, value); }
        }

        ImageSource scaledImage = null;
        Size currentSize = Size.Empty;

        protected override Size MeasureOverride(Size availableSize) {
            if(ImageSource == null)
                return Size.Empty;
            ScrollOwner.InvalidateScrollInfo();
            return currentSize = new Size(Math.Min(ImageSource.PixelWidth * Scale, availableSize.Width), Math.Min(ImageSource.PixelHeight * Scale, availableSize.Height));
        }

        static Bitmap BitmapSourceToBitmap(BitmapSource srs) {
            using(var outStream = new MemoryStream()) {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(srs, null, null, null));
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

        static Bitmap GetSourceImagePart(Image image, int scale, Point offset, Size viewportSize) {
            offset = CorrectOffset(image, scale, offset, viewportSize);

            Size scaledViewportSize = new Size(viewportSize.Width / scale + 1, viewportSize.Height / scale + 1);

            var viewportWidth = (int)scaledViewportSize.Width;
            var viewportHeight = (int)scaledViewportSize.Height;

            var width = viewportWidth > image.Width ? image.Width : viewportWidth;
            var height = viewportHeight > image.Height ? image.Height : viewportHeight;

            var destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(destRect.Width, destRect.Height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using(var graphics = CreateGraphics(destImage)) {
                using(var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, (int)offset.X, (int)offset.Y, destRect.Width, destRect.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        static Point CorrectOffset(Image image, int scale, Point offset, Size viewportSize) {
            return new Point(offset.X / scale, offset.Y / scale);
        }
        
        static Bitmap ResizeImage(Image image, int scale, Size viewportSize, Point offset, bool showGridLines) {
            var imagePart = GetSourceImagePart(image, scale, offset, viewportSize);
            Bitmap destImage = new Bitmap(imagePart.Width * scale, imagePart.Height * scale);
            var destRect = new Rectangle(0, 0, imagePart.Width * scale, imagePart.Height * scale);

            using(var graphics = CreateGraphics(destImage)) {
                using(var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(imagePart, destRect, 0, 0, imagePart.Width, imagePart.Height, GraphicsUnit.Pixel, wrapMode);
                    if(showGridLines && scale > 1) {
                        float thickness = 1f;
                        var pen = new System.Drawing.Pen(System.Drawing.Color.Gray, thickness);
                        pen.Alignment = PenAlignment.Right;
                        for(int x = scale; x < destRect.Width; x += scale) {
                            graphics.DrawLine(pen, x, 0, x, destRect.Height);
                        }
                        for(int y = scale; y < destRect.Height; y += scale) {
                            graphics.DrawLine(pen, 0, y, destRect.Width, y);
                        }
                    }
                }
            }

            return destImage;
        }
        static Graphics CreateGraphics(Bitmap destImage) {
            var graphics = Graphics.FromImage(destImage);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.CompositingQuality = CompositingQuality.AssumeLinear;
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.PageUnit = GraphicsUnit.Pixel;
            return graphics;
        }

        BitmapSource Convert(Bitmap src) {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(src.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        struct ImageRenderParameters {
            public ImageRenderParameters(int scale, Size currentSize, double horizontalOffset, double verticalOffset, bool showGridLines) : this() {
                Scale = scale;
                CurrentSize = currentSize;
                HorizontalOffset = horizontalOffset;
                VerticalOffset = verticalOffset;
                ShowGridLines = showGridLines;
            }

            public int Scale { get; }
            public Size CurrentSize { get; }
            public double HorizontalOffset { get; }
            public double VerticalOffset { get; }
            public bool ShowGridLines { get; }

            public override bool Equals(object obj) {
                if(!(obj is ImageRenderParameters)) {
                    return false;
                }

                var parameters = (ImageRenderParameters)obj;
                return Scale == parameters.Scale &&
                       EqualityComparer<Size>.Default.Equals(CurrentSize, parameters.CurrentSize) &&
                       HorizontalOffset == parameters.HorizontalOffset &&
                       VerticalOffset == parameters.VerticalOffset &&
                       ShowGridLines == parameters.ShowGridLines;
            }

            public override int GetHashCode() {
                var hashCode = 1855489041;
                hashCode = hashCode * -1521134295 + Scale.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<Size>.Default.GetHashCode(CurrentSize);
                hashCode = hashCode * -1521134295 + HorizontalOffset.GetHashCode();
                hashCode = hashCode * -1521134295 + VerticalOffset.GetHashCode();
                hashCode = hashCode * -1521134295 + ShowGridLines.GetHashCode();
                return hashCode;
            }
        }

        ImageRenderParameters renderParameters = new ImageRenderParameters();

        ImageSource ScaleImage() {
            if(ImageSource == null)
                return null;
            ImageRenderParameters newRenderParameters = new ImageRenderParameters(Scale, currentSize, HorizontalOffset, VerticalOffset, ShowGridLines);
            if(renderParameters.Equals(newRenderParameters))
                return scaledImage;
            renderParameters = newRenderParameters;
            Bitmap result = ResizeImage(BitmapSourceToBitmap(ImageSource), Scale, currentSize, new Point(HorizontalOffset, VerticalOffset), ShowGridLines);
            return Convert(result);
        }

        void DrawImage(DrawingContext drawingContext) {
            scaledImage = ScaleImage();
            drawingContext.DrawImage(scaledImage, new Rect(new Size(scaledImage.Width, scaledImage.Height)));
        }
        void DrawHighlightedPoint(DrawingContext drawingContext) {
            if(!ShowHighlightedPoint)
                return;
            var brush = new SolidColorBrush(Colors.Black);

            Func<double, int> getPixel = p => {
                int res = (int)p / Scale;
                return res * Scale;
            };

            Point leftUpCorner = new Point(getPixel(HighlightedPoint.X), getPixel(HighlightedPoint.Y));
            drawingContext.DrawRectangle(new SolidColorBrush(Colors.Transparent), new System.Windows.Media.Pen(brush, 1), new Rect(leftUpCorner.X, leftUpCorner.Y, Scale, Scale));
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if(ImageSource == null)
                return;
            DrawImage(drawingContext);
            DrawHighlightedPoint(drawingContext);
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


        public bool CanVerticallyScroll {
            get;
            set;
        }

        public bool CanHorizontallyScroll {
            get;
            set;
        }

        public double ExtentWidth {
            get { return ImageSource?.PixelWidth * Scale ?? 0; }
        }

        public double ExtentHeight {
            get { return ImageSource?.PixelHeight * Scale ?? 0; }
        }

        public double ViewportWidth {
            get { return currentSize.Width; }
        }

        public double ViewportHeight {
            get { return currentSize.Height; }
        }

        public double HorizontalOffset {
            get;
            set;
        }
        public double VerticalOffset {
            get;
            set;
        }

        public ScrollViewer ScrollOwner {
            get;
            set;
        }

        public void LineUp() {
            SetVerticalOffset(VerticalOffset - 30);
            ScrollOwner.InvalidateScrollInfo();
        }
        public void LineDown() {
            SetVerticalOffset(VerticalOffset + 30);
            ScrollOwner.InvalidateScrollInfo();
        }
        public void LineLeft() {
            SetHorizontalOffset(HorizontalOffset - 30);
            ScrollOwner.InvalidateScrollInfo();
        }
        public void LineRight() {
            SetHorizontalOffset(HorizontalOffset + 30);
            ScrollOwner.InvalidateScrollInfo();
        }

        public void PageUp() {
        }
        public void PageDown() {
        }
        public void PageLeft() {
        }
        public void PageRight() {
        }

        public void MouseWheelUp() {
            SetVerticalOffset(VerticalOffset - 120);
            ScrollOwner.InvalidateScrollInfo();
        }

        public void MouseWheelDown() {
            SetVerticalOffset(VerticalOffset + 120);
            ScrollOwner.InvalidateScrollInfo();
        }
        public void MouseWheelLeft() {
            SetHorizontalOffset(HorizontalOffset - 120);
            ScrollOwner.InvalidateScrollInfo();
        }
        public void MouseWheelRight() {
            SetHorizontalOffset(HorizontalOffset + 120);
            ScrollOwner.InvalidateScrollInfo();
        }

        public void SetHorizontalOffset(double offset) {
            HorizontalOffset = Math.Max(0, Math.Min(offset, ExtentWidth - ViewportWidth));
            InvalidateVisual();
        }

        public void SetVerticalOffset(double offset) {
            VerticalOffset = Math.Max(0, Math.Min(offset, ExtentHeight - ViewportHeight));
            InvalidateVisual();
        }

        public Rect MakeVisible(Visual visual, Rect rectangle) {
            throw new NotImplementedException();
        }
    }
}
