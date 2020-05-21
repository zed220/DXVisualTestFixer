using System;
using System.Globalization;
using System.Windows;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.ViewModels;

namespace DXVisualTestFixer.UI.Converters {
	public class DisplayTestToVisibilityConverter : BaseValueConverter {
		public bool IsError { get; set; }

		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			TestInfoModel model = value as TestInfoModel;
			if(model == null)
				return Visibility.Collapsed;
			if(model.Valid == TestState.Error)
				return IsError ? Visibility.Visible : Visibility.Collapsed;
			return IsError ? Visibility.Collapsed : Visibility.Visible;
		}
	}
}