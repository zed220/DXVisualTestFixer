using System;
using System.Globalization;
using System.Windows;

namespace DXVisualTestFixer.UI.Converters {
	public class HighlightedColorPopupIsOpenConverter : BaseMultiValueConverter {
		public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			return values.Length == 2 && values[0] != null && values[1] is bool && (bool) values[1];
		}
	}

	public class ForkNameVisibilityConverter : BaseMultiValueConverter {
		public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			if(values.Length != 2)
				return Visibility.Collapsed;
			var version = values[0] as string;
			var forkName = values[1] as string;
			if(string.IsNullOrEmpty(version) || string.IsNullOrEmpty(forkName))
				return Visibility.Collapsed;
			if(version == forkName)
				return Visibility.Collapsed;
			return Visibility.Visible;
		}
	}
}