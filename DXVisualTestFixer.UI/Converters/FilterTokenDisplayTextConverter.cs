using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
using DevExpress.Xpf.Core.FilteringUI;

namespace DXVisualTestFixer.UI.Converters {
	[ContentProperty(nameof(Predefined))]
	public class FilterTokenDisplayTextConverter : IValueConverter {
		public List<PredefinedFilterToken> Predefined { get; set; } = new List<PredefinedFilterToken>();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if(value is FilterValueInfo info)
				return $"{GetDisplayText(info.Value)} ({info.Count})";
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

		string GetDisplayText(object value) {
			var strValue = value?.ToString() ?? "None";
			return Predefined.SingleOrDefault(p => p.StrValue == strValue)?.DisplayText ?? strValue;
		}
	}

	public class PredefinedFilterToken {
		public string StrValue { get; set; }
		public string DisplayText { get; set; }
	}
}