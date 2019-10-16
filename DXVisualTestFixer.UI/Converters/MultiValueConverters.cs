using System;
using System.Globalization;

namespace DXVisualTestFixer.UI.Converters {
	public class HighlightedColorPopupIsOpenConverter : BaseMultiValueConverter {
		public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			return values.Length == 2 && values[0] != null && values[1] is bool && (bool) values[1];
		}
	}
}