using System;
using System.Globalization;
using DevExpress.Xpf.Grid;

namespace DXVisualTestFixer.UI.Converters {
	public class StackPanelToTooltipConverter : BaseMultiValueConverter {
		public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			if(values == null || values.Length == 0)
				return null;
			var rowData = values[0] as GroupRowData;
			if(rowData == null)
				return null;
			var result = string.Empty;
			foreach(var data in rowData.GroupValues) result += data.DisplayText + " ";
			result.Remove(result.Length - 1, 1);
			return result;
		}
	}
}