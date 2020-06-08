using System;
using System.Collections;
using System.Globalization;

namespace DXVisualTestFixer.UI.Converters {
	public class NoRecordsTextConverter : BaseValueConverter {
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return value is IList list && list.Count == 0 ? "All Tests Fixed" : "No Tests Found";
		}
	}
}