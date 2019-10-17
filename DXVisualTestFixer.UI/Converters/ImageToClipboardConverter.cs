using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;

namespace DXVisualTestFixer.UI.Converters {
	public abstract class ImageToClipboardConverterBase : BaseValueConverter {
		public sealed override object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null ? null : new DelegateCommand(() => { UpdateClipboard((byte[]) value); });

		protected abstract void UpdateClipboard(byte[] value);
	}

	public class ImageToClipboardConverter : ImageToClipboardConverterBase {
		protected override void UpdateClipboard(byte[] value) {
			using var ms = new MemoryStream(value);
			var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.None);
			Clipboard.SetImage(decoder.Frames[0]);
		}
	}

	public class ImageToTempFileConverter : ImageToClipboardConverterBase {
		protected override void UpdateClipboard(byte[] value) {
			var tempFilePath = GetTempImageFilePath("image");
			File.WriteAllBytes(tempFilePath, value);
			Clipboard.SetText(tempFilePath);
		}

		public static string GetTempImageFilePath(string fileName) {
			string tempDirectory = null;
			while(tempDirectory == null || Directory.Exists(tempDirectory))
				tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			return Path.Combine(tempDirectory, $"{fileName}.png");
		}
	}
}