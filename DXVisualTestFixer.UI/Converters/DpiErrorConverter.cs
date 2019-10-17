using System;
using System.Globalization;
using System.Windows.Data;
using DevExpress.Xpf.Core.FilteringUI;

namespace DXVisualTestFixer.UI.Converters {
	public class DpiErrorConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if(value is FilterValueInfo info && info.Value is int i)
				return i switch {
					0 => "Error",
					96 => "Default",
					_ => i.ToString()
				};
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}