using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DXVisualTestFixer.UI.Converters {
	public abstract class BaseValueConverter : MarkupExtension, IValueConverter {
		public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

		public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

		public override object ProvideValue(IServiceProvider serviceProvider) {
			return this;
		}
	}

	public abstract class BaseMultiValueConverter : MarkupExtension, IMultiValueConverter {
		public abstract object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

		public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotImplementedException($"Converter Type = {GetType()}");
		}

		public override object ProvideValue(IServiceProvider serviceProvider) {
			return this;
		}
	}
}