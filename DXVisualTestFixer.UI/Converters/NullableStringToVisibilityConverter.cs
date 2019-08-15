using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using DevExpress.Xpf.Core;

namespace DXVisualTestFixer.UI.Converters {
	public class NullableStringToVisibilityConverter : MarkupExtension, IValueConverter {
		readonly IValueConverter _converter = new NullableToVisibilityConverter();
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return _converter.Convert((value as string) == string.Empty ? null : value, targetType, parameter, culture);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			return _converter.ConvertBack(value, targetType, parameter, culture);
		}
        
		public override object ProvideValue(IServiceProvider serviceProvider) {
			return this;
		}
	}
}