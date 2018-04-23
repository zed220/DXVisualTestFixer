using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DXVisualTestFixer.UI.Converters {
    public class ImageToClipboardConverter : ArrayToImageConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value == null)
                return null;
            return new DelegateCommand(() => {
                PngBitmapDecoder pngd = null;
                using(MemoryStream ms = new MemoryStream((byte[])value)) {
                    pngd = new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.None);
                    BitmapSource bitmapSource = pngd.Frames[0];
                    Clipboard.SetImage(bitmapSource);
                }
            });
        }
    }
}
