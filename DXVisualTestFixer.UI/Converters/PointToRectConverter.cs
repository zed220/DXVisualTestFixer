using System;
using System.Globalization;
using System.Windows;

namespace DXVisualTestFixer.UI.Converters {
    public class PointToRectConverter : BaseValueConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value is Point p)
                return new Rect(p.X + 5, p.Y + 5, 0, 0);
            return default(Rect);
        }
    }
}
