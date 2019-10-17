using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Image = System.Drawing.Image;
using Pen = System.Drawing.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace DXVisualTestFixer.UI.Controls {
	public class ScaleImageControl : FrameworkElement, IScrollInfo {
		public static readonly DependencyProperty ImageSourceProperty;
		public static readonly DependencyProperty ScaleProperty;
		public static readonly DependencyProperty ShowGridLinesProperty;
		public static readonly DependencyProperty ShowHighlightedPointProperty;
		public static readonly DependencyProperty HighlightedPointProperty;
		public static readonly DependencyProperty HighlightedColorProperty;

		Size currentSize = Size.Empty;

		ScaledImageInfo scaledImageInfo;

		static ScaleImageControl() {
			var ownerType = typeof(ScaleImageControl);
			ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(BitmapSource), ownerType,
				new FrameworkPropertyMetadata(null,
					FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
					(d, e) => ((ScaleImageControl) d).OnImageSourceChanged()));
			ScaleProperty = DependencyProperty.Register("Scale", typeof(int), ownerType,
				new FrameworkPropertyMetadata(1,
					FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
			ShowGridLinesProperty = DependencyProperty.Register("ShowGridLines", typeof(bool), ownerType,
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
			ShowHighlightedPointProperty = DependencyProperty.Register("ShowHighlightedPoint", typeof(bool), ownerType,
				new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
			HighlightedPointProperty = DependencyProperty.Register("HighlightedPoint", typeof(Point), ownerType, new FrameworkPropertyMetadata(default(Point),
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
			HighlightedColorProperty = DependencyProperty.Register("HighlightedColor", typeof(Color), ownerType, new PropertyMetadata(default(Color)));
		}

		public BitmapSource ImageSource {
			get => (BitmapSource) GetValue(ImageSourceProperty);
			set => SetValue(ImageSourceProperty, value);
		}

		public int Scale {
			get => (int) GetValue(ScaleProperty);
			set => SetValue(ScaleProperty, value);
		}

		public bool ShowGridLines {
			get => (bool) GetValue(ShowGridLinesProperty);
			set => SetValue(ShowGridLinesProperty, value);
		}

		public bool ShowHighlightedPoint {
			get => (bool) GetValue(ShowHighlightedPointProperty);
			set => SetValue(ShowHighlightedPointProperty, value);
		}

		public Point HighlightedPoint {
			get => (Point) GetValue(HighlightedPointProperty);
			set => SetValue(HighlightedPointProperty, value);
		}

		public Color HighlightedColor {
			get => (Color) GetValue(HighlightedColorProperty);
			set => SetValue(HighlightedColorProperty, value);
		}

		void OnImageSourceChanged() {
			scaledImageInfo = null;
		}

		protected override Size MeasureOverride(Size availableSize) {
			if(ImageSource == null)
				return Size.Empty;
			ScrollOwner.InvalidateScrollInfo();
			return currentSize = new Size(Math.Min(ImageSource.PixelWidth * Scale, availableSize.Width), Math.Min(ImageSource.PixelHeight * Scale, availableSize.Height));
		}

		static Bitmap BitmapSourceToBitmap(BitmapSource srs) {
			using var outStream = new MemoryStream();
			BitmapEncoder enc = new BmpBitmapEncoder();
			enc.Frames.Add(BitmapFrame.Create(srs, null, null, null));
			enc.Save(outStream);
			return new Bitmap(outStream);
		}

		//static byte[] ImageToByteArray(Image imageIn) {
		//    using(var ms = new MemoryStream()) {
		//        imageIn.Save(ms, imageIn.RawFormat);
		//        return ms.ToArray();
		//    }
		//}

		static Bitmap GetSourceImagePart(BitmapSource imageSource, int scale, Point offset, Size viewportSize) {
			using var image = BitmapSourceToBitmap(imageSource);
			offset = CorrectOffset(image, scale, offset, viewportSize);

			var scaledViewportSize = new Size(viewportSize.Width / scale + 1, viewportSize.Height / scale + 1);

			var viewportWidth = (int) scaledViewportSize.Width;
			var viewportHeight = (int) scaledViewportSize.Height;

			var width = viewportWidth > image.Width ? image.Width : viewportWidth;
			var height = viewportHeight > image.Height ? image.Height : viewportHeight;

			var destRect = new Rectangle(0, 0, width, height);
			var destImage = new Bitmap(destRect.Width, destRect.Height);
			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using var graphics = CreateGraphics(destImage);
			using var wrapMode = new ImageAttributes();
			wrapMode.SetWrapMode(WrapMode.TileFlipXY);
			graphics.DrawImage(image, destRect, (int) offset.X, (int) offset.Y, destRect.Width, destRect.Height, GraphicsUnit.Pixel, wrapMode);
			return destImage;
		}

		static Point CorrectOffset(Image image, int scale, Point offset, Size viewportSize) {
			return new Point(offset.X / scale, offset.Y / scale);
		}

		static BitmapSource ResizeImage(BitmapSource imageSource, int scale, Size viewportSize, Point offset, bool showGridLines) {
			using var imagePart = GetSourceImagePart(imageSource, scale, offset, viewportSize);
			using var destImage = new Bitmap(imagePart.Width * scale, imagePart.Height * scale);
			var destRect = new Rectangle(0, 0, imagePart.Width * scale, imagePart.Height * scale);

			using var graphics = CreateGraphics(destImage);
			using var wrapMode = new ImageAttributes();
			wrapMode.SetWrapMode(WrapMode.TileFlipXY);
			graphics.DrawImage(imagePart, destRect, 0, 0, imagePart.Width, imagePart.Height, GraphicsUnit.Pixel, wrapMode);
			if(showGridLines && scale > 1) {
				var thickness = 1f;
				using var pen = new Pen(System.Drawing.Color.Gray, thickness);
				pen.Alignment = PenAlignment.Right;
				for(var x = scale; x < destRect.Width; x += scale) graphics.DrawLine(pen, x, 0, x, destRect.Height);
				for(var y = scale; y < destRect.Height; y += scale) graphics.DrawLine(pen, 0, y, destRect.Width, y);
			}

			return Convert(destImage);
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

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		static BitmapSource Convert(Bitmap src) {
			var intPtr = src.GetHbitmap();
			var result = Imaging.CreateBitmapSourceFromHBitmap(intPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			DeleteObject(intPtr);
			return result;
		}

		void UpdateScaledImage() {
			if(ImageSource == null) {
				scaledImageInfo = null;
				return;
			}

			var newRenderParameters = new ImageRenderParameters(Scale, currentSize, HorizontalOffset, VerticalOffset, ShowGridLines);
			if(scaledImageInfo != null && scaledImageInfo.RenderParameters.Equals(newRenderParameters))
				return;
			var scaledImage = ResizeImage(ImageSource, Scale, currentSize, new Point(HorizontalOffset, VerticalOffset), ShowGridLines);
			scaledImageInfo = new ScaledImageInfo(scaledImage, newRenderParameters);
		}

		void DrawImage(DrawingContext drawingContext) {
			UpdateScaledImage();
			drawingContext.DrawImage(scaledImageInfo.ScaledImage, scaledImageInfo.RectLazy.Value);
		}

		void DrawHighlightedPoint(DrawingContext drawingContext) {
			if(!ShowHighlightedPoint)
				return;

			int GetPixel(double p) {
				var res = (int) p / Scale;
				return res * Scale + 1;
			}

			var brush = new SolidColorBrush(Colors.Black);
			var leftUpCorner = new Point(GetPixel(HighlightedPoint.X), GetPixel(HighlightedPoint.Y));
			drawingContext.DrawRectangle(new SolidColorBrush(Colors.Transparent), new System.Windows.Media.Pen(brush, 1), new Rect(leftUpCorner.X, leftUpCorner.Y, Scale, Scale));
		}

		void UpdateHighlightedColor() {
			if(ImageSource == null || !ShowHighlightedPoint)
				return;
			var color = default(Color);
			if(scaledImageInfo.TryGetColor(HighlightedPoint, out color))
				HighlightedColor = color;
		}

		protected override void OnRender(DrawingContext drawingContext) {
			if(ImageSource == null)
				return;
			DrawImage(drawingContext);
			DrawHighlightedPoint(drawingContext);
			UpdateHighlightedColor();
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
				if(!(obj is ImageRenderParameters)) return false;

				var parameters = (ImageRenderParameters) obj;
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

		class ScaledImageInfo {
			readonly Lazy<(byte[] pixels, int stride)> imageBytesLazy;

			public ScaledImageInfo(BitmapSource scaledImage, ImageRenderParameters renderParameters) {
				ScaledImage = scaledImage;
				RenderParameters = renderParameters;
				RectLazy = new Lazy<Rect>(() => new Rect(new Size(ScaledImage.Width, ScaledImage.Height)));
				imageBytesLazy = new Lazy<(byte[], int)>(GetImageBytes);
			}

			public BitmapSource ScaledImage { get; }
			public Lazy<Rect> RectLazy { get; }
			public ImageRenderParameters RenderParameters { get; }

			(byte[], int) GetImageBytes() {
				var stride = ScaledImage.PixelWidth * 4;
				var size = ScaledImage.PixelHeight * stride;
				var pixels = new byte[size];
				ScaledImage.CopyPixels(pixels, stride, 0);
				return (pixels, stride);
			}

			public bool TryGetColor(Point point, out Color color) {
				int GetPixel(double p) {
					var res = (int) p / RenderParameters.Scale;
					return res * RenderParameters.Scale;
				}

				var bytes = imageBytesLazy.Value;
				var index = GetPixel(point.Y) * bytes.stride + 4 * GetPixel(point.X);
				var pixel = new Point(GetPixel(point.X), GetPixel(point.Y));

				color = default;
				if(!IsInBound(bytes.pixels, index + 2) || !IsInBound(bytes.pixels, index + 1) || !IsInBound(bytes.pixels, index))
					return false;
				color = Color.FromArgb(255, bytes.pixels[index + 2], bytes.pixels[index + 1], bytes.pixels[index]);
				return true;
			}

			static bool IsInBound(byte[] arr, int index) {
				return index < arr.Length && index >= 0;
			}
		}

		#region IScrollInfo

		public bool CanVerticallyScroll { get; set; }

		public bool CanHorizontallyScroll { get; set; }

		public double ExtentWidth => ImageSource?.PixelWidth * Scale ?? 0;

		public double ExtentHeight => ImageSource?.PixelHeight * Scale ?? 0;

		public double ViewportWidth => ImageSource != null ? currentSize.Width : 0;

		public double ViewportHeight => ImageSource != null ? currentSize.Height : 0;

		public double HorizontalOffset { get; set; }

		public double VerticalOffset { get; set; }

		public ScrollViewer ScrollOwner { get; set; }

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

		public void PageUp() { }

		public void PageDown() { }

		public void PageLeft() { }

		public void PageRight() { }

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

		#endregion
	}
}