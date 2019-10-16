using System;
using System.Collections.Generic;
using System.Globalization;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.Converters {
	public class HasFixedTestsConverter : BaseValueConverter {
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var tests = value as IEnumerable<ITestInfoModel>;
			if(tests == null)
				return false;
			foreach(var test in tests)
				if(test.Valid == TestState.Fixed)
					return true;
			return false;
		}
	}
}