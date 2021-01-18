using System;
using System.Collections;
using System.Globalization;
using System.Windows;

namespace DXVisualTestFixer.UI.Converters {
    public sealed class IntToVisibilityConverter : BaseValueConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value as IList)?.Count == 1 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}