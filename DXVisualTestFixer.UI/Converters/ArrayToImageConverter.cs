using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DXVisualTestFixer.UI.Converters {
    public class ArrayToImageConverter : BaseValueConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            byte[] imageData = value as byte[];
            if(imageData == null || imageData.Length == 0)
                return null;
            var image = new BitmapImage();
            using(var mem = new MemoryStream(imageData)) {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
    public class ArrayToImageSizeConverter : ArrayToImageConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var image = base.Convert(value, targetType, parameter, culture) as BitmapImage;
            if(image == null)
                return null;
            return new Point(image.PixelWidth, image.PixelHeight);
        }
    }
    public class EqualsPointsToForegroundConverter : BaseMultiValueConverter {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values.Length != 2)
                return new SolidColorBrush(Colors.Black);
            Point? first = values[0] as Point?;
            Point? second = values[1] as Point?;
            if(first == second)
                return new SolidColorBrush(Colors.Green);
            return new SolidColorBrush(Colors.Red);
        }
    }
}
