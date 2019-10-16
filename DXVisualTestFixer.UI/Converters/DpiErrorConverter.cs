using System;
using System.Globalization;
using System.Windows.Data;
using DevExpress.Xpf.Core.FilteringUI;

namespace DXVisualTestFixer.UI.Converters {
	public class DpiErrorConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if(value is FilterValueInfo info && info.Value is int i)
				switch(i) {
					case 0:
						return "Error";
					case 96:
						return "Default";
					default:
						return i.ToString();
				}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}